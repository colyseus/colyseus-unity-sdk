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

namespace SchemaTest.ArraySchemaInsertOps {
	public partial class ArraySchemaInsertOps : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ArraySchemaInsertOps() { }
		[Type(0, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> numbers = null;

		[Type(1, "array", typeof(ArraySchema<Item>))]
		public ArraySchema<Item> items = null;

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> players = null;
	}
}
