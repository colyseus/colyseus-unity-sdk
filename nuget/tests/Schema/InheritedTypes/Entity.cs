// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.InheritedTypes {
	public partial class Entity : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Entity() { }

		[global::Colyseus.Schema.Type(0, "number")]
		public double x = default(double);

		[global::Colyseus.Schema.Type(1, "number")]
		public double y = default(double);
	}
}
