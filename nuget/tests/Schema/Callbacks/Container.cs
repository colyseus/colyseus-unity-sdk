// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.Callbacks {
	public partial class Container : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Container() { }

		[global::Colyseus.Schema.Type(0, "map", typeof(global::Colyseus.Schema.MapSchema<Player>))]
		public global::Colyseus.Schema.MapSchema<Player> playersMap = new global::Colyseus.Schema.MapSchema<Player>();
	}
}
