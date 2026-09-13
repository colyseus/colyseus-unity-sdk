using Colyseus.Schema;

// Hand-written in the shape schema-codegen emits. Wire layout mirrors
// Fixtures/generate-resend-fixtures.ts.
namespace Colyseus.Tests.Resend
{
	public class RUnit : Schema.Schema
	{
		[Type(0, "string")]
		public string name = default(string);
	}

	public class RState : Schema.Schema
	{
		[Type(0, "string")]
		public string label = default(string);

		[Type(1, "number")]
		public double score = default(double);

		[Type(2, "float32")]
		public double ratio = default(double);

		[Type(3, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> order = new ArraySchema<string>();

		[Type(4, "array", typeof(ArraySchema<double>), "number")]
		public ArraySchema<double> nums = new ArraySchema<double>();

		[Type(5, "array", typeof(ArraySchema<RUnit>))]
		public ArraySchema<RUnit> units = new ArraySchema<RUnit>();

		[Type(6, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> names = new MapSchema<string>();
	}

	/// <summary>The same wire layout as older codegen emitted it: numbers as float, collections null.</summary>
	public class RStateF : Schema.Schema
	{
		[Type(0, "string")]
		public string label = default(string);

		[Type(1, "number")]
		public float score = default(float);

		[Type(2, "float32")]
		public float ratio = default(float);

		[Type(3, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> order = null;

		[Type(4, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> nums = null;

		[Type(5, "array", typeof(ArraySchema<RUnit>))]
		public ArraySchema<RUnit> units = null;

		[Type(6, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> names = null;
	}
}
