using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public sealed class DefaultMessagePackExtensionTypeHandler : MessagePackExtensionTypeHandler
	{
		public const int EXTENSION_TYPE_TIMESTAMP = -1;
		public const int EXTENSION_TYPE_DATE_TIME = 40;
		public const int EXTENSION_TYPE_DATE_TIME_OFFSET = 41;
		public const int EXTENSION_TYPE_DECIMAL = 42;
		public const int EXTENSION_TYPE_GUID = 43;
		public const int GUID_SIZE = 16;
		public const int DECIMAL_SIZE = 16;
		public const int DATE_TIME_SIZE = 16;
		public const int DATE_TIME_OFFSET_SIZE = 16;

		private static readonly Type[] DefaultExtensionTypes = new[] { typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid), typeof(DateTimeOffset), typeof(MessagePackTimestamp) };
		public static DefaultMessagePackExtensionTypeHandler Instance = new DefaultMessagePackExtensionTypeHandler(EndianBitConverter.Little);

		private readonly EndianBitConverter bitConverter;

		public override IEnumerable<Type> ExtensionTypes
		{
			get { return DefaultExtensionTypes; }
		}

		internal DefaultMessagePackExtensionTypeHandler(EndianBitConverter bitConverter)
		{
			if (bitConverter == null) throw new ArgumentNullException("bitConverter");

			this.bitConverter = bitConverter;
		}

		/// <inheritdoc />
		public override bool TryRead(sbyte type, ArraySegment<byte> data, out object value)
		{
			if (data.Array == null) throw new ArgumentNullException("data");

			value = default(object);
			switch (type)
			{
				case EXTENSION_TYPE_TIMESTAMP:
					unchecked
					{
						var seconds = 0L;
						var nanoSeconds = 0u;
						switch (data.Count)
						{
							case 4:
								seconds = this.bitConverter.ToInt32(data.Array, data.Offset);
								value = new MessagePackTimestamp(seconds, nanoSeconds);
								return true;
							case 8:
								var data64 = this.bitConverter.ToUInt64(data.Array, data.Offset);
								seconds = (int)(data64 & 0x00000003ffffffffL);
								nanoSeconds = (uint)(data64 >> 34 & uint.MaxValue);
								value = new MessagePackTimestamp(seconds, nanoSeconds);
								return true;
							case 12:
								nanoSeconds = this.bitConverter.ToUInt32(data.Array, data.Offset);
								seconds = this.bitConverter.ToInt64(data.Array, data.Offset + 4);
								value = new MessagePackTimestamp(seconds, nanoSeconds);
								return true;
							default:
								return false;
						}
					}
				case EXTENSION_TYPE_DATE_TIME:
					if (data.Count != DATE_TIME_SIZE)
						return false;
					var dateTime = new DateTime(this.bitConverter.ToInt64(data.Array, data.Offset + 1), (DateTimeKind)data.Array[data.Offset]);
					value = dateTime;
					return true;
				case EXTENSION_TYPE_DATE_TIME_OFFSET:
					if (data.Count != DATE_TIME_OFFSET_SIZE)
						return false;
					var offset = new TimeSpan(this.bitConverter.ToInt64(data.Array, data.Offset + 8));
					var ticks = this.bitConverter.ToInt64(data.Array, data.Offset);
					var dateTimeOffset = new DateTimeOffset(ticks, offset);
					value = dateTimeOffset;
					return true;
				case EXTENSION_TYPE_DECIMAL:
					if (data.Count != DECIMAL_SIZE)
						return false;
					var decimalValue = this.bitConverter.ToDecimal(data.Array, data.Offset);
					value = decimalValue;
					return true;
				case EXTENSION_TYPE_GUID:
					if (data.Count != GUID_SIZE)
						return false;

					var buffer = data.Array;
					var guidValue = new Guid
					(
							this.bitConverter.ToUInt32(buffer, data.Offset),
							this.bitConverter.ToUInt16(buffer, data.Offset + 4),
							this.bitConverter.ToUInt16(buffer, data.Offset + 6),
							buffer[data.Offset + 8],
							buffer[data.Offset + 9],
							buffer[data.Offset + 10],
							buffer[data.Offset + 11],
							buffer[data.Offset + 12],
							buffer[data.Offset + 13],
							buffer[data.Offset + 14],
							buffer[data.Offset + 15]
					);
					value = guidValue;
					return true;
				default:
					return false;
			}
		}
		/// <inheritdoc />
		public override bool TryWrite(object value, out sbyte type, ref ArraySegment<byte> data)
		{
			if (value == null)
			{
				type = 0;
				return false;
			}
			else if (value is DateTime)
			{
				type = EXTENSION_TYPE_DATE_TIME;
				if (data.Array == null || data.Count < DATE_TIME_SIZE)
					data = new ArraySegment<byte>(new byte[DATE_TIME_SIZE]);

				var dateTime = (DateTime)(object)value;
				Array.Clear(data.Array, data.Offset, DATE_TIME_SIZE);
				this.bitConverter.CopyBytes(dateTime.Ticks, data.Array, data.Offset + 1);
				data.Array[data.Offset] = (byte)dateTime.Kind;

				if (data.Count != DATE_TIME_SIZE)
					data = new ArraySegment<byte>(data.Array, data.Offset, DATE_TIME_SIZE);
				return true;
			}
			else if (value is DateTimeOffset)
			{
				type = EXTENSION_TYPE_DATE_TIME_OFFSET;
				if (data.Array == null || data.Count < DATE_TIME_OFFSET_SIZE)
					data = new ArraySegment<byte>(new byte[DATE_TIME_OFFSET_SIZE]);

				var dateTimeOffset = (DateTimeOffset)(object)value;
				this.bitConverter.CopyBytes(dateTimeOffset.DateTime.Ticks, data.Array, data.Offset);
				this.bitConverter.CopyBytes(dateTimeOffset.Offset.Ticks, data.Array, data.Offset + 8);

				if (data.Count != DATE_TIME_OFFSET_SIZE)
					data = new ArraySegment<byte>(data.Array, data.Offset, DATE_TIME_OFFSET_SIZE);
				return true;
			}
			else if (value is decimal)
			{

				type = EXTENSION_TYPE_DECIMAL;
				if (data.Array == null || data.Count < DECIMAL_SIZE)
					data = new ArraySegment<byte>(new byte[DECIMAL_SIZE]);

				var number = (decimal)(object)value;
				this.bitConverter.CopyBytes(number, data.Array, data.Offset);

				if (data.Count != DECIMAL_SIZE)
					data = new ArraySegment<byte>(data.Array, data.Offset, DECIMAL_SIZE);
				return true;
			}
			else if (value is Guid)
			{
				type = EXTENSION_TYPE_GUID;
				var guid = (Guid)(object)value;
				data = new ArraySegment<byte>(guid.ToByteArray());
				return true;
			}
			else if (value is MessagePackTimestamp)
			{
				type = EXTENSION_TYPE_TIMESTAMP;
				var timestamp = (MessagePackTimestamp)(object)value;

				unchecked
				{

					if (timestamp.Seconds <= int.MaxValue && timestamp.Seconds >= int.MinValue)
					{
						if (timestamp.NanoSeconds == 0)
						{
							const int TIMESTAMP_SIZE = 4;

							if (data.Array == null || data.Count < TIMESTAMP_SIZE)
								data = new ArraySegment<byte>(new byte[TIMESTAMP_SIZE]);

							// timestamp 32
							this.bitConverter.CopyBytes((int)timestamp.Seconds, data.Array, data.Offset);

							if (data.Count != TIMESTAMP_SIZE)
								data = new ArraySegment<byte>(data.Array, data.Offset, TIMESTAMP_SIZE);
						}
						else
						{
							const int TIMESTAMP_SIZE = 8;

							if (data.Array == null || data.Count < TIMESTAMP_SIZE)
								data = new ArraySegment<byte>(new byte[TIMESTAMP_SIZE]);

							var data64 = ((ulong)timestamp.NanoSeconds << 34) | ((ulong)timestamp.Seconds & uint.MaxValue);
							// timestamp 64
							this.bitConverter.CopyBytes(data64, data.Array, data.Offset);

							if (data.Count != TIMESTAMP_SIZE)
								data = new ArraySegment<byte>(data.Array, data.Offset, TIMESTAMP_SIZE);
						}
					}
					else
					{
						const int TIMESTAMP_SIZE = 12;

						if (data.Array == null || data.Count < TIMESTAMP_SIZE)
							data = new ArraySegment<byte>(new byte[TIMESTAMP_SIZE]);

						// timestamp 96
						this.bitConverter.CopyBytes(timestamp.NanoSeconds, data.Array, data.Offset);
						this.bitConverter.CopyBytes(timestamp.Seconds, data.Array, data.Offset + 4);

						if (data.Count != TIMESTAMP_SIZE)
							data = new ArraySegment<byte>(data.Array, data.Offset, TIMESTAMP_SIZE);
					}
				}
				return true;
			}
			else
			{
				type = default(sbyte);
				return false;
			}
		}
	}
}

