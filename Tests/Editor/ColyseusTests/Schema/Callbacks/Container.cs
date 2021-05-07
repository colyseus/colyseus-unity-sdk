// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.Callbacks {
	public partial class Container : Schema {
		[Type(0, "number")]
		public float num = default(float);

		[Type(1, "string")]
		public string str = default(string);

		[Type(2, "ref", typeof(Ref))]
		public Ref aRef = new Ref();

		[Type(3, "array", typeof(ArraySchema<Ref>))]
		public ArraySchema<Ref> arrayOfSchemas = new ArraySchema<Ref>();

		[Type(4, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> arrayOfNumbers = new ArraySchema<float>();

		[Type(5, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();
	}
}
