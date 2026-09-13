// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.InheritedTypes {
	public partial class Player : Entity {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Player() { }

		[global::Colyseus.Schema.Type(2, "string")]
		public string name = default(string);
	}
}
