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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public abstract class JsonReader : IJsonReader
	{
		protected const int DEFAULT_BUFFER_SIZE = 1024;

		private sealed class Buffer : IList<char>
		{
			private const float SHIFT_THRESHOLD = 0.5f; // position over 50% of buffer
			private const float GROW_THRESHOLD = 0.1f; // when less that 10% of space is free

			private readonly JsonReader reader;
			private char[] buffer;
			private int? lazyFixation;
			private int end;
			private readonly int initialSize;
			private bool isLast;
			private int lineNumber;
			private long lineStartIndex;
			private long charsReaded;

			private int Capacity
			{
				get { return this.buffer.Length; }
				set
				{
					if (value <= 0)
						throw new ArgumentOutOfRangeException("value");

					var newBuffer = new char[value];
					BlockCopy(this.buffer, 0, newBuffer, 0, Math.Min(newBuffer.Length, this.buffer.Length));
					this.buffer = newBuffer;
				}
			}

			public int Offset { get; private set; }
			public long CharactersReaded { get { return this.charsReaded + this.Offset; } }
			public int LineNumber { get { return this.lineNumber + 1; } }
			public int ColumnNumber { get { return (int)(this.CharactersReaded - this.lineStartIndex + 1); } }
			public char this[int index]
			{
				get
				{
					if (index < 0)
						throw new ArgumentOutOfRangeException("index");

					if (this.IsBeyondOfStream(index))
						return (char)0;

					return this.buffer[this.Offset + index];
				}
			}

			public Buffer(JsonReader reader, char[] buffer)
			{
				if (reader == null) new ArgumentNullException("reader");

				this.reader = reader;
				this.buffer = buffer ?? new char[DEFAULT_BUFFER_SIZE];
				this.initialSize = this.buffer.Length;
				this.end = 0;
				this.Offset = 0;
			}

			public void FixateNow()
			{
				if (this.lazyFixation == null) return;

				this.Fixate(this.lazyFixation.Value);
				this.lazyFixation = null;
			}
			public void Fixate(int atIndex)
			{
				if (atIndex < 0) throw new ArgumentOutOfRangeException("atIndex");

				for (var i = 0; i < atIndex; i++)
				{
					if (this[i] != '\n') continue;

					this.lineNumber++;
					this.lineStartIndex = this.CharactersReaded + i;
				}

				// ensure that fixation point in loaded range
				this.IsBeyondOfStream(atIndex);

				this.Offset += atIndex;

				// when we are at second half of buffer - we need to shift back
				if ((this.Offset / (float)this.buffer.Length) > SHIFT_THRESHOLD)
					this.ShiftToZero();
			}
			public void FixateLater(int atIndex)
			{
				if (atIndex < 0) throw new ArgumentOutOfRangeException("atIndex");

				if (this.lazyFixation != null)
					this.lazyFixation += atIndex;
				else
					this.lazyFixation = atIndex;
			}

			public bool IsBeyondOfStream(int index)
			{
				if (!this.isLast && this.Offset + index >= this.end)
					this.ReadNextBlock();

				if (this.isLast && this.Offset + index >= this.end)
					return true;

				return false;
			}

			public char[] GetChars()
			{
				return this.buffer;
			}

			public void Reset()
			{
				this.FixateNow();
				this.ShiftToZero();
				this.charsReaded = 0;
				this.lineNumber = 0;
				this.lineStartIndex = 0;
			}

			private void ReadNextBlock()
			{
				// when we are at second half of buffer - we need to shift back
				if ((this.Offset / (float)this.buffer.Length) > SHIFT_THRESHOLD)
					this.ShiftToZero();

				// check for free space
				var free = this.buffer.Length - this.end;
				if ((free / (float)this.initialSize) < GROW_THRESHOLD)
					this.Capacity += this.initialSize;

				var newEnd = this.reader.FillBuffer(this.buffer, this.end);
				this.isLast = newEnd == this.end;
				this.end = newEnd;
			}

			private void ShiftToZero()
			{
				this.charsReaded += this.Offset;

				var block = Math.Min(this.Offset, this.end - this.Offset);
				var start = this.Offset;
				var lastBlock = 0;
				while (start < this.end)
				{
					BlockCopy(this.buffer, start, this.buffer, lastBlock, Math.Min(block, this.end - start));
					lastBlock += block;
					start += block;
				}

				this.end = this.end - this.Offset;
				this.Offset = 0;
#if DEBUG
				if (this.end < this.buffer.Length) // zero unused space(just for debug)
					Array.Clear(this.buffer, this.end, this.buffer.Length - this.end);
#endif
			}

			private static void BlockCopy(char[] from, int fromIdx, char[] to, int toIdx, int len)
			{
				const int CHAR_SIZE = sizeof(char);

				System.Buffer.BlockCopy(from, fromIdx * CHAR_SIZE, to, toIdx * CHAR_SIZE, len * CHAR_SIZE);
			}

			public override string ToString()
			{
				return new string(this.buffer, this.Offset, this.end - this.Offset);
			}

			#region IList<char> Members

			int IList<char>.IndexOf(char item)
			{
				throw new NotSupportedException();
			}

			void IList<char>.Insert(int index, char item)
			{
				throw new NotSupportedException();
			}

			void IList<char>.RemoveAt(int index)
			{
				throw new NotSupportedException();
			}

			char IList<char>.this[int index]
			{
				get { return this[index]; }
				set { throw new NotImplementedException(); }
			}

			#endregion

			#region ICollection<char> Members

			void ICollection<char>.Add(char item)
			{
				throw new NotSupportedException();
			}

			void ICollection<char>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<char>.Contains(char item)
			{
				throw new NotSupportedException();
			}

			void ICollection<char>.CopyTo(char[] array, int arrayIndex)
			{
				throw new NotSupportedException();
			}

			int ICollection<char>.Count
			{
				get { return this.buffer.Length; }
			}

			bool ICollection<char>.IsReadOnly
			{
				get { return true; }
			}

			bool ICollection<char>.Remove(char item)
			{
				throw new NotSupportedException();
			}

			#endregion

			#region IEnumerable<char> Members

			IEnumerator<char> IEnumerable<char>.GetEnumerator()
			{
				return (this.buffer as IList<char>).GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.buffer.GetEnumerator();
			}

			#endregion
		}

		private sealed class LazyValueInfo : IValueInfo
		{
			private enum Kind : byte
			{
				Explicit = 0,
				QuotedString,
				String
			};

			private readonly JsonReader reader;
			private int jsonStart;
			private int jsonLen;
			private object value;
			private Kind valueKind;

			public bool HasValue { get; private set; }
			public object Raw
			{
				get
				{
					// eval lazy value
					if (this.valueKind == Kind.String)
						this.Raw = new string(this.reader.buffer.GetChars(), this.reader.buffer.Offset + this.jsonStart, this.jsonLen);
					else if (this.valueKind == Kind.QuotedString)
						this.Raw = JsonUtils.UnescapeBuffer(this.reader.buffer.GetChars(), this.reader.buffer.Offset + this.jsonStart, this.jsonLen);

					return this.value;
				}
				set
				{
					this.valueKind = Kind.Explicit;
					this.value = value;
					this.HasValue = true;
				}
			}
			public Type Type
			{
				get
				{
					if (this.valueKind != Kind.Explicit)
					{
						switch (this.reader.token)
						{
							case JsonToken.BeginArray:
								return typeof(List<object>);
							case JsonToken.Number:
								return typeof(double);
							case JsonToken.Member:
							case JsonToken.StringLiteral:
								return typeof(string);
							case JsonToken.DateTime:
								return typeof(DateTime);
							case JsonToken.Boolean:
								return typeof(bool);
						}
					}

					if (this.value != null)
						return this.value.GetType();
					else
						return typeof(object);
				}
			}
			public int LineNumber { get; private set; }
			public int ColumnNumber { get; private set; }
			public bool AsBoolean { get { return Convert.ToBoolean(this.Raw, this.reader.Context.Format); } }
			public byte AsByte { get { return Convert.ToByte(this.Raw, this.reader.Context.Format); } }
			public short AsInt16 { get { return Convert.ToInt16(this.Raw, this.reader.Context.Format); } }
			public int AsInt32 { get { return Convert.ToInt32(this.Raw, this.reader.Context.Format); } }
			public long AsInt64 { get { return Convert.ToInt64(this.Raw, this.reader.Context.Format); } }
			public sbyte AsSByte { get { return Convert.ToSByte(this.Raw, this.reader.Context.Format); } }
			public ushort AsUInt16 { get { return Convert.ToUInt16(this.Raw, this.reader.Context.Format); } }
			public uint AsUInt32 { get { return Convert.ToUInt32(this.Raw, this.reader.Context.Format); } }
			public ulong AsUInt64 { get { return Convert.ToUInt64(this.Raw, this.reader.Context.Format); } }
			public float AsSingle { get { return Convert.ToSingle(this.Raw, this.reader.Context.Format); } }
			public double AsDouble { get { return Convert.ToDouble(this.Raw, this.reader.Context.Format); } }
			public decimal AsDecimal { get { return Convert.ToDecimal(this.Raw, this.reader.Context.Format); } }
			public DateTime AsDateTime
			{
				get
				{
					if (this.Raw is DateTime) return (DateTime)this.Raw;
					else
						return DateTime.ParseExact(this.AsString, this.reader.Context.DateTimeFormats, this.reader.Context.Format,
							DateTimeStyles.AssumeUniversal);
				}
			}
			public string AsString
			{
				get
				{
					var raw = this.Raw;
					if (raw is string)
						return (string)raw;
					else if (raw is byte[])
						return Convert.ToBase64String((byte[])raw);
					else
						return Convert.ToString(raw, this.reader.Context.Format);
				}
			}

			public LazyValueInfo(JsonReader reader)
			{
				if (reader == null)
					throw new ArgumentNullException("reader");

				this.reader = reader;
			}

			public void ClearValue()
			{
				this.value = null;
				this.HasValue = false;
				this.valueKind = Kind.String;
			}

			public void SetBufferBounds(int start, int len)
			{
				if (start < 0)
					throw new ArgumentOutOfRangeException("start");
				if (len < 0)
					throw new ArgumentOutOfRangeException("len");

				this.LineNumber = this.reader.buffer.LineNumber;
				this.ColumnNumber = this.reader.buffer.ColumnNumber + start;
				this.jsonStart = start;
				this.jsonLen = len;
			}
			public void SetAsLazyString(bool quoted)
			{
				this.valueKind = quoted ? Kind.QuotedString : Kind.String;
				this.HasValue = true;
			}
		}

		private const char INSIGNIFICANT_TAB = '\t';
		private const char INSIGNIFICANT_SPACE = ' ';
		private const char INSIGNIFICANT_NEWLINE = '\n';
		private const char INSIGNIFICANT_RETURN = '\r';
		private const char INSIGNIFICANT_NAME_SEPARATOR = ':';
		private const char INSIGNIFICANT_VALUE_SEPARATOR = ',';
		private const char SIGNIFICANT_BEGIN_ARRAY = '[';
		private const char SIGNIFICANT_END_ARRAY = ']';
		private const char SIGNIFICANT_BEGIN_OBJECT = '{';
		private const char SIGNIFICANT_END_OBJECT = '}';
		private const char SIGNIFICANT_QUOTE = '\"';
		private const char SIGNIFICANT_QUOTE_ALT = '\'';

		private LazyValueInfo lazyValue;
		private JsonToken token;
		private readonly Buffer buffer;

		protected JsonReader(SerializationContext context, char[] buffer = null)
		{
			if (context == null) throw new ArgumentNullException("context");
			if (buffer != null && buffer.Length < 1024) throw new ArgumentOutOfRangeException("buffer", "Buffer should be at least 1024 chars long.");

			this.Context = context;
			this.lazyValue = new LazyValueInfo(this);
			this.buffer = new Buffer(this, buffer);
		}

		#region IJsonReader Members

		public SerializationContext Context { get; private set; }

		public IValueInfo Value
		{
			get
			{
				if (this.Token == JsonToken.None)
					this.NextToken();
				return this.lazyValue;
			}
		}

		public JsonToken Token
		{
			get
			{
				if (this.token == JsonToken.None)
					this.NextToken();

				return this.token;
			}
		}

		public object RawValue
		{
			get
			{
				if (this.token == JsonToken.None)
					this.NextToken();

				return this.Value.Raw;
			}
		}

		public long CharactersReaded
		{
			get { return this.buffer.CharactersReaded; }
		}

		public bool NextToken()
		{
			var start = 0;
			var len = 0;
			var quoted = false;
			var isMember = false;

			if (!this.NextLexeme(ref start, ref len, ref quoted, ref isMember)) // end of stream
			{
				this.token = JsonToken.EndOfStream;
				this.lazyValue.Raw = null;
				return false;
			}

			this.lazyValue.ClearValue();
			this.lazyValue.SetBufferBounds(start, len);
			if (len == 1 && !quoted && this.buffer[start] == SIGNIFICANT_BEGIN_ARRAY)
			{
				this.token = JsonToken.BeginArray;
			}
			else if (len == 1 && !quoted && this.buffer[start] == SIGNIFICANT_BEGIN_OBJECT)
			{
				this.token = JsonToken.BeginObject;
			}
			else if (len == 1 && !quoted && this.buffer[start] == SIGNIFICANT_END_ARRAY)
			{
				this.token = JsonToken.EndOfArray;
			}
			else if (len == 1 && !quoted && this.buffer[start] == SIGNIFICANT_END_OBJECT)
			{
				this.token = JsonToken.EndOfObject;
			}
			else if (len == 4 && !quoted && this.LookupAt(this.buffer, start, len, "null"))
			{
				this.token = JsonToken.Null;
			}
			else if (len == 4 && !quoted && this.LookupAt(this.buffer, start, len, "true"))
			{
				this.token = JsonToken.Boolean;
				this.lazyValue.Raw = true;
			}
			else if (len == 5 && !quoted && this.LookupAt(this.buffer, start, len, "false"))
			{
				this.token = JsonToken.Boolean;
				this.lazyValue.Raw = false;
			}
			else if (quoted && this.LookupAt(this.buffer, start, 6, "/Date(") && this.LookupAt(this.buffer, start + len - 2, 2, ")/"))
			{
				var ticks = JsonUtils.StringToInt64(this.buffer.GetChars(), this.buffer.Offset + start + 6, len - 8);

				this.token = JsonToken.DateTime;
				var dateTime = new DateTime(ticks * 0x2710L + JsonUtils.UnixEpochTicks, DateTimeKind.Utc);
				this.lazyValue.Raw = dateTime;
			}
			else if (!quoted && IsNumber(this.buffer, start, len))
			{
				this.token = JsonToken.Number;
				this.lazyValue.SetAsLazyString(false);
			}
			else
			{
				this.token = isMember ? JsonToken.Member : JsonToken.StringLiteral;
				this.lazyValue.SetAsLazyString(quoted);
			}

			this.buffer.FixateLater(start + len + (quoted ? 1 : 0));

			return true;
		}

		public bool IsEndOfStream()
		{
			return this.token == JsonToken.EndOfStream;
		}

		public void Reset()
		{
			this.buffer.Reset();
			if (this.token != JsonToken.EndOfStream)
			{
				this.lazyValue = new LazyValueInfo(this);
				this.token = JsonToken.None;
			}
		}

		#endregion

		private static bool IsNumber(Buffer buffer, int start, int len)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");


			const int INT_PART = 0;
			const int FRAC_PART = 1;
			const int EXP_PART = 2;
			const char POINT = '.';
			const char EXP = 'E';
			const char PLUS = '+';
			const char MINUS = '-';

			len = start + len;

			var part = INT_PART;

			for (var i = start; i < len; i++)
			{
				var ch = buffer[i];

				switch (part)
				{
					case INT_PART:
						if (ch == MINUS)
						{
							if (i != start)
								return false;
						}
#if !STRICT
						else if (ch == PLUS)
						{
							if (i != start)
								return false;
						}
#endif
						else if (ch == POINT)
						{
							if (i == start)
								return false; // decimal point as first character
							else
								part = FRAC_PART;
						}
						else if (char.ToUpper(ch) == EXP)
						{
							if (i == start)
								return false; // exp at first character
							else
								part = EXP_PART;
						}
						else if (!char.IsDigit(ch))
							return false; // non digit character in int part
						break;
					case FRAC_PART:
						if (char.ToUpper(ch) == EXP)
						{
							if (i == start)
								return false; // exp at first character
							else
								part = EXP_PART;
						}
						else if (!char.IsDigit(ch))
							return false; // non digit character in frac part
						break;
					case EXP_PART:
						if ((ch == PLUS || ch == MINUS))
						{
							if (char.ToUpper(buffer[i - 1]) != EXP)
								return false; // sign not at start of exp part
						}
						else if (!char.IsDigit(ch))
							return false; // non digit character in exp part
						break;
				}
			}
			return true;
		}
		private static bool IsInsignificantWhitespace(char symbol)
		{
			return symbol == INSIGNIFICANT_NEWLINE || symbol == INSIGNIFICANT_RETURN || symbol == INSIGNIFICANT_SPACE ||
				   symbol == INSIGNIFICANT_TAB;
		}
		private static bool IsInsignificant(char symbol)
		{
			return symbol == INSIGNIFICANT_NEWLINE || symbol == INSIGNIFICANT_RETURN || symbol == INSIGNIFICANT_SPACE ||
				   symbol == INSIGNIFICANT_TAB || symbol == INSIGNIFICANT_NAME_SEPARATOR || symbol == INSIGNIFICANT_VALUE_SEPARATOR;
		}
		private static bool IsLiteralTerminator(char ch, bool quoted, char quoteCh, bool escaped, bool eos, IJsonReader reader)
		{
			if (!escaped && quoted && ch == quoteCh)
				return true;
			else if (quoted && (ch == INSIGNIFICANT_NEWLINE || ch == INSIGNIFICANT_RETURN))
				throw JsonSerializationException.UnterminatedStringLiteral(reader);
			else if (eos)
			{
				if (quoted)
					throw JsonSerializationException.UnexpectedEndOfStream(reader);
				else
					return true;
			}
			else if (!quoted &&
					 (ch == SIGNIFICANT_BEGIN_ARRAY || ch == SIGNIFICANT_BEGIN_OBJECT || ch == SIGNIFICANT_END_ARRAY ||
					  ch == SIGNIFICANT_END_OBJECT || ch == INSIGNIFICANT_VALUE_SEPARATOR || ch == INSIGNIFICANT_NAME_SEPARATOR))
				return true;
			else if (!quoted && IsInsignificantWhitespace(ch))
				return true;

			return false;
		}
		private bool LookupAt(Buffer buffer, int start, int len, string matchString)
		{
			for (var i = 0; i < len; i++)
			{
				if (buffer[start + i] != matchString[i])
					return false;
			}
			return true;
		}
		private bool LookupAtSkipWhitespace(Buffer buffer, int start, int len, string matchString)
		{
			while (IsInsignificantWhitespace(buffer[start])) start++;

			for (var i = 0; i < len; i++)
			{
				if (buffer[start + i] != matchString[i])
					return false;
			}
			return true;
		}

		/// <summary>
		///     Get next lexeme from current buffer
		/// </summary>
		/// <param name="start">return position of returned lexeme in buffer</param>
		/// <param name="len">return size of returned lexeme</param>
		/// <param name="quoted">return true when string literal was quoted</param>
		/// <param name="isMember">is lexeme is object's member</param>
		/// <returns>Null in case of "end of stream", or character buffer with result</returns>
		private bool NextLexeme(ref int start, ref int len, ref bool quoted, ref bool isMember)
		{
			// apply 'lazy' fixation
			this.buffer.FixateNow();

			if (this.buffer.IsBeyondOfStream(0))
				return false;

			var position = 0;
			var ch = this.buffer[position];

			// skip insignificant characters
			while (!this.buffer.IsBeyondOfStream(position) && IsInsignificant(this.buffer[position])) position++;

			// we reached end of stream
			if (this.buffer.IsBeyondOfStream(position))
				return false;


			// tell buffer that significant characters starts here
			// this prevents buffer overgrow
			this.buffer.Fixate(position);
			position = 0;
			var literalStart = position;
			//

			ch = this.buffer[position];
			//
			// check for quote character
			var quoteCh = '\0';
			if (ch == SIGNIFICANT_QUOTE)
			{
				quoteCh = ch;
				quoted = true;
				position++;
				literalStart++;
			}
#if !STRICT
			else if (ch == SIGNIFICANT_QUOTE_ALT)
			{
				quoteCh = ch;
				quoted = true;
				position++;
				literalStart++;
			}
#endif
			var escaped = false; // is character is escaped
			var eos = false; // is end of stream
			do
			{
				eos = this.buffer.IsBeyondOfStream(position);
				escaped = ch == '\\';
				ch = this.buffer[position];
				position++;
			} while (!IsLiteralTerminator(ch, quoted, quoteCh, escaped, eos, this));

			var literalEnd = position - 1; // minus terminator character

			start = literalStart;
			len = literalEnd - literalStart;

			// special case - self terminated lexeme
			if (literalStart == literalEnd && !quoted)
				len = 1;

			isMember = this.LookupAtSkipWhitespace(this.buffer, literalEnd + (quoted ? 1 : 0), 1, ":");

			return true;
		}

		/// <summary>
		///     Fills buffer with new characters, staring from <paramref name="index" />
		/// </summary>
		/// <param name="buffer">Character buffer to fill</param>
		/// <param name="index">index from which to start</param>
		/// <returns>new buffer size</returns>
		protected abstract int FillBuffer(char[] buffer, int index);
	}
}
