using System;
using System.Collections.Generic;
using System.Text;

namespace Colyseus.Schema.Utils
{
	public class Encode
	{
        //
        // Encode counterpart of Decode for the wire primitives the room layer
        // emits (little-endian, msgpack-style). Port of @colyseus/schema
        // src/encoding/encode.ts — the dynamic "number" codec and the
        // fixed-width primitives the input layer needs.
        //

        private const double MaxSafeInteger = 9007199254740991.0;

        /// <summary>
        ///     The schema dynamic "number" codec (msgpack-style, both signs +
        ///     floats). NaN encodes as 0; ±Infinity as ±MAX_SAFE_INTEGER;
        ///     fractional values as float32 when the f32 round-trip stays
        ///     within 1e-4, else float64.
        /// </summary>
        public static void Number(List<byte> bytes, double value)
        {
            if (double.IsNaN(value))
            {
                Number(bytes, 0);
                return;
            }
            if (double.IsInfinity(value))
            {
                Number(bytes, value > 0 ? MaxSafeInteger : -MaxSafeInteger);
                return;
            }

            // JS `value !== (value|0)`: fractional OR outside int32 range → float branch
            bool isInt32 = value == Math.Floor(value) && value >= -2147483648.0 && value <= 2147483647.0;
            if (!isInt32)
            {
                if (Math.Abs(value) <= 3.4028235e+38)
                {
                    float asF32 = (float)value;
                    // precision check — 1e-4 acceptable loss (mirrors the reference)
                    if (Math.Abs(Math.Abs(asF32) - Math.Abs(value)) < 1e-4)
                    {
                        bytes.Add(0xca);
                        Float32(bytes, asF32);
                        return;
                    }
                }
                bytes.Add(0xcb);
                Float64(bytes, value);
                return;
            }

            int v = (int)value;
            if (v >= 0)
            {
                if (v < 0x80)
                {
                    bytes.Add((byte)v);
                }
                else if (v < 0x100)
                {
                    bytes.Add(0xcc);
                    bytes.Add((byte)v);
                }
                else if (v < 0x10000)
                {
                    bytes.Add(0xcd);
                    Uint16(bytes, (ushort)v);
                }
                else
                {
                    bytes.Add(0xce);
                    Uint32(bytes, (uint)v);
                }
            }
            else
            {
                if (v >= -0x20)
                {
                    bytes.Add((byte)(0xe0 | (v + 0x20)));
                }
                else if (v >= -0x80)
                {
                    bytes.Add(0xd0);
                    bytes.Add((byte)(v & 0xFF));
                }
                else if (v >= -0x8000)
                {
                    bytes.Add(0xd1);
                    Uint16(bytes, (ushort)(v & 0xFFFF));
                }
                else
                {
                    bytes.Add(0xd2);
                    Uint32(bytes, (uint)v);
                }
            }
        }

        public static void Int8(List<byte> bytes, sbyte value) => bytes.Add((byte)value);
        public static void Uint8(List<byte> bytes, byte value) => bytes.Add(value);

