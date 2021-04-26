// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaTypes {
	public partial class MapSchemaTypes : Schema {
		[Type(0, "map", typeof(MapSchema<IAmAChild>))]
		public MapSchema<IAmAChild> mapOfSchemas = new MapSchema<IAmAChild>();

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> mapOfNumbers = new MapSchema<float>();

		[Type(2, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> mapOfStrings = new MapSchema<string>();

		[Type(3, "map", typeof(MapSchema<int>), "int32")]
		public MapSchema<int> mapOfInt32 = new MapSchema<int>();
	}
}
