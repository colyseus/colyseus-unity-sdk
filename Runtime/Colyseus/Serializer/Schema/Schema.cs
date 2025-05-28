using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable InconsistentNaming

/***
  //Allowed primitive types:
  //  "string"
  //  "number"
  //  "boolean"
  //  "int8"
  //  "uint8"
  //  "int16"
  //  "uint16"
  //  "int32"
  //  "uint32"
  //  "int64"
  //  "uint64"
  //  "float32"
  //  "float64"

  //Allowed reference types:
  //  "ref"
  //  "array"
  //  "map"
***/

namespace Colyseus.Schema
{
	/// <summary>
	///     <see cref="Schema" /> <see cref="Attribute" /> wrapper class
	///     <para>Allowed primitive types:</para>
	///     <para>
	///         <em>
	///             "string", "number", "boolean", "int8", "uint8", "int16", "uint16", "int32", "uint32", "int64", "uint64",
	///             "float32", "float64"
	///         </em>
	///     </para>
	///     <para>Allowed reference types:</para>
	///     <para>
	///         <em>"ref", "array", "map"</em>
	///     </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class Type : Attribute
	{
		/// <summary>
		///     The <see cref="FieldType" /> of the <see cref="ChildType" />
		/// </summary>
		public string ChildPrimitiveType;

		/// <summary>
		///     What type of <see cref="Schema" /> this attribute is (can be null)
		/// </summary>
		public System.Type ChildType;

		/// <summary>
		///     The field type this <see cref="Attribute" /> represents
		/// </summary>
		public string FieldType;

		/// <summary>
		///     The index of where this will be stored in the <see cref="Schema" />
		/// </summary>
		public int Index;

		public Type(int index, string type, System.Type childType = null, string childPrimitiveType = null)
		{
			Index = index; // GetType().GetFields() doesn't guarantee order of fields, need to manually track them here!
			FieldType = type;
			ChildType = childType;
			ChildPrimitiveType = childPrimitiveType;
		}
	}

	/// <summary>
	///     Wrapper class containing an <see cref="int" /> offset value
	/// </summary>
	public class Iterator
	{
		/// <summary>
		///     The value used to offset when we encode/decode data
		/// </summary>
		public int Offset;
	}

	/// <summary>
	///     Byte flags used to signal specific operations to be performed on <see cref="Schema" /> data
	/// </summary>
	public enum SPEC : byte
	{
		/// <summary>
		///     A decode can be done, begin that process
		/// </summary>
		SWITCH_TO_STRUCTURE = 255,

		/// <summary>
		///     The following bytes will indicate the <see cref="Schema" /> type
		/// </summary>
		TYPE_ID = 213
	}

	/// <summary>
	///     Byte flags for <see cref="DataChange" /> operations that can be done
	/// </summary>
	[SuppressMessage("ReSharper", "MissingXmlDoc")]
	public enum OPERATION : byte
	{
		ADD = 128,
		REPLACE = 0,
		DELETE = 64,
		DELETE_AND_MOVE = 96, // ArraySchema
		DELETE_AND_ADD = 192,
		CLEAR = 10,

		/**
		 * ArraySchema operations
		 */
		REVERSE = 15,
		DELETE_BY_REFID = 33,
		ADD_BY_REFID = 129,
	}

	/// <summary>
	///     Wrapper class for a <see cref="Schema" /> change
	/// </summary>
	public class DataChange
	{
		/// <summary>
		///     The reference id of the data change
		/// </summary>
		public int RefId;

		/// <summary>
		///     The field index of the data change
		/// </summary>
		public object DynamicIndex;

		/// <summary>
		///     The field name of the data
		/// </summary>
		public string Field;

		/// <summary>
		///     An <see cref="OPERATION" /> flag for this DataChange
		/// </summary>
		public byte Op;

		/// <summary>
		///     The value of the old data
		/// </summary>
		public object PreviousValue;

		/// <summary>
		///     The value of the new data
		/// </summary>
		public object Value;
	}

	/// <summary>
	///     Interface for a collection of multiple <see cref="Schema" />s
	/// </summary>
	[SuppressMessage("ReSharper", "MissingXmlDoc")]
	public interface ISchemaCollection : IRef
	{
		bool HasSchemaChild { get; }
		string ChildPrimitiveType { get; set; }

		int Count { get; }
		object this[object key] { get; set; }

		IEnumerable GetItems();
		void ForEach(Action<object, object> action);
		void SetItems(object items);
		void Clear(List<DataChange> changes, ColyseusReferenceTracker refs);

		System.Type GetChildType();
		object GetTypeDefaultValue();

		ISchemaCollection Clone();
	}

	[SuppressMessage("ReSharper", "MissingXmlDoc")]
	public interface IArraySchema : ISchemaCollection
	{
		void OnDecodeEnd();
		void SetByIndex(int index, object value, byte operation);
		void Reverse();
	}

	[SuppressMessage("ReSharper", "MissingXmlDoc")]
	public interface IMapSchema : ISchemaCollection
	{
		void SetIndex(int index, object dynamicIndex);
		object GetIndex(int index);
		void SetByIndex(int index, object dynamicIndex, object value);
	}

	/// <summary>
	///     Interface for an object that can be tracked by a <see cref="ColyseusReferenceTracker" />
	/// </summary>
	[SuppressMessage("ReSharper", "MissingXmlDoc")]
	public interface IRef
	{
		/// <summary>
		///     The ID with which this <see cref="IRef" /> instance will be tracked
		/// </summary>
		int __refId { get; set; }

