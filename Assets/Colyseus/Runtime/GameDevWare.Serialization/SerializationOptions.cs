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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Options for controlling the serialization and deserialization process.
	/// </summary>
	[Flags]
	public enum SerializationOptions
	{
		/// <summary>
		/// Employs default behavior where polymorphic types include type metadata for accurate reconstruction.
		/// </summary>
		None = 0,
		/// <summary>
		/// Omit type metadata (e.g., "$type" in JSON) to achieve a more compact payload and a higher security posture.
		/// <para>While this reduces the flexibility of polymorphic deserialization, it effectively mitigates risks 
		/// of Remote Code Execution (RCE) by preventing attackers from specifying dangerous types in the input data.</para>
		/// </summary>
		SuppressTypeInformation = 0x1 << 1,
		/// <summary>
		/// Format the output with indentation and newlines to improve human readability during debugging or manual inspection.
		/// <para>This makes the output easier to read at the expense of a larger payload size due to added whitespace.</para>
		/// </summary>
		PrettyPrint = 0x1 << 2,
	}
}
