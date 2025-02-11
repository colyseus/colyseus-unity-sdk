//
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
//
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
//

using Colyseus.Schema;

namespace SchemaTest.InstanceSharingTypes {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player player1 = null;

		[Type(1, "ref", typeof(Player))]
		public Player player2 = null;

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> arrayOfPlayers = null;

		[Type(3, "map", typeof(MapSchema<Player>))]
		public MapSchema<Player> mapOfPlayers = null;
	}
}
