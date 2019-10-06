//
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
//
// GENERATED USING @colyseus/schema 0.4.61
//

using Colyseus.Schema;

namespace SchemaTest.Backwards {
	public class StateV1 : Schema {
		[Type(0, "string")]
		public string str = "";

		[Type(1, "map", typeof(MapSchema<PlayerV1>))]
		public MapSchema<PlayerV1> map = new MapSchema<PlayerV1>();
	}
}
