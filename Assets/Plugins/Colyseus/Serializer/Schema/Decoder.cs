using System;
using MiscUtil.Conversion;

namespace Colyseus.Schema
{
	public class Decoder
	{
		/*   
	     * Singleton
	     */
		protected static Decoder Instance = new Decoder();
		protected static LittleEndianBitConverter bitConverter = new LittleEndianBitConverter();
		public static Decoder GetInstance()
		{
			return Instance;
		}

		public Decoder()
		{
		}

		public object DecodePrimitiveType(string type, byte[] bytes, Iterator it)
		{
			if (type == "string")
			{
				return DecodeString(bytes, it);
			}
			else if (type == "number")
			{
				return DecodeNumber(bytes, it);
			}
			else if (type == "int8")
			{
				return DecodeInt8(bytes, it);
			}
			else if (type == "uint8")
			{
				return DecodeUint8(bytes, it);
			}
			else if (type == "int16")
			{
				return DecodeInt16(bytes, it);
			}
			else if (type == "uint16")
			{
				return DecodeUint16(bytes, it);
			}
			else if (type == "int32")
			{
				return DecodeInt32(bytes, it);
			}
			else if (type == "uint32")
			{
				return DecodeUint32(bytes, it);
			}
			else if (type == "int64")
			{
				return DecodeInt64(bytes, it);
			}
			else if (type == "uint64")
			{
				return DecodeUint64(bytes, it);
			}
			else if (type == "float32")
			{
				return DecodeFloat32(bytes, it);
			}
			else if (type == "float64")
			{
				return DecodeFloat64(bytes, it);
			}
			else if (type == "boolean")
			{
				return DecodeBoolean(bytes, it);
			}
			return null;
		}

		public float DecodeNumber(byte[] bytes, Iterator it)
		{
			byte prefix = bytes[it.Offset++];

			if (prefix < 0x80)
			{
				// positive fixint
				return prefix;

			}
			else if (prefix == 0xca)
			{
				// float 32
				return DecodeFloat32(bytes, it);

			}
			else if (prefix == 0xcb)
			{
				// float 64
				return (float) DecodeFloat64(bytes, it);

			}
			else if (prefix == 0xcc)
			{
				// uint 8
				return DecodeUint8(bytes, it);

			}
			else if (prefix == 0xcd)
			{
				// uint 16
				return DecodeUint16(bytes, it);

			}
			else if (prefix == 0xce)
			{
				// uint 32
				return DecodeUint32(bytes, it);

			}
			else if (prefix == 0xcf)
			{
				// uint 64
				return DecodeUint64(bytes, it);
			}
			else if (prefix == 0xd0)
			{
				// int 8
				return DecodeInt8(bytes, it);

			}
			else if (prefix == 0xd1)
			{
				// int 16
				return DecodeInt16(bytes, it);

			}
			else if (prefix == 0xd2)
			{
				// int 32
				return DecodeInt32(bytes, it);

			}
			else if (prefix == 0xd3)
			{
				// int 64
				return DecodeInt64(bytes, it);
			}
			else if (prefix > 0xdf)
			{
				// negative fixint
				return (0xff - prefix + 1) * -1;
			}

			return float.NaN;
		}

		public int DecodeInt8(byte[] bytes, Iterator it)
		{
			return ((int)DecodeUint8(bytes, it)) << 24 >> 24;
		}

		public uint DecodeUint8(byte[] bytes, Iterator it)
		{
			return bytes[it.Offset++];
		}

		public short DecodeInt16(byte[] bytes, Iterator it)
		{
			short value = bitConverter.ToInt16(bytes, it.Offset);
			it.Offset += 2;
			return value;
		}

		public ushort DecodeUint16(byte[] bytes, Iterator it)
		{
			ushort value = bitConverter.ToUInt16(bytes, it.Offset);
			it.Offset += 2;
			return value;
		}

		public int DecodeInt32(byte[] bytes, Iterator it)
		{
			int value = bitConverter.ToInt32(bytes, it.Offset);
			it.Offset += 4;
			return value;
		}

		public uint DecodeUint32(byte[] bytes, Iterator it)
		{
			uint value = bitConverter.ToUInt32(bytes, it.Offset);
			it.Offset += 4;
			return value;
		}

		public float DecodeFloat32(byte[] bytes, Iterator it)
		{
			float value = bitConverter.ToSingle(bytes, it.Offset);
			it.Offset += 4;
			return value;
		}

		public double DecodeFloat64(byte[] bytes, Iterator it)
		{
			double value = bitConverter.ToDouble(bytes, it.Offset);
			it.Offset += 8;
			return value;
		}

		public long DecodeInt64(byte[] bytes, Iterator it)
		{
			long value = bitConverter.ToInt64(bytes, it.Offset);
			it.Offset += 8;
			return value;
		}

		public ulong DecodeUint64(byte[] bytes, Iterator it)
		{
			ulong value = bitConverter.ToUInt64(bytes, it.Offset);
			it.Offset += 8;
			return value;
		}

		public bool DecodeBoolean(byte[] bytes, Iterator it)
		{
			return DecodeUint8(bytes, it) > 0;
		}

		public string DecodeString(byte[] bytes, Iterator it)
		{
			int prefix = bytes[it.Offset++];

			int length;
			if (prefix < 0xc0) 
			{
				// fixstr
				length = prefix & 0x1f;
			}
			else if (prefix == 0xd9) 
			{
				length = (int) DecodeUint8(bytes, it);
			}
			else if (prefix == 0xda)
			{
				length = (int) DecodeUint16(bytes, it);
			}
			else if (prefix == 0xdb)
			{
				length = (int) DecodeUint32(bytes, it);
			} 
			else
			{
				length = 0;
			}

			string str = System.Text.Encoding.UTF8.GetString(bytes, it.Offset, length);
			it.Offset += length;

			return str;
		}

		/*
	     * Bool checks
	     */
		public bool NilCheck(byte[] bytes, Iterator it)
		{
			return bytes[it.Offset] == (byte)SPEC.NIL;
		}

		public bool IndexChangeCheck(byte[] bytes, Iterator it)
		{
			return bytes[it.Offset] == (byte)SPEC.INDEX_CHANGE;
		}

		public bool NumberCheck(byte[] bytes, Iterator it)
		{
			byte prefix = bytes[it.Offset];
			return prefix < 0x80 || (prefix >= 0xca && prefix <= 0xd3);
		}

	}
}
