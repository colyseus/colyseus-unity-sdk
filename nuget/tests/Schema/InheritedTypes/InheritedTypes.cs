// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.InheritedTypes {
	public partial class InheritedTypes : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public InheritedTypes() { }

		[global::Colyseus.Schema.Type(0, "ref", typeof(Entity))]
		public Entity entity = null;

		[global::Colyseus.Schema.Type(1, "ref", typeof(Player))]
		public Player player = null;

		[global::Colyseus.Schema.Type(2, "ref", typeof(Bot))]
		public Bot bot = null;

		[global::Colyseus.Schema.Type(3, "ref", typeof(Entity))]
		public Entity any = null;
	}
}
