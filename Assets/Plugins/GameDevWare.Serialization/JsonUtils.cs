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
using System.Text;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	internal static class JsonUtils
	{
		private static readonly char[] ZerBuff = new char[] {'0', '0', '0', '0', '0', '0', '0', '0',};
		private static readonly char[] HexChar = "0123456789ABCDEF".ToCharArray();

		public static string UnescapeAndUnquote(string stringToUnescape)
		{
			if (stringToUnescape == null)
				throw new ArgumentNullException("stringToUnescape");


			var start = 0;
			var len = stringToUnescape.Length;

			if (stringToUnescape.Length > 0 && stringToUnescape[0] == '"')
			{
				start += 1;
				len -= 2;
			}

			return UnescapeBuffer(stringToUnescape.ToCharArray(), start, len);
		}

		public static string EscapeAndQuote(string stringToEscape)
		{
			if (stringToEscape == null)
				throw new ArgumentNullException("stringToEscape");


			var stringHasNonLatinCharacters = false;
			var newSize = stringToEscape.Length + 2;
			for (var i = 0; i < stringToEscape.Length; i++)
			{
				var charToCheck = stringToEscape[i];
				var isNonLatinOrSpecial = ((int) charToCheck < 32 || charToCheck == '\\' || charToCheck == '"');

				if (isNonLatinOrSpecial)
					newSize += 5; // encoded characters add 4 hex symbols and "u"

				stringHasNonLatinCharacters = stringHasNonLatinCharacters || isNonLatinOrSpecial;
			}

			// if it"s a latin - write as is
			if (!stringHasNonLatinCharacters)
				return string.Concat("\"", stringToEscape, "\"");

			// else tranform and write
			var sb = new StringBuilder(newSize);
			var hexBuff = new char[12]; // 4 for zeroes and 8 for number

			sb.Append('"');
			for (var i = 0; i < stringToEscape.Length; i++)
			{
				var charToCheck = stringToEscape[i];

				if ((int) charToCheck < 32 || charToCheck == '\\' || charToCheck == '"')
				{
					sb.Append("\\u");
					Buffer.BlockCopy(ZerBuff, 0, hexBuff, 0, sizeof (char)*8); // clear buffer with "0"
					var hexlen = UInt32ToHexBuffer((uint) charToCheck, hexBuff, 4);
					sb.Append(hexBuff, hexlen, 4);
				}
				else
					sb.Append(charToCheck);
			}
			sb.Append('"');

			return sb.ToString();
		}

		public static int EscapeBuffer(string value, ref int offset, char[] outputBuffer, int outputBufferOffset)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (offset < 0 || offset >= value.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (outputBuffer == null)
				throw new ArgumentNullException("outputBuffer");
			if (outputBufferOffset < 0 || outputBufferOffset >= outputBuffer.Length)
				throw new ArgumentOutOfRangeException("outputBufferOffset");


			const ushort LOWER_BOUND_CHAR = 32;
			const ushort QUOTE_CHAR = '\\';
			const ushort DOUBLE_QUOTE_CHAR = '"';

			var written = 0;
			for (; offset < value.Length; offset++)
			{
				var charCode = (ushort) value[offset];
				if (charCode < LOWER_BOUND_CHAR || charCode == QUOTE_CHAR || charCode == DOUBLE_QUOTE_CHAR)
				{
					if (outputBuffer.Length - outputBufferOffset < 6)
						return written;

					outputBuffer[outputBufferOffset++] = '\\';
					outputBuffer[outputBufferOffset++] = 'u';
					outputBufferOffset += UInt16ToPaddedHexBuffer(charCode, outputBuffer, outputBufferOffset);
					written += 6;
				}
				else
				{
					if (outputBuffer.Length - outputBufferOffset == 0)
						return written;

					// dont escape
					outputBuffer[outputBufferOffset++] = (char) charCode;
					written++;
				}
			}

			return written;
		}

		public static string UnescapeBuffer(char[] charsToUnescape, int start, int length)
		{
			if (charsToUnescape == null)
				throw new ArgumentNullException("charsToUnescape");
			if (start < 0 || start + length > charsToUnescape.Length)
				throw new ArgumentOutOfRangeException("start");


			var sb = new StringBuilder(length);
			var plainStart = start;
			var plainLen = 0;
			var end = start + length;
			for (var i = start; i < end; i++)
			{
				var ch = charsToUnescape[i];
				if (ch == '\\')
				{
					var seqLength = 1;
					// append unencoded chunk
					if (plainLen != 0)
					{
						sb.Append(charsToUnescape, plainStart, plainLen);
						plainLen = 0;
					}

					var seqKind = charsToUnescape[i + 1];
					switch (seqKind)
					{
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 't':
							sb.Append('\t');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case '\'':
							sb.Append('\'');
							break;
						case '\"':
							sb.Append('\"');
							break;
						// unicode symbol
						case 'u':
							sb.Append((char) HexStringToUInt32(charsToUnescape, i + 2, 4));
							seqLength = 5;
							break;
						// latin hex encoded symbol
						case 'x':
							sb.Append((char) HexStringToUInt32(charsToUnescape, i + 2, 2));
							seqLength = 3;
							break;
						// latin dec encoded symbol
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						case '8':
						case '9':
						case '0':
							sb.Append((char) StringToInt32(charsToUnescape, i + 1, 3));
							seqLength = 3;
							break;
						default:
#if STRICT
                            throw new Exceptions.UnknownEscapeSequence("\\" + seqKind.ToString(), null);
#else
							sb.Append(charsToUnescape[i + 1]);
							break;
#endif
					}

					// set next chunk start right after this escape
					plainStart = i + seqLength + 1;
					i += seqLength;
				}
				else
					plainLen++;
			}

			// append last unencoded chunk
			if (plainLen != 0)
				sb.Append(charsToUnescape, plainStart, plainLen);

			return sb.ToString();
		}

		public static uint HexStringToUInt32(char[] buffer, int start, int len)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const uint ZERO = (ushort) '0';
			const uint a = (ushort) 'a';
			const uint A = (ushort) 'A';

			var result = 0u;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				var d = 0u;
				if (c >= '0' && c <= '9')
					d = (c - ZERO);
				else if (c >= 'a' && c <= 'f')
					d = 10u + (c - a);
				else if (c >= 'A' && c <= 'F')
					d = 10u + (c - A);
				else
					throw new FormatException();

				result = 16u*result + d;
			}

			return result;
		}

		public static int UInt32ToHexBuffer(uint uvalue, char[] buffer, int start)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			var hex = HexChar;

			if (uvalue == 0)
			{
				buffer[start] = '0';
				return 1;
			}

			var length = 0;
			for (var i = 0; i < 8; i++)
			{
				var c = hex[((uvalue >> i*4) & 15u)];
				buffer[start + i] = c;
			}

			for (length = 8; length > 0; length--)
				if (buffer[start + length - 1] != '0')
					break;

			Array.Reverse(buffer, start, length);

			return length;
		}

		public static int UInt16ToPaddedHexBuffer(ushort uvalue, char[] buffer, int start)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			const int LENGTH = 4;
			const string HEX = "0123456789ABCDEF";

			var end = start + LENGTH;
			if (uvalue == 0)
			{
				for (var i = start; i < end; i++)
					buffer[i] = '0';
				return LENGTH;
			}

			for (var i = 0; i < LENGTH; i++)
			{
				var c = HEX[(int) ((uvalue >> i*4) & 15u)];
				buffer[end - i - 1] = c;
			}


			return LENGTH;
		}

		public static ushort PaddedHexStringToUInt16(char[] buffer, int start, int len)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const uint ZERO = (ushort) '0';
			const uint a = (ushort) 'a';
			const uint A = (ushort) 'A';

			var result = 0u;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				var d = 0u;
				if (c >= '0' && c <= '9')
					d = (c - ZERO);
				else if (c >= 'a' && c <= 'f')
					d = 10u + (c - a);
				else if (c >= 'A' && c <= 'F')
					d = 10u + (c - A);
				else
					throw new FormatException();

				result = 16u*result + d;
			}

			return checked((ushort) result);
		}

		public static long StringToInt64(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const ulong ZERO = (ushort) '0';

			var result = 0UL;
			var neg = false;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				if (i == 0 && c == '-')
				{
					neg = true;
					continue;
				}
				else if (c < '0' || c > '9')
					throw new FormatException();

				result = checked(10UL*result + (c - ZERO));
			}

			if (neg)
				return -(long) (result);
			else
				return (long) result;
		}

		public static int StringToInt32(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const uint ZERO = (ushort) '0';

			var result = 0u;
			var neg = false;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				if (i == 0 && c == '-')
				{
					neg = true;
					continue;
				}
				else if (c < '0' || c > '9')
					throw new FormatException();

				result = checked(10u*result + (c - ZERO));
			}

			if (neg)
				return -(int) (result);
			else
				return (int) result;
		}

		public static ulong StringToUInt64(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const ulong ZERO = (ushort) '0';

			var result = 0UL;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				if (c < '0' || c > '9')
					throw new FormatException();

				result = checked(10UL*result + (c - ZERO));
			}

			return result;
		}

		public static uint StringToUInt32(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			const uint ZERO = (ushort) '0';

			var result = 0U;
			for (var i = 0; i < len; i++)
			{
				var c = buffer[start + i];
				if (c < '0' || c > '9')
					throw new FormatException();

				result = checked(10*result + (c - ZERO));
			}

			return result;
		}

		public static double StringToDouble(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			/*
            const uint ZERO = (ushort)'0';
            char decimalSep = '.';

            var whole = 0UL;
            var fraction = 0U;
            var fracCount = 0;
            var neg = false;
            var decimals = false;

            for (var i = 0; i < len; i++)
            {
                var c = buffer[start + i];
                if (i == 0 && c == '-')
                {
                    neg = true;
                    continue;
                }
                else if (c == decimalSep)
                {
                    decimals = true;
                    continue;
                }
                else if (c < '0' || c > '9')
                    throw new FormatException();

                if (decimals)
                {
                    if (fracCount >= 9) // maximum precision 9 digits
                        break;
                    fraction = checked(10U * fraction + (c - ZERO));
                    fracCount++;
                }
                else
                    whole = checked(10UL * whole + (c - ZERO));
            }

            var result = checked((double)whole + (fraction / pow10d[fracCount]));

            if (neg) result = -result;

            return result;
            */

			return double.Parse(new string(buffer, start, len), formatProvider);
		}

		public static float StringToFloat(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			/*
            const uint ZERO = (ushort)'0';
            char decimalSep = '.';

            var whole = 0U;
            var fraction = 0U;
            var fracCount = 0;
            var neg = false;
            var decimals = false;

            for (var i = 0; i < len; i++)
            {
                var c = buffer[start + i];
                if (i == 0 && c == '-')
                {
                    neg = true;
                    continue;
                }
                else if (c == decimalSep)
                {
                    decimals = true;
                    continue;
                }
                else if (c < '0' || c > '9')
                    throw new FormatException();

                if (decimals)
                {
                    if (fracCount > 9) // maximum precision 9 digits
                        break;
                    fraction = checked(10U * fraction + (c - ZERO));
                    fracCount++;
                }
                else
                    whole = checked(10U * whole + (c - ZERO));
            }

            var result = checked((float)whole + (fraction / pow10s[fracCount]));

            if (neg) result = -result;

            return result;
            */

			return float.Parse(new string(buffer, start, len), formatProvider);
		}

		public static decimal StringToDecimal(char[] buffer, int start, int len, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (len < 0)
				throw new ArgumentOutOfRangeException("len");
			if (start + len > buffer.Length)
				throw new ArgumentOutOfRangeException();


			return decimal.Parse(new string(buffer, start, len), formatProvider);
		}

		public static int Int32ToBuffer(int value, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			const int ZERO = (ushort) '0';

			var idx = start;
			var neg = value < 0;
			// Take care of sign
			var uvalue = neg ? (uint) (-value) : (uint) value;
			// Conversion. Number is reversed.
			do buffer[idx++] = (char) (ZERO + (uvalue%10)); while ((uvalue /= 10) != 0);
			if (neg) buffer[idx++] = '-';

			var length = idx - start;
			// Reverse string
			Array.Reverse(buffer, start, length);

			return length;
		}

		public static int Int64ToBuffer(long value, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			const int ZERO = (ushort) '0';

			var idx = start;
			// Take care of sign
			var neg = (value < 0);
			var uvalue = neg ? (ulong) (-value) : (ulong) value;
			// Conversion. Number is reversed.
			do buffer[idx++] = (char) (ZERO + (uvalue%10)); while ((uvalue /= 10) != 0);
			if (neg) buffer[idx++] = '-';

			var length = idx - start;
			// Reverse string
			Array.Reverse(buffer, start, length);

			return length;
		}

		public static int UInt32ToBuffer(uint uvalue, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			const int ZERO = (ushort) '0';

			var idx = start;
			// Take care of sign
			// Conversion. Number is reversed.
			do buffer[idx++] = (char) (ZERO + (uvalue%10)); while ((uvalue /= 10) != 0);

			var length = idx - start;
			// Reverse string
			Array.Reverse(buffer, start, length);

			return length;
		}

		public static int UInt64ToBuffer(ulong uvalue, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			const ulong ZERO = (ulong) '0';

			var idx = start;
			// Conversion. Number is reversed.
			do buffer[idx++] = (char) (ZERO + (uvalue%10)); while ((uvalue /= 10) != 0UL);

			var length = idx - start;
			// Reverse string
			Array.Reverse(buffer, start, length);

			return length;
		}

		public static int SingleToBuffer(float value, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			var valueStr = value.ToString("R", formatProvider);
			valueStr.CopyTo(0, buffer, start, valueStr.Length);
			return valueStr.Length;
		}

		public static int DoubleToBuffer(double value, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (start < 0 || start >= buffer.Length)
				throw new ArgumentOutOfRangeException("start");


			var valueStr = value.ToString("R", formatProvider);
			valueStr.CopyTo(0, buffer, start, valueStr.Length);
			return valueStr.Length;
		}

		public static int DecimalToBuffer(decimal value, char[] buffer, int start, IFormatProvider formatProvider = null)
		{
			var valueStr = value.ToString(null, formatProvider);
			valueStr.CopyTo(0, buffer, start, valueStr.Length);
			return valueStr.Length;
		}

		private static bool LookupAt(char[] buffer, int start, int len, string matchString)
		{
			for (var i = 0; i < len; i++)
			{
				if (buffer[start + i] != matchString[i])
					return false;
			}
			return true;
		}
	}
}
