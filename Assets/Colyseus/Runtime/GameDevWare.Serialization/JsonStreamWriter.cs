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
using System.IO;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents a writer that provides fast, non-cached, forward-only access to JSON data to a <see cref="Stream"/>.
	/// </summary>
	public sealed class JsonStreamWriter : JsonWriter
	{
		private readonly StreamWriter writer;

		/// <summary>
		/// Gets the underlying stream.
		/// </summary>
		public Stream Stream { get { return writer.BaseStream; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonStreamWriter"/> class.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="context">The serialization context.</param>
		/// <param name="buffer">The character buffer to use for writing.</param>
		public JsonStreamWriter(Stream stream, SerializationContext context, char[] buffer = null)
			: base(context, buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (!stream.CanWrite) throw JsonSerializationException.StreamIsNotWriteable();


			writer = new StreamWriter(stream, context.Encoding);
		}

		/// <summary>
		/// Flushes the underlying writer.
		/// </summary>
		public override void Flush()
		{
			writer.Flush();
		}

		/// <summary>
		/// Writes a JSON string to the underlying stream.
		/// </summary>
		/// <param name="jsonString">The JSON string to write.</param>
		public override void WriteJson(string jsonString)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");


			writer.Write(jsonString);
			this.CharactersWritten += jsonString.Length;
		}

		/// <summary>
		/// Writes a portion of a character array as JSON to the underlying stream.
		/// </summary>
		/// <param name="jsonString">The character array containing the JSON.</param>
		/// <param name="index">The starting index in the array.</param>
		/// <param name="charactersToWrite">The number of characters to write.</param>
		public override void WriteJson(char[] jsonString, int index, int charactersToWrite)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");
			if (index < 0 || index >= jsonString.Length)
				throw new ArgumentOutOfRangeException("index");
			if (charactersToWrite < 0 || index + charactersToWrite > jsonString.Length)
				throw new ArgumentOutOfRangeException("charactersToWrite");


			if (charactersToWrite == 0)
				return;

			writer.Write(jsonString, index, charactersToWrite);
			this.CharactersWritten += charactersToWrite;
		}
	}
}
