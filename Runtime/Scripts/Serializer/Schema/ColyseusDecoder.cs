using System;
using System.Text;
using MiscUtil.Conversion;

namespace Colyseus.Schema
{
    /// <summary>
    ///     Utility class for decoding values from the server
    /// </summary>
    public class ColyseusDecoder
    {
        /// <summary>
        ///     The bit converter used to decode data
        /// </summary>
        public static LittleEndianBitConverter bitConverter = new LittleEndianBitConverter();

        /// <summary>
        ///     Singleton instance
        /// </summary>
        protected static ColyseusDecoder Instance = new ColyseusDecoder();

        /// <summary>
        ///     Getter function for the singleton <see cref="Instance" />
        /// </summary>
        /// <returns>The singleton <see cref="Instance" /></returns>
        public static ColyseusDecoder GetInstance()
        {
            return Instance;
        }

        /// <summary>
        ///     Decodes incoming data into an <see cref="object" /> based off of the <paramref name="type" /> provided
        /// </summary>
        /// <param name="type">What type of <see cref="object" /> we expect this data to be.
        ///     <para>Will determine the Decode method used</para>
        /// </param>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns>A decoded <see cref="object" /> that has been decoded with a <paramref name="type" /> specified method</returns>
        public object DecodePrimitiveType(string type, byte[] bytes, Iterator it)
        {
            if (type == "string")
            {
                return DecodeString(bytes, it);
            }

            if (type == "number")
            {
                return DecodeNumber(bytes, it);
            }

            if (type == "int8")
            {
                return DecodeInt8(bytes, it);
            }

            if (type == "uint8")
            {
                return DecodeUint8(bytes, it);
            }

            if (type == "int16")
            {
                return DecodeInt16(bytes, it);
            }

            if (type == "uint16")
            {
                return DecodeUint16(bytes, it);
            }

            if (type == "int32")
            {
                return DecodeInt32(bytes, it);
            }

            if (type == "uint32")
            {
                return DecodeUint32(bytes, it);
            }

            if (type == "int64")
            {
                return DecodeInt64(bytes, it);
            }

            if (type == "uint64")
            {
                return DecodeUint64(bytes, it);
            }

            if (type == "float32")
            {
                return DecodeFloat32(bytes, it);
            }

            if (type == "float64")
            {
                return DecodeFloat64(bytes, it);
            }

            if (type == "boolean")
            {
                return DecodeBoolean(bytes, it);
            }

