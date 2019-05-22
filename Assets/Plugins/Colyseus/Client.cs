using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;

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
		public string Id;

		public Auth Auth;

		/// <summary>
		/// Occurs when the <see cref="Client"/> connection has been established, and Client <see cref="Id"/> is available.
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

		protected UriBuilder endpoint;
		protected Connection connection;

		protected Dictionary<string, IRoom> rooms = new Dictionary<string, IRoom> ();
		protected Dictionary<int, IRoom> connectingRooms = new Dictionary<int, IRoom> ();

		protected int _requestId;
		protected Dictionary<int, Action<RoomAvailable[]>> roomsAvailableRequests = new Dictionary<int, Action<RoomAvailable[]>>();

		protected byte previousCode = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class with
		/// the specified Colyseus Game Server Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		/// A <see cref="string"/> that represents the WebSocket URL to connect.
		/// </param>
		public Client (string _endpoint, string _id = null)
		{
			Id = _id;
			endpoint = new UriBuilder(new Uri (_endpoint));
			Auth = new Auth(endpoint.Uri);

			connection = CreateConnection();
			connection.OnClose += (sender, e) =>
			{
				if (OnClose != null)
					OnClose.Invoke(sender, e);
			};

		}

		public IEnumerator Connect()
		{
			return connection.Connect ();
		}

		public void Recv()
		{
			byte[] data = connection.Recv();
            while (data != null)
            {
                ParseMessage(data);
                data = connection.Recv();
            }

			// TODO: this may not be a good idea?
			foreach (var room in rooms) {
				room.Value.Recv ();
			}
		}

		/// <summary>
		/// Request <see cref="Client"/> to join in a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to join.</param>
		/// <param name="options">Custom join request options</param>
		public Room<T> Join<T>(string roomName, Dictionary<string, object> options = null)
		{
			if (options == null)
			{
				options = new Dictionary<string, object>();
			}

			int requestId = ++_requestId;
			options.Add("requestId", requestId);

			if (Auth.HasToken)
			{
				options.Add("token", Auth.Token);
			}

			var room = new Room<T>(roomName, options);
			connectingRooms.Add(requestId, room);

			connection.Send(new object[] { Protocol.JOIN_REQUEST, roomName, options });

			return room;
		}

		public Room<IndexedDictionary<string, object>> Join (string roomName, Dictionary<string, object> options = null)
		{
			return Join<IndexedDictionary<string, object>>(roomName, options);
		}

		/// <summary>
		/// Request <see cref="Client"/> to rejoin a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to rejoin.</param>
		/// <param name="sessionId">sessionId of client's previous connection</param>
		public Room<T> ReJoin<T>(string roomName, string sessionId)
		{
			Dictionary<string, object> options = new Dictionary<string, object>();
			options.Add("sessionId", sessionId);

			return Join<T>(roomName, options);
		}

		public Room<IndexedDictionary<string, object>> ReJoin (string roomName, string sessionId)
		{
			return ReJoin<IndexedDictionary<string, object>>(roomName, sessionId);
		}

		/// <summary>
		/// Request <see cref="Client"/> to join in a <see cref="Room"/>.
		/// </summary>
		/// <param name="roomName">The name of the Room to join.</param>
		/// <param name="callback">Callback to receive list of available rooms</param>
		public void GetAvailableRooms (string roomName, Action<RoomAvailable[]> callback)
		{
			int requestId = ++_requestId;
			connection.Send (new object[]{Protocol.ROOM_LIST, requestId, roomName});
			roomsAvailableRequests.Add (requestId, callback);
		}

		/// <summary>
		/// Close <see cref="Client"/> connection and leave all joined rooms.
		/// </summary>
		public void Close()
		{
			connection.Close();
		}

		protected Connection CreateConnection (string path = "", Dictionary<string, object> options = null)
		{
			if (options == null) {
				options = new Dictionary<string, object> ();
			}

			if (Id != null) {
				options.Add ("colyseusid", Id);
			}

			var list = new List<string>();
			foreach(var item in options)
			{
				list.Add(item.Key + "=" + ((item.Value != null) ? Convert.ToString(item.Value) : "null") );
			}

			UriBuilder uriBuilder = new UriBuilder(endpoint.Uri)
			{
				Path = path,
				Query = string.Join("&", list.ToArray())
			};

			return new Connection (uriBuilder.Uri);
		}

        private void ParseMessage (byte[] bytes)
		{

			if (previousCode == 0)
			{
				var code = bytes[0];

				if (code == Protocol.USER_ID)
				{
					Id = System.Text.Encoding.UTF8.GetString(bytes, 2, bytes[1]);

					if (OnOpen != null)
						OnOpen.Invoke(this, EventArgs.Empty);

				}
				else if (code == Protocol.JOIN_REQUEST)
				{
					var requestId = bytes[1];

					IRoom room;
					if (connectingRooms.TryGetValue(requestId, out room))
					{
						room.Id = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes[2]);

						endpoint.Path = "/" + room.Id;
						endpoint.Query = "colyseusid=" + this.Id;

						var processPath = "";
						var nextIndex = 3 + room.Id.Length;
						if (bytes.Length > nextIndex)
						{
							processPath = System.Text.Encoding.UTF8.GetString(bytes, nextIndex + 1, bytes[nextIndex]) + "/";
						}

						room.SetConnection(CreateConnection(processPath + room.Id, room.Options));
						room.OnLeave += OnLeaveRoom;

						if (rooms.ContainsKey(room.Id))
						{
							rooms.Remove(room.Id);
						}
						rooms.Add(room.Id, room);
						connectingRooms.Remove(requestId);

					}
					else
					{
						throw new Exception("can't join room using requestId " + requestId.ToString());
					}

				}
				else if (code == Protocol.JOIN_ERROR)
				{
					string message = System.Text.Encoding.UTF8.GetString(bytes, 2, bytes[1]);
					if (OnError != null)
						OnError.Invoke(this, new ErrorEventArgs(message));

				}
				else if (code == Protocol.ROOM_LIST)
				{
					previousCode = code;
				}
			}
			else
			{
				if (previousCode == Protocol.ROOM_LIST)
				{
					var message = MsgPack.Deserialize<List<object>>(new MemoryStream(bytes));
					var requestId = Convert.ToInt32(message[0]);
					List<object> _rooms = (List<object>)message[1];
					RoomAvailable[] availableRooms = new RoomAvailable[_rooms.Count];

					for (int i = 0; i < _rooms.Count; i++)
					{
						IDictionary<string, object> room = (IDictionary<string, object>)_rooms[i];
						RoomAvailable _room = ObjectExtensions.ToObject<RoomAvailable>(_rooms[i]);
						availableRooms[i] = _room;
					}

					roomsAvailableRequests[requestId].Invoke(availableRooms);
					roomsAvailableRequests.Remove(requestId);
				}

				previousCode = 0;
			}

		}

		protected void OnLeaveRoom (object sender, EventArgs args)
		{
			IRoom room = (IRoom) sender;
			rooms.Remove (room.Id);
		}

	}

}
