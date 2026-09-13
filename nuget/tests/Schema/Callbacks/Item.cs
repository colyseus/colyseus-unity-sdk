// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.Callbacks {
	public partial class Item : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Item() { }

		[global::Colyseus.Schema.Type(0, "string")]
		public string name = default(string);

		[global::Colyseus.Schema.Type(1, "number")]
		public double value = default(double);
	}
}
