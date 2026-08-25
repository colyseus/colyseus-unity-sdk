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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Provides a streaming, forward-only interface for reading JSON-like data.
	/// <para>This reader tokenizes input into discrete structural elements, allowing for efficient, low-level parsing 
	/// without the memory overhead of loading an entire object graph into memory. Developers can use it directly 
	/// to implement custom deserialization logic or to selectively extract data from large streams.</para>
	/// </summary>
	public interface IJsonReader
	{
		/// <summary>
		/// Gets the serialization context, providing shared configuration and state for the reading process.
		/// </summary>
		SerializationContext Context { get; }

		/// <summary>
		/// Gets the type of the current token (e.g., BeginObject, Number, StringLiteral). 
		/// Use this to drive state-machine-based parsing logic.
		/// </summary>
		JsonToken Token { get; }
		/// <summary>
		/// Gets the raw, untyped value of the current token.
		/// </summary>
		object RawValue { get; }
		/// <summary>
		/// Gets a wrapper providing type-safe access and lazy conversion for the current token's value.
		/// </summary>
		IValueInfo Value { get; }

		/// <summary>
		/// Advances the reader to the next structural element or value in the stream.
		/// </summary>
		/// <returns>True if the next token was successfully read; false if the end of the stream was reached.</returns>
		bool NextToken();

		/// <summary>
		/// Checks if the reader has exhausted the input stream.
		/// </summary>
		/// <returns>True if no more tokens are available; otherwise, false.</returns>
		bool IsEndOfStream();

		/// <summary>
		///     Resets Line/Column numbers, CharactersRead and Token information of reader
		/// </summary>
		void Reset();
	}
}
