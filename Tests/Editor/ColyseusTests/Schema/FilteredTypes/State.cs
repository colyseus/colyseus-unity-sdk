// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.FilteredTypes {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player playerOne = new Player();

		[Type(1, "ref", typeof(Player))]
		public Player playerTwo = new Player();

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> players = new ArraySchema<Player>();
	}
}
