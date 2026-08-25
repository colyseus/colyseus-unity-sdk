using System;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Interface providing information about a value being read.
	/// </summary>
	public interface IValueInfo
	{
		/// <summary>
		/// Gets a value indicating whether this instance has a value.
		/// </summary>
		bool HasValue { get; }
		/// <summary>
		/// Gets the raw value.
		/// </summary>
		object Raw { get; }
		/// <summary>
		/// Gets the type of the value.
		/// </summary>
		Type Type { get; }
		/// <summary>
		/// Gets the value as a boolean.
		/// </summary>
		bool AsBoolean { get; }
		/// <summary>
		/// Gets the value as a byte.
		/// </summary>
		byte AsByte { get; }
		/// <summary>
		/// Gets the value as a 16-bit signed integer.
		/// </summary>
		short AsInt16 { get; }
		/// <summary>
		/// Gets the value as a 32-bit signed integer.
		/// </summary>
		int AsInt32 { get; }
		/// <summary>
		/// Gets the value as a 64-bit signed integer.
		/// </summary>
		long AsInt64 { get; }
		/// <summary>
		/// Gets the value as an 8-bit signed integer.
		/// </summary>
		sbyte AsSByte { get; }
		/// <summary>
		/// Gets the value as a 16-bit unsigned integer.
		/// </summary>
		ushort AsUInt16 { get; }
		/// <summary>
		/// Gets the value as a 32-bit unsigned integer.
		/// </summary>
		uint AsUInt32 { get; }
		/// <summary>
		/// Gets the value as a 64-bit unsigned integer.
		/// </summary>
		ulong AsUInt64 { get; }
		/// <summary>
		/// Gets the value as a single-precision floating-point number.
		/// </summary>
		float AsSingle { get; }
		/// <summary>
		/// Gets the value as a double-precision floating-point number.
		/// </summary>
		double AsDouble { get; }
		/// <summary>
		/// Gets the value as a decimal number.
		/// </summary>
		decimal AsDecimal { get; }
		/// <summary>
		/// Gets the value as a string.
		/// </summary>
		string AsString { get; }
		/// <summary>
		/// Gets the value as a date and time.
		/// </summary>
		DateTime AsDateTime { get; }

		/// <summary>
		/// Gets the line number where the value was found.
		/// </summary>
		int LineNumber { get; }
		/// <summary>
		/// Gets the column number where the value was found.
		/// </summary>
		int ColumnNumber { get; }
	}
}
