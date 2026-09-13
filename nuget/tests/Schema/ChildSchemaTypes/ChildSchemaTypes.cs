// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.30
// 

namespace SchemaTest.ChildSchemaTypes {
	public partial class ChildSchemaTypes : global::Colyseus.Schema.Schema {
#if UNITY_5_3_OR_NEWER
		[global::UnityEngine.Scripting.Preserve]
#endif
		public ChildSchemaTypes() { }

		[global::Colyseus.Schema.Type(0, "ref", typeof(IAmAChild))]
		public IAmAChild child = null;

		[global::Colyseus.Schema.Type(1, "ref", typeof(IAmAChild))]
		public IAmAChild secondChild = null;
	}
}
