/* 
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

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
	public sealed class BinarySerializer : TypeSerializer
	{
		public static readonly BinarySerializer Instance = new BinarySerializer();

		public override Type SerializedType { get { return typeof(byte[]); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
				return null;

			if (reader.RawValue is byte[])
			{
				return reader.RawValue;
			}
			else
			{
				var value = reader.RawValue as string;
				if (value == null)
					return null;

				var buffer = Convert.FromBase64String(value);
				return buffer;
			}
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			if (value != null && value is byte[] == false) throw JsonSerializationException.TypeIsNotValid(this.GetType(), "be array of bytes");

			var bytes = (byte[])value;
			if (writer is MsgPackWriter)
			{
				((MsgPackWriter)writer).Write(bytes);
			}
			else
			{
				var base64String = Convert.ToBase64String(bytes);
				writer.WriteString(base64String);
			}
		}

		public override string ToString()
		{
			return "byte[] as Base64";
		}
	}
}
