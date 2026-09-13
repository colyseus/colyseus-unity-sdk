// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.InstanceSharingTypes {
	public partial class State : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public State() { }

		[global::Colyseus.Schema.Type(0, "ref", typeof(Player))]
		public Player player1 = null;

		[global::Colyseus.Schema.Type(1, "ref", typeof(Player))]
		public Player player2 = null;

		[global::Colyseus.Schema.Type(2, "array", typeof(global::Colyseus.Schema.ArraySchema<Player>))]
		public global::Colyseus.Schema.ArraySchema<Player> arrayOfPlayers = new global::Colyseus.Schema.ArraySchema<Player>();

		[global::Colyseus.Schema.Type(3, "map", typeof(global::Colyseus.Schema.MapSchema<Player>))]
		public global::Colyseus.Schema.MapSchema<Player> mapOfPlayers = new global::Colyseus.Schema.MapSchema<Player>();
	}
}
