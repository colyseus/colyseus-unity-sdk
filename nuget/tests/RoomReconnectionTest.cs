using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Colyseus;

namespace Colyseus.Tests
{
	/// <summary>
	///     Leave() after the connection dropped: a consented leave ends automatic
	///     reconnection instead of letting a retry revive the room.
	///
	///     The room runs on a stub connection; reconnection attempts are real
	///     sockets. When a test watches them they go to a local listener that
	///     either stalls the upgrade, so an attempt stays connecting, or
	///     completes it but never sends JOIN_ROOM, so it stays open but
	///     unjoined. Otherwise they go to a port that refuses them.
	/// </summary>
	[TestFixture]
	public class RoomReconnectionTest
	{
		// nothing listens here: an attempt is refused at once
		private const string Unreachable = "ws://127.0.0.1:1";

		private const int RetryDelayMs = 50;

		private class StubConnection : Connection
		{
			public readonly List<byte[]> Sent = new List<byte[]>();

			public StubConnection(string url) : base(url, null)
			{
				IsOpen = true;
				Transmit = data =>
				{
					Sent.Add((byte[])data.Clone());
					return Task.CompletedTask;
				};
			}
		}

		private class TestRoom : Room<SchemaTest.Phase0.P0State>
		{
			public readonly List<int> Leaves = new List<int>();
			public readonly List<string> ErrorsAfterLeave = new List<string>();
			public int Drops;
			public int Reconnects;

			public TestRoom() : base("reconnection")
			{
				RoomId = "reconnection";
				OnLeave += code => { lock (Leaves) { Leaves.Add(code); } };
				OnDrop += _ => Interlocked.Increment(ref Drops);
				OnReconnect += () => Interlocked.Increment(ref Reconnects);
				OnError += (code, message) =>
				{
					lock (Leaves) { if (Leaves.Count > 0) { ErrorsAfterLeave.Add(message); } }
				};
			}

			public void Feed(byte[] frame)
			{
				ParseMessage(frame);
			}

			// the request's close callback runs while the room handles the close
			public Task LeavingOnClose;
			public void LeaveWhenTheConnectionCloses()
			{
				_ = SendRequest("x", null, typeof(object), (ok, payload, faulted) => { }, reason => LeavingOnClose = Leave());
			}
		}

		/// <summary>
		///     Accepts reconnection attempts and reads each upgrade request.
		///     By default it never answers, so an attempt stays connecting; with
		///     <c>answerUpgrade</c> it completes the upgrade but never sends
		///     JOIN_ROOM, so an attempt stays open and unjoined.
		/// </summary>
		private class FakeServer : IDisposable
		{
			private readonly bool answerUpgrade;
			private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
			private readonly List<TcpClient> attempts = new List<TcpClient>();
			private readonly TaskCompletionSource<TcpClient> firstAttempt =
				new TaskCompletionSource<TcpClient>(TaskCreationOptions.RunContinuationsAsynchronously);
			private readonly TaskCompletionSource<bool> clientClosed =
				new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			public FakeServer(bool answerUpgrade = false)
			{
				this.answerUpgrade = answerUpgrade;
				listener.Start();
				_ = AcceptLoop();
			}

			public string Url => "ws://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;

			public int Attempts { get { lock (attempts) { return attempts.Count; } } }

			/// <summary>Resolves once the first attempt has sent its upgrade request.</summary>
			public Task<TcpClient> FirstAttempt => firstAttempt.Task;

			/// <summary>Resolves once an upgraded attempt sends a close frame or hangs up.</summary>
			public Task<bool> ClientClosed => clientClosed.Task;

			private async Task AcceptLoop()
			{
				try
				{
					while (true)
					{
						var client = await listener.AcceptTcpClientAsync();
						lock (attempts) { attempts.Add(client); }
						_ = Serve(client);
					}
				}
				catch (Exception)
				{
					// listener stopped
				}
			}

