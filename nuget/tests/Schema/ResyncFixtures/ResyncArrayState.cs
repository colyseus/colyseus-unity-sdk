// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.11
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace SchemaTest.ResyncFixtures {
	public partial class ResyncArrayState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ResyncArrayState() { }
		[Type(0, "array", typeof(ArraySchema<Unit>))]
		public ArraySchema<Unit> arr = null;
	}
}
