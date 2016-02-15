using System;
using System.Collections;
using System.Collections.Generic;

using MsgPack;
using MsgPack.Serialization.CollectionSerializers;

using WebSocketSharp;
using System.IO;
using MsgPack.Serialization;
using System.Reflection;

namespace Colyseus
{
	public class Client
	{
		public string id = null;

		protected WebSocket ws;
		protected Hashtable rooms = new Hashtable();
		protected List<EnqueuedMethod> enqueuedMethods = new List<EnqueuedMethod>();

		public Client (string url)
		{
			this.ws = new WebSocket (url);

			this.ws.OnOpen += onOpen;
			this.ws.OnMessage += onMessage;

			this.ws.ConnectAsync ();
		}

		void onOpen (object sender, EventArgs e)
		{
			if (this.enqueuedMethods.Count > 0) {
				for (int i = 0; i < this.enqueuedMethods.Count; i++) {
					EnqueuedMethod enqueuedMethod = this.enqueuedMethods [i];

					Type thisType = this.GetType();
					MethodInfo method = thisType.GetMethod(enqueuedMethod.methodName);
					method.Invoke(this, enqueuedMethod.arguments);
				}
			}
		}

		void onMessage (object sender, MessageEventArgs e)
		{
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (e.RawData);
			Console.WriteLine (raw.ToString ());

			if (raw.Value.IsList) {
				var message = raw.Value.AsList ();
				var code = message [0].AsInt32 ();

				int roomId = 0;
				try {
					roomId = message [1].AsInt32 ();
				} catch	(InvalidOperationException exception) {
				}

				if (code == Protocol.USER_ID) {
					this.id = message [1].AsString ();
					// TODO: call OnOpen callback

				} else if (code == Protocol.JOIN_ROOM) {
					var roomName = message [2].AsString ();

					if (this.rooms.ContainsKey (roomName)) {
						this.rooms [roomId] = this.rooms [roomName];
						this.rooms.Remove (roomName);
					}

					Room room = (Room) this.rooms [roomId];
					room.id = roomId;
					// TODO: emit room "join" event

				} else if (code == Protocol.JOIN_ERROR) {
					// this.rooms [roomId];

					// TODO: emit room "error" event;

					this.rooms.Remove (roomId);

				} else if (code == Protocol.LEAVE_ROOM) {
					
					// TODO emit room "leave" event;

				} else if (code == Protocol.ROOM_STATE) {
					object state = message [2];
						
					Room room = (Room)this.rooms [roomId];
					Console.WriteLine ("room state!");
					room.state = state;
					// TODO: emit room "update" event.

				} else if (code == Protocol.ROOM_STATE_PATCH) {
//					JsonDiffPatch

				} else if (code == Protocol.ROOM_DATA) {
					// this.rooms[ roomId ].emit('data', message[2])

					// TODO: emit room "data" event.
				}
			}
		}

		public Room Join (string roomName, object options = null)
		{
			if (!this.rooms.ContainsKey (roomName)) {
				this.rooms.Add (roomName, new Room (this, roomName));
			}

			if (this.ws.ReadyState == WebSocketState.Open) {
				this.Send(new object[]{Protocol.JOIN_ROOM, roomName});

			} else {
				// WebSocket not connected.
				// Enqueue it to be called when readyState == OPEN
				this.enqueuedMethods.Add(new EnqueuedMethod("Join", new object[]{ roomName, options }));
			}

			return (Room) this.rooms[ roomName ];
		}


		public void Close ()
		{
			this.ws.CloseAsync ();
		}

		public void Send (object[] data) 
		{
			var stream = new MemoryStream();
			var serializer = MessagePackSerializer.Get<object[]>();
			serializer.Pack( stream, data );

			this.ws.SendAsync (stream.ToArray(), delegate(bool success) {
				Console.WriteLine("Wrote?" + success.ToString());
			});
		}
	}

	public class ProtocolMessage {
		public int code;
		public object data;

		public ProtocolMessage () {}
		public ProtocolMessage (int code, object data) 
		{
			this.code = code;
			this.data = data;
		}
	}

	public class EnqueuedMethod {
		public string methodName;
		public object[] arguments;

		public EnqueuedMethod (string methodName, object[] arguments) 
		{
			this.methodName = methodName;
			this.arguments = arguments;
		}
	}
}