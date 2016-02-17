using System;

using WebSocketSharp;
using Newtonsoft.Json;
using JsonDiffPatch;

namespace Colyseus
{
	public class Room
	{
		protected Client client;
		public String name;

		private int _id = 0;
		private Newtonsoft.Json.Linq.JToken _state = null;

		public event EventHandler OnJoin;
		public event EventHandler OnError;
		public event EventHandler OnLeave;
		public event EventHandler OnPatch;
		public event EventHandler OnData;
		public event EventHandler<RoomUpdateEventArgs> OnUpdate;

		public Room (Client client, String name)
		{
			this.client = client;
			this.name = name;
		}

		public int id
		{
			get { return this._id; }
			set {
				this._id = value;
				this.OnJoin.Emit (this, EventArgs.Empty);
			}
		}

		public Newtonsoft.Json.Linq.JToken state
		{
			get { return this._state; }
			set {
				this._state = value;
				this.OnUpdate.Emit(this, new RoomUpdateEventArgs(this, value, null));
			}
		}

		public void ReceiveData (object data)
		{
			this.OnData.Emit (this, new MessageEventArgs (this, data));
		}


		public void Leave (bool requestLeave = true) 
		{
			if (requestLeave && this._id > 0) {
				this.Send (new object[]{ Protocol.LEAVE_ROOM, this._id });

			} else {
				this.OnLeave.Emit (this, EventArgs.Empty);
			}
		}

		public void ApplyPatches (PatchDocument patches)
		{
			this.OnPatch.Emit (this, new MessageEventArgs(this, patches));

			var patcher = new JsonPatcher();
			patcher.Patch(ref this._state, patches);

			this.OnUpdate.Emit (this, new RoomUpdateEventArgs(this, this._state, patches));
			
		}

		public void Send (object data) 
		{
			this.client.Send(new object[]{Protocol.ROOM_DATA, this._id, data});
		}
	}
}

