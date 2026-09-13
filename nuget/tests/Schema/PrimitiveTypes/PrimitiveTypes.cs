// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.PrimitiveTypes {
	public partial class PrimitiveTypes : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public PrimitiveTypes() { }

		[global::Colyseus.Schema.Type(0, "int8")]
		public sbyte int8 = default(sbyte);

		[global::Colyseus.Schema.Type(1, "uint8")]
		public byte uint8 = default(byte);

		[global::Colyseus.Schema.Type(2, "int16")]
		public short int16 = default(short);

		[global::Colyseus.Schema.Type(3, "uint16")]
		public ushort uint16 = default(ushort);

		[global::Colyseus.Schema.Type(4, "int32")]
		public int int32 = default(int);

		[global::Colyseus.Schema.Type(5, "uint32")]
		public uint uint32 = default(uint);

		[global::Colyseus.Schema.Type(6, "int64")]
		public long int64 = default(long);

		[global::Colyseus.Schema.Type(7, "uint64")]
		public ulong uint64 = default(ulong);

		[global::Colyseus.Schema.Type(8, "float32")]
		public double float32 = default(double);

		[global::Colyseus.Schema.Type(9, "float64")]
		public double float64 = default(double);

		[global::Colyseus.Schema.Type(10, "number")]
		public double varint_int8 = default(double);

		[global::Colyseus.Schema.Type(11, "number")]
		public double varint_uint8 = default(double);

		[global::Colyseus.Schema.Type(12, "number")]
		public double varint_int16 = default(double);

		[global::Colyseus.Schema.Type(13, "number")]
		public double varint_uint16 = default(double);

		[global::Colyseus.Schema.Type(14, "number")]
		public double varint_int32 = default(double);

		[global::Colyseus.Schema.Type(15, "number")]
		public double varint_uint32 = default(double);

		[global::Colyseus.Schema.Type(16, "number")]
		public double varint_int64 = default(double);

		[global::Colyseus.Schema.Type(17, "number")]
		public double varint_uint64 = default(double);

		[global::Colyseus.Schema.Type(18, "number")]
		public double varint_float32 = default(double);

		[global::Colyseus.Schema.Type(19, "number")]
		public double varint_float64 = default(double);

		[global::Colyseus.Schema.Type(20, "string")]
		public string str = default(string);

		[global::Colyseus.Schema.Type(21, "boolean")]
		public bool boolean = default(bool);
	}
}
