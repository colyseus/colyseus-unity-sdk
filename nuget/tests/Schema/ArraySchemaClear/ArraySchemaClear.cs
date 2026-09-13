// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.ArraySchemaClear {
	public partial class ArraySchemaClear : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public ArraySchemaClear() { }

		[global::Colyseus.Schema.Type(0, "array", typeof(global::Colyseus.Schema.ArraySchema<double>), "number")]
		public global::Colyseus.Schema.ArraySchema<double> items = new global::Colyseus.Schema.ArraySchema<double>();
	}
}
