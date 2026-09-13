// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.Callbacks {
	public partial class Vec3 : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public Vec3() { }

		[global::Colyseus.Schema.Type(0, "number")]
		public double x = 1;

		[global::Colyseus.Schema.Type(1, "number")]
		public double y = 2;

		[global::Colyseus.Schema.Type(2, "number")]
		public double z = 3;
	}
}
