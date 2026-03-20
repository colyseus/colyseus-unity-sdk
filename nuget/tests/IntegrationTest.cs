using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Colyseus;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	[TestFixture]
	public class IntegrationTest
	{
		private Client client;

		[SetUp]
		public void Init()
		{
			client = new Client("ws://localhost:2567");
		}

		[Test]
		public async Task JoinRoom()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");

			Assert.IsNotNull(room);
			Assert.IsNotNull(room.RoomId);
			Assert.IsNotNull(room.SessionId);
			Assert.AreEqual("my_room", room.Name);

			await room.Leave();
		}

		[Test]
		public async Task ReceiveInitialState()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};

			var received = await WithTimeout(stateReceived.Task, 5000);
			Assert.IsTrue(received, "Should receive initial state");

			var state = room.State;
			Assert.IsNotNull(state.players);
			Assert.IsTrue(state.players.Count >= 1, "Should have at least 1 player (self)");
			Assert.IsNotNull(state.players[room.SessionId], "Player should exist with own sessionId");

			// Player should have an initial item (sword)
			var self = state.players[room.SessionId];
			Assert.AreEqual(1, self.items.Count);
			Assert.AreEqual("sword", self.items[0].name);

			await room.Leave();
		}

		[Test]
		public async Task SendAndReceiveMessage_Move()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			// Send move message
			await room.Send("move", new { x = 100, y = 200 });

			// Wait for state update
			await Task.Delay(200);

			var self = room.State.players[room.SessionId];
			Assert.AreEqual(100f, self.x);
			Assert.AreEqual(200f, self.y);

			await room.Leave();
		}

		[Test]
		public async Task SendAndReceiveMessage_AddItem()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var self = room.State.players[room.SessionId];
			var initialCount = self.items.Count;

			await room.Send("add_item", new { name = "shield" });
			await Task.Delay(200);

			Assert.AreEqual(initialCount + 1, self.items.Count);
			Assert.AreEqual("shield", self.items[self.items.Count - 1].name);

			await room.Leave();
		}

		[Test]
		public async Task SendAndReceiveMessage_RemoveItem()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var self = room.State.players[room.SessionId];
			var initialCount = self.items.Count;
			Assert.IsTrue(initialCount > 0, "Should have at least 1 item");

			await room.Send("remove_item", new { });
			await Task.Delay(200);

			Assert.AreEqual(initialCount - 1, self.items.Count);

			await room.Leave();
		}

		[Test]
		public async Task SendAndReceiveMessage_AddBot()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var initialCount = room.State.players.Count;

			await room.Send("add_bot", new { });
			await Task.Delay(200);

			Assert.AreEqual(initialCount + 1, room.State.players.Count);

			// Find the bot
			Player bot = null;
			room.State.players.ForEach((key, player) =>
			{
				if (player.isBot)
					bot = player;
			});
			Assert.IsNotNull(bot, "Should have a bot player");
			Assert.IsTrue(bot.isBot);

			await room.Leave();
		}

		[Test]
		public async Task HostAssignment()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			Assert.IsNotNull(room.State.host, "First player should be assigned as host");
			Assert.AreEqual(room.SessionId, room.State.currentTurn, "First player should have the current turn");

			await room.Leave();
		}

		[Test]
		public async Task BroadcastMessage_Weather()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var weatherReceived = new TaskCompletionSource<object>();

			room.OnMessage<object>("weather", (message) =>
			{
				if (!weatherReceived.Task.IsCompleted)
					weatherReceived.TrySetResult(message);
			});

			// Server broadcasts weather every 4 seconds
			var weather = await WithTimeout(weatherReceived.Task, 6000);
			Assert.IsNotNull(weather, "Should receive weather broadcast");

			await room.Leave();
		}

		[Test]
		public async Task MultipleClients_SeeEachOther()
		{
			var client2 = new Client("ws://localhost:2567");

			var room1 = await client.Create<MyRoomState>("my_room");
			var stateReceived1 = new TaskCompletionSource<bool>();

			room1.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived1.Task.IsCompleted)
					stateReceived1.TrySetResult(true);
			};
			await WithTimeout(stateReceived1.Task, 5000);

			// Second client joins the same room
			var room2 = await client2.JoinById<MyRoomState>(room1.RoomId);
			await Task.Delay(500);

			// Both clients should see both players
			Assert.AreEqual(2, room1.State.players.Count, "Room1 should see 2 players");
			Assert.AreEqual(2, room2.State.players.Count, "Room2 should see 2 players");

			Assert.IsNotNull(room1.State.players[room2.SessionId], "Room1 should see Room2's player");
			Assert.IsNotNull(room2.State.players[room1.SessionId], "Room2 should see Room1's player");

			await room1.Leave();
			await room2.Leave();
		}

		[Test]
		public async Task OnLeave_ConsentedLeave()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var leaveCode = new TaskCompletionSource<int>();

			room.OnLeave += (code) =>
			{
				if (!leaveCode.Task.IsCompleted)
					leaveCode.TrySetResult(code);
			};

			await room.Leave(consented: true);

			var code = await WithTimeout(leaveCode.Task, 5000);
			Assert.IsTrue(code > 0, $"Should receive a close code, got {code}");
		}

		[Test]
		public async Task Reconnection_DropAndReconnect()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			// Send a move so we can verify state persists after reconnection
			await room.Send("move", new { x = 42, y = 84 });
			await Task.Delay(200);

			var dropReceived = new TaskCompletionSource<int>();
			var reconnectReceived = new TaskCompletionSource<bool>();

			room.OnDrop += (code) =>
			{
				if (!dropReceived.Task.IsCompleted)
					dropReceived.TrySetResult(code);
			};

			room.OnReconnect += () =>
			{
				if (!reconnectReceived.Task.IsCompleted)
					reconnectReceived.TrySetResult(true);
			};

			// Disable min uptime check so reconnection triggers immediately
			room.Reconnection.MinUptime = 0;

			// Force-drop the connection
			room.Connection.Drop();

			var dropCode = await WithTimeout(dropReceived.Task, 5000);
			Assert.IsTrue(
				dropCode == (int)CloseCode.ABNORMAL_CLOSURE ||
				dropCode == (int)CloseCode.NO_STATUS_RECEIVED ||
				dropCode == (int)CloseCode.GOING_AWAY,
				$"Expected a reconnectable close code, got {dropCode}"
			);

			var reconnected = await WithTimeout(reconnectReceived.Task, 10000);
			Assert.IsTrue(reconnected, "Should reconnect successfully");

			// Verify state is preserved after reconnection
			await Task.Delay(200);
			var self = room.State.players[room.SessionId];
			Assert.IsNotNull(self, "Player should still exist after reconnection");
			Assert.AreEqual(42f, self.x, "X position should be preserved");
			Assert.AreEqual(84f, self.y, "Y position should be preserved");

			await room.Leave();
		}

		[Test]
		public async Task Reconnection_PlayerDisconnectedFlag()
		{
			var client2 = new Client("ws://localhost:2567");

			var room1 = await client.Create<MyRoomState>("my_room");
			var stateReceived1 = new TaskCompletionSource<bool>();

			room1.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived1.Task.IsCompleted)
					stateReceived1.TrySetResult(true);
			};
			await WithTimeout(stateReceived1.Task, 5000);

			var room2 = await client2.JoinById<MyRoomState>(room1.RoomId);
			await Task.Delay(500);

			// Delay reconnection so we can observe the disconnected flag
			room2.Reconnection.MinUptime = 0;
			room2.Reconnection.MinDelay = 2000;
			room2.Reconnection.Delay = 2000;

			// Drop client2's connection
			room2.Connection.Drop();

			// Wait for room1 to see the disconnected flag
			await Task.Delay(1000);
			var player2InRoom1 = room1.State.players[room2.SessionId];
			Assert.IsNotNull(player2InRoom1, "Player2 should still exist (waiting for reconnect)");
			Assert.IsTrue(player2InRoom1.disconnected, "Player2 should be marked as disconnected");

			// Wait for room2 to reconnect
			var reconnectReceived = new TaskCompletionSource<bool>();
			room2.OnReconnect += () =>
			{
				if (!reconnectReceived.Task.IsCompleted)
					reconnectReceived.TrySetResult(true);
			};
			await WithTimeout(reconnectReceived.Task, 10000);
			await Task.Delay(500);

			// Verify disconnected flag is cleared
			player2InRoom1 = room1.State.players[room2.SessionId];
			Assert.IsFalse(player2InRoom1.disconnected, "Player2 should no longer be disconnected");

			await room1.Leave();
			await room2.Leave();
		}

		[Test]
		public async Task Reconnection_SendMessageAfterReconnect()
		{
			var room = await client.JoinOrCreate<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var reconnectReceived = new TaskCompletionSource<bool>();
			room.OnReconnect += () =>
			{
				if (!reconnectReceived.Task.IsCompleted)
					reconnectReceived.TrySetResult(true);
			};

			room.Reconnection.MinUptime = 0;
			room.Connection.Drop();

			await WithTimeout(reconnectReceived.Task, 10000);

			// Send a message after reconnection
			await room.Send("move", new { x = 999, y = 888 });
			await Task.Delay(200);

			var self = room.State.players[room.SessionId];
			Assert.AreEqual(999f, self.x, "Should process messages after reconnect");
			Assert.AreEqual(888f, self.y, "Should process messages after reconnect");

			await room.Leave();
		}

		[Test]
		public async Task Callbacks_OnAdd_OnRemove_Players()
		{
			var client2 = new Client("ws://localhost:2567");

			var room1 = await client.Create<MyRoomState>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room1.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var callbacks = Callbacks.Get(room1);

			var addedSessionIds = new List<string>();
			var removedSessionIds = new List<string>();

			callbacks.OnAdd(state => state.players, (sessionId, player) =>
			{
				addedSessionIds.Add(sessionId);
			});

			callbacks.OnRemove(state => state.players, (sessionId, player) =>
			{
				removedSessionIds.Add(sessionId);
			});

			// Self should already trigger onAdd
			Assert.Contains(room1.SessionId, addedSessionIds);

			// Second client joins
			var room2 = await client2.JoinById<MyRoomState>(room1.RoomId);
			await Task.Delay(500);

			Assert.Contains(room2.SessionId, addedSessionIds);
			Assert.AreEqual(2, addedSessionIds.Count);

			// Second client leaves
			await room2.Leave();
			await Task.Delay(500);

			Assert.Contains(room2.SessionId, removedSessionIds);
			Assert.AreEqual(1, removedSessionIds.Count);

			await room1.Leave();
		}

		[Test]
		public async Task JoinRoom_DynamicSchema()
		{
			var room = await client.JoinOrCreate<DynamicSchema>("my_room");

			Assert.IsNotNull(room);
			Assert.IsNotNull(room.RoomId);
			Assert.IsNotNull(room.SessionId);
			Assert.AreEqual("my_room", room.Name);

			await room.Leave();
		}

		[Test]
		public async Task ReceiveInitialState_DynamicSchema()
		{
			var room = await client.JoinOrCreate<DynamicSchema>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};

			var received = await WithTimeout(stateReceived.Task, 5000);
			Assert.IsTrue(received, "Should receive initial state");

			var players = room.State.Get<MapSchema<DynamicSchema>>("players");
			Assert.IsNotNull(players);
			Assert.IsTrue(players.Count >= 1, "Should have at least 1 player (self)");
			Assert.IsNotNull(players[room.SessionId], "Player should exist with own sessionId");

			// Player should have an initial item (sword)
			var self = players[room.SessionId];
			var items = self.Get<ArraySchema<DynamicSchema>>("items");
			Assert.AreEqual(1, items.Count);
			Assert.AreEqual("sword", items[0].Get<string>("name"));

			await room.Leave();
		}

		[Test]
		public async Task Callbacks_OnAdd_OnRemove_Players_DynamicSchema()
		{
			var client2 = new Client("ws://localhost:2567");

			var room1 = await client.Create<DynamicSchema>("my_room");
			var stateReceived = new TaskCompletionSource<bool>();

			room1.OnStateChange += (state, isFirstState) =>
			{
				if (isFirstState && !stateReceived.Task.IsCompleted)
					stateReceived.TrySetResult(true);
			};
			await WithTimeout(stateReceived.Task, 5000);

			var callbacks = Callbacks.Get(room1);

			var addedSessionIds = new List<string>();
			var removedSessionIds = new List<string>();

			callbacks.OnAdd<DynamicSchema>("players", (sessionId, player) =>
			{
				addedSessionIds.Add(sessionId);
			});

			callbacks.OnRemove<DynamicSchema>("players", (sessionId, player) =>
			{
				removedSessionIds.Add(sessionId);
			});

			// Self should already trigger onAdd
			Assert.Contains(room1.SessionId, addedSessionIds);

			// Second client joins
			var room2 = await client2.JoinById<DynamicSchema>(room1.RoomId);
			await Task.Delay(500);

			Assert.Contains(room2.SessionId, addedSessionIds);
			Assert.AreEqual(2, addedSessionIds.Count);

			// Second client leaves
			await room2.Leave();
			await Task.Delay(500);

			Assert.Contains(room2.SessionId, removedSessionIds);
			Assert.AreEqual(1, removedSessionIds.Count);

			await room1.Leave();
		}

		private async Task<T> WithTimeout<T>(Task<T> task, int timeoutMs)
		{
			using var cts = new CancellationTokenSource(timeoutMs);
			var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token));
			if (completedTask == task)
			{
				cts.Cancel();
				return await task;
			}
			return default;
		}
	}
}
