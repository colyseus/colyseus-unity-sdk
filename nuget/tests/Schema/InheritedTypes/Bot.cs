// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.InheritedTypes {
	public partial class Bot : Player {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Bot() { }

		[global::Colyseus.Schema.Type(3, "number")]
		public double power = default(double);
	}
}
