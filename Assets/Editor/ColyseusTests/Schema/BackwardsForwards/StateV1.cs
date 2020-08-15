// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.0-alpha.42
// 

using Colyseus.Schema;

namespace SchemaTest.BackwardsForwards {
	public class StateV1 : Schema {
		[Type(0, "string")]
		public string str = default(string);

		[Type(1, typeof(MapSchema<PlayerV1>))]
		public MapSchema<PlayerV1> map = new MapSchema<PlayerV1>();
	}
}
