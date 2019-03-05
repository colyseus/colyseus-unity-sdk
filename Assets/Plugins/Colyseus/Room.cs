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

	/// <summary>
	/// </summary>
	public class Room<T>
	{
		public string id;
		public string name;
		public string sessionId;

		public Dictionary<string, object> options;

		public Connection connection;

		public string serializerId;
		protected Serializer<T> serializer;

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
		public event EventHandler<DataEventArgs> OnMessage;

		/// <summary>
		/// Occurs after applying the patched state on this <see cref="Room"/>.
		/// </summary>
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
			this.name = name;
			this.options = options;
		}

		public void Recv ()
		{
			byte[] data = connection.Recv();
			if (data != null)
			{
				ParseMessage(data);
			}
		}

		public IEnumerator Connect ()
		{
			return connection.Connect ();
		}

		public void SetConnection (Connection connection)
		{
			this.connection = connection;

			this.connection.OnClose += (object sender, EventArgs e) => {
				if (OnLeave != null) {
					OnLeave.Invoke (this, e);
				}
			};

			this.connection.OnError += (object sender, ErrorEventArgs e) => {
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
				OnStateChange.Invoke (this, new StateChangeEventArgs<T>(serializer.GetState()));
			}
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public void Leave (bool consented = true)
		{
			if (id != null) {
				if (consented)
				{
					connection.Send(new object[] { Protocol.LEAVE_ROOM }); 
				}
				else
				{
					connection.Close();
				}

			} else {
				OnLeave.Invoke (this, new EventArgs ());
			}
		}

		/// <summary>
		/// Send data to this room.
		/// </summary>
		/// <param name="data">Data to be sent</param>
		public void Send (object data)
		{
			connection.Send(new object[]{Protocol.ROOM_DATA, id, data});
		}

		protected void ParseMessage (byte[] bytes)
		{
			if (previousCode == 0)
			{
				byte code = bytes[0];

				if (code == Protocol.JOIN_ROOM)
				{
					var offset = 1;

					sessionId = System.Text.Encoding.UTF8.GetString(bytes, offset, bytes.Length);
					offset += sessionId.Length;

					serializerId = System.Text.Encoding.UTF8.GetString(bytes, offset, bytes.Length);
					offset += serializerId.Length;

					serializer = (Colyseus.Serializer<T>) new FossilDeltaSerializer<T>();

					if (OnJoin != null)
					{
						OnJoin.Invoke(this, new EventArgs());
					}

				}
				else if (code == Protocol.JOIN_ERROR)
				{
					var message = System.Text.Encoding.UTF8.GetString(bytes, 1, bytes.Length);
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
					var message = MsgPack.Deserialize<List<object>>(new MemoryStream(bytes));
					OnMessage.Invoke(this, new DataEventArgs(message));

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
