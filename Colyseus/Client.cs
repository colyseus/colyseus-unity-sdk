using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using MsgPack;
using MsgPack.Serialization.CollectionSerializers;

using WebSocketSharp;
using MsgPack.Serialization;

using Newtonsoft.Json;
using JsonDiffPatch;

namespace Colyseus
{
	public class Client
	{
		public string id = null;

		protected WebSocket ws;
		protected Hashtable rooms = new Hashtable();
		protected List<EnqueuedMethod> enqueuedMethods = new List<EnqueuedMethod>();

		// Events
		public event EventHandler OnOpen;
		public event EventHandler OnClose;
		public event EventHandler OnReconnect;
		public event EventHandler<MessageEventArgs> OnMessage;
		public event EventHandler<MessageEventArgs> OnError;

		public Client (string url)
		{
			this.ws = new WebSocket (url);

			this.ws.OnOpen += OnOpenHandler;
			this.ws.OnMessage += OnMessageHandler;

			this.ws.ConnectAsync ();
		}

		void OnOpenHandler (object sender, EventArgs e)
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

		void OnMessageHandler (object sender, WebSocketSharp.MessageEventArgs e)
		{
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (e.RawData);
			Console.WriteLine (raw.ToString ());

			if (raw.Value.IsList) {
				var message = raw.Value.AsList ();
				var code = message [0].AsInt32 ();

				// Parse roomId or roomName
				int roomId = 0;
				string roomName = null;
				try {
					roomId = message [1].AsInt32 ();
				} catch (InvalidOperationException ex1) {
					try {
						roomName = message[1].AsString();
					} catch (InvalidOperationException ex2) {
					}
				}

				if (code == Protocol.USER_ID) {
					this.id = message [1].AsString ();
					this.OnOpen.Emit (this, EventArgs.Empty);

				} else if (code == Protocol.JOIN_ROOM) {
					roomName = message[2].AsString();

					if (this.rooms.ContainsKey (roomName)) {
						this.rooms [roomId] = this.rooms [roomName];
						this.rooms.Remove (roomName);
					}

					Room room = (Room) this.rooms [roomId];
					room.id = roomId;

				} else if (code == Protocol.JOIN_ERROR) {
					Room room = (Room) this.rooms [roomName];

					this.OnError.Emit(this, new MessageEventArgs(room));

					this.rooms.Remove (roomName);

				} else if (code == Protocol.LEAVE_ROOM) {
					Room room = (Room) this.rooms [roomId];
					room.Leave (false);

				} else if (code == Protocol.ROOM_STATE) {

//					Newtonsoft.Json.Linq.JToken
					object state = message [2];
						
					Room room = (Room)this.rooms [roomId];
					room.state = Newtonsoft.Json.Linq.JToken.Parse (message [2].ToString ());

				} else if (code == Protocol.ROOM_STATE_PATCH) {
					PatchDocument patches = PatchDocument.Parse (message [2].ToString());

					Room room = (Room) this.rooms [roomId];
					room.ApplyPatches(patches);

				} else if (code == Protocol.ROOM_DATA) {
					Room room = (Room) this.rooms [roomId];
					room.ReceiveData (message [2]);
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