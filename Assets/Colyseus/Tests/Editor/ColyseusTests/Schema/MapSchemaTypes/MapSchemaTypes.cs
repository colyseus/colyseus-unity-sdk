// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaTypes {
	public partial class MapSchemaTypes : Schema {
		[Type(0, "map", typeof(MapSchema<IAmAChild>))]
		public MapSchema<IAmAChild> mapOfSchemas = null;

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> mapOfNumbers = null;

		[Type(2, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> mapOfStrings = null;

		[Type(3, "map", typeof(MapSchema<int>), "int32")]
		public MapSchema<int> mapOfInt32 = null;
	}
}
