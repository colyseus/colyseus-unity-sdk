using System;
using System.IO;
using System.Threading.Tasks;
using GameDevWare.Serialization;

namespace Colyseus
{
	public delegate void ColyseusOpenEventHandler();
	public delegate void ColyseusCloseEventHandler(NativeWebSocket.WebSocketCloseCode code);
	public delegate void ColyseusErrorEventHandler(string message);

	public class RoomAvailable
	{
		public string roomId { get; set; }
		public uint clients { get; set; }
		public uint maxClients { get; set; }
		public object metadata { get; set; }
	}

	public interface IRoom
	{
		event ColyseusCloseEventHandler OnLeave;

		Task Connect();
		Task Leave(bool consented);
	}

	public class Room<T> : IRoom
	{
		public delegate void RoomOnMessageEventHandler(object message);
		public delegate void RoomOnStateChangeEventHandler(T state, bool isFirstState);

		public string Id;
		public string Name;
		public string SessionId;

		public Connection Connection;

		public string SerializerId;
		protected ISerializer<T> serializer;

		protected byte previousCode = 0;

		/// <summary>
		/// Occurs when the <see cref="Client"/> successfully connects to the <see cref="Room"/>.
		/// </summary>
		public event ColyseusOpenEventHandler OnJoin;

		/// <summary>
		/// Occurs when some error has been triggered in the room.
		/// </summary>
		public event ColyseusErrorEventHandler OnError;

		/// <summary>
		/// Occurs when <see cref="Client"/> leaves this room.
		/// </summary>
		public event ColyseusCloseEventHandler OnLeave;

		/// <summary>
		/// Occurs when server sends a message to this <see cref="Room"/>
		/// </summary>
		public event RoomOnMessageEventHandler OnMessage;

		/// <summary>
		/// Occurs after applying the patched state on this <see cref="Room"/>.
		/// </summary>
		public event RoomOnStateChangeEventHandler OnStateChange;

		/// <summary>
		/// Initializes a new instance of the <see cref="Room"/> class.
		/// It synchronizes state automatically with the server and send and receive messaes.
		/// </summary>
		/// <param name="client">
		/// The <see cref="Client"/> client connection instance.
		/// </param>
		/// <param name="name">The name of the room</param>
		public Room (string name)
		{
			Name = name;
		}

		public async Task Connect()
		{
			await Connection.Connect();
		}

		public void SetConnection (Connection connection)
		{
			Connection = connection;

			Connection.OnClose += (code) => OnLeave?.Invoke(code);
			Connection.OnError += (message) => OnError?.Invoke(message);
			Connection.OnMessage += (bytes) => ParseMessage(bytes);
		}

		public void SetState(byte[] encodedState)
		{
			serializer.SetState(encodedState);
			OnStateChange?.Invoke (serializer.GetState(), true);
		}

		public T State
		{
			get { return serializer.GetState(); }
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public async Task Leave (bool consented = true)
		{
			if (Id != null) {
				if (consented)
				{
					await Connection.Send(new object[] { Protocol.LEAVE_ROOM }); 
				}
				else
				{
					await Connection.Close();
				}

			} else if (OnLeave != null) {
				OnLeave?.Invoke (NativeWebSocket.WebSocketCloseCode.Normal);
			}
		}

		/// <summary>
		/// Send data to this room.
		/// </summary>
		/// <param name="data">Data to be sent</param>
		public async Task Send (object data)
		{
			await Connection.Send(new object[]{Protocol.ROOM_DATA, data});
		}

		public Listener<Action<PatchObject>> Listen(Action<PatchObject> callback)
		{
			if (string.IsNullOrEmpty(SerializerId))
			{
				throw new Exception("room.Listen() should be called after room.OnJoin");
			}
			return ((FossilDeltaSerializer)serializer).State.Listen(callback);
		}

		public Listener<Action<DataChange>> Listen(string segments, Action<DataChange> callback, bool immediate = false)
		{
			if (string.IsNullOrEmpty(SerializerId))
			{
				throw new Exception("room.Listen() should be called after room.OnJoin");
			}
			return ((FossilDeltaSerializer)serializer).State.Listen(segments, callback, immediate);
		}

		protected async void ParseMessage (byte[] bytes)
		{
			if (previousCode == 0)
			{
				byte code = bytes[0];

				if (code == Protocol.JOIN_ROOM)
				{
					var offset = 1;

					SerializerId = System.Text.Encoding.UTF8.GetString(bytes, offset+1, bytes[offset]);
					offset += SerializerId.Length + 1;

					if (SerializerId == "schema")
					{
						serializer = new SchemaSerializer<T>();

					} else if (SerializerId == "fossil-delta")
					{
						serializer = (ISerializer<T>) new FossilDeltaSerializer();
					}

					// TODO: use serializer defined by the back-end.
					// serializer = (Colyseus.Serializer) new FossilDeltaSerializer();

					if (bytes.Length > offset)
					{
						serializer.Handshake(bytes, offset);
					}

					OnJoin?.Invoke();
				}
				else if (code == Protocol.JOIN_ERROR)
				{
					var message = System.Text.Encoding.UTF8.GetString(bytes, 2, bytes[1]);
					OnError?.Invoke(message);

				}
				else if (code == Protocol.LEAVE_ROOM)
				{
					await Leave();

				}
				else 
				{
					previousCode = code;

				}
			} else
			{
				if (previousCode == Protocol.ROOM_STATE)
				{
					SetState(bytes);
				}
				else if (previousCode == Protocol.ROOM_STATE_PATCH)
				{
					Patch(bytes);
				}
				else if (previousCode == Protocol.ROOM_DATA)
				{
					var message = MsgPack.Deserialize<object>(new MemoryStream(bytes));
					OnMessage?.Invoke(message);

				}
				previousCode = 0;
			}
		}

		protected void Patch (byte[] delta)
		{
			serializer.Patch(delta);
			OnStateChange?.Invoke(serializer.GetState(), false);
		}
	}
}
