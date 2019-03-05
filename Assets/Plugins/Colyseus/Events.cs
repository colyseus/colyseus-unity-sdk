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
		public string Message = null;

		/// <summary>
		/// </summary>
		public ErrorEventArgs (string message)
		{
			this.Message = message;
		}
	}

	/// <summary>
	/// Representation of a message received from the server.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Message coming from the server.
		/// </summary>
		public object Message;

		/// <summary>
		/// </summary>
		public MessageEventArgs (object _message)
		{
			Message = _message;
		}
	}

	/// <summary>
	/// Room Update Message
	/// </summary>
	//public class StateChangeEventArgs<T> : EventArgs
	public class StateChangeEventArgs : EventArgs
	{
		/// <summary>
		/// New state of the <see cref="Room" />
		/// </summary>
		//public T State { get; private set; }

		public IndexedDictionary<string, object> State { get; private set; }


		/// <summary>
		/// </summary>
		public StateChangeEventArgs (IndexedDictionary<string, object> state)
		{
			this.State = state;
		}
	}
}

