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
using System.Collections;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class DictionaryEntrySerializer : TypeSerializer
	{
		public const string KEY_MEMBER_NAME = "Key";
		public const string VALUE_MEMBER_NAME = "Value";

		public override Type SerializedType { get { return typeof(DictionaryEntry); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.BeginArray)
			{
				var entry = new DictionaryEntry();
				reader.ReadArrayBegin();
				entry.Key = reader.ReadValue(typeof(object));
				entry.Value = reader.ReadValue(typeof(object));
				reader.ReadArrayEnd(nextToken: false);
				return entry;
			}
			else if (reader.Token == JsonToken.BeginObject)
			{
				var entry = new DictionaryEntry();
				reader.ReadObjectBegin();
				while (reader.Token != JsonToken.EndOfObject)
				{
					var memberName = reader.ReadMember();
					switch (memberName)
					{
						case KEY_MEMBER_NAME:
							entry.Key = reader.ReadValue(typeof(object));
							break;
						case VALUE_MEMBER_NAME:
							entry.Value = reader.ReadValue(typeof(object));
							break;
						case ObjectSerializer.TYPE_MEMBER_NAME:
							reader.ReadValue(typeof(object));
							break;
						default:
							throw new SerializationException(string.Format("Unknown member found '{0}' while '{1}' or '{2}' are expected.", memberName, KEY_MEMBER_NAME, VALUE_MEMBER_NAME));
					}
				}
				reader.ReadObjectEnd(nextToken: false);
				return entry;
			}
			else
			{
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.BeginObject, JsonToken.BeginArray);
			}
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var entry = (DictionaryEntry)value;
			writer.WriteObjectBegin(2);
			writer.WriteMember(KEY_MEMBER_NAME);
			writer.WriteValue(entry.Key, typeof(object));
			writer.WriteMember(VALUE_MEMBER_NAME);
			writer.WriteValue(entry.Value, typeof(object));
			writer.WriteObjectEnd();
		}
	}
}
