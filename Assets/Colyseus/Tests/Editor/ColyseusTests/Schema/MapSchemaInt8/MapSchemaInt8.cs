// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.MapSchemaInt8 {
	public partial class MapSchemaInt8 : Schema {
		[Type(0, "string")]
		public string status = default(string);

		[Type(1, "map", typeof(MapSchema<sbyte>), "int8")]
		public MapSchema<sbyte> mapOfInt8 = null;
	}
}
