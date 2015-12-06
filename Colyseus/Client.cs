using System;
using WebSocketSharp;
using MsgPack;

namespace Colyseus
{
	public class Client
	{
		WebSocket ws;

		public Client (string url)
		{
			this.ws = new WebSocket (url);
			this.ws.ConnectAsync ();

			this.ws.OnOpen += onOpen;
			this.ws.OnMessage += onMessage;
		}

		void onOpen (object sender, EventArgs e)
		{
		}

		void onMessage (object sender, MessageEventArgs e)
		{
			MessagePackObject message = Unpacking.UnpackObject(e.RawData)
		}

		public void send (string data)
		{
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