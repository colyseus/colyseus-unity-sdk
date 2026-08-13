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
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents a JSON member name, which can be stored as a string or a character array.
	/// </summary>
	public struct JsonMember : IEquatable<JsonMember>, IEquatable<string>
	{
		internal string NameString;
		internal char[] NameChars;
		internal bool IsEscapedAndQuoted;

		/// <summary>
		/// Gets the length of the member name.
		/// </summary>
		public int Length
		{
			get { return this.NameString != null ? this.NameString.Length : this.NameChars.Length; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMember"/> struct with a string name.
		/// </summary>
		/// <param name="name">The member name.</param>
		public JsonMember(string name)
			: this(name, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMember"/> struct with a string name and escaping info.
		/// </summary>
		/// <param name="name">The member name.</param>
		/// <param name="escapedAndQuoted">True if the name is already escaped and quoted; otherwise, false.</param>
		public JsonMember(string name, bool escapedAndQuoted)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			this.NameString = name;
			this.IsEscapedAndQuoted = escapedAndQuoted;
			this.NameChars = null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMember"/> struct with a character array name.
		/// </summary>
		/// <param name="name">The member name.</param>
		public JsonMember(char[] name)
			: this(name, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMember"/> struct with a character array name and escaping info.
		/// </summary>
		/// <param name="name">The member name.</param>
		/// <param name="escapedAndQuoted">True if the name is already escaped and quoted; otherwise, false.</param>
		public JsonMember(char[] name, bool escapedAndQuoted)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			this.NameChars = name;
			this.IsEscapedAndQuoted = escapedAndQuoted;
			this.NameString = null;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A 32-bit signed integer hash code.</returns>
		public override int GetHashCode()
		{
			return this.NameString != null ? this.NameString.GetHashCode() : this.NameChars.Aggregate(0, (a, c) => a ^ (int) c);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>True if the objects are equal; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is JsonMember)
				return this.Equals((JsonMember) obj);
			else if (obj is string)
				return this.Equals((string) obj);
			else
				return false;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>True if the objects are equal; otherwise, false.</returns>
		public bool Equals(JsonMember other)
		{
			return this.ToString().Equals(other.ToString(), StringComparison.Ordinal);
		}

		/// <summary>
		/// Indicates whether the current object is equal to a string.
		/// </summary>
		/// <param name="other">A string to compare with this object.</param>
		/// <returns>True if the objects are equal; otherwise, false.</returns>
		public bool Equals(string other)
		{
			return this.ToString().Equals(other, StringComparison.Ordinal);
		}

		/// <summary>
		/// Explicitly converts a <see cref="JsonMember"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="member">The <see cref="JsonMember"/> to convert.</param>
		public static explicit operator string(JsonMember member)
		{
			return member.ToString();
		}

		/// <summary>
		/// Explicitly converts a <see cref="string"/> to a <see cref="JsonMember"/>.
		/// </summary>
		/// <param name="memberName">The string to convert.</param>
		public static explicit operator JsonMember(string memberName)
		{
			return new JsonMember(memberName);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			var name = NameString;
			if (this.NameChars != null)
				name = new string(NameChars, 0, NameChars.Length);

			// this is used in tests, so perf is not primary
			if (this.IsEscapedAndQuoted)
			{
				if (name.EndsWith(":"))
					name = name.Substring(0, name.Length - 1);

				name = JsonUtils.UnescapeAndUnquote(name);
			}

			return name;
		}
	}
}
