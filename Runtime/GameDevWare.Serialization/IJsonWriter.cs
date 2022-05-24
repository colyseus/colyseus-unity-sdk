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
	public interface IJsonWriter
	{
		SerializationContext Context { get; }

		void Flush();

		void Write(string value);
		void Write(JsonMember value);
		void Write(int number);
		void Write(uint number);
		void Write(long number);
		void Write(ulong number);
		void Write(float number);
		void Write(double number);
		void Write(decimal number);
		void Write(bool value);
		void Write(DateTime dateTime);
		void Write(DateTimeOffset dateTimeOffset);
		void WriteObjectBegin(int numberOfMembers);
		void WriteObjectEnd();
		void WriteArrayBegin(int numberOfMembers);
		void WriteArrayEnd();
		void WriteNull();

		void WriteJson(string jsonString);
		void WriteJson(char[] jsonString, int index, int charCount);

		void Reset();
	}
}
