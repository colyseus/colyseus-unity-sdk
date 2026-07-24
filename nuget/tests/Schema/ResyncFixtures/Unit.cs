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
	public partial class Unit : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public Unit() { }
		[Type(0, "string")]
		public string name = default(string);

		[Type(1, "number")]
		public float hp = default(float);

		[Type(2, "array", typeof(ArraySchema<Gem>))]
		public ArraySchema<Gem> gems = null;
	}
}
