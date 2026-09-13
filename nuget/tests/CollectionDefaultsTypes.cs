using Colyseus.Schema;

// Hand-written in the shape schema-codegen emits: collections default-initialized,
// "number" and "float32" as double. Wire layout mirrors
// Fixtures/generate-collection-fixtures.ts.
namespace Colyseus.Tests.CollectionDefaults
{
	public class DItem : Schema.Schema
	{
		[Type(0, "uint8")]
		public byte v = default(byte);
	}

	public class DHero : Schema.Schema
	{
		[Type(0, "number")]
		public double x = default(double);

		[Type(1, "uint8")]
		public byte hp = default(byte);

		[Type(2, "array", typeof(ArraySchema<DItem>))]
		public ArraySchema<DItem> loot = new ArraySchema<DItem>();
	}

	public class DState : Schema.Schema
	{
		[Type(0, "map", typeof(MapSchema<DHero>))]
		public MapSchema<DHero> heroes = new MapSchema<DHero>();

		[Type(1, "uint16")]
		public ushort tick = default(ushort);

		[Type(2, "map", typeof(MapSchema<DHero>))]
		public MapSchema<DHero> open = new MapSchema<DHero>();
	}

	/// <summary>float32 on the wire, received into double destinations.</summary>
	public class WState : Schema.Schema
	{
		[Type(0, "float32")]
		public double f = default(double);

		[Type(1, "array", typeof(ArraySchema<double>), "float32")]
		public ArraySchema<double> nums = new ArraySchema<double>();

		[Type(2, "map", typeof(MapSchema<double>), "float32")]
		public MapSchema<double> tags = new MapSchema<double>();
	}

	/// <summary>A reconciled entity with a float32 heading, declared float (older codegen output) and double.</summary>
	public class HeadingF : Schema.Schema
	{
		[Type(0, "number")]
		public double x = default(double);

		[Type(1, "float32")]
		public float dir = default(float);
	}

	public class HeadingD : Schema.Schema
	{
		[Type(0, "number")]
		public double x = default(double);

		[Type(1, "float32")]
		public double dir = default(double);
	}

	public class TurnInput : Schema.Schema
	{
		[Type(0, "number")]
		public double turn = default(double);
	}
}
