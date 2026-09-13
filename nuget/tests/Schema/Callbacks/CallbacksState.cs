// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.Callbacks {
	public partial class CallbacksState : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public CallbacksState() { }

		[global::Colyseus.Schema.Type(0, "ref", typeof(Container))]
		public Container container = null;
	}
}
