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
	public partial class AccelInput : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public AccelInput() { }
		[Type(0, "number")]
		public float ax = default(float);
	}
}
