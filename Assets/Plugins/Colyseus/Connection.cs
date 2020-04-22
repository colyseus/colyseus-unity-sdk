using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using NativeWebSocket;
using GameDevWare.Serialization;

namespace Colyseus
{

	public class Connection : WebSocket
	{
		public bool IsOpen = false;
		protected bool ProcessingMessageQueue = false;

		public Connection(string url, Dictionary<string, string> headers) : base(url, headers)
		{
			Initialize();
		}

		private void Initialize()
		{
			OnOpen += _OnOpen;
			OnClose += _OnClose;
		}

#if UNITY_WEBGL && !UNITY_EDITOR
#else
		public async void ProcessMessageQueue()
		{
			ProcessingMessageQueue = true;
			while (ProcessingMessageQueue)
			{
				DispatchMessageQueue();

				// probably should be waiting until a new frame started or so
				await Task.Delay(TimeSpan.FromSeconds(1.0f / 120.0f));
			}
		}
#endif

		protected void _OnOpen ()
		{
			IsOpen = true;

#if UNITY_WEBGL && !UNITY_EDITOR
#else
			ProcessMessageQueue();
#endif
		}

		protected void _OnClose (WebSocketCloseCode code)
		{
			ProcessingMessageQueue = false;
			IsOpen = false;
		}
	}
}

