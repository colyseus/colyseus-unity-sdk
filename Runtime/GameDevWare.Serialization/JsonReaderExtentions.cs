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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Provides extension methods for <see cref="IJsonReader"/> to facilitate reading various JSON tokens and values.
	/// </summary>
	public static class JsonReaderExtentions
	{
		/// <summary>
		/// Asserts that the current token is <see cref="JsonToken.BeginArray"/> and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		public static void ReadArrayBegin(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			if (reader.Token != JsonToken.BeginArray)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.BeginArray);
			if (reader.IsEndOfStream())
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.EndOfArray);

			if (nextToken)
				reader.NextToken();
		}
		/// <summary>
		/// Asserts that the current token is <see cref="JsonToken.EndOfArray"/> and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		public static void ReadArrayEnd(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			if (reader.Token != JsonToken.EndOfArray)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.EndOfArray);

			if (nextToken)
				reader.NextToken();
		}

		/// <summary>
		/// Asserts that the current token is <see cref="JsonToken.BeginObject"/> and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		public static void ReadObjectBegin(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			if (reader.Token != JsonToken.BeginObject)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.BeginObject);
			if (reader.IsEndOfStream())
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.EndOfObject);

			if (nextToken)
				reader.NextToken();
		}
		/// <summary>
		/// Asserts that the current token is <see cref="JsonToken.EndOfObject"/> and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		public static void ReadObjectEnd(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (reader.Token != JsonToken.EndOfObject)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.EndOfObject);


			if (nextToken)
				reader.NextToken();
		}

		/// <summary>
		/// Reads an object member name and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The member name.</returns>
		public static string ReadMember(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			if (reader.Token != JsonToken.Member)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.Member);

			var memberName = (string)reader.RawValue;

			if (nextToken)
				reader.NextToken();

			return memberName;
		}

		/// <summary>
		/// Reads a byte value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The byte value.</returns>
		public static byte ReadByte(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(byte);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsByte;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable byte value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The byte value, or null.</returns>
		public static byte? ReadByteOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(byte?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsByte;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a signed byte value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The signed byte value.</returns>
		public static sbyte ReadSByte(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(sbyte);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsSByte;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable signed byte value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The signed byte value, or null.</returns>
		public static sbyte? ReadSByteOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(sbyte?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsSByte;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 16-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 16-bit signed integer value.</returns>
		public static short ReadInt16(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(short);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsInt16;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 16-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 16-bit signed integer value, or null.</returns>
		public static short? ReadInt16OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(short?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsInt16;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 32-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 32-bit signed integer value.</returns>
		public static int ReadInt32(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(int);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsInt32;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 32-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 32-bit signed integer value, or null.</returns>
		public static int? ReadInt32OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(int?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsInt32;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 64-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 64-bit signed integer value.</returns>
		public static long ReadInt64(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(long);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsInt64;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 64-bit signed integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 64-bit signed integer value, or null.</returns>
		public static long? ReadInt64OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(long?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsInt64;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}
			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 16-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 16-bit unsigned integer value.</returns>
		public static ushort ReadUInt16(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(ushort);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsUInt16;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 16-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 16-bit unsigned integer value, or null.</returns>
		public static ushort? ReadUInt16OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(ushort?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsUInt16;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 32-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 32-bit unsigned integer value.</returns>
		public static uint ReadUInt32(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(uint);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsUInt32;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 32-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 32-bit unsigned integer value, or null.</returns>
		public static uint? ReadUInt32OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(uint?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsUInt32;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a 64-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 64-bit unsigned integer value.</returns>
		public static ulong ReadUInt64(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(ulong);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsUInt64;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable 64-bit unsigned integer value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The 64-bit unsigned integer value, or null.</returns>
		public static ulong? ReadUInt64OrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(ulong?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsUInt64;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a single-precision floating-point value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The single-precision floating-point value.</returns>
		public static float ReadSingle(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(float);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsSingle;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable single-precision floating-point value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The single-precision floating-point value, or null.</returns>
		public static float? ReadSingleOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(float?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsSingle;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a double-precision floating-point value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The double-precision floating-point value.</returns>
		public static double ReadDouble(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(double);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsDouble;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable double-precision floating-point value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The double-precision floating-point value, or null.</returns>
		public static double? ReadDoubleOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(double?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsDouble;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a decimal value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The decimal value.</returns>
		public static decimal ReadDecimal(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(decimal);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number)
				value = reader.Value.AsDecimal;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable decimal value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The decimal value, or null.</returns>
		public static decimal? ReadDecimalOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(decimal?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
					value = reader.Value.AsDecimal;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a boolean value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The boolean value.</returns>
		public static bool ReadBoolean(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(bool);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Boolean)
				value = reader.Value.AsBoolean;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Boolean);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable boolean value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The boolean value, or null.</returns>
		public static bool? ReadBooleanOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(bool?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Boolean:
					value = reader.Value.AsBoolean;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Boolean);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a <see cref="DateTime"/> value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The <see cref="DateTime"/> value.</returns>
		public static DateTime ReadDateTime(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(DateTime);
			if (reader.Token == JsonToken.Member || reader.Token == JsonToken.StringLiteral || reader.Token == JsonToken.Number || reader.Token == JsonToken.DateTime)
				value = reader.Value.AsDateTime;
			else
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number, JsonToken.DateTime);

			if (nextToken)
				reader.NextToken();

			return value;
		}
		/// <summary>
		/// Reads a nullable <see cref="DateTime"/> value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The <see cref="DateTime"/> value, or null.</returns>
		public static DateTime? ReadDateTimeOrNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			var value = default(DateTime?);
			switch (reader.Token)
			{
				case JsonToken.Null:
					value = null;
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
				case JsonToken.DateTime:
					value = reader.Value.AsDateTime;
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number, JsonToken.DateTime);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Reads a string value and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The string value.</returns>
		public static string ReadString(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			var stringValue = default(string);
			switch (reader.Token)
			{
				case JsonToken.Null:
					break;
				case JsonToken.Member:
				case JsonToken.StringLiteral:
				case JsonToken.Number:
				case JsonToken.DateTime:
				case JsonToken.Boolean:
					stringValue = Convert.ToString(reader.RawValue, reader.Context.Format);
					break;
				default:
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.StringLiteral, JsonToken.Number, JsonToken.DateTime, JsonToken.Boolean);
			}

			if (nextToken)
				reader.NextToken();

			return stringValue;
		}

		/// <summary>
		/// Asserts that the current token is <see cref="JsonToken.Null"/> and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		public static void ReadNull(this IJsonReader reader, bool nextToken = true)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			if (reader.Token != JsonToken.Null)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.Null);

			if (nextToken)
				reader.NextToken();
		}

		/// <summary>
		/// Reads a value of the specified type and optionally moves to the next token.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <param name="valueType">The type of the value to read.</param>
		/// <param name="nextToken">A value indicating whether to advance to the next token after reading.</param>
		/// <returns>The deserialized value.</returns>
		public static object ReadValue(this IJsonReader reader, Type valueType, bool nextToken = true)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Context.Hierarchy.Count >= reader.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(reader, (ulong)reader.Context.MaxHierarchyDepth);

			// try guess type
			if (valueType == typeof(object))
				valueType = reader.Value.Type;

			var value = default(object);
			var isNullable = valueType.IsValueType == false || valueType.IsInstantiationOf(typeof(Nullable<>));
			if (reader.Token == JsonToken.Null && isNullable)
			{
				value = null;
			}
			else
			{
				if (isNullable && valueType.IsValueType)
					valueType = valueType.GetGenericArguments()[0]; // get subtype of Nullable<T>

				var serializer = reader.Context.GetSerializerForType(valueType);
				value = serializer.Deserialize(reader);
			}

			if (nextToken)
				reader.NextToken();

			return value;
		}

		/// <summary>
		/// Advances the reader through all tokens and returns a string representation for debugging.
		/// </summary>
		/// <param name="reader">The JSON reader.</param>
		/// <returns>A string representation of the tokens.</returns>
		public static string DebugPrintTokens(this IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var output = new StringBuilder();
			var stack = new Stack<JsonToken>();
			stack.Push(JsonToken.None);
			while (reader.NextToken())
			{
				var strValue = reader.Token + (reader.Value.HasValue && reader.Value != null ? "[<" + reader.Value.Type.Name + "> " + JsonUtils.EscapeAndQuote(reader.Value.AsString).Trim('"') + "]" : "");

				if (stack.Peek() != JsonToken.Member)
				{
					var endingTokenIndent = (reader.Token == JsonToken.EndOfObject || reader.Token == JsonToken.EndOfArray ? -1 : 0);
					output.Append(Environment.NewLine);
					for (var i = 0; i < System.Linq.Enumerable.Count(stack, t => t != JsonToken.Member && t != JsonToken.None) + endingTokenIndent; i++)
						output.Append("\t");
				}
				else
				{
					output.Append(" ");
				}

				output.Append(strValue);

				if (reader.Token == JsonToken.EndOfObject || reader.Token == JsonToken.EndOfArray || stack.Peek() == JsonToken.Member)
					stack.Pop();
				if (reader.Token == JsonToken.BeginObject || reader.Token == JsonToken.BeginArray || reader.Token == JsonToken.Member)
					stack.Push(reader.Token);
			}
			return output.ToString();
		}
	}
}
