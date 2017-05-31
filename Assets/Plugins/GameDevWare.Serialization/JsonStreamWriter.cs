﻿/* 
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

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
	public sealed class JsonStreamWriter : JsonWriter
	{
		private readonly StreamWriter writer;

		public Stream Stream { get { return writer.BaseStream; } }

		public JsonStreamWriter(Stream stream, SerializationContext context) : base(context)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (!stream.CanWrite) throw JsonSerializationException.StreamIsNotWriteable();


			writer = new StreamWriter(stream, context.Encoding);
		}

		public override void Flush()
		{
			writer.Flush();
		}

		public override void WriteJson(string jsonString)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");


			writer.Write(jsonString);
			this.CharactersWritten += jsonString.Length;
		}

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
