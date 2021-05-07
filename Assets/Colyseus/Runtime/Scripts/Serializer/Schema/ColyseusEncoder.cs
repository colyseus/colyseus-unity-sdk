using System;

// ReSharper disable InconsistentNaming

namespace Colyseus.Schema
{
    public class ColyseusEncoder
    {
        /// <summary>
        ///     Singleton instance
        /// </summary>
        protected static ColyseusEncoder Instance = new ColyseusEncoder();

        /// <summary>
        ///     Getter function for the singleton <see cref="Instance" />
        /// </summary>
        /// <returns>The singleton <see cref="Instance" /></returns>
        public static ColyseusEncoder GetInstance()
        {
            return Instance;
        }

        /// <summary>
        ///     Retrieves the initial bytes from <paramref name="encodedType" /> based on it's length
        /// </summary>
        /// <param name="encodedType">The incoming "type" encoded to a <see cref="byte" />[]</param>
        /// <returns>The important bytes we need based upon the incoming type</returns>
        /// <exception cref="Exception"></exception>
        public byte[] getInitialBytesFromEncodedType(byte[] encodedType)
        {
            byte[] initialBytes = {ColyseusProtocol.ROOM_DATA};

            if (encodedType.Length < 0x20)
            {
                initialBytes = addByteToArray(initialBytes, new[] {(byte) (encodedType.Length | 0xa0)});
            }
            else if (encodedType.Length < 0x100)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] {0xd9});
                initialBytes = uint8(initialBytes, encodedType.Length);
            }
            else if (encodedType.Length < 0x10000)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] {0xda});
                initialBytes = uint16(initialBytes, encodedType.Length);
            }
            else if (encodedType.Length < 0x7fffffff)
            {
                initialBytes = addByteToArray(initialBytes, new byte[] {0xdb});
                initialBytes = uint32(initialBytes, encodedType.Length);
            }
            else
            {
                throw new Exception("String too long");
            }

            return initialBytes;
        }

        private byte[] addByteToArray(byte[] byteArray, byte[] newBytes)
        {
            byte[] bytes = new byte[byteArray.Length + newBytes.Length];
            Buffer.BlockCopy(byteArray, 0, bytes, 0, byteArray.Length);
            Buffer.BlockCopy(newBytes, 0, bytes, byteArray.Length, newBytes.Length);
            return bytes;
        }

        private byte[] uint8(byte[] bytes, int value)
        {
            return addByteToArray(bytes, new[] {(byte) (value & 255)});
        }

        private byte[] uint16(byte[] bytes, int value)
        {
            byte[] a1 = addByteToArray(bytes, new[] {(byte) (value & 255)});
            return addByteToArray(a1, new[] {(byte) ((value >> 8) & 255)});
        }

        private byte[] uint32(byte[] bytes, int value)
        {
            int b4 = value >> 24;
            int b3 = value >> 16;
            int b2 = value >> 8;
            int b1 = value;
            byte[] a1 = addByteToArray(bytes, new[] {(byte) (b1 & 255)});
            byte[] a2 = addByteToArray(a1, new[] {(byte) (b2 & 255)});
            byte[] a3 = addByteToArray(a2, new[] {(byte) (b3 & 255)});
            return addByteToArray(a3, new[] {(byte) (b4 & 255)});
        }
    }
}