// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.45
// 

using Colyseus.Schema;

namespace SchemaTest.Callbacks {
	public partial class Container : Schema {
		[Type(0, "map", typeof(MapSchema<Player>))]
		public MapSchema<Player> playersMap = null;
	}
}
