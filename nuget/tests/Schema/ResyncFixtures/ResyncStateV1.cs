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
	public partial class ResyncStateV1 : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ResyncStateV1() { }
		[Type(0, "map", typeof(MapSchema<ResyncPlayerV1>))]
		public MapSchema<ResyncPlayerV1> players = null;
	}
}
