// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.MapSchemaInt8 {
	public partial class MapSchemaInt8 : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public MapSchemaInt8() { }

		[global::Colyseus.Schema.Type(0, "string")]
		public string status = "Hello world";

		[global::Colyseus.Schema.Type(1, "map", typeof(global::Colyseus.Schema.MapSchema<sbyte>), "int8")]
		public global::Colyseus.Schema.MapSchema<sbyte> mapOfInt8 = new global::Colyseus.Schema.MapSchema<sbyte>();
	}
}
