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
using System.Globalization;
using GameDevWare.Serialization.MessagePack;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class MsgPackExtensionTypeSerializer : TypeSerializer
	{
		private const string DATA_MEMBER_NAME = "$data";
		private const string TYPE_MEMBER_NAME = "$type";

		/// <inheritdoc />
		public override Type SerializedType { get { return typeof(MessagePackExtensionType); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader.Token == JsonToken.Null)
			{
				return null;
			}
			else if (reader.RawValue is MessagePackExtensionType)
			{
				return (MessagePackExtensionType)reader.RawValue;
			}
			else if (reader.RawValue is byte[])
			{
				return new MessagePackExtensionType((byte[])reader.RawValue);
			}
			else if (reader.RawValue is string)
			{
				return new MessagePackExtensionType(Convert.FromBase64String(reader.Value.AsString));
			}

			// { "$binary" : "<bindata>", "$type" : "<t>" }
			// http://www.mongodb.org/display/DOCS/Mongo+Extended+JSON

			reader.ReadObjectBegin();
			var base64Binary = default(string);
			var subtypeHex = default(string);
			while (reader.Token != JsonToken.EndOfObject)
			{
				var member = reader.ReadMember();
				switch (member)
				{
					case DATA_MEMBER_NAME:
						base64Binary = reader.ReadString();
						break;
					case TYPE_MEMBER_NAME:
						subtypeHex = reader.ReadString();
						break;
					default:
						reader.ReadValue(typeof(object)); // skip value
						break;
				}
			}
			reader.ReadObjectEnd(nextToken: false);

			if (subtypeHex == null) subtypeHex = "0";
			if (base64Binary == null) base64Binary = "";

			var binaryType = unchecked((sbyte)byte.Parse(subtypeHex, NumberStyles.HexNumber));
			var binary = Convert.FromBase64String(base64Binary);

			var value = new MessagePackExtensionType(binaryType, binary);
			return value;
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			var extensionType = (MessagePackExtensionType)value;

			var msgPackWriter = writer as MsgPackWriter;
			if (msgPackWriter != null)
			{
				msgPackWriter.Write(extensionType.Type, extensionType.ToArraySegment());
				return;
			}

			writer.WriteObjectBegin(2);
			writer.WriteMember(DATA_MEMBER_NAME);
			writer.WriteString(extensionType.ToBase64());
			writer.WriteMember(TYPE_MEMBER_NAME);
			writer.Write(unchecked((byte)extensionType.Type).ToString("X2"));
			writer.WriteObjectEnd();
		}
	}
}