            return null;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a <see cref="float" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a <see cref="float" /></returns>
        public float DecodeNumber(byte[] bytes, Iterator it)
        {
            byte prefix = bytes[it.Offset++];

            if (prefix < 0x80)
            {
                // positive fixint
                return prefix;
            }

            if (prefix == 0xca)
            {
                // float 32
                return DecodeFloat32(bytes, it);
            }

            if (prefix == 0xcb)
            {
                // float 64
                return (float) DecodeFloat64(bytes, it);
            }

            if (prefix == 0xcc)
            {
                // uint 8
                return DecodeUint8(bytes, it);
            }

            if (prefix == 0xcd)
            {
                // uint 16
                return DecodeUint16(bytes, it);
            }

            if (prefix == 0xce)
            {
                // uint 32
                return DecodeUint32(bytes, it);
            }

            if (prefix == 0xcf)
            {
                // uint 64
                return DecodeUint64(bytes, it);
            }

            if (prefix == 0xd0)
            {
                // int 8
                return DecodeInt8(bytes, it);
            }

            if (prefix == 0xd1)
            {
                // int 16
                return DecodeInt16(bytes, it);
            }

            if (prefix == 0xd2)
            {
                // int 32
                return DecodeInt32(bytes, it);
            }

            if (prefix == 0xd3)
            {
                // int 64
                return DecodeInt64(bytes, it);
            }

            if (prefix > 0xdf)
            {
                // negative fixint
                return (0xff - prefix + 1) * -1;
            }

            return float.NaN;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into an 8-bit <see cref="int" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into an 8-bit <see cref="int" /></returns>
        public sbyte DecodeInt8(byte[] bytes, Iterator it)
        {
            return Convert.ToSByte((DecodeUint8(bytes, it) << 24) >> 24);
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into an 8-bit <see cref="uint" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into an 8-bit <see cref="uint" /></returns>
        public byte DecodeUint8(byte[] bytes, Iterator it)
        {
            return bytes[it.Offset++];
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 16-bit <see cref="int" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 16-bit <see cref="int" /></returns>
        public short DecodeInt16(byte[] bytes, Iterator it)
        {
            short value = bitConverter.ToInt16(bytes, it.Offset);
            it.Offset += 2;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 16-bit <see cref="uint" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 16-bit <see cref="uint" /></returns>
        public ushort DecodeUint16(byte[] bytes, Iterator it)
        {
            ushort value = bitConverter.ToUInt16(bytes, it.Offset);
            it.Offset += 2;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 32-bit <see cref="int" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 32-bit <see cref="int" /></returns>
        public int DecodeInt32(byte[] bytes, Iterator it)
        {
            int value = bitConverter.ToInt32(bytes, it.Offset);
            it.Offset += 4;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 32-bit <see cref="uint" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 32-bit <see cref="uint" /></returns>
        public uint DecodeUint32(byte[] bytes, Iterator it)
        {
            uint value = bitConverter.ToUInt32(bytes, it.Offset);
            it.Offset += 4;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 32-bit <see cref="float" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 32-bit <see cref="float" /></returns>
        public float DecodeFloat32(byte[] bytes, Iterator it)
        {
            float value = bitConverter.ToSingle(bytes, it.Offset);
            it.Offset += 4;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 64-bit <see cref="float" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 64-bit <see cref="float" /></returns>
        public double DecodeFloat64(byte[] bytes, Iterator it)
        {
            double value = bitConverter.ToDouble(bytes, it.Offset);
            it.Offset += 8;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 64-bit <see cref="int" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 64-bit <see cref="int" /></returns>
        public long DecodeInt64(byte[] bytes, Iterator it)
        {
            long value = bitConverter.ToInt64(bytes, it.Offset);
            it.Offset += 8;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a 64-bit <see cref="uint" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a 64-bit <see cref="uint" /></returns>
        public ulong DecodeUint64(byte[] bytes, Iterator it)
        {
            ulong value = bitConverter.ToUInt64(bytes, it.Offset);
            it.Offset += 8;
            return value;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a <see cref="bool" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a <see cref="bool" /></returns>
        public bool DecodeBoolean(byte[] bytes, Iterator it)
        {
            return DecodeUint8(bytes, it) > 0;
        }

        /// <summary>
        ///     Decode method to decode <paramref name="bytes" /> into a <see cref="string" />
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns><paramref name="bytes" /> decoded into a <see cref="string" /></returns>
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
                length = DecodeUint16(bytes, it);
            }
            else if (prefix == 0xdb)
            {
                length = (int) DecodeUint32(bytes, it);
            }
            else
            {
                length = 0;
            }

            string str = Encoding.UTF8.GetString(bytes, it.Offset, length);
            it.Offset += length;

            return str;
        }

        /// <summary>
        ///     Checks if
        ///     <code>bytes[it.Offset] == (byte)SPEC.SWITCH_TO_STRUCTURE</code>
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns>
        ///     True if the current <see cref="Iterator.Offset" /> works with this array of <paramref name="bytes" />, false
        ///     otherwise
        /// </returns>
        public bool SwitchStructureCheck(byte[] bytes, Iterator it)
        {
            return bytes[it.Offset] == (byte) SPEC.SWITCH_TO_STRUCTURE;
        }

        /// <summary>
        ///     Checks if the incoming <paramref name="bytes" /> is a number
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it">The iterator who's <see cref="Iterator.Offset" /> will be used to Decode the data</param>
        /// <returns>True if <paramref name="bytes" /> can be resolved into a number, false otherwise</returns>
        public bool NumberCheck(byte[] bytes, Iterator it)
        {
            byte prefix = bytes[it.Offset];
            return prefix < 0x80 || prefix >= 0xca && prefix <= 0xd3;
        }
    }
}