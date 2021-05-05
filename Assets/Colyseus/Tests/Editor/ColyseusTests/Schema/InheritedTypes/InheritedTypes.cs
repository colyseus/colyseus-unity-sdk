// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.InheritedTypes {
	public partial class InheritedTypes : Schema {
		[Type(0, "ref", typeof(Entity))]
		public Entity entity = new Entity();

		[Type(1, "ref", typeof(Player))]
		public Player player = new Player();

		[Type(2, "ref", typeof(Bot))]
		public Bot bot = new Bot();

		[Type(3, "ref", typeof(Entity))]
		public Entity any = new Entity();
	}
}
