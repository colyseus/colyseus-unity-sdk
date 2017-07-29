using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using MsgPack;
using MsgPack.Serialization;

#if !WINDOWS_UWP
using WebSocketSharp;
#endif

namespace Colyseus 
{

	public class Connection : WebSocket
	{
		public Uri uri;
		public bool IsOpen = false;

		protected MessagePackSerializer<object[]> serializer;
		protected Queue<byte[]> _enqueuedCalls = new Queue<byte[]>();

		public Connection (Uri uri) : base(uri)
		{
			this.uri = uri;

			Debug.Log ("CONSTRUCTING CONNECTION!");
			Debug.Log (uri.ToString());

			// Prepare MessagePack Serializers
			MessagePackSerializer.PrepareType<MessagePackObject>();
			MessagePackSerializer.PrepareType<object[]>();
			MessagePackSerializer.PrepareType<byte[]>();

			this.serializer = MessagePackSerializer.Get<object[]>();

			this.OnOpen += _OnOpen;
			this.OnClose += _OnClose;
		}

		public void Send(object[] data)
		{
			Debug.Log ("Sending data: IsOpen? " + ((IsOpen) ? "YES" : "NO"));
			Debug.Log (data);
			var packedData = this.serializer.PackSingleObject(data);

			if (!this.IsOpen) {
				this._enqueuedCalls.Enqueue(packedData);

			} else {
				
				base.Send(packedData);
			}
		}

		protected void _OnOpen (object sender, EventArgs e)
		{
			Debug.Log ("Connection opened!");

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

