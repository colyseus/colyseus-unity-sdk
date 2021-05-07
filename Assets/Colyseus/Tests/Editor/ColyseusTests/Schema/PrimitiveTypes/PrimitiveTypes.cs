// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.PrimitiveTypes {
	public partial class PrimitiveTypes : Schema {
		[Type(0, "int8")]
		public sbyte int8 = default(sbyte);

		[Type(1, "uint8")]
		public byte uint8 = default(byte);

		[Type(2, "int16")]
		public short int16 = default(short);

		[Type(3, "uint16")]
		public ushort uint16 = default(ushort);

		[Type(4, "int32")]
		public int int32 = default(int);

		[Type(5, "uint32")]
		public uint uint32 = default(uint);

		[Type(6, "int64")]
		public long int64 = default(long);

		[Type(7, "uint64")]
		public ulong uint64 = default(ulong);

		[Type(8, "float32")]
		public float float32 = default(float);

		[Type(9, "float64")]
		public double float64 = default(double);

		[Type(10, "number")]
		public float varint_int8 = default(float);

		[Type(11, "number")]
		public float varint_uint8 = default(float);

		[Type(12, "number")]
		public float varint_int16 = default(float);

		[Type(13, "number")]
		public float varint_uint16 = default(float);

		[Type(14, "number")]
		public float varint_int32 = default(float);

		[Type(15, "number")]
		public float varint_uint32 = default(float);

		[Type(16, "number")]
		public float varint_int64 = default(float);

		[Type(17, "number")]
		public float varint_uint64 = default(float);

		[Type(18, "number")]
		public float varint_float32 = default(float);

		[Type(19, "number")]
		public float varint_float64 = default(float);

		[Type(20, "string")]
		public string str = default(string);

		[Type(21, "boolean")]
		public bool boolean = default(bool);
	}
}