			private async Task Serve(TcpClient client)
			{
				try
				{
					var stream = client.GetStream();
					var buffer = new byte[4096];

					// cancelling before the request is written can leave the TCP connection open (a .NET quirk)
					var request = "";
					while (!request.Contains("\r\n\r\n"))
					{
						var n = await stream.ReadAsync(buffer, 0, buffer.Length);
						if (n == 0) { return; }
						request += System.Text.Encoding.ASCII.GetString(buffer, 0, n);
					}
					firstAttempt.TrySetResult(client);

					if (!answerUpgrade) { return; }

					var key = System.Text.RegularExpressions.Regex.Match(request, @"Sec-WebSocket-Key:\s*(\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
					var accept = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create().ComputeHash(
						System.Text.Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
					var response = System.Text.Encoding.ASCII.GetBytes(
						"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n");
					await stream.WriteAsync(response, 0, response.Length);

					// a close frame (opcode 8) or a hang-up
					var read = await stream.ReadAsync(buffer, 0, buffer.Length);
					if (read == 0 || (buffer[0] & 0x0F) == 8) { clientClosed.TrySetResult(true); }
				}
				catch (Exception)
				{
					clientClosed.TrySetResult(true); // reset by the client, or disposed
				}
			}

			public void Dispose()
			{
				listener.Stop();
				lock (attempts) { foreach (var client in attempts) { client.Dispose(); } }
			}
		}

		private static (TestRoom room, StubConnection stub) JoinedRoom(string url = Unreachable)
		{
			var room = new TestRoom();
			var stub = new StubConnection(url);
			room.SetConnection(stub);
			room.Feed(RoomProtocolTest.JoinFrame);

			room.Reconnection.MinUptime = 0;
			room.Reconnection.MinDelay = 0;
			room.Reconnection.Backoff = (attempt, delay) => RetryDelayMs;
			return (room, stub);
		}

		private static void Drop(StubConnection stub)
		{
			stub.Dispatch(ConnectionEvent.Closed((int)CloseCode.ABNORMAL_CLOSURE));
		}

		// false when it doesn't complete in time
		private static async Task<bool> Settles(Task task, int ms = 1000)
		{
			return await Task.WhenAny(task, Task.Delay(ms)) == task;
		}

		// true once the client end of the socket hangs up
		private static Task<bool> HungUp(TcpClient attempt, int ms = 2000)
		{
			async Task Drain()
			{
				var stream = attempt.GetStream();
				var buffer = new byte[1024];
				try { while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) { } }
				catch (IOException) { }
				catch (ObjectDisposedException) { }
			}
			return Settles(Drain(), ms);
		}

		[Test]
		public async Task LeaveCancelsThePendingRetryTest()
		{
			using var server = new FakeServer();
			var (room, stub) = JoinedRoom(server.Url);

			Drop(stub);
			Assert.IsTrue(room.Reconnection.IsReconnecting);

			Assert.IsTrue(await Settles(room.Leave()));
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.IsFalse(room.Reconnection.IsReconnecting);

			await Task.Delay(RetryDelayMs * 4); // past the retry delay
			Assert.AreEqual(0, server.Attempts);
		}

		[Test]
		public async Task LeaveAbortsAnAttemptStillConnectingTest()
		{
			using var server = new FakeServer();
			var (room, stub) = JoinedRoom(server.Url);

			// the drop, then the aborted attempt's close
			var closes = 0;
			var secondClose = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var deliver = stub.Dispatch;
			stub.Dispatch = e =>
			{
				deliver(e);
				if (e.Type == ConnectionEvent.Kind.Close && Interlocked.Increment(ref closes) == 2) { secondClose.TrySetResult(true); }
			};

			Drop(stub);
			Assert.IsTrue(await Settles(server.FirstAttempt, 2000), "no reconnection attempt");

			Assert.IsTrue(await Settles(room.Leave()));
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.IsTrue(await HungUp(server.FirstAttempt.Result), "attempt still connecting");

			// the aborted attempt's error and close are delivered, and ignored: no error, no retry
			Assert.IsTrue(await Settles(secondClose.Task), "aborted attempt never closed");
			lock (room.Leaves) { CollectionAssert.IsEmpty(room.ErrorsAfterLeave); }
			Assert.AreEqual(1, room.Reconnection.RetryCount);
			Assert.AreEqual(1, room.Drops);
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.IsFalse(room.Reconnection.IsReconnecting);
		}

		[Test]
		public async Task LeaveWhileAnAttemptIsJoiningClosesItTest()
		{
			using var server = new FakeServer(answerUpgrade: true);
			var (room, stub) = JoinedRoom(server.Url);

			var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			stub.OnOpen += () => opened.TrySetResult(true);

			Drop(stub);
			Assert.IsTrue(await Settles(opened.Task, 2000), "attempt never opened");
			var sent = stub.Sent.Count;

			// open, but no JOIN_ROOM yet: still reconnecting
			Assert.IsTrue(await Settles(room.Leave()));
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.AreEqual(sent, stub.Sent.Count, "LEAVE_ROOM sent on the attempt");
			Assert.IsTrue(await Settles(server.ClientClosed, 2000), "attempt left open");
		}

