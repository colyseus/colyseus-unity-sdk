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
		public string Message;

		/// <summary>
		/// </summary>
		public ErrorEventArgs (string message)
		{
			Message = message;
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
	public class StateChangeEventArgs<T> : EventArgs
	{
		/// <summary>
		/// New state of the <see cref="Room" />
		/// </summary>
		public T State { get; private set; }

		//public IndexedDictionary<string, object> State { get; private set; }

		/// <summary>	
		/// Boolean representing if the event is setting the state of the <see cref="Room" /> for the first time.	
		/// </summary>
		public bool IsFirstState;

		/// <summary>
		/// </summary>
		public StateChangeEventArgs (T state, bool isFirstState = false)
		{
			State = state;
			IsFirstState = isFirstState;
		}
	}
}

