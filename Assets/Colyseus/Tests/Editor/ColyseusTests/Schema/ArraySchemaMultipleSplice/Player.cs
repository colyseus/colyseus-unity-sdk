// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.24
// 

using Colyseus.Schema;

namespace SchemaTest.ArraySchemaMultipleSplice {
	public partial class Player : Schema {
		[Type(0, "array", typeof(ArraySchema<Item>))]
		public ArraySchema<Item> items = new ArraySchema<Item>();
	}
}
