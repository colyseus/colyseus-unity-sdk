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
	public partial class ResyncState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ResyncState() { }
		[Type(0, "map", typeof(MapSchema<Unit>))]
		public MapSchema<Unit> units = null;

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> trees = null;
	}
}
