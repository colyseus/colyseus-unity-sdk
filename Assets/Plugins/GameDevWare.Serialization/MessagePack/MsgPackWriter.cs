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
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public class MsgPackWriter : IJsonWriter
	{
		private readonly SerializationContext context;
		private readonly Stream outputStream;
		private readonly byte[] buffer;
		private readonly EndianBitConverter bitConverter;
		private long bytesWritten;

		public MsgPackWriter(Stream stream, SerializationContext context)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (context == null) throw new ArgumentNullException("context");
			if (!stream.CanWrite) throw JsonSerializationException.StreamIsNotReadable();

			this.context = context;
			this.outputStream = stream;
			this.buffer = new byte[32];
			this.bitConverter = EndianBitConverter.Big;
			this.bytesWritten = 0;
		}

		public SerializationContext Context
		{
			get { return this.context; }
		}

		public long CharactersWritten
		{
			get { return this.bytesWritten; }
		}

		public void Flush()
		{
			this.outputStream.Flush();
		}

		public void Write(string value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}

			var bytes = this.context.Encoding.GetBytes(value);
			if (bytes.Length < 32)
			{
				var formatByte = (byte)(bytes.Length | (byte)MsgPackType.FixStrStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (bytes.Length <= byte.MaxValue)
			{
				this.WriteType(MsgPackType.Str8);
				this.buffer[0] = (byte)bytes.Length;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (bytes.Length <= ushort.MaxValue)
			{
				this.WriteType(MsgPackType.Str16);
				this.bitConverter.CopyBytes((ushort)bytes.Length, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.Str32);
				this.bitConverter.CopyBytes((uint)bytes.Length, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}

			this.outputStream.Write(bytes, 0, bytes.Length);
			this.bytesWritten += bytes.Length;
		}

		public void Write(JsonMember value)
		{
			var name = value.NameString;
			if (value.IsEscapedAndQuoted)
				name = JsonUtils.UnescapeAndUnquote(name);

			this.WriteString(name);
		}

		public void Write(int number)
		{
			if (number > -32 && number < 0)
			{
				var formatByte = (byte)((byte)Math.Abs(number) | (byte)MsgPackType.NegativeFixIntStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (number >= 0 && number < 128)
			{
				var formatByte = (byte)((byte)number | (byte)MsgPackType.PositiveFixIntStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (number <= sbyte.MaxValue && number >= sbyte.MinValue)
			{
				this.WriteType(MsgPackType.Int8);
				this.buffer[0] = (byte)(sbyte)number;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (number <= short.MaxValue && number >= short.MinValue)
			{
				this.WriteType(MsgPackType.Int16);
				this.bitConverter.CopyBytes((short)number, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.Int32);
				this.bitConverter.CopyBytes((int)number, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}
		}

		public void Write(uint number)
		{
			if (number < 128)
			{
				var formatByte = (byte)((byte)number | (byte)MsgPackType.PositiveFixIntStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (number <= byte.MaxValue)
			{
				this.WriteType(MsgPackType.UInt8);
				this.buffer[0] = (byte)number;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (number <= ushort.MaxValue)
			{
				this.WriteType(MsgPackType.UInt16);
				this.bitConverter.CopyBytes((ushort)number, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.UInt32);
				this.bitConverter.CopyBytes((uint)number, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}
		}

		public void Write(long number)
		{
			if (number <= int.MaxValue && number >= int.MinValue)
			{
				this.Write((int)number);
				return;
			}

			this.WriteType(MsgPackType.Int64);
			this.bitConverter.CopyBytes(number, this.buffer, 0);
			this.outputStream.Write(this.buffer, 0, 8);
			this.bytesWritten += 8;
		}

		public void Write(ulong number)
		{
			if (number <= uint.MaxValue)
			{
				this.Write((uint)number);
				return;
			}

			this.WriteType(MsgPackType.UInt64);
			this.bitConverter.CopyBytes(number, this.buffer, 0);
			this.outputStream.Write(this.buffer, 0, 8);
			this.bytesWritten += 8;
		}

		public void Write(float number)
		{
			this.WriteType(MsgPackType.Float32);
			this.bitConverter.CopyBytes(number, this.buffer, 0);
			this.outputStream.Write(this.buffer, 0, 4);
			this.bytesWritten += 4;
		}

		public void Write(double number)
		{
			this.WriteType(MsgPackType.Float64);
			this.bitConverter.CopyBytes(number, this.buffer, 0);
			this.outputStream.Write(this.buffer, 0, 8);
			this.bytesWritten += 8;
		}

		public void Write(decimal number)
		{
			var extensionData = this.GetWriteBuffer();
			var extensionType = default(sbyte);
			if (this.context.ExtensionTypeHandler.TryWrite(number, out extensionType, ref extensionData))
			{
				this.Write(extensionType, extensionData);
			}
			else
			{
				var decimalStr = number.ToString(null, this.context.Format);
				this.Write(decimalStr);
			}			
		}

		public void Write(bool value)
		{
			if (value)
				this.WriteType(MsgPackType.True);
			else
				this.WriteType(MsgPackType.False);
		}

		public void Write(DateTime dateTime)
		{
			var extensionData = this.GetWriteBuffer();
			var extensionType = default(sbyte);
			if (this.context.ExtensionTypeHandler.TryWrite(dateTime, out extensionType, ref extensionData))
			{
				this.Write(extensionType, extensionData);
			}
			else
			{
				if (dateTime.Kind == DateTimeKind.Unspecified)
					dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);

				var dateTimeFormat = this.Context.DateTimeFormats.FirstOrDefault() ?? "o";
				if (dateTimeFormat.IndexOf('z') >= 0 && dateTime.Kind != DateTimeKind.Local)
					dateTime = dateTime.ToLocalTime();

				var dateString = dateTime.ToString(dateTimeFormat, this.Context.Format);

				this.Write(dateString);
			}
		}

		public void Write(DateTimeOffset dateTimeOffset)
		{
			var extensionData = this.GetWriteBuffer();
			var extensionType = default(sbyte);
			if (this.context.ExtensionTypeHandler.TryWrite(dateTimeOffset, out extensionType, ref extensionData))
			{
				this.Write(extensionType, extensionData);
			}
			else
			{
				var dateTimeFormat = this.Context.DateTimeFormats.FirstOrDefault() ?? "o";
				var dateString = dateTimeOffset.ToString(dateTimeFormat, this.Context.Format);
				this.Write(dateString);
			}
		}

		public void Write(byte[] value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}

			if (value.Length < byte.MaxValue)
			{
				this.buffer[0] = (byte)MsgPackType.Bin8;
				this.buffer[1] = (byte)value.Length;
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else if (value.Length < ushort.MaxValue)
			{
				this.buffer[0] = (byte)MsgPackType.Bin16;
				this.bitConverter.CopyBytes(checked((ushort)value.LongLength), this.buffer, 1);
				this.outputStream.Write(this.buffer, 0, 3);
				this.bytesWritten += 3;
			}
			else
			{
				this.buffer[0] = (byte)MsgPackType.Bin32;
				this.bitConverter.CopyBytes(checked((uint)value.LongLength), this.buffer, 1);
				this.outputStream.Write(this.buffer, 0, 5);
				this.bytesWritten += 5;
			}
			this.outputStream.Write(value, 0, value.Length);
			this.bytesWritten += value.Length;
		}

		public void Write(sbyte type, ArraySegment<byte> data)
		{
			if (data.Array == null)
			{
				this.WriteNull();
				return;
			}

			if (data.Count == 1)
			{
				this.WriteType(MsgPackType.FixExt1);
			}
			else if (data.Count == 2)
			{
				this.WriteType(MsgPackType.FixExt2);
			}
			else if (data.Count == 4)
			{
				this.WriteType(MsgPackType.FixExt4);
			}
			else if (data.Count == 8)
			{
				this.WriteType(MsgPackType.FixExt8);
			}
			else if (data.Count == 16)
			{
				this.WriteType(MsgPackType.FixExt16);
			}
			else if (data.Count <= byte.MaxValue)
			{
				this.WriteType(MsgPackType.Ext8);
				this.buffer[0] = (byte)data.Count;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (data.Count <= ushort.MaxValue)
			{
				this.WriteType(MsgPackType.Ext16);
				this.bitConverter.CopyBytes((ushort)data.Count, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.Ext32);
				this.bitConverter.CopyBytes((uint)data.Count, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}

			// write extension type
			this.buffer[0] = unchecked((byte)type);
			this.outputStream.Write(this.buffer, 0, 1);
			this.bytesWritten += 1;

			this.outputStream.Write(data.Array, data.Offset, data.Count);
			this.bytesWritten += data.Count;
		}

		public void WriteObjectBegin(int numberOfMembers)
		{
			if (numberOfMembers < 0) throw new ArgumentOutOfRangeException("numberOfMembers");

			if (numberOfMembers < 16)
			{
				var formatByte = (byte)(numberOfMembers | (byte)MsgPackType.FixMapStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten += 1;
			}
			else if (numberOfMembers <= ushort.MaxValue)
			{
				this.WriteType(MsgPackType.Map16);
				this.bitConverter.CopyBytes((ushort)numberOfMembers, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.Map32);
				this.bitConverter.CopyBytes((int)numberOfMembers, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}
		}

		public void WriteObjectEnd()
		{
		}

		public void WriteArrayBegin(int numberOfMembers)
		{
			if (numberOfMembers < 0) throw new ArgumentOutOfRangeException("numberOfMembers");

			if (numberOfMembers < 16)
			{
				var formatByte = (byte)(numberOfMembers | (byte)MsgPackType.FixArrayStart);
				this.buffer[0] = formatByte;
				this.outputStream.Write(this.buffer, 0, 1);
				this.bytesWritten++;
			}
			else if (numberOfMembers <= ushort.MaxValue)
			{
				this.WriteType(MsgPackType.Array16);
				this.bitConverter.CopyBytes((ushort)numberOfMembers, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 2);
				this.bytesWritten += 2;
			}
			else
			{
				this.WriteType(MsgPackType.Array32);
				this.bitConverter.CopyBytes((int)numberOfMembers, this.buffer, 0);
				this.outputStream.Write(this.buffer, 0, 4);
				this.bytesWritten += 4;
			}
		}

		public void WriteArrayEnd()
		{
		}

		public void WriteNull()
		{
			this.WriteType(MsgPackType.Nil);
		}

		private void WriteType(MsgPackType token)
		{
			this.buffer[0] = (byte)token;
			this.outputStream.Write(this.buffer, 0, 1);
			this.bytesWritten++;
		}

		public void WriteJson(string jsonString)
		{
			throw new NotSupportedException();
		}

		public void WriteJson(char[] jsonString, int index, int charCount)
		{
			throw new NotSupportedException();
		}

		public void Reset()
		{
			this.bytesWritten = 0;
			Array.Clear(this.buffer, 0, this.buffer.Length);
		}

		internal ArraySegment<byte> GetWriteBuffer()
		{
			return new ArraySegment<byte>(this.buffer, 16, this.buffer.Length - 16);
		}
	}
}
