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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents a JSON token type.
	/// </summary>
	public enum JsonToken
	{
		/// <summary>
		/// No token.
		/// </summary>
		None = 0,
		/// <summary>
		/// Beginning of an array.
		/// </summary>
		BeginArray,
		/// <summary>
		/// End of an array.
		/// </summary>
		EndOfArray,
		/// <summary>
		/// Beginning of an object.
		/// </summary>
		BeginObject,
		/// <summary>
		/// End of an object.
		/// </summary>
		EndOfObject,
		/// <summary>
		/// Object member name.
		/// </summary>
		Member,
		/// <summary>
		/// Numeric value.
		/// </summary>
		Number,
		/// <summary>
		/// String literal.
		/// </summary>
		StringLiteral,
		/// <summary>
		/// DateTime value.
		/// </summary>
		DateTime,
		/// <summary>
		/// Null value.
		/// </summary>
		Null,
		/// <summary>
		/// Boolean value.
		/// </summary>
		Boolean,
		/// <summary>
		/// End of the input stream.
		/// </summary>
		EndOfStream
	}
}
