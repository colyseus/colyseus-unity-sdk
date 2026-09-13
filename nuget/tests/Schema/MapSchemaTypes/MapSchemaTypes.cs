// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.MapSchemaTypes {
	public partial class MapSchemaTypes : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public MapSchemaTypes() { }

		[global::Colyseus.Schema.Type(0, "map", typeof(global::Colyseus.Schema.MapSchema<IAmAChild>))]
		public global::Colyseus.Schema.MapSchema<IAmAChild> mapOfSchemas = new global::Colyseus.Schema.MapSchema<IAmAChild>();

		[global::Colyseus.Schema.Type(1, "map", typeof(global::Colyseus.Schema.MapSchema<double>), "number")]
		public global::Colyseus.Schema.MapSchema<double> mapOfNumbers = new global::Colyseus.Schema.MapSchema<double>();

		[global::Colyseus.Schema.Type(2, "map", typeof(global::Colyseus.Schema.MapSchema<string>), "string")]
		public global::Colyseus.Schema.MapSchema<string> mapOfStrings = new global::Colyseus.Schema.MapSchema<string>();

		[global::Colyseus.Schema.Type(3, "map", typeof(global::Colyseus.Schema.MapSchema<int>), "int32")]
		public global::Colyseus.Schema.MapSchema<int> mapOfInt32 = new global::Colyseus.Schema.MapSchema<int>();
	}
}
