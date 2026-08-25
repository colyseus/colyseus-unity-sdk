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
		public Dictionary<string, Utils.QuantizeDescriptor> FieldQuantizedDescriptors = new Dictionary<string, Utils.QuantizeDescriptor>();

		/// <summary>
		///     Parses a ReflectionField into the appropriate dictionaries (5.0
		///     reflection format — see PORTING/sdk-ports-quantized-reflection.md).
		/// </summary>
		public void ParseFieldType(ReflectionField field, int fieldIndex)
		{
			var fieldName = field.name;
			var fieldType = field.type;
			var referencedType = field.referencedType;

			FieldsByIndex[fieldIndex] = fieldName;

			if (field.quantized != null)
			{
				// schema-typed descriptor → resolved codec
				FieldTypes[fieldName] = "quantized";
				FieldQuantizedDescriptors[fieldName] = Utils.Quantize.Resolve(
					field.quantized.min, field.quantized.max, field.quantized.bits, field.quantized.mode == 1);
				return;
			}

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
			else
			{
				// Primitive types: "string", "int32", "float32", "number", "boolean", etc.
				FieldTypes[fieldName] = fieldType;
			}

			// 5.0: primitive collection children ride their own childPrimitive
			// slot (the 4.x "array:string" colon packing is gone)
			if (referencedType < 0 && (fieldType == "map" || fieldType == "array"))
			{
				FieldChildPrimitiveTypes[fieldName] = field.childPrimitive;
				FieldChildTypes[fieldName] = fieldType == "map"
					? typeof(MapSchema<object>)
					: typeof(ArraySchema<object>);
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

		internal override Dictionary<string, Utils.QuantizeDescriptor> fieldQuantizedDescriptors =>
			Definition?.FieldQuantizedDescriptors ?? _emptyFieldQuantizedDescriptors;

		/// <summary>
		///     Values are boxed into <see cref="_values" />, so nothing here needs narrowing — which also
		///     matches the server, where every <c>"number"</c> is a float64.
		/// </summary>
		internal override bool AcceptsWideNumber(int index) => true;

		private static readonly Dictionary<string, Utils.QuantizeDescriptor> _emptyFieldQuantizedDescriptors =
			new Dictionary<string, Utils.QuantizeDescriptor>();

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
