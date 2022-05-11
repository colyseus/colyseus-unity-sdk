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
using System.Linq;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class DateTimeSerializer : TypeSerializer
	{
		public override Type SerializedType { get { return typeof(DateTime); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.DateTime || reader.RawValue is DateTime)
				return reader.Value.AsDateTime;

			var dateTimeStr = reader.ReadString(false);
			try
			{
				var value = default(DateTime);
				if (!DateTime.TryParse(dateTimeStr, reader.Context.Format, DateTimeStyles.RoundtripKind, out value))
					value = DateTime.ParseExact(dateTimeStr, reader.Context.DateTimeFormats, reader.Context.Format, DateTimeStyles.RoundtripKind);

				return value;
			}
			catch (FormatException fe)
			{
				throw new SerializationException(string.Format("Failed to parse date '{0}' in with pattern '{1}'.", dateTimeStr, reader.Context.DateTimeFormats[0]), fe);
			}
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var dataTime = (DateTime)value;
			writer.Write(dataTime);
		}
	}
}