		[Test]
		public async Task LeaveWhileTheCloseIsDispatchedKeepsTheServerCodeTest()
		{
			var (room, stub) = JoinedRoom();
			room.LeaveWhenTheConnectionCloses();

			stub.Dispatch(ConnectionEvent.Closed(4321)); // kicked

			Assert.IsNotNull(room.LeavingOnClose, "Leave() didn't run during the close");
			Assert.IsTrue(await Settles(room.LeavingOnClose));
			CollectionAssert.AreEqual(new[] { 4321 }, room.Leaves);
		}

		[Test]
		public async Task LeaveBeforeJoinAbortsTheConnectionTest()
		{
			using var server = new FakeServer();
			var room = new TestRoom();
			room.SetConnection(new Connection(server.Url, null));
			_ = room.Connect();
			Assert.IsTrue(await Settles(server.FirstAttempt, 2000), "no connection attempt");

			// no JOIN_ROOM yet, so no serializer to tear down
			var leaving = room.Leave();
			Assert.IsTrue(await Settles(leaving));
			await leaving;

			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.IsTrue(await HungUp(server.FirstAttempt.Result), "attempt still connecting");
		}

		[Test]
		public async Task LeaveBeforeSetConnectionLeavesConsentedTest()
		{
			var room = new TestRoom();

			// no connection to close
			await room.Leave(false);
			CollectionAssert.IsEmpty(room.Leaves);

			await room.Leave();
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
		}

		[Test]
		public async Task ReconnectionCompletingAfterLeaveIsIgnoredTest()
		{
			var (room, stub) = JoinedRoom();

			Drop(stub);
			Assert.IsTrue(await Settles(room.Leave()));
			var sent = stub.Sent.Count;

			// an attempt the transport couldn't abort gets through
			stub.Dispatch(ConnectionEvent.Opened());
			stub.Dispatch(ConnectionEvent.Received(RoomProtocolTest.RejoinFrame));

			Assert.AreEqual(0, room.Reconnects);
			Assert.AreEqual(sent, stub.Sent.Count, "JOIN_ROOM acknowledged");
			Assert.AreEqual("tokA", room.ReconnectionToken.Token);
			Assert.IsFalse(room.Reconnection.IsReconnecting);
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
		}

		[Test]
		public async Task LeaveFromOnDropDoesNotReconnectTest()
		{
			var (room, stub) = JoinedRoom();

			Task leaving = null;
			room.OnDrop += _ => leaving = room.Leave();

			Drop(stub);

			Assert.IsTrue(await Settles(leaving));
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.AreEqual(0, room.Reconnection.RetryCount);
			Assert.IsFalse(room.Reconnection.IsReconnecting);
		}

		[Test]
		public async Task SocketDyingAfterLeaveRoomSentEndsConsentedTest()
		{
			var (room, stub) = JoinedRoom();

			// LEAVE_ROOM goes out, but the server never closes the socket
			var leaving = room.Leave();
			CollectionAssert.AreEqual(new[] { Protocol.LEAVE_ROOM }, stub.Sent[stub.Sent.Count - 1]);

			Drop(stub);

			Assert.IsTrue(await Settles(leaving));
			CollectionAssert.AreEqual(new[] { (int)CloseCode.CONSENTED }, room.Leaves);
			Assert.AreEqual(0, room.Drops);
			Assert.AreEqual(0, room.Reconnection.RetryCount);
			Assert.IsFalse(room.Reconnection.IsReconnecting);
		}

		[Test]
		public async Task LeaveAfterRoomLeftCompletesImmediatelyTest()
		{
			var (room, stub) = JoinedRoom();
			var sent = stub.Sent.Count;

			stub.Dispatch(ConnectionEvent.Closed(4321)); // kicked
			CollectionAssert.AreEqual(new[] { 4321 }, room.Leaves);

			var leaving = room.Leave();
			Assert.IsTrue(leaving.IsCompleted);
			await leaving;

			CollectionAssert.AreEqual(new[] { 4321 }, room.Leaves);
			Assert.AreEqual(sent, stub.Sent.Count, "LEAVE_ROOM sent");
		}

		[Test]
		public async Task RetriesRunningOutLeavesOnceTest()
		{
			var (room, stub) = JoinedRoom();
			room.Reconnection.Backoff = (attempt, delay) => 0;
			room.Reconnection.MaxRetries = 2;

			var left = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			room.OnLeave += _ => left.TrySetResult(true);

			Drop(stub);

			Assert.IsTrue(await Settles(left.Task, 5000));
			await Task.Delay(100);
			CollectionAssert.AreEqual(new[] { (int)CloseCode.FAILED_TO_RECONNECT }, room.Leaves);
			Assert.IsFalse(room.Reconnection.IsReconnecting);
		}
	}
}
