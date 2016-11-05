using System;

using MsgPack;

namespace Colyseus
{

	/// <summary>
	/// Representation of a message received from the server.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Target <see cref="Room"/> affected by this message. May be null.
		/// </summary>
		public Room room = null;

		/// <summary>
		/// Data coming from the server.
		/// </summary>
		public object data = null;

		/// <summary>
		/// </summary>
		public MessageEventArgs (Room room, object data = null)
		{
			this.room = room;
			this.data = data;
		}
	}

	/// <summary>
	/// Room Update Message
	/// </summary>
	public class RoomUpdateEventArgs : EventArgs
	{
		/// <summary>
		/// Affected <see cref="Room" /> instance.
		/// </summary>
		public Room room = null;

		/// <summary>
		/// New state of the <see cref="Room" />
		/// </summary>
		public MessagePackObject state;

		/// <summary>
		/// Patches applied to the <see cref="Room" /> state.
		/// </summary>
		public string patches = null;

		/// <summary>
		/// </summary>
		public RoomUpdateEventArgs (Room room, MessagePackObject state, string patches = null)
		{
			this.room = room;
			this.state = state;
			this.patches = patches;
		}
	}
}

