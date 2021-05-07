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
using System.IO;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class StreamSerializer : TypeSerializer
	{
		public static readonly StreamSerializer Instance = new StreamSerializer();

		public override Type SerializedType { get { return typeof(Stream); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
				return null;

			if (reader.RawValue is Stream)
				return reader.RawValue;
			else if(reader.RawValue is byte[])
				return new MemoryStream((byte[])reader.RawValue);
			else
			{
				var base64Str = Convert.ToString(reader.RawValue, reader.Context.Format);
				var bytes = Convert.FromBase64String(base64Str);
				return new MemoryStream(bytes);
			}
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var stream = value as Stream;
			if (value != null && stream == null) throw JsonSerializationException.TypeIsNotValid(this.GetType(), "be a Stream");
			if (!stream.CanRead) throw new JsonSerializationException("Stream couldn't be readed.", JsonSerializationException.ErrorCode.StreamIsNotReadable);

			if (stream.CanSeek)
			{
				var position = stream.Position;
				var buffer = new byte[stream.Length - stream.Position];
				stream.Read(buffer, 0, buffer.Length);
				BinarySerializer.Instance.Serialize(writer, buffer);
				stream.Position = position;
			}
			else
			{
				var tmpStream = new MemoryStream();
				var buffer = new byte[ushort.MaxValue];
				var readed = 0;

				while ((readed = stream.Read(buffer, 0, buffer.Length)) > 0)
					tmpStream.Write(buffer, 0, readed);

				BinarySerializer.Instance.Serialize(writer, tmpStream.ToArray());
			}
		}

		public override string ToString()
		{
			return "stream";
		}
	}
}
