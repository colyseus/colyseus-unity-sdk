// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.BackwardsForwards {
	public partial class PlayerV2 : Schema {
		[Type(0, "number")]
		public float x = default(float);

		[Type(1, "number")]
		public float y = default(float);

		[Type(2, "string")]
		public string name = default(string);

		[Type(3, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();
	}
}
