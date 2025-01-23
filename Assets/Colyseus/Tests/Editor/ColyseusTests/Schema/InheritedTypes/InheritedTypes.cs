//
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
//
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
//

using Colyseus.Schema;

namespace SchemaTest.InheritedTypes {
	public partial class InheritedTypes : Schema {
		[Type(0, "ref", typeof(Entity))]
		public Entity entity = null;

		[Type(1, "ref", typeof(Player))]
		public Player player = null;

		[Type(2, "ref", typeof(Bot))]
		public Bot bot = null;

		[Type(3, "ref", typeof(Entity))]
		public Entity any = null;
	}
}
