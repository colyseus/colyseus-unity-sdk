/*
	Copyright (c) 2026 Denis Zykov, GameDevWare.com

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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	/// <summary>
	/// Serializer for <see cref="DateTimeOffset"/> values.
	/// </summary>
	public sealed class DateTimeOffsetSerializer : TypeSerializer
	{
		/// <inheritdoc />
		public override Type SerializedType { get { return typeof(DateTimeOffset); } }

		/// <inheritdoc />
		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Value.Raw is DateTimeOffset)
				return reader.Value.Raw;
			else if (reader.Token == JsonToken.DateTime || reader.Value.Raw is DateTime)
				return new DateTimeOffset(reader.Value.AsDateTime);

			var dateTimeOffsetStr = reader.ReadString(false);
			try
			{
				var value = default(DateTimeOffset);
				if (DateTimeOffset.TryParseExact(dateTimeOffsetStr, reader.Context.DateTimeFormats, reader.Context.Format, DateTimeStyles.RoundtripKind, out value))
					return value;

				if (DateTimeOffset.TryParseExact(dateTimeOffsetStr, "o", reader.Context.Format, DateTimeStyles.RoundtripKind, out value))
					return value;

				if (!DateTimeOffset.TryParse(dateTimeOffsetStr, reader.Context.Format, DateTimeStyles.RoundtripKind, out value))
					value = DateTimeOffset.ParseExact(dateTimeOffsetStr, reader.Context.DateTimeFormats, reader.Context.Format, DateTimeStyles.RoundtripKind);

				return value;
			}
			catch (FormatException fe)
			{
				throw JsonSerializationException.FailedToParseDateTime(reader, dateTimeOffsetStr, reader.Context.DateTimeFormats[0], fe);
			}
		}

		/// <inheritdoc />
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var dateTimeOffset = (DateTimeOffset)value;
			writer.Write(dateTimeOffset);
		}
	}
}
