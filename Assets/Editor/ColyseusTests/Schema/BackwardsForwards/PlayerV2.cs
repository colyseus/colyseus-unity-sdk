//
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
//
// GENERATED USING @colyseus/schema 0.4.61
//

using Colyseus.Schema;

namespace SchemaTest.Forwards {
	public class PlayerV2 : Schema {
		[Type(0, "number")]
		public float x = 0;

		[Type(1, "number")]
		public float y = 0;

		[Type(2, "string")]
		public string name = "";

		[Type(3, "array", "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();
	}
}
