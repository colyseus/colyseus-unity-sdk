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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class PrimitiveSerializer : TypeSerializer
	{
		private readonly Type primitiveType;
		private readonly TypeCode primitiveTypeCode;

		public override Type SerializedType { get { return this.primitiveType; } }

		public PrimitiveSerializer(Type primitiveType)
		{
			if (primitiveType == null) throw new ArgumentNullException("primitiveType");

			if (primitiveType.IsGenericType && primitiveType.GetGenericTypeDefinition() == typeof(Nullable<>))
				throw JsonSerializationException.TypeIsNotValid(typeof(PrimitiveSerializer), "can't be nullable type");

			this.primitiveType = primitiveType;
			this.primitiveTypeCode = Type.GetTypeCode(primitiveType);

			if (this.primitiveTypeCode == TypeCode.Object || this.primitiveTypeCode == TypeCode.Empty ||
				this.primitiveTypeCode == TypeCode.DBNull)
				throw JsonSerializationException.TypeIsNotValid(this.GetType(), "be a primitive type");
		}

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
			{
				if (this.primitiveTypeCode == TypeCode.String)
					return null;

				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.Boolean, JsonToken.DateTime, JsonToken.Null, JsonToken.Number, JsonToken.StringLiteral);
			}

			var value = default(object);
			switch (primitiveTypeCode)
			{
				case TypeCode.Boolean:
					value = reader.ReadBoolean(false);
					break;
				case TypeCode.Byte:
					value = reader.ReadByte(false);
					break;
				case TypeCode.DateTime:
					value = reader.ReadDateTime(false);
					break;
				case TypeCode.Decimal:
					value = reader.ReadDecimal(false);
					break;
				case TypeCode.Double:
					value = reader.ReadDouble(false);
					break;
				case TypeCode.Int16:
					value = reader.ReadInt16(false);
					break;
				case TypeCode.Int32:
					value = reader.ReadInt32(false);
					break;
				case TypeCode.Int64:
					value = reader.ReadInt64(false);
					break;
				case TypeCode.SByte:
					value = reader.ReadSByte(false);
					break;
				case TypeCode.Single:
					value = reader.ReadSingle(false);
					break;
				case TypeCode.UInt16:
					value = reader.ReadUInt16(false);
					break;
				case TypeCode.UInt32:
					value = reader.ReadUInt32(false);
					break;
				case TypeCode.UInt64:
					value = reader.ReadUInt64(false);
					break;
				default:
					var valueStr = reader.ReadString(false);
					value = Convert.ChangeType(valueStr, this.primitiveType, reader.Context.Format);
					break;
			}
			return value;
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			switch (primitiveTypeCode)
			{
				case TypeCode.Boolean:
					writer.WriteBoolean((bool)value);
					break;
				case TypeCode.Byte:
					writer.WriteNumber((byte)value);
					break;
				case TypeCode.DateTime:
					writer.WriteDateTime((DateTime)value);
					break;
				case TypeCode.Decimal:
					writer.WriteNumber((decimal)value);
					break;
				case TypeCode.Double:
					writer.WriteNumber((double)value);
					break;
				case TypeCode.Int16:
					writer.WriteNumber((short)value);
					break;
				case TypeCode.Int32:
					writer.WriteNumber((int)value);
					break;
				case TypeCode.Int64:
					writer.WriteNumber((long)value);
					break;
				case TypeCode.SByte:
					writer.WriteNumber((sbyte)value);
					break;
				case TypeCode.Single:
					writer.WriteNumber((float)value);
					break;
				case TypeCode.UInt16:
					writer.WriteNumber((ushort)value);
					break;
				case TypeCode.UInt32:
					writer.WriteNumber((uint)value);
					break;
				case TypeCode.UInt64:
					writer.WriteNumber((ulong)value);
					break;
				default:
					var valueStr = default(string);

					if (value is IFormattable)
						valueStr = (string)Convert.ChangeType(value, typeof(string), writer.Context.Format);
					else
						valueStr = value.ToString();

					writer.WriteString(valueStr);
					break;
			}
		}
	}
}
