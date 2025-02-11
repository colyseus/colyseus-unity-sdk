using System;

namespace NativeWebSocket
{
	public class WebSocketUnexpectedException : WebSocketException
	{
		public WebSocketUnexpectedException() { }
		public WebSocketUnexpectedException(string message) : base(message) { }
		public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
	}
}