using System;
using MiscUtil.Conversion;

namespace Colyseus.Schema
{
    public class Encoder
    {
        /*
		 * Singleton
		 */
        protected static Encoder Instance = new Encoder();

        public static Encoder GetInstance()
        {
            return Instance;
        }

        public Encoder()
        {

        }

        public byte[] getInitialBytesFromEncodedType(byte[] encodedType)
        {
            byte[] initialBytes = { Protocol.ROOM_DATA };

            if (encodedType.Length < 0x20)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] { (byte)(encodedType.Length | 0xa0) });
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
                throw new System.Exception("String too long");
            }

            return initialBytes;
        }

        private byte[] addByteToArray(byte[] byteArray, byte[] newBytes)
        {
            byte[] bytes = new byte[byteArray.Length + newBytes.Length];
            System.Buffer.BlockCopy(byteArray, 0, bytes, 0, byteArray.Length);
            System.Buffer.BlockCopy(newBytes, 0, bytes, byteArray.Length, newBytes.Length);
            return bytes;
        }

        private byte[] uint8(byte[] bytes, int value)
        {
            return addByteToArray(bytes, new byte[] { (byte)(value & 255) });
        }

        private byte[] uint16(byte[] bytes, int value)
        {
            var a1 = addByteToArray(bytes, new byte[] { (byte)(value & 255) });
            return addByteToArray(a1, new byte[] { (byte)((value >> 8) & 255) });
        }

        private byte[] uint32(byte[] bytes, int value)
        {
            var b4 = value >> 24;
            var b3 = value >> 16;
            var b2 = value >> 8;
            var b1 = value;
            var a1 = addByteToArray(bytes, new byte[] { (byte)(b1 & 255) });
            var a2 = addByteToArray(a1, new byte[] { (byte)(b2 & 255) });
            var a3 = addByteToArray(a2, new byte[] { (byte)(b3 & 255) });
            return addByteToArray(a3, new byte[] { (byte)(b4 & 255) });
        }
    }
}
