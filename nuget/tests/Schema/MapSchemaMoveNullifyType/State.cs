// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.MapSchemaMoveNullifyType {
	public partial class State : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public State() { }

		[global::Colyseus.Schema.Type(0, "map", typeof(global::Colyseus.Schema.MapSchema<double>), "number")]
		public global::Colyseus.Schema.MapSchema<double> previous = new global::Colyseus.Schema.MapSchema<double>();

		[global::Colyseus.Schema.Type(1, "map", typeof(global::Colyseus.Schema.MapSchema<double>), "number")]
		public global::Colyseus.Schema.MapSchema<double> current = new global::Colyseus.Schema.MapSchema<double>();
	}
}
