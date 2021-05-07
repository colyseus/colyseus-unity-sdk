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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public class MsgPackReader : IJsonReader
	{
		private static readonly object TrueObject = true;
		private static readonly object FalseObject = false;

		internal class MsgPackValueInfo : IValueInfo
		{
			private readonly MsgPackReader reader;
			private object value;

			internal MsgPackValueInfo(MsgPackReader reader)
			{
				if (reader == null) throw new ArgumentNullException("reader");

				this.reader = reader;
			}

			public JsonToken Token { get; private set; }
			public bool HasValue { get; private set; }
			public object Raw
			{
				get { return this.value; }
				set
				{
					this.value = value;
					this.HasValue = true;
				}
			}
			public Type Type
			{
				get
				{
					if (this.HasValue && this.value != null)
						return this.value.GetType();
					else
					{
						switch (this.Token)
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
						return typeof(object);
					}
				}
			}
			public int LineNumber { get { return 0; } }
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
					if (this.Raw is DateTime)
						return (DateTime)this.Raw;
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

			public void Reset()
			{
				this.value = null;
				this.HasValue = false;
				this.Token = JsonToken.None;
				this.ColumnNumber = 0;
			}
			public void SetValue(object rawValue, JsonToken token, int position)
			{
				this.HasValue = token == JsonToken.Boolean || token == JsonToken.DateTime ||
					token == JsonToken.Member || token == JsonToken.Null ||
					token == JsonToken.Number || token == JsonToken.StringLiteral;
				this.value = rawValue;
				this.Token = token;
				this.ColumnNumber = position;
			}

			public override string ToString()
			{
				if (this.HasValue)
					return Convert.ToString(this.value);
				else
					return "<no value>";
			}
		}
		internal struct ClosingToken
		{
			public JsonToken Token;
			public long Counter;
		}

		private readonly Stream inputStream;
		private readonly byte[] buffer;
		private readonly EndianBitConverter bitConverter;
		private readonly Stack<ClosingToken> closingTokens;
		private int bufferOffset;
		private int bufferReaded;
		private int bufferAvailable;
		private bool isEndOfStream;
		private int totalBytesReaded;

		public SerializationContext Context { get; private set; }
		JsonToken IJsonReader.Token
		{
			get
			{
				if (this.Value.Token == JsonToken.None)
					this.NextToken();
				if (this.isEndOfStream)
					return JsonToken.EndOfStream;

				return this.Value.Token;
			}
		}
		object IJsonReader.RawValue
		{
			get
			{
				if (this.Value.Token == JsonToken.None)
					this.NextToken();

				return this.Value.Raw;
			}
		}
		IValueInfo IJsonReader.Value
		{
			get
			{
				if (this.Value.Token == JsonToken.None)
					this.NextToken();

				return this.Value;
			}
		}
		internal MsgPackValueInfo Value { get; private set; }

		public MsgPackReader(Stream stream, SerializationContext context, Endianness endianness = Endianness.BigEndian)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (context == null) throw new ArgumentNullException("context");
			if (!stream.CanRead) throw JsonSerializationException.StreamIsNotReadable();

			this.Context = context;
			this.inputStream = stream;
			this.buffer = new byte[8 * 1024]; // 8kb
			this.bufferOffset = 0;
			this.bufferReaded = 0;
			this.bufferAvailable = 0;
			this.bitConverter = endianness == Endianness.BigEndian ? EndianBitConverter.Big : (EndianBitConverter)EndianBitConverter.Little;
			this.closingTokens = new Stack<ClosingToken>();

			this.Value = new MsgPackValueInfo(this);
		}

		public bool NextToken()
		{
			this.Value.Reset();

			if (this.closingTokens.Count > 0 && this.closingTokens.Peek().Counter == 0)
			{
				var closingToken = this.closingTokens.Pop();
				this.Value.SetValue(null, closingToken.Token, this.totalBytesReaded);

				this.DecrementClosingTokenCounter();

				return true;
			}

			if (!this.ReadToBuffer(1, throwOnEos: false))
			{
				this.isEndOfStream = true;
				this.Value.SetValue(null, JsonToken.EndOfStream, this.totalBytesReaded);
				return false;
			}

			var pos = this.totalBytesReaded;
			var formatValue = this.buffer[this.bufferOffset];
			if (formatValue >= (byte)MsgPackType.FixArrayStart && formatValue <= (byte)MsgPackType.FixArrayEnd)
			{
				var arrayCount = formatValue - (byte)MsgPackType.FixArrayStart;

				this.closingTokens.Push(new ClosingToken { Token = JsonToken.EndOfArray, Counter = arrayCount + 1 });
				this.Value.SetValue(null, JsonToken.BeginArray, pos);
			}
			else if (formatValue >= (byte)MsgPackType.FixStrStart && formatValue <= (byte)MsgPackType.FixStrEnd)
			{
				var strCount = formatValue - (byte)MsgPackType.FixStrStart;
				var strBytes = this.ReadBytes(strCount);
				var strValue = this.Context.Encoding.GetString(strBytes.Array, strBytes.Offset, strBytes.Count);

				var strTokenType = JsonToken.StringLiteral;
				if (this.closingTokens.Count > 0)
				{
					var closingToken = this.closingTokens.Peek();
					if (closingToken.Token == JsonToken.EndOfObject && closingToken.Counter > 0 && closingToken.Counter % 2 == 0)
						strTokenType = JsonToken.Member;
				}
				this.Value.SetValue(strValue, strTokenType, pos);
			}
			else if (formatValue >= (byte)MsgPackType.FixMapStart && formatValue <= (byte)MsgPackType.FixMapEnd)
			{
				var mapCount = formatValue - (byte)MsgPackType.FixMapStart;
				this.closingTokens.Push(new ClosingToken { Token = JsonToken.EndOfObject, Counter = mapCount * 2 + 1 });
				this.Value.SetValue(null, JsonToken.BeginObject, pos);
			}
			else if (formatValue >= (byte)MsgPackType.NegativeFixIntStart)
			{
				var value = unchecked((sbyte)formatValue);
				this.Value.SetValue(value, JsonToken.Number, pos);
			}
			else if (formatValue <= (byte)MsgPackType.PositiveFixIntEnd)
			{
				var value = unchecked((byte)formatValue);
				this.Value.SetValue(value, JsonToken.Number, pos);
			}
			else
			{
				switch ((MsgPackType)formatValue)
				{
					case MsgPackType.Nil:
						this.Value.SetValue(null, JsonToken.Null, pos);
						break;
					case MsgPackType.Array16:
					case MsgPackType.Array32:
						var arrayCount = 0L;
						if (formatValue == (int)MsgPackType.Array16)
						{
							this.ReadToBuffer(2, throwOnEos: true);
							arrayCount = this.bitConverter.ToUInt16(this.buffer, this.bufferOffset);
						}
						else if (formatValue == (int)MsgPackType.Array32)
						{
							this.ReadToBuffer(4, throwOnEos: true);
							arrayCount = this.bitConverter.ToUInt32(this.buffer, this.bufferOffset);
						}
						this.closingTokens.Push(new ClosingToken { Token = JsonToken.EndOfArray, Counter = arrayCount + 1 });
						this.Value.SetValue(null, JsonToken.BeginArray, pos);
						break;
					case MsgPackType.Map16:
					case MsgPackType.Map32:
						var mapCount = 0L;
						if (formatValue == (int)MsgPackType.Map16)
						{
							this.ReadToBuffer(2, throwOnEos: true);
							mapCount = this.bitConverter.ToUInt16(this.buffer, this.bufferOffset);
						}
						else if (formatValue == (int)MsgPackType.Map32)
						{
							this.ReadToBuffer(4, throwOnEos: true);
							mapCount = this.bitConverter.ToUInt32(this.buffer, this.bufferOffset);
						}
						this.closingTokens.Push(new ClosingToken { Token = JsonToken.EndOfObject, Counter = mapCount * 2 + 1 });
						this.Value.SetValue(null, JsonToken.BeginObject, pos);
						break;
					case MsgPackType.Str16:
					case MsgPackType.Str32:
					case MsgPackType.Str8:
						var strBytesCount = 0L;
						if (formatValue == (int)MsgPackType.Str8)
						{
							this.ReadToBuffer(1, throwOnEos: true);
							strBytesCount = this.buffer[this.bufferOffset];
						}
						else if (formatValue == (int)MsgPackType.Str16)
						{
							this.ReadToBuffer(2, throwOnEos: true);
							strBytesCount = this.bitConverter.ToUInt16(this.buffer, this.bufferOffset);
						}
						else if (formatValue == (int)MsgPackType.Str32)
						{
							this.ReadToBuffer(4, throwOnEos: true);
							strBytesCount = this.bitConverter.ToUInt32(this.buffer, this.bufferOffset);
						}

						var strTokenType = JsonToken.StringLiteral;
						if (this.closingTokens.Count > 0)
						{
							var closingToken = this.closingTokens.Peek();
							if (closingToken.Token == JsonToken.EndOfObject && closingToken.Counter > 0 && closingToken.Counter % 2 == 0)
								strTokenType = JsonToken.Member;
						}

						var strBytes = this.ReadBytes(strBytesCount);
						var strValue = this.Context.Encoding.GetString(strBytes.Array, strBytes.Offset, strBytes.Count);
						this.Value.SetValue(strValue, strTokenType, pos);
						break;
					case MsgPackType.Bin32:
					case MsgPackType.Bin16:
					case MsgPackType.Bin8:
						var bytesCount = 0L;
						if (formatValue == (int)MsgPackType.Bin8)
						{
							this.ReadToBuffer(1, throwOnEos: true);
							bytesCount = this.buffer[this.bufferOffset];
						}
						else if (formatValue == (int)MsgPackType.Bin16)
						{
							this.ReadToBuffer(2, throwOnEos: true);
							bytesCount = this.bitConverter.ToUInt16(this.buffer, this.bufferOffset);
						}
						else if (formatValue == (int)MsgPackType.Bin32)
						{
							this.ReadToBuffer(4, throwOnEos: true);
							bytesCount = this.bitConverter.ToUInt32(this.buffer, this.bufferOffset);
						}

						var bytes = this.ReadBytes(bytesCount, forceNewBuffer: true);
						this.Value.SetValue(bytes.Array, JsonToken.StringLiteral, pos);
						break;
					case MsgPackType.FixExt1:
					case MsgPackType.FixExt16:
					case MsgPackType.FixExt2:
					case MsgPackType.FixExt4:
					case MsgPackType.FixExt8:
					case MsgPackType.Ext32:
					case MsgPackType.Ext16:
					case MsgPackType.Ext8:
						var dataCount = 0L;
						if (formatValue == (int)MsgPackType.FixExt1)
							dataCount = 1;
						else if (formatValue == (int)MsgPackType.FixExt2)
							dataCount = 2;
						else if (formatValue == (int)MsgPackType.FixExt4)
							dataCount = 4;
						else if (formatValue == (int)MsgPackType.FixExt8)
							dataCount = 8;
						else if (formatValue == (int)MsgPackType.FixExt16)
							dataCount = 16;
						if (formatValue == (int)MsgPackType.Ext8)
						{
							this.ReadToBuffer(1, throwOnEos: true);
							dataCount = this.buffer[this.bufferOffset];
						}
						else if (formatValue == (int)MsgPackType.Ext16)
						{
							this.ReadToBuffer(2, throwOnEos: true);
							dataCount = this.bitConverter.ToUInt16(this.buffer, this.bufferOffset);
						}
						else if (formatValue == (int)MsgPackType.Ext32)
						{
							this.ReadToBuffer(4, throwOnEos: true);
							dataCount = this.bitConverter.ToUInt32(this.buffer, this.bufferOffset);
						}

						this.ReadToBuffer(1, true);
						var extensionType = unchecked((sbyte)this.buffer[this.bufferOffset]);

						var data = this.ReadBytes(dataCount);
						this.Value.SetValue(this.ReadExtensionType(extensionType, data), JsonToken.StringLiteral, pos);
						break;
					case MsgPackType.False:
						this.Value.SetValue(FalseObject, JsonToken.Boolean, pos);
						break;
					case MsgPackType.True:
						this.Value.SetValue(TrueObject, JsonToken.Boolean, pos);
						break;
					case MsgPackType.Float32:
						this.ReadToBuffer(4, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToSingle(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.Float64:
						this.ReadToBuffer(8, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToDouble(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.Int8:
						this.ReadToBuffer(1, throwOnEos: true);
						this.Value.SetValue(unchecked((sbyte)this.buffer[this.bufferOffset]), JsonToken.Number, pos);
						break;
					case MsgPackType.Int16:
						this.ReadToBuffer(2, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToInt16(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.Int32:
						this.ReadToBuffer(4, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToInt32(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.Int64:
						this.ReadToBuffer(8, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToInt64(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.UInt8:
						this.ReadToBuffer(1, throwOnEos: true);
						this.Value.SetValue(this.buffer[this.bufferOffset], JsonToken.Number, pos);
						break;
					case MsgPackType.UInt16:
						this.ReadToBuffer(2, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToUInt16(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.UInt32:
						this.ReadToBuffer(4, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToUInt32(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.UInt64:
						this.ReadToBuffer(8, throwOnEos: true);
						this.Value.SetValue(this.bitConverter.ToUInt64(this.buffer, this.bufferOffset), JsonToken.Number, pos);
						break;
					case MsgPackType.PositiveFixIntStart:
					case MsgPackType.PositiveFixIntEnd:
					case MsgPackType.FixMapStart:
					case MsgPackType.FixMapEnd:
					case MsgPackType.FixArrayStart:
					case MsgPackType.FixArrayEnd:
					case MsgPackType.FixStrStart:
					case MsgPackType.FixStrEnd:
					case MsgPackType.Unused:
					case MsgPackType.NegativeFixIntStart:
					case MsgPackType.NegativeFixIntEnd:
					default:
						throw new UnknownMsgPackFormatException(formatValue);
				}
			}

			this.DecrementClosingTokenCounter();

			return true;
		}
		public void Reset()
		{
			Array.Clear(this.buffer, 0, this.buffer.Length);
			this.bufferOffset = 0;
			this.bufferAvailable = 0;
			this.bufferReaded = 0;
			this.totalBytesReaded = 0;
			this.Value.Reset();
		}
		public bool IsEndOfStream()
		{
			return this.isEndOfStream;
		}

		private bool ReadToBuffer(int bytesRequired, bool throwOnEos)
		{
			this.bufferAvailable -= this.bufferReaded;
			this.bufferOffset += this.bufferReaded;
			this.bufferReaded = 0;

			if (this.bufferAvailable < bytesRequired)
			{
				if (this.bufferAvailable > 0)
					Buffer.BlockCopy(this.buffer, this.bufferOffset, this.buffer, 0, this.bufferAvailable);

				this.bufferOffset = 0;
				while (this.bufferAvailable < bytesRequired)
				{
					var read = this.inputStream.Read(this.buffer, this.bufferAvailable, this.buffer.Length - this.bufferAvailable);
					this.bufferAvailable += read;

					if (read != 0 || this.bufferAvailable >= bytesRequired)
						continue;

					if (throwOnEos)
						JsonSerializationException.UnexpectedEndOfStream(this);
					else
						return false;
				}
			}

			this.bufferReaded = bytesRequired;
			this.totalBytesReaded += bytesRequired;
			return true;
		}
		private ArraySegment<byte> ReadBytes(long bytesRequired, bool forceNewBuffer = false)
		{
			if (bytesRequired > int.MaxValue) throw new ArgumentOutOfRangeException("bytesRequired");

			this.bufferAvailable -= this.bufferReaded;
			this.bufferOffset += this.bufferReaded;
			this.bufferReaded = 0;

			if (this.bufferAvailable >= bytesRequired && !forceNewBuffer)
			{
				var bytes = new ArraySegment<byte>(this.buffer, this.bufferOffset, (int)bytesRequired);

				this.bufferAvailable -= (int)bytesRequired;
				this.bufferOffset += (int)bytesRequired;
				this.totalBytesReaded += (int)bytesRequired;

				return bytes;
			}
			else
			{
				var bytes = new byte[bytesRequired];
				var bytesOffset = 0;
				if (this.bufferAvailable > 0 && bytesOffset < bytes.Length)
				{
					var bytesToCopy = Math.Min(bytes.Length - bytesOffset, this.bufferAvailable);
					Buffer.BlockCopy(this.buffer, this.bufferOffset, bytes, bytesOffset, bytesToCopy);

					bytesOffset += bytesToCopy;
					this.bufferOffset += bytesToCopy;

					this.bufferAvailable -= bytesToCopy;
					this.totalBytesReaded += bytesToCopy;
				}

				if (this.bufferAvailable == 0)
					this.bufferOffset = 0;

				while (bytesOffset < bytes.Length)
				{
					var read = this.inputStream.Read(bytes, bytesOffset, bytes.Length - bytesOffset);

					bytesOffset += read;
					this.totalBytesReaded += read;

					if (read == 0 && bytesOffset < bytes.Length)
						throw JsonSerializationException.UnexpectedEndOfStream(this);
				}

				return new ArraySegment<byte>(bytes, 0, bytes.Length);
			}
		}
		private object ReadExtensionType(sbyte type, ArraySegment<byte> data)
		{
			var value = default(object);
			if (this.Context.ExtensionTypeHandler.TryRead(type, data, out value))
			{
				return value;
			}
			else
			{
				if (ReferenceEquals(data.Array, this.buffer))
					data = new ArraySegment<byte>((byte[])data.Array.Clone(), data.Offset, data.Count);

				return new MessagePackExtensionType(type, data);
			}
		}

		private void DecrementClosingTokenCounter()
		{
			if (this.closingTokens.Count > 0)
			{
				var closingToken = this.closingTokens.Pop();
				closingToken.Counter--;
				this.closingTokens.Push(closingToken);
			}
		}
	}
}
