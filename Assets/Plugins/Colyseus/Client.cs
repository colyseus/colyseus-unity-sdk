using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

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

		protected Connection connection;

		protected Dictionary<string, Room> rooms = new Dictionary<string, Room> ();
		protected Dictionary<int, Room> connectingRooms = new Dictionary<int, Room> ();

		protected int requestId;
		protected Dictionary<int, Action<RoomAvailable[]>> roomsAvailableRequests = new Dictionary<int, Action<RoomAvailable[]>>();
		protected RoomAvailable[] roomsAvailableResponse = {
			new RoomAvailable()
		};

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
		public event EventHandler<ErrorEventArgs> OnError;

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
		public Client (string endpoint, string id = null)
		{
			this.id = id;
			this.endpoint = new UriBuilder(new Uri (endpoint));
			this.connection = CreateConnection();
			this.connection.OnClose += (object sender, EventArgs e) => this.OnClose.Invoke(sender, e);
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

			// TODO: this may not be a good idea?
			foreach (var room in this.rooms) {
				room.Value.Recv ();
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

			int requestId = ++this.requestId;
			options.Add ("requestId", requestId);

			var room = new Room (roomName, options);
			this.connectingRooms.Add (requestId, room);

			this.connection.Send (new object[]{Protocol.JOIN_ROOM, roomName, options});

			return room;
		}

		/// <summary>
		/// Request <see cref="Client"/> to rejoin a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to rejoin.</param>
		/// <param name="sessionId">sessionId of client's previous connection</param>
		public Room ReJoin (string roomName, string sessionId)
		{
			Dictionary<string, object> options = new Dictionary<string, object> ();
			options.Add ("sessionId", sessionId);

			return this.Join(roomName, options);
		}

		/// <summary>
		/// Request <see cref="Client"/> to join in a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to join.</param>
		/// <param name="callback">Callback to receive list of available rooms</param>
		public void GetAvailableRooms (string roomName, Action<RoomAvailable[]> callback)
		{
			int requestId = ++this.requestId;
			this.connection.Send (new object[]{Protocol.ROOM_LIST, requestId, roomName});

			this.roomsAvailableRequests.Add (requestId, callback);

			// // USAGE
			// this.client.GetAvailableRooms ("chat", (RoomAvailable[] obj) => {
			// 	for (int i = 0; i < obj.Length; i++) {
			// 		Debug.Log (obj [i].roomId);
			// 		Debug.Log (obj [i].clients);
			// 		Debug.Log (obj [i].maxClients);
			// 		Debug.Log (obj [i].metadata);
			// 	}
			//});
		}

		/// <summary>
		/// Close <see cref="Client"/> connection and leave all joined rooms.
		/// </summary>
		public void Close()
		{
			this.connection.Close();
		}

		protected Connection CreateConnection (string path = "", Dictionary<string, object> options = null)
		{
			if (options == null) {
				options = new Dictionary<string, object> ();
			}

			if (this.id != null) {
				options.Add ("colyseusid", this.id);
			}

			var list = new List<string>();
			foreach(var item in options)
			{
				list.Add(item.Key + "=" + item.Value);
			}

			UriBuilder uriBuilder = new UriBuilder(this.endpoint.Uri);
			uriBuilder.Path = path;
			uriBuilder.Query = string.Join("&", list.ToArray());

			return new Connection (uriBuilder.Uri);
		}

        private void ParseMessage (byte[] recv)
		{
			var message = MsgPack.Deserialize<List<object>> (new MemoryStream(recv));
			var code = (byte) message [0];

			if (code == Protocol.USER_ID) {
				this.id = (string) message [1];

				if (this.OnOpen != null)
					this.OnOpen.Invoke (this, EventArgs.Empty);

			} else if (code == Protocol.JOIN_ROOM) {
				var requestId = (byte) message [2];

				Room room;
				if (this.connectingRooms.TryGetValue (requestId, out room)) {
					room.id = (string) message [1];

					this.endpoint.Path = "/" + room.id;
					this.endpoint.Query = "colyseusid=" + this.id;

					room.SetConnection (CreateConnection(room.id, room.options));
					room.OnLeave += OnLeaveRoom;

					this.rooms.Add (room.id, room);
					this.connectingRooms.Remove (requestId);

				} else {
					throw new Exception ("can't join room using requestId " + requestId.ToString());
				}

			} else if (code == Protocol.JOIN_ERROR) {
				if (this.OnError != null)
					this.OnError.Invoke (this, new ErrorEventArgs ((string) message [2]));

            } else if (code == Protocol.ROOM_LIST) {

                var requestId = Convert.ToInt32(message[1]);
                List<object> _rooms = (List<object>)message[2];
                RoomAvailable[] rooms = new RoomAvailable[_rooms.Count];

                for (int i = 0; i < _rooms.Count; i++) {
                    IDictionary<string, object> room = (IDictionary<string, object>)_rooms[i];
                    RoomAvailable _room = ObjectExtensions.ToObject<RoomAvailable>(_rooms[i]);
                    rooms[i] = _room;
                }

                this.roomsAvailableRequests[requestId].Invoke(rooms);
                this.roomsAvailableRequests.Remove(requestId);

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

	}

}
