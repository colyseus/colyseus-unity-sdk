using System;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public interface IValueInfo
	{
		bool HasValue { get; }
		object Raw { get; }
		Type Type { get; }
		bool AsBoolean { get; }
		byte AsByte { get; }
		short AsInt16 { get; }
		int AsInt32 { get; }
		long AsInt64 { get; }
		sbyte AsSByte { get; }
		ushort AsUInt16 { get; }
		uint AsUInt32 { get; }
		ulong AsUInt64 { get; }
		float AsSingle { get; }
		double AsDouble { get; }
		decimal AsDecimal { get; }
		string AsString { get; }
		DateTime AsDateTime { get; }

		int LineNumber { get; }
		int ColumnNumber { get; }
	}
}