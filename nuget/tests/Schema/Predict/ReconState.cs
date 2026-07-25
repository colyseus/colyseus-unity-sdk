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

namespace SchemaTest.Predict {
	public partial class ReconState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ReconState() { }
		[Type(0, "number")]
		public float x = default(float);

		[Type(1, "number")]
		public float vx = default(float);
	}
}
