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
	/// Provides a streaming, forward-only interface for writing JSON-like data.
	/// <para>This writer manages data buffering and structural integrity, allowing for manual construction of 
	/// valid JSON or MessagePack payloads. It is ideal for high-performance logging, custom data exporters, 
	/// or specialized serialization strategies where fine-grained control over the output format is required.</para>
	/// </summary>
	public interface IJsonWriter
	{
		/// <summary>
		/// Gets the serialization context, providing shared configuration and state for the writing process.
		/// </summary>
		SerializationContext Context { get; }

		/// <summary>
		/// Forces the writer to commit all buffered data to the underlying stream or storage. 
		/// Always call this at the end of a writing operation.
		/// </summary>
		void Flush();

		/// <summary>
		/// Writes a string literal, handling any necessary escaping.
		/// </summary>
		/// <param name="value">The value to append to the stream.</param>
		void Write(string value);
		/// <summary>
		/// Writes a structural member identifier (e.g., an object property name).
		/// </summary>
		/// <param name="value">The member name to write.</param>
		void Write(JsonMember value);
		/// <summary>
		/// Writes a 32-bit integer.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(int number);
		/// <summary>
		/// Writes an unsigned 32-bit integer.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(uint number);
		/// <summary>
		/// Writes a 64-bit integer.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(long number);
		/// <summary>
		/// Writes an unsigned 64-bit integer.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(ulong number);
		/// <summary>
		/// Writes a single-precision floating-point number.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(float number);
		/// <summary>
		/// Writes a double-precision floating-point number.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(double number);
		/// <summary>
		/// Writes a high-precision decimal number.
		/// </summary>
		/// <param name="number">The value to append.</param>
		void Write(decimal number);
		/// <summary>
		/// Writes a boolean value.
		/// </summary>
		/// <param name="value">The value to append.</param>
		void Write(bool value);
		/// <summary>
		/// Writes a date and time value.
		/// </summary>
		/// <param name="dateTime">The value to append.</param>
		void Write(DateTime dateTime);
		/// <summary>
		/// Writes a date, time, and timezone offset.
		/// </summary>
		/// <param name="dateTimeOffset">The value to append.</param>
		void Write(DateTimeOffset dateTimeOffset);
		/// <summary>
		/// Begins a new object container. 
		/// </summary>
		/// <param name="numberOfMembers">The expected number of members; primarily for MessagePack optimizations.</param>
		void WriteObjectBegin(int numberOfMembers);
		/// <summary>
		/// Closes the current object container.
		/// </summary>
		void WriteObjectEnd();
		/// <summary>
		/// Begins a new array container.
		/// </summary>
		/// <param name="numberOfMembers">The expected number of elements; primarily for MessagePack optimizations.</param>
		void WriteArrayBegin(int numberOfMembers);
		/// <summary>
		/// Closes the current array container.
		/// </summary>
		void WriteArrayEnd();
		/// <summary>
		/// Appends a null literal to the stream.
		/// </summary>
		void WriteNull();

		/// <summary>
		/// Appends a raw JSON string to the output without escaping. 
		/// Use this for pre-formatted data.
		/// </summary>
		/// <param name="jsonString">The raw payload to append.</param>
		void WriteJson(string jsonString);
		/// <summary>
		/// Appends a raw JSON character sequence to the output without escaping.
		/// </summary>
		/// <param name="jsonString">The raw character buffer.</param>
		/// <param name="index">The starting position.</param>
		/// <param name="charCount">The number of characters to copy.</param>
		void WriteJson(char[] jsonString, int index, int charCount);

		/// <summary>
		/// Recycles the writer by clearing internal buffers and resetting its state for a new operation.
		/// </summary>
		void Reset();
	}
}
