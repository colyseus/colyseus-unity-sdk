/* 
	Copyright (c) 2019 Denis Zykov, GameDevWare.com

	This a part of "Json & MessagePack Serialization" Unity Asset - https://www.assetstore.unity3d.com/#!/content/59918

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/
using System;
using GameDevWare.Serialization.MessagePack;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class GuidSerializer : TypeSerializer
	{
		public override Type SerializedType { get { return typeof(Guid); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var guidStr = reader.ReadString(false);
			var value = new Guid(guidStr);
			return value;
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var messagePackWriter = writer as MsgPackWriter;
			if (messagePackWriter != null)
			{
				// try to write it as Message Pack extension type
				var extensionType = default(sbyte);
				var buffer = messagePackWriter.GetWriteBuffer();
				if (messagePackWriter.Context.ExtensionTypeHandler.TryWrite(value, out extensionType, ref buffer))
				{
					messagePackWriter.Write(extensionType, buffer);
					return;
				}
				// if not, continue default serialization
			}

			var guid = (Guid)value;
			var guidStr = guid.ToString();
			writer.Write(guidStr);
		}
	}
}
