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
using GameDevWare.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	/// <summary>
	///     Representation of extension types in Message Pack. This type is immutable by design
	/// </summary>
	[TypeSerializer(typeof(MsgPackExtensionTypeSerializer))]
	public sealed class MessagePackExtensionType : IEquatable<MessagePackExtensionType>, IComparable, IComparable<MessagePackExtensionType>
	{
		private static readonly byte[] EmptyBytes = new byte[0];

		private readonly ArraySegment<byte> data;
		private readonly int hashCode;

		public int Length
		{
			get { return this.data.Count; }
		}
		public sbyte Type { get; private set; }

		// ReSharper disable PossibleNullReferenceException
		public byte this[int index]
		{
			get { return this.data.Array[this.data.Offset + index]; }
		}

		// ReSharper restore PossibleNullReferenceException

		public MessagePackExtensionType()
		{
			this.Type = 0; // generic binary
			this.data = new ArraySegment<byte>(EmptyBytes, 0, 0);
		}
		public MessagePackExtensionType(byte[] binaryData)
			: this(0, binaryData) { }
		public MessagePackExtensionType(sbyte type, byte[] binaryData)
			: this(type, new ArraySegment<byte>(binaryData, 0, binaryData.Length))
		{
		}
		public MessagePackExtensionType(sbyte type, ArraySegment<byte> binaryData)
		{
			if (binaryData.Array == null) throw new ArgumentNullException("binaryData");

			this.data = binaryData;
			this.Type = type;

			var buffer = this.data.Array ?? EmptyBytes;
			if (this.data.Count >= 4)
			{
				this.hashCode = unchecked(this.data.Count * 17 + BitConverter.ToInt32(buffer, this.data.Offset));
			}
			else
			{
				this.hashCode = this.data.Count;
				for (var i = this.data.Offset; i < this.data.Offset + this.data.Count; i++)
					this.hashCode += unchecked(buffer[i] * 114);
			}
		}

		public void CopyTo(byte[] destination, int index, int bytesToCopy)
		{
			Buffer.BlockCopy(this.data.Array ?? EmptyBytes, this.data.Offset, destination, index, Math.Min(bytesToCopy, this.Length));
		}
		public byte[] ToByteArray()
		{
			if (this.data.Offset != 0 || this.Length != this.data.Count)
			{
				var byteArray = new byte[this.Length];
				Buffer.BlockCopy(this.data.Array ?? EmptyBytes, this.data.Offset, byteArray, 0, byteArray.Length);
				return byteArray;
			}

			return this.data.Array;
		}
		public ArraySegment<byte> ToArraySegment()
		{
			return this.data;
		}
		public string ToBase64()
		{
			return Convert.ToBase64String(this.data.Array ?? EmptyBytes, this.data.Offset, this.Length);
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as MessagePackExtensionType);
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}

		public bool Equals(MessagePackExtensionType other)
		{
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;

			if (this.Length != other.Length) return false;
			if (this.GetHashCode() != other.GetHashCode()) return false;

			for (var i = 0; i < this.Length; i++)
			{
				if (this[i] != other[i]) return false;
			}

			return true;
		}
		public int CompareTo(object obj)
		{
			return this.CompareTo(obj as MessagePackExtensionType);
		}
		public int CompareTo(MessagePackExtensionType other)
		{
			if (other == null) return 1;

			// wee need to align buffers with different sizes
			for (int i = 0, j = 0; i < this.Length || j < other.Length; i++, j++)
			{
				// we need offsets
				var io = this.Length - other.Length;
				var jo = other.Length - this.Length;

				// only negative offset is needed
				if (io > 0) io = 0;
				if (jo > 0) jo = 0;

				// get bytes with offsets
				var ib = i + io >= 0 ? this[i + io] : (byte)0;
				var jb = j + jo >= 0 ? other[j + jo] : (byte)0;

				// compare
				if (ib > jb) return 1;

				if (jb > ib) return -1;
			}

			return 0;
		}

		public static bool operator ==(MessagePackExtensionType a, MessagePackExtensionType b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

			return a.Equals(b);
		}
		public static bool operator !=(MessagePackExtensionType a, MessagePackExtensionType b)
		{
			return !(a == b);
		}

		public static explicit operator byte[] (MessagePackExtensionType messagePackExtension)
		{
			if (messagePackExtension != null)
				return messagePackExtension.ToByteArray();
			else
				return null;
		}
		public static explicit operator ArraySegment<byte>(MessagePackExtensionType messagePackExtension)
		{
			if (messagePackExtension == null) throw new ArgumentNullException("messagePackExtension");
			return messagePackExtension.ToArraySegment();
		}

		public override string ToString()
		{
			return Convert.ToBase64String(this.data.Array ?? EmptyBytes, this.data.Offset, Math.Min(this.Length, 64)) + ( this.Length > 64 ? "..." : "");
		}
	}
}
