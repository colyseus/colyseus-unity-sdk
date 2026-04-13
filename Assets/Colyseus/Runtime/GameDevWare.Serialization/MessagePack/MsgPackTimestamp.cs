/*
	Copyright (c) 2026 Denis Zykov, GameDevWare.com

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
	/// Represents a MessagePack timestamp with second and nanosecond precision.
	/// </summary>
	[TypeSerializer(typeof(MsgPackTimestampSerializer))]
	public struct MessagePackTimestamp : IEquatable<MessagePackTimestamp>, IComparable<MessagePackTimestamp>
	{
		/// <summary>
		/// The maximum number of nanoseconds allowed in a timestamp (999,999,999).
		/// </summary>
		public const int MAX_NANO_SECONDS = 999999999;

		/// <summary>
		/// The number of seconds elapsed since the Unix epoch (1970-01-01T00:00:00Z).
		/// </summary>
		public readonly long Seconds;
		/// <summary>
		/// The number of nanoseconds within the second, ranging from 0 to 999,999,999.
		/// </summary>
		public readonly uint NanoSeconds;

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagePackTimestamp"/> struct with the specified seconds and nanoseconds.
		/// </summary>
		/// <param name="seconds">The number of seconds since the Unix epoch.</param>
		/// <param name="nanoSeconds">The number of nanoseconds (will be capped at <see cref="MAX_NANO_SECONDS"/>).</param>
		public MessagePackTimestamp(long seconds, uint nanoSeconds)
		{
			if (nanoSeconds > MAX_NANO_SECONDS)
				nanoSeconds = MAX_NANO_SECONDS;

			this.Seconds = seconds;
			this.NanoSeconds = nanoSeconds;
		}

		/// <summary>
		/// Explicitly converts a <see cref="MessagePackTimestamp"/> to a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="timestamp">The timestamp to convert.</param>
		/// <returns>A <see cref="DateTime"/> representing the same point in time.</returns>
		public static explicit operator DateTime(MessagePackTimestamp timestamp)
		{
			return new DateTime(JsonUtils.UnixEpochTicks + ((TimeSpan)timestamp).Ticks, DateTimeKind.Unspecified);
		}
		/// <summary>
		/// Explicitly converts a <see cref="MessagePackTimestamp"/> to a <see cref="TimeSpan"/>.
		/// </summary>
		/// <param name="timestamp">The timestamp to convert.</param>
		/// <returns>A <see cref="TimeSpan"/> representing the duration since the Unix epoch.</returns>
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

		/// <summary>
		/// Determines whether one <see cref="MessagePackTimestamp"/> is greater than another.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if <paramref name="a"/> is greater than <paramref name="b"/>; otherwise, false.</returns>
		public static bool operator >(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) == 1;
		}
		/// <summary>
		/// Determines whether one <see cref="MessagePackTimestamp"/> is less than another.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if <paramref name="a"/> is less than <paramref name="b"/>; otherwise, false.</returns>
		public static bool operator <(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) == -1;
		}
		/// <summary>
		/// Determines whether one <see cref="MessagePackTimestamp"/> is greater than or equal to another.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if <paramref name="a"/> is greater than or equal to <paramref name="b"/>; otherwise, false.</returns>
		public static bool operator >=(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) != -1;
		}
		/// <summary>
		/// Determines whether one <see cref="MessagePackTimestamp"/> is less than or equal to another.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if <paramref name="a"/> is less than or equal to <paramref name="b"/>; otherwise, false.</returns>
		public static bool operator <=(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.CompareTo(b) != 1;
		}
		/// <summary>
		/// Determines whether two <see cref="MessagePackTimestamp"/> instances are equal.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if the timestamps are equal; otherwise, false.</returns>
		public static bool operator ==(MessagePackTimestamp a, MessagePackTimestamp b)
		{
			return a.Equals(b);
		}
		/// <summary>
		/// Determines whether two <see cref="MessagePackTimestamp"/> instances are not equal.
		/// </summary>
		/// <param name="a">The first timestamp to compare.</param>
		/// <param name="b">The second timestamp to compare.</param>
		/// <returns>True if the timestamps are not equal; otherwise, false.</returns>
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
