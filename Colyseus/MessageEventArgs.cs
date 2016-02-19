using System;

using JsonDiffPatch;

namespace Colyseus
{
	// Class aliases
	using PatchDocument = JsonDiffPatch.PatchDocument;
	using JToken = Newtonsoft.Json.Linq.JToken;

	public class MessageEventArgs : EventArgs
	{
		public Room room = null;
		public object data = null;

		public MessageEventArgs (Room room, object data = null)
		{
			this.room = room;
			this.data = data;
		}
	}

	public class RoomUpdateEventArgs : EventArgs
	{
		
		public Room room = null;
		public JToken state = null;
		public PatchDocument patches;

		public RoomUpdateEventArgs (Room room, JToken state, PatchDocument patches = null)
		{
			this.room = room;
			this.state = state;
			this.patches = patches;
		}
	}
}

