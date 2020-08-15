// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.0-alpha.44
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaInt8 {
	public class MapSchemaInt8 : Schema {
		[Type(0, "string")]
		public string status = default(string);

		[Type(1, "map", typeof(MapSchema<int>), "int8")]
		public MapSchema<int> mapOfInt8 = new MapSchema<int>();
	}
}
