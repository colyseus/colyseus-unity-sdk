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
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public struct JsonMember : IEquatable<JsonMember>, IEquatable<string>
	{
		internal string NameString;
		internal char[] NameChars;
		internal bool IsEscapedAndQuoted;

		public int Length
		{
			get { return this.NameString != null ? this.NameString.Length : this.NameChars.Length; }
		}

		public JsonMember(string name)
			: this(name, false)
		{
		}

		public JsonMember(string name, bool escapedAndQuoted)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			this.NameString = name;
			this.IsEscapedAndQuoted = escapedAndQuoted;
			this.NameChars = null;
		}

		public JsonMember(char[] name)
			: this(name, false)
		{
		}

		public JsonMember(char[] name, bool escapedAndQuoted)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			this.NameChars = name;
			this.IsEscapedAndQuoted = escapedAndQuoted;
			this.NameString = null;
		}

		public override int GetHashCode()
		{
			return this.NameString != null ? this.NameString.GetHashCode() : this.NameChars.Aggregate(0, (a, c) => a ^ (int) c);
		}

		public override bool Equals(object obj)
		{
			if (obj is JsonMember)
				return this.Equals((JsonMember) obj);
			else if (obj is string)
				return this.Equals((string) obj);
			else
				return false;
		}

		public bool Equals(JsonMember other)
		{
			return this.ToString().Equals(other.ToString(), StringComparison.Ordinal);
		}

		public bool Equals(string other)
		{
			return this.ToString().Equals(other, StringComparison.Ordinal);
		}

		public static explicit operator string(JsonMember member)
		{
			return member.ToString();
		}

		public static explicit operator JsonMember(string memberName)
		{
			return new JsonMember(memberName);
		}

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
