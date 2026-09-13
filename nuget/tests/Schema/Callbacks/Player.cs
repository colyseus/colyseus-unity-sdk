// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.Callbacks {
	public partial class Player : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Player() { }

		[global::Colyseus.Schema.Type(0, "ref", typeof(Vec3))]
		public Vec3 position = null;

		[global::Colyseus.Schema.Type(1, "map", typeof(global::Colyseus.Schema.MapSchema<Item>))]
		public global::Colyseus.Schema.MapSchema<Item> items = new global::Colyseus.Schema.MapSchema<Item>();
	}
}
