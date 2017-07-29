using System;
using System.Collections;
using System.Collections.Generic;

using MsgPack;
using MsgPack.Serialization;

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
			: base(new MessagePackObject(new MessagePackObjectDictionary()))
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

		public void SetState( MessagePackObject state, int remoteCurrentTime, int remoteElapsedTime)
		{
			this.Set(state);

			// Creates serializer.
			var serializer = MessagePackSerializer.Get <MessagePackObject>();

			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs(state, true));

			this._previousState = serializer.PackSingleObject (state);
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public void Leave ()
		{
			if (this.id != null) {
				this.Send (new object[]{ Protocol.LEAVE_ROOM, this.id });
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
			UnpackingResult<MessagePackObject> raw = Unpacking.UnpackObject (recv);

			var message = raw.Value.AsList ();
			var code = message [0].AsInt32 ();

			if (code == Protocol.JOIN_ROOM) {
				this.sessionId = message [1].AsString ();

				if (this.OnJoin != null)
					this.OnJoin.Invoke (this, new EventArgs());

			} else if (code == Protocol.JOIN_ERROR) {
				if (this.OnError != null)
					this.OnError.Invoke (this, new ErrorEventArgs(message [2].AsString ()));

			} else if (code == Protocol.LEAVE_ROOM) {
				this.Leave ();

			} else if (code == Protocol.ROOM_STATE) {
				var state = message [2];
				var remoteCurrentTime = message [3].AsInt32();
				var remoteElapsedTime = message [4].AsInt32();

				this.SetState (state, remoteCurrentTime, remoteElapsedTime);

			} else if (code == Protocol.ROOM_STATE_PATCH) {
				IList<MessagePackObject> patchBytes = message [2].AsList();
				byte[] patches = new byte[patchBytes.Count];

				int idx = 0;
				foreach (MessagePackObject obj in patchBytes)
				{
					patches[idx] = obj.AsByte();
					idx++;
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

			var serializer = MessagePackSerializer.Get <MessagePackObject>();
			var newState = serializer.UnpackSingleObject (this._previousState);

			this.Set(newState);

			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs(this.data));
		}
	}
}
