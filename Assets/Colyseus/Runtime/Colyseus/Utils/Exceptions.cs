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
		/// The error code the server returned.
		/// <para>
		/// For <c>client.Http</c> requests this is the HTTP status. For matchmaking
		/// (<c>JoinOrCreate</c>, <c>Create</c>, <c>Join</c>, <c>JoinById</c>, <c>Reconnect</c>)
		/// it is the server's <see cref="ErrorCode"/>, or the custom code of a <c>ServerError</c>
		/// thrown by the room, even when the server responds with a standard HTTP status
		/// (<c>matchMaker.controller.standardHttpStatus</c>).
		/// </para>
		/// </summary>
		public int StatusCode;

		/// <summary>
		/// The raw response body, or <c>null</c> when the HTTP client didn't capture it.
		/// </summary>
		public string Body;

		public HttpException(int statusCode, string message) : base(message)
		{
			StatusCode = statusCode;
		}

		public HttpException(int statusCode, string message, string body) : this(statusCode, message)
		{
			Body = body;
		}
	}

}
