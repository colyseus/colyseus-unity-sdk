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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public enum MsgPackExtType : byte
	{
		None = 0,
		DateTimeOld = 1,
		DecimalOld = 2,

		DateTime = 40,
		DateTimeOffset = 41,
		Decimal = 42
	}
}
