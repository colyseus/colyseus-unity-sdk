/* 
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
using System.Text;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public sealed class JsonStringBuilderWriter : JsonWriter
	{
		private readonly StringBuilder stringBuilder;

		public StringBuilder Builder
		{
			get { return stringBuilder; }
		}

		public JsonStringBuilderWriter(StringBuilder stringBuilder, SerializationContext context)
			: base(context)
		{
			if (stringBuilder == null)
				throw new ArgumentNullException("builder");


			this.stringBuilder = stringBuilder;
		}


		public override void Flush()
		{
		}

		public override void WriteJson(string jsonString)
		{
			if (jsonString == null)
				throw new ArgumentNullException("jsonString");


			stringBuilder.Append(jsonString);
			this.CharactersWritten += jsonString.Length;
		}

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

			stringBuilder.Append(jsonString, offset, charactersToWrite);
			this.CharactersWritten += charactersToWrite;
		}

		public override string ToString()
		{
			return stringBuilder.ToString();
		}
	}
}
