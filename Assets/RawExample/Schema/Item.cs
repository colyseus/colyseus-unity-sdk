// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 4.0.7
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

public partial class Item : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public Item() { }
	[Type(0, "string")]
	public string name = default(string);

	[Type(1, "number")]
	public float value = default(float);
}

