using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

using UnityEngine;

namespace Colyseus
{
	/// <summary>
	/// </summary>
	public class Room : DeltaContainer
	{
		public string id;
		public string name;
		public string sessionId;

		protected Connection connection;
		protected byte[] _previousState = null;

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
		public event EventHandler OnError;

		/// <summary>
		/// Occurs when <see cref="Client"/> leaves this room.
		/// </summary>
		public event EventHandler OnLeave;

		/// <summary>
		/// Occurs when server sends a message to this <see cref="Room"/>
		/// </summary>
		public event EventHandler<MessageEventArgs> OnData;

		/// <summary>
		/// Occurs after applying the patched state on this <see cref="Room"/>.
		/// </summary>
		public event EventHandler<RoomUpdateEventArgs> OnUpdate;

		/// <summary>
		/// Initializes a new instance of the <see cref="Room"/> class.
		/// It synchronizes state automatically with the server and send and receive messaes.
		/// </summary>
		/// <param name="client">
		/// The <see cref="Client"/> client connection instance.
		/// </param>
		/// <param name="name">The name of the room</param>
		public Room (String name)
			: base(new IndexedDictionary<string, object>())
		{
			this.name = name;
		}

		public void Recv ()
		{
			byte[] data = this.connection.Recv();
			if (data != null)
			{
				this.ParseMessage(data);
			}
		}

		public IEnumerator Connect ()
		{
			return this.connection.Connect ();
		}

		public void SetConnection (Connection connection)
		{
			this.connection = connection;
			this.OnReadyToConnect.Invoke (this, new EventArgs());
		}

		public void SetState( IndexedDictionary<string, object> state, uint remoteCurrentTime, uint remoteElapsedTime)
		{
			this.Set(state);

			// Deserialize
			var serializationOutput = new MemoryStream();
			MsgPack.Serialize (state, serializationOutput);

			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs(state, true));

			this._previousState = serializationOutput.ToArray();
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public void Leave ()
		{
			if (this.id != null) {
				this.connection.Close ();
			}
		}

		/// <summary>
		/// Send data to this room.
		/// </summary>
		/// <param name="data">Data to be sent</param>
		public void Send (object data)
		{
			this.connection.Send(new object[]{Protocol.ROOM_DATA, this.id, data});
		}

		protected void ParseMessage (byte[] recv)
		{
			var message = MsgPack.Deserialize<List<object>> (new MemoryStream(recv));
			var code = (byte) message [0];

			if (code == Protocol.JOIN_ROOM) {
				this.sessionId = (string) message [1];

				if (this.OnJoin != null)
					this.OnJoin.Invoke (this, new EventArgs());

			} else if (code == Protocol.JOIN_ERROR) {
				if (this.OnError != null)
					this.OnError.Invoke (this, new ErrorEventArgs((string) message [2]));

			} else if (code == Protocol.LEAVE_ROOM) {
				this.Leave ();

			} else if (code == Protocol.ROOM_STATE) {
				var state = (IndexedDictionary<string, object>) message [2];

				// TODO:
				// https://github.com/deniszykov/msgpack-unity3d/issues/8

				// var remoteCurrentTime = (double) message [3];
				// var remoteElapsedTime = (int) message [4];

				// this.SetState (state, remoteCurrentTime, remoteElapsedTime);

				this.SetState (state, 0, 0);

			} else if (code == Protocol.ROOM_STATE_PATCH) {

				var data = (List<object>) message [2];
				byte[] patches = new byte[data.Count];

				uint i = 0;
				foreach (var b in data) {
					patches [i] = Convert.ToByte(b);
					i++;
				}

				this.Patch (patches);

			} else if (code == Protocol.ROOM_DATA) {
				if (this.OnData != null)
					this.OnData.Invoke(this, new MessageEventArgs(message[2]));
			}
		}

		protected void Patch (byte[] delta)
		{
			this._previousState = Fossil.Delta.Apply (this._previousState, delta);

			var newState = MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(this._previousState));

			this.Set(newState);

			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs(this.data));
		}
	}
}
