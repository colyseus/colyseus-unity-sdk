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

		protected Queue<byte[]> _enqueuedCalls = new Queue<byte[]>();
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

		public async Task Send(byte[] data)
		{
			if (!IsOpen) {
				_enqueuedCalls.Enqueue(data);

			} else {

				await base.Send(data);
			}
		}

#if !UNITY_WEBGL
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

		protected async void _OnOpen ()
		{
			IsOpen = true;

			// send enqueued commands while connection wasn't open
			if (_enqueuedCalls.Count > 0) {
				do {
					await Send(_enqueuedCalls.Dequeue());
				} while (_enqueuedCalls.Count > 0);
			}

#if !UNITY_WEBGL
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

