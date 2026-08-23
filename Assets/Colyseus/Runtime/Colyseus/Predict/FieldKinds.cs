using System;
using System.Collections.Generic;

namespace Colyseus.Predict
{
	/// <summary>
	///     The one place the prediction layer decides what a schema field IS —
	///     by declared type for attach-time questions, by live value for the
	///     pose/smoothing split — so every face answers the same way.
	/// </summary>
	internal static class FieldKinds
	{
		private static readonly HashSet<string> NumericTypes = new HashSet<string>
		{
			"number", "int8", "uint8", "int16", "uint16", "int32", "uint32",
			"int64", "uint64", "float32", "float64", "quantized",
		};

		/// <summary>A declared type that smoothing can curve.</summary>
		public static bool IsNumericType(string fieldType) => NumericTypes.Contains(fieldType);

		/// <summary>
		///     A declared type a rollback mirror can copy: ref/array/map are object
		///     types and can't roll back by value; everything else (numbers,
		///     booleans, strings) is an ordinary immutable value.
		/// </summary>
		public static bool IsScalarType(string fieldType)
			=> fieldType != "ref" && fieldType != "array" && fieldType != "map";

		/// <summary>A live value the error term can act on — booleans and strings copy verbatim instead.</summary>
		public static bool IsNumericValue(object value)
		{
			switch (Convert.GetTypeCode(value))
			{
				case TypeCode.SByte: case TypeCode.Byte:
				case TypeCode.Int16: case TypeCode.UInt16:
				case TypeCode.Int32: case TypeCode.UInt32:
				case TypeCode.Int64: case TypeCode.UInt64:
				case TypeCode.Single: case TypeCode.Double:
					return true;
				default:
					return false;
			}
		}
	}
}
