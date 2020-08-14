// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.0-alpha.42
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaTypes {
	public class MapSchemaTypes : Schema {
		[Type(0, typeof(MapSchema<IAmAChild>))]
		public MapSchema<IAmAChild> mapOfSchemas = new MapSchema<IAmAChild>();

		[Type(1, typeof(MapSchema<float>), "number")]
		public MapSchema<float> mapOfNumbers = new MapSchema<float>();

		[Type(2, typeof(MapSchema<string>), "string")]
		public MapSchema<string> mapOfStrings = new MapSchema<string>();

		[Type(3, typeof(MapSchema<int>), "int32")]
		public MapSchema<int> mapOfInt32 = new MapSchema<int>();
	}
}
