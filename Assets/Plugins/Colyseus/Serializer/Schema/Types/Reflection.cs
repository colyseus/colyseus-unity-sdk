namespace Colyseus.Schema
{
	public class ReflectionField : Schema
	{
		[Type(0, "string")]
		public string name;

		[Type(1, "string")]
		public string type;

		[Type(2, "number")]
		public float referencedType;
	}

	public class ReflectionType : Schema
	{
		[Type(0, "number")]
		public float id;

		[Type(1, "array", typeof(ArraySchema<ReflectionField>))]
		public ArraySchema<ReflectionField> fields = new ArraySchema<ReflectionField>();
	}

	public class Reflection : Schema
	{
		[Type(0, "array", typeof(ArraySchema<ReflectionType>))]
		public ArraySchema<ReflectionType> types = new ArraySchema<ReflectionType>();

		[Type(1, "number")]
		public float rootType;
	}
}