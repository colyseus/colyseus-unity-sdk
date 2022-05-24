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

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	public enum MsgPackType : byte
	{
		PositiveFixIntStart = 0x00,
		PositiveFixIntEnd = 0x7f,
		FixMapStart = 0x80,
		FixMapEnd = 0x8f,
		FixArrayStart = 0x90,
		FixArrayEnd = 0x9f,
		FixStrStart = 0xa0,
		FixStrEnd = 0xbf,
		Nil = 0xc0,
		Unused = 0xc1,
		False = 0xc2,
		True = 0xc3,
		Bin8 = 0xc4,
		Bin16 = 0xc5,
		Bin32 = 0xc6,
		Ext8 = 0xc7,
		Ext16 = 0xc8,
		Ext32 = 0xc9,
		Float32 = 0xca,
		Float64 = 0xcb,
		UInt8 = 0xcc,
		UInt16 = 0xcd,
		UInt32 = 0xce,
		UInt64 = 0xcf,
		Int8 = 0xd0,
		Int16 = 0xd1,
		Int32 = 0xd2,
		Int64 = 0xd3,
		FixExt1 = 0xd4,
		FixExt2 = 0xd5,
		FixExt4 = 0xd6,
		FixExt8 = 0xd7,
		FixExt16 = 0xd8,
		Str8 = 0xd9,
		Str16 = 0xda,
		Str32 = 0xdb,
		Array16 = 0xdc,
		Array32 = 0xdd,
		Map16 = 0xde,
		Map32 = 0xdf,
		NegativeFixIntStart = 0xe0,
		NegativeFixIntEnd = 0xff
	}
}
