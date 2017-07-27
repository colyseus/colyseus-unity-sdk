using System;

#if !WINDOWS_UWP
using WebSocketSharp;
#endif

namespace Colyseus 
{
	public class Connection : WebSocket
	{
		public Connection (Uri uri) : base(uri)
		{
		}
	}
}

