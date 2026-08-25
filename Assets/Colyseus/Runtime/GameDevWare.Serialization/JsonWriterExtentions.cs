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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Provides extension methods for <see cref="IJsonWriter"/> to simplify writing various data types.
	/// </summary>
	public static class JsonWriterExtentions
	{
		/// <summary>
		/// Writes a member name to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="memberName">The name of the member to write.</param>
		public static void WriteMember(this IJsonWriter writer, string memberName)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (memberName == null) throw new ArgumentNullException("memberName");

			writer.Write((JsonMember)memberName);
		}

		/// <summary>
		/// Writes a <see cref="DateTime"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="date">The date and time value to write.</param>
		public static void WriteDateTime(this IJsonWriter writer, DateTime date)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(date);
		}
		/// <summary>
		/// Writes a nullable <see cref="DateTime"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="date">The nullable date and time value to write.</param>
		public static void WriteDateTime(this IJsonWriter writer, DateTime? date)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (date == null)
				writer.WriteNull();
			else
				writer.Write(date.Value);
		}

		/// <summary>
		/// Writes a <see cref="bool"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="value">The boolean value to write.</param>
		public static void WriteBoolean(this IJsonWriter writer, bool value)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(value);
		}
		/// <summary>
		/// Writes a nullable <see cref="bool"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="value">The nullable boolean value to write.</param>
		public static void WriteBoolean(this IJsonWriter writer, bool? value)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (value == null)
				writer.WriteNull();
			else
				writer.Write(value.Value);
		}

		/// <summary>
		/// Writes a <see cref="byte"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The byte value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, byte number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes an <see cref="sbyte"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The sbyte value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, sbyte number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="short"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The short value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, short number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="ushort"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The ushort value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, ushort number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes an <see cref="int"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The integer value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, int number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="uint"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The uint value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, uint number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="long"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The long value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, long number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="ulong"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The ulong value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, ulong number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="float"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The float value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, float number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="double"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The double value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, double number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a <see cref="decimal"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The decimal value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, decimal number)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			writer.Write(number);
		}
		/// <summary>
		/// Writes a nullable <see cref="byte"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable byte value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, byte? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="sbyte"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable sbyte value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, sbyte? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="short"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable short value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, short? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="ushort"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable ushort value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, ushort? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="int"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable integer value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, int? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="uint"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable uint value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, uint? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="long"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable long value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, long? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="ulong"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable ulong value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, ulong? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="float"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable float value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, float? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="double"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable double value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, double? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}
		/// <summary>
		/// Writes a nullable <see cref="decimal"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="number">The nullable decimal value to write.</param>
		public static void WriteNumber(this IJsonWriter writer, decimal? number)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (number == null)
				writer.WriteNull();
			else
				writer.Write(number.Value);
		}

		/// <summary>
		/// Writes a <see cref="string"/> value to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="literal">The string value to write.</param>
		public static void WriteString(this IJsonWriter writer, string literal)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			if (literal == null)
				writer.WriteNull();
			else
				writer.Write(literal);
		}

		/// <summary>
		/// Writes an object value of a specific type to the JSON writer.
		/// </summary>
		/// <param name="writer">The JSON writer to use.</param>
		/// <param name="value">The object value to write.</param>
		/// <param name="valueType">The declared type of the value.</param>
		/// <exception cref="JsonSerializationException">Thrown when the serialization graph is too deep.</exception>
		public static void WriteValue(this IJsonWriter writer, object value, Type valueType)
		{
			if (writer == null) throw new ArgumentNullException("writer");

			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			if (writer.Context.Hierarchy.Count >= writer.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(writer, (ulong)writer.Context.MaxHierarchyDepth);

			var actualValueType = value.GetType();
			var serializer = writer.Context.GetSerializerForType(actualValueType);
			//var objectSerializer = serializer as ObjectSerializer;
			//if (objectSerializer != null && valueType == actualValueType)
			//	objectSerializer.SuppressTypeInformation = true; // no need to write type information on when type is obvious

			serializer.Serialize(writer, value);
		}
	}
}
