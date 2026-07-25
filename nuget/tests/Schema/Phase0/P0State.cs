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

namespace SchemaTest.Phase0 {
	public partial class P0State : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public P0State() { }
		[Type(0, "string")]
		public string msg = default(string);

		[Type(1, "number")]
		public float n = default(float);
	}
}
