// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.45
// 

using Colyseus.Schema;

namespace SchemaTest.Callbacks {
	public partial class Player : Schema {
		[Type(0, "ref", typeof(Vec3))]
		public Vec3 position = null;

		[Type(1, "map", typeof(MapSchema<Item>))]
		public MapSchema<Item> items = null;
	}
}
