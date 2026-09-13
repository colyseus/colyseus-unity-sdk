// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.BackwardsForwards {
	public partial class StateV2 : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public StateV2() { }

		[global::Colyseus.Schema.Type(0, "string")]
		public string str = default(string);

		[global::System.Obsolete("field 'map' is deprecated.", true)]
		[global::Colyseus.Schema.Type(1, "map", typeof(global::Colyseus.Schema.MapSchema<PlayerV2>))]
		public global::Colyseus.Schema.MapSchema<PlayerV2> map = new global::Colyseus.Schema.MapSchema<PlayerV2>();

		[global::Colyseus.Schema.Type(2, "number")]
		public double countdown = default(double);
	}
}
