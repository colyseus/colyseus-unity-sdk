// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaMoveNullifyType {
	public partial class State : Schema {
		[Type(0, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> previous = null;

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> current = null;
	}
}
