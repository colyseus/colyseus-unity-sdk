// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.BackwardsForwards {
	public partial class StateV1 : Schema {
		[Type(0, "string")]
		public string str = default(string);

		[Type(1, "map", typeof(MapSchema<PlayerV1>))]
		public MapSchema<PlayerV1> map = new MapSchema<PlayerV1>();
	}
}
