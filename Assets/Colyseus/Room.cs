using System;

using WebSocketSharp;

using MsgPack;

namespace Colyseus
{
	/// <summary>
	/// </summary>
	public class Room
	{
		private Client client;

		/// <summary>
		/// Name of the <see cref="Room"/>.
		/// </summary>
		public String name;
		public MessagePackObject state;

		private int _id = 0;
		private byte[] _previousState = null;

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
		/// Occurs when server send patched state, before <see cref="OnUpdate"/>.
		/// </summary>
		public event EventHandler<MessageEventArgs> OnPatch;

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
		public Room (Client client, String name)
		{
			this.client = client;
			this.name = name;
//			this.patcher = new Patcher ();
		}

		/// <summary>
		/// Contains the id of this room, used internally for communication.
		/// </summary>
		public int id
		{
			get { return this._id; }
			set {
				this._id = value;
				this.OnJoin.Emit (this, EventArgs.Empty);
			}
		}


		public void SetState( MessagePackObject state, int remoteCurrentTime, int remoteElapsedTime)
		{
			this.state = state;

			// TODO: 
			// Create a "clock" for remoteCurrentTime / remoteElapsedTime to match the JavaScript API.

			// Creates serializer.
			var serializer = MsgPack.Serialization.MessagePackSerializer.Get <byte[]>();
			this._previousState = serializer.PackSingleObject (state.ToByteArray (ByteOrder.Big));

			this.OnUpdate.Emit (this, new RoomUpdateEventArgs (this, state, null));
		}

		/// <summary>
		/// Leave the room. 
		/// </summary>
		public void Leave (bool requestLeave = true) 
		{
			if (requestLeave && this._id > 0) {
				this.Send (new object[]{ Protocol.LEAVE_ROOM, this._id });

			} else {
				this.OnLeave.Emit (this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Send data to this room.
		/// </summary>
		/// <param name="data">Data to be sent</param>
		public void Send (object data) 
		{
			this.client.Send(new object[]{Protocol.ROOM_DATA, this._id, data});
		}

		/// <summary>Internal usage, shouldn't be called.</summary>
		public void ReceiveData (object data)
		{
			this.OnData.Emit (this, new MessageEventArgs (this, data));
		}

		/// <summary>Internal usage, shouldn't be called.</summary>
		public void ApplyPatch (byte[] delta)
		{
			this._previousState = Fossil.Delta.Apply (this._previousState, delta);

			var serializer = MsgPack.Serialization.MessagePackSerializer.Get <MessagePackObject>();
			var newState = serializer.UnpackSingleObject (this._previousState);

			this.state = newState;

//			this.OnPatch.Emit (this, new MessageEventArgs(this, patches));

//			this.patcher.Patch (ref this._state, patches);
			
			this.OnUpdate.Emit (this, new RoomUpdateEventArgs(this, this.state, null));
		}

//		public void ApplyPatches (JArray patches)
//		{
//			this.OnPatch.Emit (this, new MessageEventArgs(this, patches));
//
//			this.patcher.Patch (ref this._state, patches);
//
//			//			var patcher = new JsonPatcher();
//			//			patcher.Patch(ref this._state, patches);
//
//			this.OnUpdate.Emit (this, new RoomUpdateEventArgs(this, (JToken) this._state, patches));
//		}

		/// <summary>Internal usage, shouldn't be called.</summary>
		public void EmitError (MessageEventArgs args)
		{
			this.OnError.Emit (this, args);
		}
	}
}

