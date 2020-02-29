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

		public Connection(string url, Dictionary<string, string> headers) : base(url, headers)
		{
			Initialize();
		}

		private void Initialize()
		{
			OnOpen += _OnOpen;
			OnClose += _OnClose;
		}

		public async Task Send(object[] data)
		{
			var serializationOutput = new MemoryStream();
			MsgPack.Serialize(data, serializationOutput, SerializationOptions.SuppressTypeInformation);

			byte[] packedData = serializationOutput.ToArray ();

			if (!this.IsOpen) {
				_enqueuedCalls.Enqueue(packedData);

			} else {

				await Send(packedData);
			}
		}

		protected async void _OnOpen ()
		{
			IsOpen = true;

			// send enqueued commands while connection wasn't open
			if (_enqueuedCalls.Count > 0) {
				do {
					await Send(_enqueuedCalls.Dequeue());
				} while (_enqueuedCalls.Count > 0);
			}
		}

		protected void _OnClose (WebSocketCloseCode code)
		{
			IsOpen = false;
		}
	}
}

