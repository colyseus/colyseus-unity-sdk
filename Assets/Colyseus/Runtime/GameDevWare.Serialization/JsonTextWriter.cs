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
	/// Represents a JSON writer that writes to a <see cref="TextWriter"/>.
	/// </summary>
	public sealed class JsonTextWriter : JsonWriter
	{
		private readonly TextWriter writer;

		private TextWriter Writer
		{
			get { return writer; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonTextWriter"/> class.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
		/// <param name="context">The serialization context.</param>
		/// <param name="buffer">The character buffer to use.</param>
		public JsonTextWriter(TextWriter writer, SerializationContext context, char[] buffer = null)
			: base(context, buffer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");


			this.writer = writer;
		}

		/// <inheritdoc />
		public override void Flush()
		{
			writer.Flush();
		}

		/// <inheritdoc />
		public override void WriteJson(string jsonString)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");


			writer.Write(jsonString);
			this.CharactersWritten += jsonString.Length;
		}

		/// <inheritdoc />
		public override void WriteJson(char[] jsonString, int offset, int charactersToWrite)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");
			if (offset < 0 || offset >= jsonString.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (charactersToWrite < 0 || offset + charactersToWrite > jsonString.Length)
				throw new ArgumentOutOfRangeException("charactersToWrite");


			if (charactersToWrite == 0)
				return;

			writer.Write(jsonString, offset, charactersToWrite);
			this.CharactersWritten += charactersToWrite;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return writer.ToString();
		}
	}
}
