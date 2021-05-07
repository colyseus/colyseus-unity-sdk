using System;

namespace Colyseus
{
	/// <summary>
	/// Custom exception thrown when there is an issue with Match Making
	/// </summary>
	public class CSAMatchMakeException : Exception
	{
		/// <summary>
		/// The error code the server returned
		/// </summary>
		public int Code;
		public CSAMatchMakeException(int code, string message) : base(message)
		{
			Code = code;
		}
	}
}
