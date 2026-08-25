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
namespace GameDevWare.Serialization.MessagePack
{
	/// <summary>
	/// MessagePack type markers.
	/// </summary>
	public enum MsgPackType : byte
	{
		/// <summary>
		/// Positive fixint start marker.
		/// </summary>
		PositiveFixIntStart = 0x00,
		/// <summary>
		/// Positive fixint end marker.
		/// </summary>
		PositiveFixIntEnd = 0x7f,
		/// <summary>
		/// FixMap start marker.
		/// </summary>
		FixMapStart = 0x80,
		/// <summary>
		/// FixMap end marker.
		/// </summary>
		FixMapEnd = 0x8f,
		/// <summary>
		/// FixArray start marker.
		/// </summary>
		FixArrayStart = 0x90,
		/// <summary>
		/// FixArray end marker.
		/// </summary>
		FixArrayEnd = 0x9f,
		/// <summary>
		/// FixStr start marker.
		/// </summary>
		FixStrStart = 0xa0,
		/// <summary>
		/// FixStr end marker.
		/// </summary>
		FixStrEnd = 0xbf,
		/// <summary>
		/// Nil marker.
		/// </summary>
		Nil = 0xc0,
		/// <summary>
		/// Unused marker.
		/// </summary>
		Unused = 0xc1,
		/// <summary>
		/// False marker.
		/// </summary>
		False = 0xc2,
		/// <summary>
		/// True marker.
		/// </summary>
		True = 0xc3,
		/// <summary>
		/// Bin8 marker.
		/// </summary>
		Bin8 = 0xc4,
		/// <summary>
		/// Bin16 marker.
		/// </summary>
		Bin16 = 0xc5,
		/// <summary>
		/// Bin32 marker.
		/// </summary>
		Bin32 = 0xc6,
		/// <summary>
		/// Ext8 marker.
		/// </summary>
		Ext8 = 0xc7,
		/// <summary>
		/// Ext16 marker.
		/// </summary>
		Ext16 = 0xc8,
		/// <summary>
		/// Ext32 marker.
		/// </summary>
		Ext32 = 0xc9,
		/// <summary>
		/// Float32 marker.
		/// </summary>
		Float32 = 0xca,
		/// <summary>
		/// Float64 marker.
		/// </summary>
		Float64 = 0xcb,
		/// <summary>
		/// UInt8 marker.
		/// </summary>
		UInt8 = 0xcc,
		/// <summary>
		/// UInt16 marker.
		/// </summary>
		UInt16 = 0xcd,
		/// <summary>
		/// UInt32 marker.
		/// </summary>
		UInt32 = 0xce,
		/// <summary>
		/// UInt64 marker.
		/// </summary>
		UInt64 = 0xcf,
		/// <summary>
		/// Int8 marker.
		/// </summary>
		Int8 = 0xd0,
		/// <summary>
		/// Int16 marker.
		/// </summary>
		Int16 = 0xd1,
		/// <summary>
		/// Int32 marker.
		/// </summary>
		Int32 = 0xd2,
		/// <summary>
		/// Int64 marker.
		/// </summary>
		Int64 = 0xd3,
		/// <summary>
		/// FixExt1 marker.
		/// </summary>
		FixExt1 = 0xd4,
		/// <summary>
		/// FixExt2 marker.
		/// </summary>
		FixExt2 = 0xd5,
		/// <summary>
		/// FixExt4 marker.
		/// </summary>
		FixExt4 = 0xd6,
		/// <summary>
		/// FixExt8 marker.
		/// </summary>
		FixExt8 = 0xd7,
		/// <summary>
		/// FixExt16 marker.
		/// </summary>
		FixExt16 = 0xd8,
		/// <summary>
		/// Str8 marker.
		/// </summary>
		Str8 = 0xd9,
		/// <summary>
		/// Str16 marker.
		/// </summary>
		Str16 = 0xda,
		/// <summary>
		/// Str32 marker.
		/// </summary>
		Str32 = 0xdb,
		/// <summary>
		/// Array16 marker.
		/// </summary>
		Array16 = 0xdc,
		/// <summary>
		/// Array32 marker.
		/// </summary>
		Array32 = 0xdd,
		/// <summary>
		/// Map16 marker.
		/// </summary>
		Map16 = 0xde,
		/// <summary>
		/// Map32 marker.
		/// </summary>
		Map32 = 0xdf,
		/// <summary>
		/// Negative fixint start marker.
		/// </summary>
		NegativeFixIntStart = 0xe0,
		/// <summary>
		/// Negative fixint end marker.
		/// </summary>
		NegativeFixIntEnd = 0xff
	}
}
