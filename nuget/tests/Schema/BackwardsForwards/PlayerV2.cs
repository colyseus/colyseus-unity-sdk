// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.BackwardsForwards {
	public partial class PlayerV2 : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public PlayerV2() { }

		[global::Colyseus.Schema.Type(0, "number")]
		public double x = default(double);

		[global::Colyseus.Schema.Type(1, "number")]
		public double y = default(double);

		[global::Colyseus.Schema.Type(2, "string")]
		public string name = "Jake Badlands";

		[global::Colyseus.Schema.Type(3, "array", typeof(global::Colyseus.Schema.ArraySchema<string>), "string")]
		public global::Colyseus.Schema.ArraySchema<string> arrayOfStrings = new global::Colyseus.Schema.ArraySchema<string>();
	}
}
