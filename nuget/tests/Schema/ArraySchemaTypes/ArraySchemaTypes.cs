// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.ArraySchemaTypes {
	public partial class ArraySchemaTypes : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public ArraySchemaTypes() { }

		[global::Colyseus.Schema.Type(0, "array", typeof(global::Colyseus.Schema.ArraySchema<IAmAChild>))]
		public global::Colyseus.Schema.ArraySchema<IAmAChild> arrayOfSchemas = new global::Colyseus.Schema.ArraySchema<IAmAChild>();

		[global::Colyseus.Schema.Type(1, "array", typeof(global::Colyseus.Schema.ArraySchema<double>), "number")]
		public global::Colyseus.Schema.ArraySchema<double> arrayOfNumbers = new global::Colyseus.Schema.ArraySchema<double>();

		[global::Colyseus.Schema.Type(2, "array", typeof(global::Colyseus.Schema.ArraySchema<string>), "string")]
		public global::Colyseus.Schema.ArraySchema<string> arrayOfStrings = new global::Colyseus.Schema.ArraySchema<string>();

		[global::Colyseus.Schema.Type(3, "array", typeof(global::Colyseus.Schema.ArraySchema<int>), "int32")]
		public global::Colyseus.Schema.ArraySchema<int> arrayOfInt32 = new global::Colyseus.Schema.ArraySchema<int>();
	}
}
