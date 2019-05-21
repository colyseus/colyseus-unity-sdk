using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;

namespace Colyseus
{
	public class RoomAvailable
	{
		public string roomId { get; set; }
		public uint clients { get; set; }
		public uint maxClients { get; set; }
		public object metadata { get; set; }
	}

	public interface IRoom 
	{
		string Id { get; set; }
		Dictionary<string, object> Options { get; set; }

		void Recv();
		void SetConnection(Connection connection);

		event EventHandler OnLeave;
	}

	/// <summary>
	/// </summary>

	// public class Room<T> : IRoom
	public class Room<T> : IRoom
	{
		protected string id;
		public string Id
		{
			get { return id;  }
			set { id = value; }
		}

		public string Name;
		public string SessionId;

		protected Dictionary<string, object> options;
		public Dictionary<string, object> Options
		{
			get { return options;  }
			set { options = value; }
		}

		public Connection Connection;

		public string SerializerId;
		protected ISerializer<T> serializer;

		protected byte previousCode = 0;

		/// <summary>
		/// Occurs when <see cref="Room"/> is able to connect to the server.
		/// </summary>
		public event EventHandler OnReadyToConnect;

		/// <summary>
		/// Occurs when the <see cref="Client"/> successfully connects to the <see cref="Room"/>.
		/// </summary>
		public event EventHandler OnJoin;

		/// <summary>
		/// Occurs when some error has been triggered in the room.
		/// </summary>
		public event EventHandler<ErrorEventArgs> OnError;

		/// <summary>
		/// Occurs when <see cref="Client"/> leaves this room.
		/// </summary>
		public event EventHandler OnLeave;

		/// <summary>
		/// Occurs when server sends a message to this <see cref="Room"/>
		/// </summary>
		public event EventHandler<MessageEventArgs> OnMessage;

		/// <summary>
		/// Occurs after applying the patched state on this <see cref="Room"/>.
		/// </summary>
		//public event EventHandler<StateChangeEventArgs<T>> OnStateChange;
		public event EventHandler<StateChangeEventArgs<T>> OnStateChange;

		/// <summary>
		/// Initializes a new instance of the <see cref="Room"/> class.
		/// It synchronizes state automatically with the server and send and receive messaes.
		/// </summary>
		/// <param name="client">
		/// The <see cref="Client"/> client connection instance.
		/// </param>
		/// <param name="name">The name of the room</param>
		public Room (string name, Dictionary<string, object> options = null)
		{
			Name = name;
			Options = options;
		}

		public void Recv ()
        {
            byte[] data = Connection.Recv();
            while (data != null)
            {
                ParseMessage(data);
                data = Connection.Recv();
            }
        }

		public IEnumerator Connect()
		{
			return Connection.Connect();
		}

		public void SetConnection (Connection connection)
		{
			Connection = connection;

			Connection.OnClose += (object sender, EventArgs e) => {
				if (OnLeave != null) {
					OnLeave.Invoke (this, e);
				}
			};

			Connection.OnError += (object sender, ErrorEventArgs e) => {
				if (OnError != null) {
					OnError.Invoke(this, e);
				}
			};

			OnReadyToConnect.Invoke (this, new EventArgs());
		}

		public void SetState(byte[] encodedState)
		{
			serializer.SetState(encodedState);

			if (OnStateChange != null) {
				OnStateChange.Invoke (this, new StateChangeEventArgs<T>(serializer.GetState(), true));
			}
		}

		public T State
		{
			get { return serializer.GetState(); }
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public void Leave (bool consented = true)
		{
			if (Id != null) {
				if (consented)
				{
					Connection.Send(new object[] { Protocol.LEAVE_ROOM }); 
				}
				else
				{
					Connection.Close();
				}

			} else if (OnLeave != null) {
				OnLeave.Invoke (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Send data to this room.
		/// </summary>
		/// <param name="data">Data to be sent</param>
		public void Send (object data)
		{
			Connection.Send(new object[]{Protocol.ROOM_DATA, Id, data});
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

		protected void ParseMessage (byte[] bytes)
		{
			if (previousCode == 0)
			{
				byte code = bytes[0];

				if (code == Protocol.JOIN_ROOM)
				{
					var offset = 1;

					SessionId = System.Text.Encoding.UTF8.GetString(bytes, offset+1, bytes[offset]);
					offset += SessionId.Length + 1;

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

					if (OnJoin != null)
					{
						OnJoin.Invoke(this, new EventArgs());
					}

				}
				else if (code == Protocol.JOIN_ERROR)
				{
					var message = System.Text.Encoding.UTF8.GetString(bytes, 2, bytes[1]);
					OnError.Invoke(this, new ErrorEventArgs(message));

				}
				else if (code == Protocol.LEAVE_ROOM)
				{
					Leave();

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
					OnMessage.Invoke(this, new MessageEventArgs(message));

				}
				previousCode = 0;
			}
		}

		protected void Patch (byte[] delta)
		{
			serializer.Patch(delta);

			if (OnStateChange != null)
				OnStateChange.Invoke(this, new StateChangeEventArgs<T>(serializer.GetState()));
		}
	}
}
