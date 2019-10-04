// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 0.4.54
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaInt8 {
	public class MapSchemaInt8 : Schema {
		[Type(0, "string")]
		public string status = "";

		[Type(1, "map", "int8")]
		public MapSchema<int> mapOfInt8 = new MapSchema<int>();
	}
}
