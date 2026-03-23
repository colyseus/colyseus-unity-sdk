// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.ArraySchemaTypes {
	public partial class ArraySchemaTypes : Schema {
		[Type(0, "array", typeof(ArraySchema<IAmAChild>))]
		public ArraySchema<IAmAChild> arrayOfSchemas = null;

		[Type(1, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> arrayOfNumbers = null;

		[Type(2, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = null;

		[Type(3, "array", typeof(ArraySchema<int>), "int32")]
		public ArraySchema<int> arrayOfInt32 = null;
	}
}
