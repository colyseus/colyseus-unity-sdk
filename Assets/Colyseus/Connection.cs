using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

#if !WINDOWS_UWP
using WebSocketSharp;
#endif

namespace Colyseus
{

	public class Connection : WebSocket
	{
		public Uri uri;
		public bool IsOpen = false;

		protected Queue<byte[]> _enqueuedCalls = new Queue<byte[]>();

		public Connection (Uri uri) : base(uri)
		{
			this.uri = uri;

			this.OnOpen += _OnOpen;
			this.OnClose += _OnClose;
		}

		public void Send(object[] data)
		{
			var serializationOutput = new MemoryStream ();
			MsgPack.Serialize (data, serializationOutput);

			var packedData = serializationOutput.ToArray ();

			if (!this.IsOpen) {
				this._enqueuedCalls.Enqueue(packedData);

			} else {

				base.Send(packedData);
			}
		}

		protected void _OnOpen (object sender, EventArgs e)
		{
			this.IsOpen = true;

			// send enqueued commands while connection wasn't open
			if (this._enqueuedCalls.Count > 0) {
				do {
					this.Send(this._enqueuedCalls.Dequeue());
				} while (this._enqueuedCalls.Count > 0);
			}
		}

		protected void _OnClose (object sender, EventArgs e)
		{
			this.IsOpen = false;
		}
	}
}

