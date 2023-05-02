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
namespace GameDevWare.Serialization
{
	public static class JsonWriterExtentions
	{
		public static void WriteMember(this IJsonWriter writer, string memberName)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (memberName == null) throw new ArgumentNullException("memberName");

			writer.Write((JsonMember)memberName);
		}

		public static void WriteDateTime(this IJsonWriter writer, DateTime date)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(date);
		}
		public static void WriteDateTime(this IJsonWriter writer, DateTime? date)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (date == null)
				writer.WriteNull();
			else
				writer.Write(date.Value);
		}

		public static void WriteBoolean(this IJsonWriter writer, bool value)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(value);
		}
		public static void WriteBoolean(this IJsonWriter writer, bool? value)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (value == null)
				writer.WriteNull();
			else
				writer.Write(value.Value);
		}

		public static void WriteNumber(this IJsonWriter writer, byte number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, sbyte number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, short number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, ushort number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, int number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, uint number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, long number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, ulong number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, float number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, double number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, decimal number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		public static void WriteNumber(this IJsonWriter writer, byte? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, sbyte? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, short? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, ushort? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, int? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, uint? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, long? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, ulong? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, float? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, double? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		public static void WriteNumber(this IJsonWriter writer, decimal? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}

		public static void WriteString(this IJsonWriter writer, string literal)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (literal == null)
				writer.WriteNull();
			else
				writer.Write(literal);
		}

		public static void WriteValue(this IJsonWriter writer, object value, Type valueType)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			var actualValueType = value.GetType();
			var serializer = writer.Context.GetSerializerForType(actualValueType);
			//var objectSerializer = serializer as ObjectSerializer;
			//if (objectSerializer != null && valueType == actualValueType)
			//	objectSerializer.SuppressTypeInformation = true; // no need to write type information on when type is obvious

			serializer.Serialize(writer, value);
		}
	}
}
