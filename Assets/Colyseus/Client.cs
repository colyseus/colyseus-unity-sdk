using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using MsgPack;
using MsgPack.Serialization;

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
		public string id;
		protected UriBuilder endpoint;

		protected Room room;
		protected Dictionary<string, Room> rooms = new Dictionary<string, Room> ();

		protected Connection connection;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class with
		/// the specified Colyseus Game Server Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		/// A <see cref="string"/> that represents the WebSocket URL to connect.
		/// </param>
		public Client (string endpoint, string id = "")
		{
			this.id = id;

			this.endpoint = new UriBuilder(new Uri (endpoint));
			this.endpoint.Query = "colyseusid=" + this.id;
			this.connection = new Connection (this.endpoint.Uri);
		}

		public IEnumerator Connect()
		{
			return this.connection.Connect ();
		}

		public void Recv()
		{
			byte[] data = this.connection.Recv();
			if (data != null)
			{
				this.ParseMessage(data);
			}
		}

		/// <summary>
		/// Request <see cref="Client"/> to join in a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to join.</param>
		/// <param name="options">Custom join request options</param>
		public Room Join (string roomName, Dictionary<string, object> options = null)
		{
			if (options == null) {
				options = new Dictionary<string, object> ();
			}
			
			this.room = new Room (roomName);

			this.connection.Send (new object[]{Protocol.JOIN_ROOM, roomName, options});

			return this.room;
		}

        void ParseMessage (byte[] recv)
		{
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (recv);

			var message = raw.Value.AsList ();
			var code = message [0].AsInt32 ();

			if (code == Protocol.USER_ID) {
				this.id = message [1].AsString ();

				if (this.OnOpen != null)
					this.OnOpen.Invoke (this, EventArgs.Empty);

			} else if (code == Protocol.JOIN_ROOM) {
				var room = this.room;
				room.id = message [1].AsString ();

				this.endpoint.Path = "/" + room.id;
				this.endpoint.Query = "colyseusid=" + this.id;

				room.Connect (new Connection (this.endpoint.Uri));
				room.OnLeave += OnLeaveRoom;

				this.rooms.Add (room.id, room);

			} else if (code == Protocol.JOIN_ERROR) {
				if (this.OnError != null)
					this.OnError.Invoke (this, new ErrorEventArgs (message [2].AsString ()));

			} else {
				if (this.OnMessage != null)
					this.OnMessage.Invoke (this, new MessageEventArgs (message));
            }
		}

		protected void OnLeaveRoom (object sender, EventArgs args) 
		{
			Room room = (Room)sender;
			this.rooms.Remove (room.id);
		}

		/// <summary>
		/// Close <see cref="Client"/> connection and leave all joined rooms.
		/// </summary>
		public void Close()
		{
			this.connection.Close();
		}

		public string error
		{
			get { return this.connection.error; }
		}
	}

}
