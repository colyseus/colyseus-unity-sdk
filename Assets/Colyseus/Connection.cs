using System;
using WebSocketSharp;

using MsgPack;
using MsgPack.Serialization;

#if !WINDOWS_UWP
using WebSocketSharp;
#endif

namespace Colyseus 
{

	public class Connection : WebSocket
	{
		protected MessagePackSerializer<object[]> serializer;

		public Connection (Uri uri) : base(uri)
		{
			// Prepare MessagePack Serializers
			MessagePackSerializer.PrepareType<MessagePackObject>();
			MessagePackSerializer.PrepareType<object[]>();
			MessagePackSerializer.PrepareType<byte[]>();

			this.serializer = MessagePackSerializer.Get<object[]>();
		}

		public void Send(object[] data)
		{
			base.Send(this.serializer.PackSingleObject(data));
		}
	}
}