		object GetByIndex(int index);
		void DeleteByIndex(int index);
	}

	/// <summary>
	///     Data structure representing a <see cref="ColyseusRoom{T}" />'s state (synchronizeable data)
	/// </summary>
	public class Schema : IRef
	{
		private static readonly Dictionary<System.Type, Metadata> _cachedMetadata = new Dictionary<System.Type, Metadata>();

		private readonly Metadata _metadata;

		/// <summary>
		///     Map of the <see cref="Type.ChildPrimitiveType" />s that this schema uses
		/// </summary>
		internal Dictionary<string, string> fieldChildPrimitiveTypes => _metadata.FieldChildPrimitiveTypes;

		/// <summary>
		///     Map of the <see cref="Type.ChildType" />s that this schema uses
		/// </summary>
		internal Dictionary<string, System.Type> fieldChildTypes => _metadata.FieldChildTypes;

		/// <summary>
		///     Map of the fields in this schema using {<see cref="Type.Index" />,
		/// </summary>
		internal Dictionary<int, string> fieldsByIndex => _metadata.FieldsByIndex;

		/// <summary>
		///     Map of the field types in this schema
		/// </summary>
		internal Dictionary<string, string> fieldTypes => _metadata.FieldTypes;

		public Schema()
		{
			var type = GetType();
			if (!_cachedMetadata.TryGetValue(type, out _metadata))
			{
				_metadata = CreateMetadata(type);
				_cachedMetadata.Add(type, _metadata);
			}
		}


		private static Metadata CreateMetadata(System.Type type)
		{
			var metadata = new Metadata();

			FieldInfo[] fields = type.GetFields();
			foreach (FieldInfo field in fields)
			{
				object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
				for (int i = 0; i < typeAttributes.Length; i++)
				{
					Type t = (Type)typeAttributes[i];
					metadata.FieldsByIndex.Add(t.Index, field.Name);
					metadata.FieldTypes.Add(field.Name, t.FieldType);

					if (t.ChildPrimitiveType != null)
					{
						metadata.FieldChildPrimitiveTypes.Add(field.Name, t.ChildPrimitiveType);
					}

					if (t.ChildType != null)
					{
						metadata.FieldChildTypes.Add(field.Name, t.ChildType);
					}
				}
			}

			return metadata;
		}

		/// <summary>
		///     Allow get and set of property values by its <paramref name="propertyName" />
		/// </summary>
		/// <param name="propertyName">The object's field name</param>
		public object this[string propertyName]
		{
			get { return GetType().GetField(propertyName).GetValue(this); }
			set
			{
				FieldInfo field = GetType().GetField(propertyName);
				field.SetValue(this, value);
			}
		}

		/// <summary>
		///     <see cref="IRef" /> implementation - ID with which to reference this <see cref="Schema" />
		/// </summary>
		public int __refId { get; set; }

		/// <summary>
		///     Get a field by it's index
		/// </summary>
		/// <param name="index">Index of the field to get</param>
		/// <returns>The <see cref="object" /> at that index (if it exists)</returns>
		public object GetByIndex(int index)
		{
			fieldsByIndex.TryGetValue(index, out var fieldName);
			return this[fieldName];
		}

		/// <summary>
		///     Remove the field by it's index
		/// </summary>
		/// <param name="index">Index of the field to remove</param>
		public void DeleteByIndex(int index)
		{
			fieldsByIndex.TryGetValue(index, out var fieldName);
			this[fieldName] = null;
		}

		/// <summary>
		///     Getter function, required for <see cref="ColyseusReferenceTracker.GarbageCollection" />
		/// </summary>
		/// <returns>
		///     <see cref="fieldChildTypes" />
		/// </returns>
		internal Dictionary<string, System.Type> GetFieldChildTypes()
		{
			// This is required for "garbage collection" inside ReferenceTracker.
			return fieldChildTypes;
		}

		/// <summary>
		///     Check if this <see cref="Schema" /> has a <see cref="Schema" /> child
		/// </summary>
		/// <param name="toCheck"><see cref="Schema" /> type to check for</param>
		/// <returns>True if found, false otherwise</returns>
		public static bool CheckSchemaChild(System.Type toCheck)
		{
			System.Type generic = typeof(Schema);

			while (toCheck != null && toCheck != typeof(object))
			{
				System.Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

				if (generic == cur)
				{
					return true;
				}

				toCheck = toCheck.BaseType;
			}

			return false;
		}

		/// <summary>
		///		Metadata for Schema type
		/// </summary>
		private class Metadata
		{
			/// <summary>
			///     Map of the <see cref="Type.ChildPrimitiveType" />s that this schema uses
			/// </summary>
			public Dictionary<string, string> FieldChildPrimitiveTypes = new Dictionary<string, string>();

			/// <summary>
			///     Map of the <see cref="Type.ChildType" />s that this schema uses
			/// </summary>
			public Dictionary<string, System.Type> FieldChildTypes = new Dictionary<string, System.Type>();

			/// <summary>
			///     Map of the fields in this schema using {<see cref="Type.Index" />,
			/// </summary>
			public Dictionary<int, string> FieldsByIndex = new Dictionary<int, string>();

			/// <summary>
			///     Map of the field types in this schema
			/// </summary>
			public Dictionary<string, string> FieldTypes = new Dictionary<string, string>();
		}
	}
}
