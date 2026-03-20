// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.FilteredTypes {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player playerOne = null;

		[Type(1, "ref", typeof(Player))]
		public Player playerTwo = null;

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> players = null;
	}
}
