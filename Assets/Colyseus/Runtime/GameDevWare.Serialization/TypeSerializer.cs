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
	/// Base class for type-specific serializers.
	/// </summary>
	public abstract class TypeSerializer
	{
		/// <summary>
		/// Gets the type serialized by this instance.
		/// </summary>
		public abstract Type SerializedType { get; }

		/// <summary>
		/// Deserializes an object from the specified reader.
		/// </summary>
		/// <param name="reader">The reader to deserialize from.</param>
		/// <returns>The deserialized object.</returns>
		public abstract object Deserialize(IJsonReader reader);
		/// <summary>
		/// Serializes an object using the specified writer.
		/// </summary>
		/// <param name="writer">The writer to serialize to.</param>
		/// <param name="value">The object to serialize.</param>
		public abstract void Serialize(IJsonWriter writer, object value);
	}
}
