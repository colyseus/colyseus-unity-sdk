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
	public partial class Crate : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public Crate() { }
		[Type(0, "boolean")]
		public bool alive = default(bool);

		[Type(1, "string")]
		public string owner = default(string);

		[Type(2, "number")]
		public float hp = default(float);
	}
}
