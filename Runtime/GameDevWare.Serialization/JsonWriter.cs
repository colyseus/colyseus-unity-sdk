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
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public abstract class JsonWriter : IJsonWriter
	{
		[Flags]
		private enum Structure : byte
		{
			None = 0,
			IsContainer = 0x1,
			IsObject = 0x2 | IsContainer,
			IsArray = 0x4 | IsContainer,
			IsBegining = 0x1 << 7,
			IsBeginingOfContainer = IsContainer | IsBegining
		}

		private const long JS_NUMBER_MAX_VALUE_INT64 = 9007199254740992L;
		private const ulong JS_NUMBER_MAX_VALUE_U_INT64 = 9007199254740992UL;
		private const double JS_NUMBER_MAX_VALUE_DOUBLE = 9007199254740992.0;
		private const double JS_NUMBER_MAX_VALUE_SINGLE = 9007199254740992.0f;
		private const decimal JS_NUMBER_MAX_VALUE_DECIMAL = 9007199254740992.0m;

		private static readonly char[] Tabs = new char[] { '\t', '\t', '\t', '\t', '\t', '\t', '\t', '\t', '\t', '\t' };
		private static readonly char[] Newline = "\r\n".ToCharArray();
		private static readonly char[] NameSeparator = ":".ToCharArray();
		private static readonly char[] ValueSeparator = ",".ToCharArray();
		private static readonly char[] ArrayBegin = "[".ToCharArray();
		private static readonly char[] ArrayEnd = "]".ToCharArray();
		private static readonly char[] ObjectBegin = "{".ToCharArray();
		private static readonly char[] ObjectEnd = "}".ToCharArray();
		private static readonly char[] Null = "null".ToCharArray();
		private static readonly char[] True = "true".ToCharArray();
		private static readonly char[] False = "false".ToCharArray();

		private readonly Stack<Structure> structStack = new Stack<Structure>(10);
		private readonly char[] outputBuffer = new char[512];

		public SerializationContext Context { get; private set; }
		public long CharactersWritten { get; protected set; }
		public int InitialPadding { get; set; }

		protected JsonWriter(SerializationContext context)
		{
			if (context == null) throw new ArgumentNullException("context");

			this.Context = context;
		}

		public abstract void Flush();
		public abstract void WriteJson(string jsonString);
		public abstract void WriteJson(char[] jsonString, int offset, int charactersToWrite);

		public void Write(string value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}

			this.WriteFormatting(JsonToken.StringLiteral);

			var len = value.Length;
			var offset = 0;
			outputBuffer[0] = '"';
			this.WriteJson(outputBuffer, 0, 1);
			while (offset < len)
			{
				var writtenInBuffer = JsonUtils.EscapeBuffer(value, ref offset, outputBuffer, 0);
				this.WriteJson(outputBuffer, 0, writtenInBuffer);
			}
			outputBuffer[0] = '"';
			this.WriteJson(outputBuffer, 0, 1);
		}
		public void Write(JsonMember member)
		{
			this.WriteFormatting(JsonToken.Member);

			if (member.IsEscapedAndQuoted)
			{
				if (member.NameString != null)
					this.WriteJson(member.NameString);
				else
					this.WriteJson(member.NameChars, 0, member.NameChars.Length);
			}
			else
			{
				if (member.NameString != null)
					this.WriteString(member.NameString);
				else
					this.WriteString(new string(member.NameChars));

				this.WriteJson(NameSeparator, 0, NameSeparator.Length);
			}
		}
		public void Write(int number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.Int32ToBuffer(number, outputBuffer, 0, this.Context.Format);
			this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(uint number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.UInt32ToBuffer(number, outputBuffer, 0, this.Context.Format);
			this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(long number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.Int64ToBuffer(number, outputBuffer, 0, this.Context.Format);

			if (number > JS_NUMBER_MAX_VALUE_INT64)
				this.WriteString(new string(outputBuffer, 0, len));
			else
				this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(ulong number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.UInt64ToBuffer(number, outputBuffer, 0, this.Context.Format);

			if (number > JS_NUMBER_MAX_VALUE_U_INT64)
				this.WriteString(new string(outputBuffer, 0, len));
			else
				this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(float number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.SingleToBuffer(number, outputBuffer, 0, this.Context.Format);
			if (number > JS_NUMBER_MAX_VALUE_SINGLE)
				this.WriteString(new string(outputBuffer, 0, len));
			else
				this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(double number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.DoubleToBuffer(number, outputBuffer, 0, this.Context.Format);
			if (number > JS_NUMBER_MAX_VALUE_DOUBLE)
				this.WriteString(new string(outputBuffer, 0, len));
			else
				this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(decimal number)
		{
			this.WriteFormatting(JsonToken.Number);

			var len = JsonUtils.DecimalToBuffer(number, outputBuffer, 0, this.Context.Format);
			if (number > JS_NUMBER_MAX_VALUE_DECIMAL)
				this.WriteString(new string(outputBuffer, 0, len));
			else
				this.WriteJson(outputBuffer, 0, len);
		}
		public void Write(DateTime dateTime)
		{
			this.WriteFormatting(JsonToken.DateTime);

			if (dateTime.Kind == DateTimeKind.Unspecified)
				dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);

			var dateTimeFormat = this.Context.DateTimeFormats.FirstOrDefault() ?? "o";
			if (dateTimeFormat.IndexOf('z') >= 0 && dateTime.Kind != DateTimeKind.Local)
				dateTime = dateTime.ToLocalTime();

			var dateString = dateTime.ToString(dateTimeFormat, this.Context.Format);

			this.Write(dateString);
		}
		public void Write(DateTimeOffset dateTimeOffset)
		{
			this.WriteFormatting(JsonToken.DateTime);

			var dateTimeFormat = this.Context.DateTimeFormats.FirstOrDefault() ?? "o";
			var dateString = dateTimeOffset.ToString(dateTimeFormat, this.Context.Format);
			this.Write(dateString);
		}
		public void Write(bool value)
		{
			this.WriteFormatting(JsonToken.Boolean);

			if (value)
				this.WriteJson(True, 0, True.Length);
			else
				this.WriteJson(False, 0, False.Length);
		}
		public void WriteObjectBegin(int numberOfMembers)
		{
			this.WriteFormatting(JsonToken.BeginObject);

			structStack.Push(Structure.IsObject | Structure.IsBegining);
			this.WriteJson(ObjectBegin, 0, ObjectBegin.Length);
		}
		public void WriteObjectEnd()
		{
			this.WriteFormatting(JsonToken.EndOfObject);

			structStack.Pop();
			this.WriteNewlineAndPad(0);
			this.WriteJson(ObjectEnd, 0, ObjectEnd.Length);
		}
		public void WriteArrayBegin(int numberOfMembers)
		{
			this.WriteFormatting(JsonToken.BeginArray);

			structStack.Push(Structure.IsArray | Structure.IsBegining);
			this.WriteJson(ArrayBegin, 0, ArrayBegin.Length);
		}
		public void WriteArrayEnd()
		{
			this.WriteFormatting(JsonToken.EndOfArray);

			structStack.Pop();
			this.WriteJson(ArrayEnd, 0, ArrayEnd.Length);
		}
		public void WriteNull()
		{
			this.WriteFormatting(JsonToken.Null);

			this.WriteJson(Null, 0, Null.Length);
		}

		public void Reset()
		{
			this.CharactersWritten = 0;
			this.structStack.Clear();
		}

		private void WriteNewlineAndPad(int correction)
		{
			if ((this.Context.Options & SerializationOptions.PrettyPrint) != SerializationOptions.PrettyPrint)
				return;

			// add padings and linebreaks
			this.WriteJson(Newline, 0, Newline.Length);
			var tabs = this.structStack.Count + correction;
			while (tabs > 0)
			{
				this.WriteJson(Tabs, 0, Math.Min(tabs, Tabs.Length));
				tabs -= Tabs.Length;
			}
		}
		private void WriteFormatting(JsonToken token)
		{
			if (this.structStack.Count <= 0)
				return;

			var stackPeek = this.structStack.Peek();
			var isNotMemberValue = ((stackPeek & Structure.IsObject) != Structure.IsObject || token == JsonToken.Member);
			var isEndToken = token == JsonToken.EndOfArray || token == JsonToken.EndOfObject;

			if ((stackPeek & Structure.IsContainer) != Structure.IsContainer || !isNotMemberValue)
				return;

			// it's a begining of container we add padding and remove "is begining" flag
			if ((stackPeek & Structure.IsBeginingOfContainer) == Structure.IsBeginingOfContainer)
			{
				stackPeek = this.structStack.Pop();
				this.structStack.Push(stackPeek ^ Structure.IsBegining); // revert "is begining"
			}
			// else if it's new array's value or new object's member put comman and padding
			else if (!isEndToken)
				this.WriteJson(ValueSeparator, 0, ValueSeparator.Length);

			// padding
			// pad only before member in object container(not before value, it's ugly)
			this.WriteNewlineAndPad(this.InitialPadding + (isEndToken ? -1 : 0));
		}
	}
}
