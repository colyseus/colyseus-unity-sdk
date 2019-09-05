// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 0.4.54
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaTypes {
	public class MapSchemaTypes : Schema {
		[Type(0, "map", typeof(MapSchema<IAmAChild>))]
		public MapSchema<IAmAChild> mapOfSchemas = new MapSchema<IAmAChild>();

		[Type(1, "map", "number")]
		public MapSchema<float> mapOfNumbers = new MapSchema<float>();

		[Type(2, "map", "string")]
		public MapSchema<string> mapOfStrings = new MapSchema<string>();

		[Type(3, "map", "int32")]
		public MapSchema<int> mapOfInt32 = new MapSchema<int>();
	}
}
