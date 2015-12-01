using System;
using WebSocketSharp;

namespace Colyseus
{
	public class Client
	{
		WebSocket ws;

		public Client (string url)
		{
			this.ws = new WebSocket (url);
			this.ws.ConnectAsync ();
		}

		public Room join (string roomName)
		{
			this.ws.SendAsync ();
		}

		public void Close ()
		{
			this.ws.CloseAsync ();
		}

	}
}