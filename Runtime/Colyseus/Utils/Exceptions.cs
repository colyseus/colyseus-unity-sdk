using System;

namespace Colyseus
{
	/// <summary>
	/// Custom exception thrown when there is an issue with Match Making
	/// </summary>
	public class MatchMakeException : Exception
	{
		/// <summary>
		/// The error code the server returned
		/// </summary>
		public int Code;
		public MatchMakeException(int code, string message) : base(message)
		{
			Code = code;
		}
	}

	public class HttpException : Exception
	{
		/// <summary>
		/// The error code the server returned
		/// </summary>
		public int StatusCode;

		public HttpException(int statusCode, string message) : base(message)
		{
			StatusCode = statusCode;
		}
	}

}
