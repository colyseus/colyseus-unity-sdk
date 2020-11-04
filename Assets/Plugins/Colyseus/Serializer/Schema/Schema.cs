using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

/***
  Allowed primitive types:
    "string"
    "number"
    "boolean"
    "int8"
    "uint8"
    "int16"
    "uint16"
    "int32"
    "uint32"
    "int64"
    "uint64"
    "float32"
    "float64"

  Allowed reference types:
    "ref"
    "array"
    "map"
***/

namespace Colyseus.Schema
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class Type : Attribute
	{
		public int Index;
		public string FieldType;
		public System.Type ChildType;
		public string ChildPrimitiveType;

		public Type(int index, string type, System.Type childType = null, string childPrimitiveType = null)
		{
			Index = index; // GetType().GetFields() doesn't guarantee order of fields, need to manually track them here!
			FieldType = type;
			ChildType = childType;
			ChildPrimitiveType = childPrimitiveType;
		}
	}

	public class Iterator
	{
		public int Offset = 0;
	}

	public enum SPEC : byte
	{
		SWITCH_TO_STRUCTURE = 255,
		TYPE_ID = 213
	}

	public enum OPERATION : byte
	{
		ADD = 128,
		REPLACE = 0,
		DELETE = 64,
		DELETE_AND_ADD = 192,
		CLEAR = 10,
	}

	public class DataChange
	{
		public byte Op;
		public string Field;
		public object DynamicIndex;
		public object Value;
		public object PreviousValue;
	}

	public delegate void OnChangeEventHandler(List<DataChange> changes);
	public delegate void KeyValueEventHandler<T, K>(T value, K key);
	public delegate void OnRemoveEventHandler();

	public interface ISchemaCollection : IRef
	{
		void InvokeOnAdd(object item, object index);
		void InvokeOnChange(object item, object index);
		void InvokeOnRemove(object item, object index);

		IDictionary GetItems();
		void SetItems(object items);
		void TriggerAll();
		void Clear(ReferenceTracker refs);

		System.Type GetChildType();
		object GetTypeDefaultValue();
		bool ContainsKey(object key);

		bool HasSchemaChild { get; }
		string ChildPrimitiveType { get; set; }

		int Count { get; }
		object this[object key] { get; set; }

		void SetIndex(int index, object dynamicIndex);
		object GetIndex(int index);
		void SetByIndex(int index, object dynamicIndex, object value);

		ISchemaCollection Clone();
	}

	public interface IRef
	{
		int __refId { get; set; }

		object GetByIndex(int index);
		void DeleteByIndex(int index);

		void MoveEventHandlers(IRef previousInstance);
	}

	public class Schema : IRef
	{
		protected Dictionary<int, string> fieldsByIndex = new Dictionary<int, string>();
		protected Dictionary<string, string> fieldTypes = new Dictionary<string, string>();
		protected Dictionary<string, string> fieldChildPrimitiveTypes = new Dictionary<string, string>();
		protected Dictionary<string, System.Type> fieldChildTypes = new Dictionary<string, System.Type>();

		public event OnChangeEventHandler OnChange;
		public event OnRemoveEventHandler OnRemove;

		public int __refId { get; set; }

		private ReferenceTracker refs;

		public Schema()
		{
			FieldInfo[] fields = GetType().GetFields();
			foreach (FieldInfo field in fields)
			{
				object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
				for (var i = 0; i < typeAttributes.Length; i++)
				{
					Type t = (Type)typeAttributes[i];
					fieldsByIndex.Add(t.Index, field.Name);
					fieldTypes.Add(field.Name, t.FieldType);

					if (t.ChildPrimitiveType != null)
					{
						fieldChildPrimitiveTypes.Add(field.Name, t.ChildPrimitiveType);
					}

					if (t.ChildType != null)
					{
						fieldChildTypes.Add(field.Name, t.ChildType);
					}
				}
			}
		}

		/* allow to retrieve property values by its string name */
		public object this[string propertyName]
		{
			get
			{
				return GetType().GetField(propertyName).GetValue(this);
			}
			set
			{
				var field = GetType().GetField(propertyName);
				field.SetValue(this, value);
			}
		}

		public Dictionary<string, System.Type> GetFieldChildTypes()
		{
			// This is required for "garbage collection" inside ReferenceTracker.
			return fieldChildTypes;
		}

		public void MoveEventHandlers(IRef previousInstance)
		{
			OnChange = ((Schema)previousInstance).OnChange;
			OnRemove = ((Schema)previousInstance).OnRemove;

			foreach (var item in ((Schema)previousInstance).fieldsByIndex)
			{
				var child = GetByIndex(item.Key);
				if (child is IRef)
				{
					((IRef)child).MoveEventHandlers((IRef)previousInstance.GetByIndex(item.Key));
				}
			}
		}

		public void Decode(byte[] bytes, Iterator it = null, ReferenceTracker refs = null)
		{
			var decode = Decoder.GetInstance();

			if (it == null) { it = new Iterator(); }
			if (refs == null) { refs = new ReferenceTracker(); }

			var totalBytes = bytes.Length;

			int refId = 0;
			IRef _ref = this;

			this.refs = refs;
			refs.Add(refId, _ref);

			var changes = new List<DataChange>();
			var allChanges = new OrderedDictionary(); // Dictionary<int, List<DataChange>>
			allChanges.Add(refId, changes);

			while (it.Offset < totalBytes)
			{
				var _byte = bytes[it.Offset++];

				if (_byte == (byte)SPEC.SWITCH_TO_STRUCTURE)
				{
					refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));
					_ref = refs.Get(refId);

					//
					// Trying to access a reference that haven't been decoded yet.
					//
					if (_ref == null) { throw new Exception("refId not found: " + refId); }

					// create empty list of changes for this refId.
					changes = new List<DataChange>();
					allChanges[(object)refId] = changes;

					continue;
				}

				bool isSchema = _ref is Schema;

				var operation = (byte) ((isSchema)
					? (_byte >> 6) << 6 // "compressed" index + operation
					: _byte); // "uncompressed" index + operation (array/map items)

				if (operation == (byte)OPERATION.CLEAR)
				{
					((ISchemaCollection)_ref).Clear(refs);
					continue;
				}

				int fieldIndex;
				string fieldName = null;
				string fieldType = null;

				System.Type childType = null;

				if (isSchema)
				{
					fieldIndex = _byte % ((operation == 0) ? 255 : operation); // FIXME: JS allows (0 || 255)
					((Schema)_ref).fieldsByIndex.TryGetValue(fieldIndex, out fieldName);

					// fieldType = ((Schema)_ref).fieldTypes[fieldName];
					((Schema)_ref).fieldTypes.TryGetValue(fieldName ?? "", out fieldType);
					((Schema)_ref).fieldChildTypes.TryGetValue(fieldName ?? "", out childType);
				}
				else
				{
					fieldName = ""; // FIXME

					fieldIndex = Convert.ToInt32(decode.DecodeNumber(bytes, it));
					if (((ISchemaCollection)_ref).HasSchemaChild)
					{
						fieldType = "ref";
						childType = ((ISchemaCollection)_ref).GetChildType();
					}
					else
					{
						fieldType = ((ISchemaCollection)_ref).ChildPrimitiveType;
					}
				}

				object value = null;
				object previousValue = null;
				object dynamicIndex = null;

				if (!isSchema)
				{
					previousValue = _ref.GetByIndex(fieldIndex);

					if ((operation & (byte)OPERATION.ADD) == (byte)OPERATION.ADD)
					{
						// MapSchema dynamic index.
						dynamicIndex = (((ISchemaCollection)_ref).GetItems() is OrderedDictionary)
							? (object)decode.DecodeString(bytes, it)
							: fieldIndex;

						((ISchemaCollection)_ref).SetIndex(fieldIndex, dynamicIndex);
					} else
					{
						dynamicIndex = ((ISchemaCollection)_ref).GetIndex(fieldIndex);
					}
				}
				else if (fieldName != null) // FIXME: duplicate check
				{
					previousValue = ((Schema)_ref)[fieldName];
				}

				//
				// Delete operations
				//
				if ((operation & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE)
				{
					if (operation != (byte)OPERATION.DELETE_AND_ADD)
					{
						_ref.DeleteByIndex(fieldIndex);
					}

					// Flag `refId` for garbage collection.
					if (previousValue != null && previousValue is IRef)
					{
						refs.Remove(((IRef)previousValue).__refId);
					}

					value = null;
				}

				if (fieldName == null)
				{
					//
					// keep skipping next bytes until reaches a known structure
					// by local decoder.
					//
					Iterator nextIterator = new Iterator() { Offset = it.Offset };

					while (it.Offset < totalBytes)
					{
						if (decode.SwitchStructureCheck(bytes, it))
						{
							nextIterator.Offset = it.Offset + 1;
							if (refs.Has(Convert.ToInt32(decode.DecodeNumber(bytes, nextIterator)))) {
								break;
							}
						}

						it.Offset++;
					}

					continue;

				}
				else if (operation == (byte)OPERATION.DELETE)
				{
					//
					// FIXME: refactor me.
					// Don't do anything.
					//
				}
				else if (fieldType == "ref")
				{
					refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));
					value = refs.Get(refId);

					if (operation != (byte)OPERATION.REPLACE)
					{
						var concreteChildType = GetSchemaType(bytes, it, childType);

						if (value == null)
						{
							value = CreateTypeInstance(concreteChildType);

							if (previousValue != null)
							{
								((Schema)value).MoveEventHandlers((Schema)previousValue);

								if (
									((IRef)previousValue).__refId > 0 &&
									refId != ((IRef)previousValue).__refId
								)
								{
									refs.Remove(((IRef)previousValue).__refId);
								}
							}
						}

						refs.Add(refId, (IRef)value, (value != previousValue));
					}
				}
				else if (childType == null)
				{
					// primitive values
					value = decode.DecodePrimitiveType(fieldType, bytes, it);
				}
				else
				{
					refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));
					value = refs.Get(refId);

					ISchemaCollection valueRef = (refs.Has(refId))
						? (ISchemaCollection)previousValue
						: (ISchemaCollection)Activator.CreateInstance(childType);

					value = valueRef.Clone();

					// keep reference to nested childPrimitiveType.
					string childPrimitiveType;
					((Schema)_ref).fieldChildPrimitiveTypes.TryGetValue(fieldName, out childPrimitiveType);
					((ISchemaCollection)value).ChildPrimitiveType = childPrimitiveType;

					if (previousValue != null)
					{
						((ISchemaCollection)value).MoveEventHandlers(((ISchemaCollection)previousValue));

						if (
							((IRef)previousValue).__refId > 0 &&
							refId != ((IRef)previousValue).__refId
						)
						{
							refs.Remove(((IRef)previousValue).__refId);

							var deletes = new List<DataChange>();
							var items = ((ISchemaCollection)previousValue).GetItems();

							foreach (var key in items.Keys)
							{
								deletes.Add(new DataChange()
								{
									DynamicIndex = key,
									Op = (byte)OPERATION.DELETE,
									Value = null,
									PreviousValue = items[key]
								});
							}

							allChanges[(object)((IRef)previousValue).__refId] = deletes;
						}
					}

					refs.Add(refId, (IRef)value, (valueRef != previousValue));
				}

				bool hasChange = (previousValue != value);

				if (value != null)
				{
					if (value is IRef)
					{
						((IRef)value).__refId = refId;
					}

					if (_ref is Schema)
					{
						((Schema)_ref)[fieldName] = value;
					}
					else if (_ref is ISchemaCollection)
					{
						((ISchemaCollection)_ref).SetByIndex(fieldIndex, dynamicIndex, value);
					}
				}

				if (hasChange)
				{
					changes.Add(new DataChange
					{
						Op = operation,
						Field = fieldName,
						DynamicIndex = dynamicIndex,
						Value = value,
						PreviousValue = previousValue
					});
				}
			}

			TriggerChanges(ref allChanges);

			refs.GarbageCollection();
		}

		public void TriggerAll()
		{
			//
			// first state not received from the server yet.
			// nothing to trigger.
			//
			if (refs == null) { return;  }

			var allChanges = new OrderedDictionary();
			TriggerAllFillChanges(this, ref allChanges);
			TriggerChanges(ref allChanges);
		}

		protected void TriggerAllFillChanges(IRef currentRef, ref OrderedDictionary allChanges)
		{
			// skip recursive structures...
			if (allChanges.Contains(currentRef.__refId)) { return; }

			var changes = new List<DataChange>();
			allChanges[(object)currentRef.__refId] = changes;

			if (currentRef is Schema)
			{
				foreach (var fieldName in ((Schema)currentRef).fieldsByIndex.Values)
				{
					var value = ((Schema)currentRef)[fieldName];
					changes.Add(new DataChange
					{
						Field = fieldName,
						Op = (byte)OPERATION.ADD,
						Value = value
					});

					if (value is IRef)
					{
						TriggerAllFillChanges((IRef)value, ref allChanges);
					}
				}
			} else
			{
				var items = ((ISchemaCollection)currentRef).GetItems();
				foreach (object key in items.Keys)
				{
					var child = items[key];

					changes.Add(new DataChange
					{
						Field = (string) key,
						DynamicIndex = key,
						Op = (byte)OPERATION.ADD,
						Value = child
					});

					if (child is IRef)
					{
						TriggerAllFillChanges((IRef)child, ref allChanges);
					}
				}
			}
		}

		protected void TriggerChanges(ref OrderedDictionary allChanges)
		{
			foreach (object refId in allChanges.Keys)
			{
				List<DataChange> changes = (List<DataChange>)allChanges[refId];

				IRef _ref = refs.Get((int)refId);
				bool isSchema = _ref is Schema;

				foreach (DataChange change in changes)
				{
					//const listener = ref['$listeners'] && ref['$listeners'][change.field];

					if (!isSchema)
					{
						ISchemaCollection container = ((ISchemaCollection)_ref);

						if (change.Op == (byte)OPERATION.ADD && change.PreviousValue == container.GetTypeDefaultValue())
						{
							container.InvokeOnAdd(change.Value, change.DynamicIndex);

						}
						else if (change.Op == (byte)OPERATION.DELETE)
						{
							//
							// FIXME: `previousValue` should always be avaiiable.
							// ADD + DELETE operations are still encoding DELETE operation.
							//
							if (change.PreviousValue != container.GetTypeDefaultValue())
							{
								container.InvokeOnRemove(change.PreviousValue, change.DynamicIndex ?? change.Field);
							}

						}
						else if (change.Op == (byte)OPERATION.DELETE_AND_ADD)
						{
							if (change.PreviousValue != container.GetTypeDefaultValue())
							{
								container.InvokeOnRemove(change.PreviousValue, change.DynamicIndex);
							}
							container.InvokeOnAdd(change.Value, change.DynamicIndex);

						}
						else if (
							change.Op == (byte)OPERATION.REPLACE ||
							change.Value != change.PreviousValue
						)
						{
							container.InvokeOnChange(change.Value, change.DynamicIndex);
						}
					}

					//
					// trigger onRemove on child structure.
					//
					if (
						(change.Op & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE &&
						change.PreviousValue is Schema
					)
					{
						((Schema)change.PreviousValue).OnRemove?.Invoke();
					}
				}

				if (isSchema)
				{
					((Schema)_ref).OnChange?.Invoke(changes);
				}
			}
		}

		protected System.Type GetSchemaType(byte[] bytes, Iterator it, System.Type defaultType)
		{
			System.Type type = defaultType;

			if (bytes[it.Offset] == (byte)SPEC.TYPE_ID)
			{
				it.Offset++;
				int typeId = Convert.ToInt32(Decoder.GetInstance().DecodeNumber(bytes, it));
				type = Context.GetInstance().Get(typeId);
			}

			return type;
		}

		protected object CreateTypeInstance(System.Type type)
		{
			return Activator.CreateInstance(type);
		}

		public object GetByIndex(int index)
		{
			string fieldName;
			fieldsByIndex.TryGetValue(index, out fieldName);
			return this[fieldName];
		}

		public void DeleteByIndex(int index)
		{
			string fieldName;
			fieldsByIndex.TryGetValue(index, out fieldName);
			this[fieldName] = null;
		}

		public static bool CheckSchemaChild(System.Type toCheck)
		{
			System.Type generic = typeof(Schema);

			while (toCheck != null && toCheck != typeof(object))
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

				if (generic == cur)
				{
					return true;
				}

				toCheck = toCheck.BaseType;
			}

			return false;
		}
	}

}
