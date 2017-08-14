using System;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

namespace Colyseus
{
	/// <summary>
	/// Representation of a message received from the server.
	/// </summary>
	public class ErrorEventArgs : EventArgs
	{
		/// <summary>
		/// The error message
		/// </summary>
		public string message = null;

		/// <summary>
		/// </summary>
		public ErrorEventArgs (string message)
		{
			this.message = message;
		}
	}

	/// <summary>
	/// Representation of a message received from the server.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Data coming from the server.
		/// </summary>
		public object data = null;

		/// <summary>
		/// </summary>
		public MessageEventArgs (object data)
		{
			this.data = data;
		}
	}

	/// <summary>
	/// Room Update Message
	/// </summary>
	public class RoomUpdateEventArgs : EventArgs
	{
		/// <summary>
		/// New state of the <see cref="Room" />
		/// </summary>
		public IndexedDictionary<string, object> state;

		/// <summary>
		/// Boolean representing if the event is setting the state of the <see cref="Room" /> for the first time.
		/// </summary>
		public bool isFirstState;

		/// <summary>
		/// </summary>
		public RoomUpdateEventArgs (IndexedDictionary<string, object> state, bool isFirstState = false)
		{
			this.state = state;
			this.isFirstState = isFirstState;
		}
	}
}

