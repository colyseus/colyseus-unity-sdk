using System;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	/// <summary>
	///     Per-type metadata built from server handshake Reflection data.
	///     Analogous to a generated Schema subclass's [Type] attributes,
	///     but constructed at runtime.
	/// </summary>
	public class DynamicTypeDefinition
	{
		public float TypeId;
		public Dictionary<int, string> FieldsByIndex = new Dictionary<int, string>();
		public Dictionary<string, string> FieldTypes = new Dictionary<string, string>();
		public Dictionary<string, System.Type> FieldChildTypes = new Dictionary<string, System.Type>();
		public Dictionary<string, string> FieldChildPrimitiveTypes = new Dictionary<string, string>();
		public Dictionary<string, float> FieldReferencedTypes = new Dictionary<string, float>();

		/// <summary>
		///     Parses a ReflectionField into the appropriate dictionaries.
		/// </summary>
		/// <param name="fieldIndex">The field index from the reflection data</param>
		/// <param name="fieldName">The field name</param>
		/// <param name="fieldType">The field type string (e.g. "ref", "map", "array", "string", "int32")</param>
		/// <param name="referencedType">The referenced type id, or -1 for primitives</param>
		public void ParseFieldType(int fieldIndex, string fieldName, string fieldType, float referencedType)
		{
			FieldsByIndex[fieldIndex] = fieldName;

			if (referencedType >= 0)
			{
				FieldReferencedTypes[fieldName] = referencedType;
			}

			if (fieldType == "ref")
			{
				FieldTypes[fieldName] = "ref";
				if (referencedType >= 0)
				{
					FieldChildTypes[fieldName] = typeof(DynamicSchema);
				}
			}
			else if (fieldType == "map")
			{
				FieldTypes[fieldName] = "map";
				if (referencedType >= 0)
				{
					FieldChildTypes[fieldName] = typeof(MapSchema<DynamicSchema>);
				}
			}
			else if (fieldType == "array")
			{
				FieldTypes[fieldName] = "array";
				if (referencedType >= 0)
				{
					FieldChildTypes[fieldName] = typeof(ArraySchema<DynamicSchema>);
				}
			}
			else if (fieldType.Contains(":"))
			{
				// Collection of primitives: "map:string", "array:number", etc.
				var parts = fieldType.Split(':');
				var collectionType = parts[0];
				var primitiveType = parts[1];

				FieldTypes[fieldName] = collectionType;
				FieldChildPrimitiveTypes[fieldName] = primitiveType;

				if (collectionType == "map")
				{
					FieldChildTypes[fieldName] = typeof(MapSchema<object>);
				}
				else if (collectionType == "array")
				{
					FieldChildTypes[fieldName] = typeof(ArraySchema<object>);
				}
			}
			else
			{
				// Primitive types: "string", "int32", "float32", "number", "boolean", etc.
				FieldTypes[fieldName] = fieldType;
			}
		}
	}

	/// <summary>
	///     A Schema subclass that stores values in a dictionary rather than
	///     requiring compile-time generated fields. Use with Room&lt;DynamicSchema&gt;
	///     to skip code generation entirely.
	/// </summary>
	public class DynamicSchema : Schema
	{
		internal DynamicTypeDefinition Definition;

		private Dictionary<string, object> _values = new Dictionary<string, object>();

		/// <summary>
		///     Parameterless constructor required for Activator.CreateInstance
		/// </summary>
		public DynamicSchema() { }

		public override object this[string propertyName]
		{
			get
			{
				_values.TryGetValue(propertyName, out var value);
				return value;
			}
			set
			{
				_values[propertyName] = value;
			}
		}

		internal override Dictionary<int, string> fieldsByIndex =>
			Definition?.FieldsByIndex ?? _emptyFieldsByIndex;

		internal override Dictionary<string, string> fieldTypes =>
			Definition?.FieldTypes ?? _emptyFieldTypes;

		internal override Dictionary<string, System.Type> fieldChildTypes =>
			Definition?.FieldChildTypes ?? _emptyFieldChildTypes;

		internal override Dictionary<string, string> fieldChildPrimitiveTypes =>
			Definition?.FieldChildPrimitiveTypes ?? _emptyFieldChildPrimitiveTypes;

		/// <summary>
		///     Typed convenience accessor for field values.
		/// </summary>
		public T Get<T>(string fieldName)
		{
			_values.TryGetValue(fieldName, out var value);
			if (value == null)
			{
				return default(T);
			}
			if (value is T typedValue)
			{
				return typedValue;
			}
			return (T)Convert.ChangeType(value, typeof(T));
		}

		// Shared empty dictionaries to avoid allocations when Definition is null
		private static readonly Dictionary<int, string> _emptyFieldsByIndex = new Dictionary<int, string>();
		private static readonly Dictionary<string, string> _emptyFieldTypes = new Dictionary<string, string>();
		private static readonly Dictionary<string, System.Type> _emptyFieldChildTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, string> _emptyFieldChildPrimitiveTypes = new Dictionary<string, string>();
	}
}
