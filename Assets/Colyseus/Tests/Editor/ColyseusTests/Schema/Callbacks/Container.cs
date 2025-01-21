// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.Callbacks {
	public partial class Container : Schema {
		[Type(0, "number")]
		public float num = default(float);

		[Type(1, "string")]
		public string str = default(string);

		[Type(2, "ref", typeof(Ref))]
		public Ref aRef = null;

		[Type(3, "array", typeof(ArraySchema<Ref>))]
		public ArraySchema<Ref> arrayOfSchemas = null;

		[Type(4, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> arrayOfNumbers = null;

		[Type(5, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = null;
	}
}
