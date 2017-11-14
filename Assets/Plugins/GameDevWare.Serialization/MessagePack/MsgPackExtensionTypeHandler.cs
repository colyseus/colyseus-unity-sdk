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
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public abstract class MessagePackExtensionTypeHandler
	{
		public abstract IEnumerable<Type> ExtensionTypes { get; }

		public abstract bool TryRead(sbyte type, ArraySegment<byte> data, out object value);
		public abstract bool TryWrite(object value, out sbyte type, ref ArraySegment<byte> data);

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("Extension Types: {0}", string.Join(", ", this.ExtensionTypes.Select(t => t.ToString()).ToArray()));
		}
	}
}
