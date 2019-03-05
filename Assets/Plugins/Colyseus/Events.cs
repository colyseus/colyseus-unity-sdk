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
	public class DataEventArgs : EventArgs
	{
		/// <summary>
		/// Message coming from the server.
		/// </summary>
		public object Data;

		/// <summary>
		/// </summary>
		public DataEventArgs (object _data)
		{
			Data = _data;
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

		/// <summary>
		/// </summary>
		public StateChangeEventArgs (T state)
		{
			this.State = state;
		}
	}
}

