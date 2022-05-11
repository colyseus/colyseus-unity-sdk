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
	[TypeSerializer(typeof(MsgPackTimestampSerializer))]
	public struct MessagePackTimestamp : IEquatable<MessagePackTimestamp>, IComparable<MessagePackTimestamp>
	{
		public const int MAX_NANO_SECONDS = 999999999;

		public readonly long Seconds;
		public readonly uint NanoSeconds;

		public MessagePackTimestamp(long seconds, uint nanoSeconds)
		{
			if (nanoSeconds > MAX_NANO_SECONDS)
				nanoSeconds = MAX_NANO_SECONDS;

			this.Seconds = seconds;
			this.NanoSeconds = nanoSeconds;
		}

		public static explicit operator DateTime(MessagePackTimestamp timestamp)
		{
			return new DateTime(JsonUtils.UnixEpochTicks + ((TimeSpan)timestamp).Ticks, DateTimeKind.Unspecified);
		}
		public static explicit operator TimeSpan(MessagePackTimestamp timestamp)
		{
			return TimeSpan.FromSeconds(timestamp.Seconds) + TimeSpan.FromTicks(timestamp.NanoSeconds / 100);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return unchecked(this.Seconds.GetHashCode() * 17 + this.NanoSeconds.GetHashCode());
		}
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is MessagePackTimestamp)
				return this.Equals((MessagePackTimestamp)obj);
			else
				return false;
		}
		/// <inheritdoc />
		public bool Equals(MessagePackTimestamp other)
		{
			return this.Seconds.Equals(other.Seconds) && this.NanoSeconds.Equals(other.NanoSeconds);
		}
		/// <inheritdoc />
		public int CompareTo(MessagePackTimestamp other)
		{
			var cmp = this.Seconds.CompareTo(other.Seconds);
			if (cmp != 0)
				return cmp;
			return this.NanoSeconds.CompareTo(other.NanoSeconds);
		}

		public static bool operator >(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) == 1;
		}
		public static bool operator <(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) == -1;
		}
		public static bool operator >=(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) != -1;
		}
		public static bool operator <=(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) != 1;
		}
		public static bool operator ==(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return !a.Equals(b);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("seconds: {0}, nanoseconds: {1}", this.Seconds, this.NanoSeconds);
		}
	}
}
