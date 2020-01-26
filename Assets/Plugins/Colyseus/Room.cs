using System;
using System.IO;
using System.Threading.Tasks;
using GameDevWare.Serialization;

namespace Colyseus
{
	public delegate void ColyseusOpenEventHandler();
	public delegate void ColyseusCloseEventHandler(NativeWebSocket.WebSocketCloseCode code);
	public delegate void ColyseusErrorEventHandler(string message);

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

		public void SetState(byte[] encodedState, int offset)
		{
			serializer.SetState(encodedState, offset);
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

				if (bytes.Length > offset)
				{
					serializer.Handshake(bytes, offset);
				}

				OnJoin?.Invoke();

				// Acknowledge JOIN_ROOM
				await Connection.Send(new object[] { Protocol.JOIN_ROOM });
			}
			else if (code == Protocol.JOIN_ERROR)
			{
				var message = System.Text.Encoding.UTF8.GetString(bytes, 2, bytes[1]);
				OnError?.Invoke(message);

			}
			else if (code == Protocol.ROOM_DATA_SCHEMA)
			{
				Type messageType = Schema.Context.GetInstance().Get(bytes[1]);

				var message = (Schema.Schema) Activator.CreateInstance(messageType);
				message.Decode(bytes, new Schema.Iterator { Offset = 2 });

				OnMessage?.Invoke(message);
			}
			else if (code == Protocol.LEAVE_ROOM)
			{
				await Leave();

			}
			else if (code == Protocol.ROOM_STATE)
			{
				SetState(bytes, 1);
			}
			else if (code == Protocol.ROOM_STATE_PATCH)
			{
				Patch(bytes, 1);
			}
			else if (code == Protocol.ROOM_DATA)
			{
				// TODO: de-serialize message with an offset, to avoid creating a new buffer
				var message = MsgPack.Deserialize<object>(new MemoryStream(
					ArrayUtils.SubArray(bytes, 1, bytes.Length-1)
				));
				OnMessage?.Invoke(message);
			}
		}

		protected void Patch (byte[] delta, int offset)
		{
			serializer.Patch(delta, offset);
			OnStateChange?.Invoke(serializer.GetState(), false);
		}
	}
}
