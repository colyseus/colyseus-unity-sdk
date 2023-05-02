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
	public sealed class EnumSerializer : TypeSerializer
	{
		private readonly Type enumType;
		private readonly Type enumBaseType;

		public override Type SerializedType { get { return this.enumType; } }

		public EnumSerializer(Type enumType)
		{
			if (enumType == null) throw new ArgumentNullException("enumType");
			if (!enumType.IsEnum) throw JsonSerializationException.TypeIsNotValid(this.GetType(), "be a Enum");

			this.enumType = enumType;
			this.enumBaseType = Enum.GetUnderlyingType(enumType);
		}

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.StringLiteral)
				return Enum.Parse(this.enumType, reader.ReadString(false), true);
			else if (reader.Token == JsonToken.Number)
				return Enum.ToObject(this.enumType, reader.ReadValue(this.enumBaseType, false));
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.Number, JsonToken.StringLiteral);
		}

		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var valueStr = Convert.ToString(value, writer.Context.Format);
			writer.WriteString(valueStr);
		}
	}
}
