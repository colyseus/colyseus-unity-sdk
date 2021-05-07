// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.InstanceSharing {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player player1 = new Player();

		[Type(1, "ref", typeof(Player))]
		public Player player2 = new Player();

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> arrayOfPlayers = new ArraySchema<Player>();

		[Type(3, "map", typeof(MapSchema<Player>))]
		public MapSchema<Player> mapOfPlayers = new MapSchema<Player>();
	}
}
