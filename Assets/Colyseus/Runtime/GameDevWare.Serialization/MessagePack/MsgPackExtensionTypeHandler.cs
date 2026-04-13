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
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	/// <summary>
	/// Provides a mechanism for handling custom MessagePack extension types (type codes from -1 to -128).
	/// <para>This handler allows developers to inject high-performance binary serialization for types not covered 
	/// by the core MessagePack specification. Use it to implement compact, domain-specific binary representations 
	/// for structures like compressed textures, network packets, or complex mathematical types. 
	/// See <see cref="DefaultMessagePackExtensionTypeHandler"/> for built-in support of common .NET types.</para>
	/// </summary>
	public abstract class MessagePackExtensionTypeHandler
	{
		/// <summary>
		/// Gets the set of types supported by this extension handler.
		/// </summary>
		public abstract IEnumerable<Type> ExtensionTypes { get; }

		/// <summary>
		/// Attempts to deserialize a value from a raw binary extension payload.
		/// </summary>
		/// <param name="type">The specific MessagePack extension type code (-1 to -128) encountered in the stream.</param>
		/// <param name="data">The raw binary payload associated with the extension.</param>
		/// <param name="value">The resulting deserialized object if successful.</param>
		/// <returns>True if the type code was recognized and successfully deserialized; otherwise, false.</returns>
		public abstract bool TryRead(sbyte type, ArraySegment<byte> data, out object value);
		/// <summary>
		/// Attempts to serialize a value into a raw binary extension payload.
		/// </summary>
		/// <param name="value">The object instance to serialize.</param>
		/// <param name="type">The extension type code to be written to the stream.</param>
		/// <param name="data">The buffer where the binary representation should be written.</param>
		/// <returns>True if the value's type was recognized and successfully serialized; otherwise, false.</returns>
		public abstract bool TryWrite(object value, out sbyte type, ref ArraySegment<byte> data);

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("Extension Types: {0}", string.Join(", ", this.ExtensionTypes.Select(t => t.ToString()).ToArray()));
		}
	}
}
