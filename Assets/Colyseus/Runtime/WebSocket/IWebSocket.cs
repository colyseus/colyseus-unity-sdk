namespace NativeWebSocket
{
	public interface IWebSocket
	{
		public event WebSocketOpenEventHandler OnOpen;
		public event WebSocketMessageEventHandler OnMessage;
		public event WebSocketErrorEventHandler OnError;
		public event WebSocketCloseEventHandler OnClose;

		public WebSocketState State { get; }
	}
}