using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using MsgPack;
using MsgPack.Serialization.CollectionSerializers;

using WebSocketSharp;
using MsgPack.Serialization;

using Newtonsoft.Json.Linq;
//using JsonDiffPatch;

namespace Colyseus
{
	/// <summary>
	/// Colyseus.Client
	/// </summary>
	/// <remarks>
	/// Provides integration between Colyseus Game Server through WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
	/// </remarks>
	public class Client
	{
		/// <summary>
		/// Unique <see cref="Client"/> identifier.
		/// </summary>
		public string id = null;

		private WebSocket ws;
		private Hashtable rooms = new Hashtable();
		private List<EnqueuedMethod> enqueuedMethods = new List<EnqueuedMethod>();

		// Events

		/// <summary>
		/// Occurs when the <see cref="Client"/> connection has been established, and Client <see cref="id"/> is available.
		/// </summary>
		public event EventHandler OnOpen;

		/// <summary>
		/// Occurs when the <see cref="Client"/> connection has been closed.
		/// </summary>
		public event EventHandler OnClose;

		/// <summary>
		/// Occurs when the <see cref="Client"/> gets an error.
		/// </summary>
		public event EventHandler OnError;

		/// <summary>
		/// Occurs when the <see cref="Client"/> receives a message from server.
		/// </summary>
		public event EventHandler<MessageEventArgs> OnMessage;

		// TODO: implement auto-reconnect feature
		// public event EventHandler OnReconnect; 

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class with
		/// the specified Colyseus Game Server Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		/// A <see cref="string"/> that represents the WebSocket URL to connect.
		/// </param>
		public Client (string endpoint)
		{
			this.ws = new WebSocket (endpoint);

			this.ws.OnOpen += OnOpenHandler;
			this.ws.OnMessage += OnMessageHandler;
			this.ws.OnClose += OnCloseHandler;
			this.ws.OnError += OnErrorHandler;

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

		void OnCloseHandler (object sender, CloseEventArgs e)
		{
			this.OnClose.Emit (this, e);
		}

		void OnMessageHandler (object sender, WebSocketSharp.MessageEventArgs e)
		{
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (e.RawData);

			var message = raw.Value.AsList ();
			var code = message [0].AsInt32 ();

			// Parse roomId or roomName
			Room room = null;
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

				room = (Room) this.rooms [roomId];
				room.id = roomId;

			} else if (code == Protocol.JOIN_ERROR) {
				room = (Room) this.rooms [roomName];

				MessageEventArgs error = new MessageEventArgs(room, message);
				room.EmitError (error);
				this.OnError.Emit(this, error);

				this.rooms.Remove (roomName);

			} else if (code == Protocol.LEAVE_ROOM) {
				room = (Room) this.rooms [roomId];
				room.Leave (false);

			} else if (code == Protocol.ROOM_STATE) {
				object state = message [2];

				room = (Room)this.rooms [roomId];
				room.state = JToken.Parse (message [2].ToString ());

			} else if (code == Protocol.ROOM_STATE_PATCH) {
				room = (Room) this.rooms [roomId];
				room.ApplyPatches(JArray.Parse ( message [2].ToString() ));

			} else if (code == Protocol.ROOM_DATA) {
				room = (Room) this.rooms [roomId];
				room.ReceiveData (message [2]);
			}

			this.OnMessage.Emit (this, new MessageEventArgs(room, message));
		}

		/// <summary>
		/// Request <see cref="Client"/> to join in a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to join.</param>
		/// <param name="options">Custom join request options</param>
		public Room Join (string roomName, object options = null)
		{
			if (!this.rooms.ContainsKey (roomName)) {
				this.rooms.Add (roomName, new Room (this, roomName));
			}

			if (this.ws.ReadyState == WebSocketState.Open) {
				this.Send(new object[]{Protocol.JOIN_ROOM, roomName});

			} else {
				// If WebSocket is not connected yet, enqueue call to when its ready.
				this.enqueuedMethods.Add(new EnqueuedMethod("Join", new object[]{ roomName, options }));
			}

			return (Room) this.rooms[ roomName ];
		}

		private void OnErrorHandler(object sender, EventArgs args)
		{
			this.OnError.Emit (sender, args);
		}

		/// <summary>
		/// Close <see cref="Client"/> connection and leave all joined rooms.
		/// </summary>
		public void Close ()
		{
			this.ws.CloseAsync ();
		}

		/// <summary>
		/// Send data to all connected rooms.
		/// </summary>
		/// <param name="data">Data to be sent to all connected rooms.</param>
		public void Send (object[] data) 
		{
			var stream = new MemoryStream();
			var serializer = MessagePackSerializer.Get<object[]>();
			serializer.Pack( stream, data );

			this.ws.SendAsync (stream.ToArray(), delegate(bool success) {
				// sent successfully
			});
		}
	}

	class EnqueuedMethod {
		public string methodName;
		public object[] arguments;

		public EnqueuedMethod (string methodName, object[] arguments) 
		{
			this.methodName = methodName;
			this.arguments = arguments;
		}
	}
}