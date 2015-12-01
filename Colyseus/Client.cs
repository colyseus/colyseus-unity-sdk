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
		}
	}
}