// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 0.4.54
// 

using Colyseus.Schema;

namespace SchemaTest.ArraySchemaTypes {
	public class ArraySchemaTypes : Schema {
		[Type(0, "array", typeof(ArraySchema<IAmAChild>))]
		public ArraySchema<IAmAChild> arrayOfSchemas = new ArraySchema<IAmAChild>();

		[Type(1, "array", "number")]
		public ArraySchema<float> arrayOfNumbers = new ArraySchema<float>();

		[Type(2, "array", "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();

		[Type(3, "array", "int32")]
		public ArraySchema<int> arrayOfInt32 = new ArraySchema<int>();
	}
}
