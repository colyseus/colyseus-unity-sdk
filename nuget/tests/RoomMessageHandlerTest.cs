using System;
using System.Collections.Generic;
using NUnit.Framework;
using Colyseus;

namespace Colyseus.Tests
{
	/// <summary>
	///     room.OnMessage() registration: multiple handlers per message type, and
	///     the Action returned by each registration that unregisters it again.
	/// </summary>
	[TestFixture]
	public class RoomMessageHandlerTest
	{
		private class TestRoom : Room<SchemaTest.Phase0.P0State>
		{
			public TestRoom() : base("messages")
			{
				RoomId = "messages";
			}

			public void Feed(byte[] frame)
			{
				ParseMessage(frame);
			}

			public bool HasHandler(string type)
			{
				return OnMessageHandlers.TryGetValue(type, out var handler) && !handler.IsEmpty;
			}
		}

		// [ROOM_DATA]["move" fixstr][msgpack payload]
		private static byte[] StringFrame(string type, params byte[] payload)
		{
			var frame = new List<byte> { Protocol.ROOM_DATA, (byte)(0xa0 | type.Length) };
			foreach (var c in type)
			{
				frame.Add((byte)c);
			}
			frame.AddRange(payload);
			return frame.ToArray();
		}

		// [ROOM_DATA][type as positive fixint][msgpack payload]
		private static byte[] NumberFrame(byte type, byte payload)
		{
			return new byte[] { Protocol.ROOM_DATA, type, payload };
		}

		[Test]
		public void MultipleHandlersForSameTypeTest()
		{
			var room = new TestRoom();

			var calls = new List<string>();
			room.OnMessage<int>("move", value => calls.Add("first:" + value));
			room.OnMessage<int>("move", value => calls.Add("second:" + value));

			room.Feed(StringFrame("move", 7));

			// invoked in registration order
			CollectionAssert.AreEqual(new[] { "first:7", "second:7" }, calls);
		}

		[Test]
		public void UnregisterSingleHandlerTest()
		{
			var room = new TestRoom();

			var calls = new List<string>();
			var removeFirst = room.OnMessage<int>("move", value => calls.Add("first:" + value));
			room.OnMessage<int>("move", value => calls.Add("second:" + value));

			removeFirst();
			room.Feed(StringFrame("move", 7));

			CollectionAssert.AreEqual(new[] { "second:7" }, calls);

			// the surviving handler keeps the type registered
			Assert.IsTrue(room.HasHandler("move"));
		}

		[Test]
		public void UnregisterLastHandlerLeavesTypeUnhandledTest()
		{
			var room = new TestRoom();

			var calls = 0;
			var remove = room.OnMessage<int>("move", _ => calls++);
			Assert.IsTrue(room.HasHandler("move"));

			remove();
			Assert.IsFalse(room.HasHandler("move"));

			room.Feed(StringFrame("move", 7));
			Assert.AreEqual(0, calls);
		}

		[Test]
		public void UnregisterIsIdempotentTest()
		{
			var room = new TestRoom();

			var calls = 0;
			var remove = room.OnMessage<int>("move", _ => calls++);

			remove();
			Assert.DoesNotThrow(() => remove());

			// a re-registration is untouched by the stale remover
			room.OnMessage<int>("move", _ => calls++);
			remove();

			room.Feed(StringFrame("move", 7));
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void ConflictingMessageTypeThrowsTest()
		{
			var room = new TestRoom();

			room.OnMessage<int>("move", _ => { });

			var ex = Assert.Throws<Exception>(() => room.OnMessage<string>("move", _ => { }));
			StringAssert.Contains("already registered", ex.Message);
		}

		[Test]
		public void MessageTypeIsFreeToChangeOnceEmptyTest()
		{
			var room = new TestRoom();

			var remove = room.OnMessage<int>("move", _ => { });
			remove();

			// no handlers left, so the type no longer conflicts
			string received = null;
			Assert.DoesNotThrow(() => room.OnMessage<string>("move", value => received = value));

			room.Feed(StringFrame("move", 0xa2, (byte)'h', (byte)'i'));
			Assert.AreEqual("hi", received);
		}

		[Test]
		public void MultipleHandlersForNumericTypeTest()
		{
			var room = new TestRoom();

			var calls = new List<string>();
			var removeFirst = room.OnMessage<int>(3, value => calls.Add("first:" + value));
			room.OnMessage<int>(3, value => calls.Add("second:" + value));

			room.Feed(NumberFrame(3, 7));
			CollectionAssert.AreEqual(new[] { "first:7", "second:7" }, calls);

			calls.Clear();
			removeFirst();

			room.Feed(NumberFrame(3, 7));
			CollectionAssert.AreEqual(new[] { "second:7" }, calls);
		}
	}
}