        public static void Int16(List<byte> bytes, short value) => Uint16(bytes, (ushort)value);
        public static void Uint16(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)(value & 0xFF));
            bytes.Add((byte)((value >> 8) & 0xFF));
        }

        public static void Int32(List<byte> bytes, int value) => Uint32(bytes, (uint)value);
        public static void Uint32(List<byte> bytes, uint value)
        {
            bytes.Add((byte)(value & 0xFF));
            bytes.Add((byte)((value >> 8) & 0xFF));
            bytes.Add((byte)((value >> 16) & 0xFF));
            bytes.Add((byte)((value >> 24) & 0xFF));
        }

        public static void Int64(List<byte> bytes, long value) => Uint64(bytes, (ulong)value);
        public static void Uint64(List<byte> bytes, ulong value)
        {
            Uint32(bytes, (uint)(value & 0xFFFFFFFF));
            Uint32(bytes, (uint)(value >> 32));
        }

        public static void Float32(List<byte> bytes, float value)
        {
            bytes.AddRange(BitConverter.GetBytes(value)); // LE on all supported platforms
        }

        public static void Float64(List<byte> bytes, double value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        public static void Boolean(List<byte> bytes, bool value) => bytes.Add(value ? (byte)1 : (byte)0);

        /// <summary>fixstr / str8 / str16 / str32 with utf8 payload. null encodes as empty.</summary>
        public static void String(List<byte> bytes, string value)
        {
            if (value == null) { value = ""; }
            byte[] utf8 = Encoding.UTF8.GetBytes(value);

            if (utf8.Length < 0x20)
            {
                bytes.Add((byte)(utf8.Length | 0xa0));
            }
            else if (utf8.Length < 0x100)
            {
                bytes.Add(0xd9);
                bytes.Add((byte)utf8.Length);
            }
            else if (utf8.Length < 0x10000)
            {
                bytes.Add(0xda);
                Uint16(bytes, (ushort)utf8.Length);
            }
            else
            {
                bytes.Add(0xdb);
                Uint32(bytes, (uint)utf8.Length);
            }
            bytes.AddRange(utf8);
        }

        /// <summary>
        ///     Retrieves the initial bytes from <paramref name="encodedType" /> based on it's length
        /// </summary>
        /// <param name="encodedType">The incoming "type" encoded to a <see cref="byte" />[]</param>
        /// <returns>The important bytes we need based upon the incoming type</returns>
        /// <exception cref="Exception"></exception>
        public static byte[] getInitialBytesFromEncodedType(byte[] encodedType, byte protocol)
        {
            byte[] initialBytes = { protocol };

            if (encodedType.Length < 0x20)
            {
                initialBytes = addByteToArray(initialBytes, new[] { (byte)(encodedType.Length | 0xa0) });
            }
            else if (encodedType.Length < 0x100)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] { 0xd9 });
                initialBytes = uint8(initialBytes, encodedType.Length);
            }
            else if (encodedType.Length < 0x10000)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] { 0xda });
                initialBytes = uint16(initialBytes, encodedType.Length);
            }
            else if (encodedType.Length < 0x7fffffff)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] { 0xdb });
                initialBytes = uint32(initialBytes, encodedType.Length);
            }
            else
            {
                throw new Exception("String too long");
            }

            return initialBytes;
        }

        /// <summary>
        ///     The schema "number" codec for non-negative integers
        ///     (msgpack-style, little-endian): positive fixint &lt; 0x80, then
        ///     uint8 / uint16 / uint32 prefixes.
        /// </summary>
        public static byte[] EncodeUintNumber(uint value)
        {
            if (value < 0x80)
            {
                return new[] { (byte)value };
            }
            if (value < 0x100)
            {
                return new byte[] { 0xcc, (byte)value };
            }
            if (value < 0x10000)
            {
                return uint16(new byte[] { 0xcd }, (int)value);
            }
            return uint32(new byte[] { 0xce }, (int)value);
        }

        private static byte[] addByteToArray(byte[] byteArray, byte[] newBytes)
        {
            byte[] bytes = new byte[byteArray.Length + newBytes.Length];
            Buffer.BlockCopy(byteArray, 0, bytes, 0, byteArray.Length);
            Buffer.BlockCopy(newBytes, 0, bytes, byteArray.Length, newBytes.Length);
            return bytes;
        }

        private static byte[] uint8(byte[] bytes, int value)
        {
            return addByteToArray(bytes, new[] { (byte)(value & 255) });
        }

        private static byte[] uint16(byte[] bytes, int value)
        {
            byte[] a1 = addByteToArray(bytes, new[] { (byte)(value & 255) });
            return addByteToArray(a1, new[] { (byte)((value >> 8) & 255) });
        }

        private static byte[] uint32(byte[] bytes, int value)
        {
            int b4 = value >> 24;
            int b3 = value >> 16;
            int b2 = value >> 8;
            int b1 = value;
            byte[] a1 = addByteToArray(bytes, new[] { (byte)(b1 & 255) });
            byte[] a2 = addByteToArray(a1, new[] { (byte)(b2 & 255) });
            byte[] a3 = addByteToArray(a2, new[] { (byte)(b3 & 255) });
            return addByteToArray(a3, new[] { (byte)(b4 & 255) });
        }

    }

}

