using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using MsgPack;
using MsgPack.Serialization;

#if !WINDOWS_UWP
using WebSocketSharp;
#endif
using UnityEngine;

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
		public WebSocket ws;
		private Dictionary<string, Room> rooms = new Dictionary<string, Room> ();

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
			MessagePackSerializer.PrepareType<MessagePackObject>();
			MessagePackSerializer.PrepareType<object[]>();
			MessagePackSerializer.PrepareType<byte[]>();

			this.ws = new WebSocket (new Uri(endpoint));

			//this.ws.OnMessage += OnMessageHandler;
			//this.ws.OnClose += OnCloseHandler;
			//this.ws.OnError += OnErrorHandler;
		}

		public IEnumerator Connect()
		{
			return this.ws.Connect();
		}

		public void Recv()
		{
			byte[] data = this.ws.Recv();
			if (data != null)
			{
				this.ParseMessage(data);
			}
		}

#if !WINDOWS_UWP
        void OnCloseHandler (object sender, CloseEventArgs e)
		{
			this.OnClose.Emit (this, e);
		}
#else 
        void OnCloseHandler(object sender, EventArgs e)
        {
            this.OnClose.Invoke(this, e);
        }
#endif

        void ParseMessage (byte[] recv)
		{
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (recv);

			var message = raw.Value.AsList ();
			var code = message [0].AsInt32 ();

            // Parse roomId or roomName
            Room room = null;
			int roomIdInt32 = 0;
			string roomId = "0";
			string roomName = null;

			try {
				roomIdInt32 = message[1].AsInt32();
				roomId = roomIdInt32.ToString();
			} catch (InvalidOperationException) {
				try {
					roomName = message[1].AsString();
				} catch (InvalidOperationException) {}
			}

			if (code == Protocol.USER_ID) {
				this.id = message [1].AsString ();
                this.OnOpen.Invoke(this, EventArgs.Empty);
            } else if (code == Protocol.JOIN_ROOM) {
				roomName = message[2].AsString();

				if (this.rooms.ContainsKey (roomName)) {
					this.rooms [roomId] = this.rooms [roomName];
					this.rooms.Remove (roomName);
				}

				room = this.rooms [roomId];
				room.id = roomIdInt32;

			} else if (code == Protocol.JOIN_ERROR) {
				room = this.rooms [roomName];

				MessageEventArgs error = new MessageEventArgs(room, message);
				room.EmitError (error);
                this.OnError.Invoke(this, error);
                this.rooms.Remove (roomName);

			} else if (code == Protocol.LEAVE_ROOM) {
				room = this.rooms [roomId];
				room.Leave (false);

			} else if (code == Protocol.ROOM_STATE) {

				var state = message [2];
				var remoteCurrentTime = message [3].AsInt32();
				var remoteElapsedTime = message [4].AsInt32();

				room = this.rooms [roomId];
				// JToken.Parse (message [2].ToString ())
				room.SetState (state, remoteCurrentTime, remoteElapsedTime);

			} else if (code == Protocol.ROOM_STATE_PATCH) {
				room = this.rooms [roomId];

				IList<MessagePackObject> patchBytes = message [2].AsList();
				byte[] patches = new byte[patchBytes.Count];

				int idx = 0;
				foreach (MessagePackObject obj in patchBytes)
				{
					patches[idx] = obj.AsByte();
					idx++;
				}

				room.ApplyPatch (patches);

			} else if (code == Protocol.ROOM_DATA) {
				room = this.rooms [roomId];
				room.ReceiveData (message [2]);
                this.OnMessage.Invoke(this, new MessageEventArgs(room, message[2]));
            }
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

			this.Send(new object[]{Protocol.JOIN_ROOM, roomName, options});

			return this.rooms[ roomName ];
		}

		private void OnErrorHandler(object sender, EventArgs args)
		{
            this.OnError.Invoke(sender, args);
        }

		/// <summary>
		/// Send data to all connected rooms.
		/// </summary>
		/// <param name="data">Data to be sent to all connected rooms.</param>
		public void Send (object[] data)
		{
			var serializer = MessagePackSerializer.Get<object[]>();
			this.ws.Send(serializer.PackSingleObject(data));
		}

		/// <summary>
		/// Close <see cref="Client"/> connection and leave all joined rooms.
		/// </summary>
		public void Close()
		{
			this.ws.Close();
		}

		public string error
		{
			get { return this.ws.error; }
		}
	}

}
