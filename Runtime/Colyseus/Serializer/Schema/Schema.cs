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
        DELETE_AND_ADD = 192,
        CLEAR = 10
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

        void InvokeOnAdd(object item, object index);
        void InvokeOnChange(object item, object index);
        void InvokeOnRemove(object item, object index);

        IDictionary GetItems();
        void SetItems(object items);
        void Clear(ref List<DataChange> changes, ref ColyseusReferenceTracker refs);

        System.Type GetChildType();
        object GetTypeDefaultValue();
        bool ContainsKey(object key);

        void SetIndex(int index, object dynamicIndex);
        object GetIndex(int index);
        void SetByIndex(int index, object dynamicIndex, object value);

        ISchemaCollection Clone();
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

        bool HasCallbacks();
        void MoveEventHandlers(IRef previousInstance);
    }

    /// <summary>
    ///     Data structure representing a <see cref="ColyseusRoom{T}" />'s state (synchronizeable data)
    /// </summary>
    public class Schema : IRef
    {
        public SchemaCallbacks __callbacks = null;

        /// <summary>
        ///     Map of the <see cref="Type.ChildPrimitiveType" />s that this schema uses
        /// </summary>
        protected Dictionary<string, string> fieldChildPrimitiveTypes = new Dictionary<string, string>();

        /// <summary>
        ///     Map of the <see cref="Type.ChildType" />s that this schema uses
        /// </summary>
        protected Dictionary<string, System.Type> fieldChildTypes = new Dictionary<string, System.Type>();

        /// <summary>
        ///     Map of the fields in this schema using {<see cref="Type.Index" />,
        /// </summary>
        protected Dictionary<int, string> fieldsByIndex = new Dictionary<int, string>();

        /// <summary>
        ///     Map of the field types in this schema
        /// </summary>
        protected Dictionary<string, string> fieldTypes = new Dictionary<string, string>();

        private ColyseusReferenceTracker refs;

        public Schema()
        {
            FieldInfo[] fields = GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
                for (int i = 0; i < typeAttributes.Length; i++)
                {
                    Type t = (Type) typeAttributes[i];
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
        ///     Attaches a callback that is triggered whenever a property on this Schema instance applies a change from the server.
        /// </summary>
		/// <returns>An Action that, when called, removes the registered callback</returns>
        public Action OnChange (OnChangeEventHandler handler)
		{
            if (__callbacks == null) __callbacks = new SchemaCallbacks();

            __callbacks.OnChange += handler;

            return () => __callbacks.OnChange -= handler;
        }

        /// <summary>
        ///     Attaches a callback that is triggered whenever this Schema instance has been removed from the server.
        /// </summary>
		/// <returns>An Action that, when called, removes the registered callback</returns>
        public Action OnRemove (OnRemoveEventHandler handler)
        {
            if (__callbacks == null) __callbacks = new SchemaCallbacks();

            __callbacks.OnRemove += handler;

            return () => __callbacks.OnRemove -= handler;
        }

        ///// <inheritdoc cref="OnChangeEventHandler" />
        //public event OnChangeEventHandler OnChange;

        ///// <inheritdoc cref="OnRemoveEventHandler" />
        //public event OnRemoveEventHandler OnRemove;

        /// <summary>
        ///     Update this <see cref="Schema" />'s EventHandlers
        /// </summary>
        /// <param name="previousInstance">The instance of an <see cref="IRef" /> from which we will copy the EventHandlers</param>
        public void MoveEventHandlers(IRef previousInstance)
        {
            __callbacks = ((Schema)previousInstance).__callbacks;

            foreach (KeyValuePair<int, string> item in ((Schema) previousInstance).fieldsByIndex)
            {
                object child = GetByIndex(item.Key);
                if (child is IRef)
                {
                    ((IRef) child).MoveEventHandlers((IRef) previousInstance.GetByIndex(item.Key));
                }
            }
        }

        /// <summary>
        ///     Get a field by it's index
        /// </summary>
        /// <param name="index">Index of the field to get</param>
        /// <returns>The <see cref="object" /> at that index (if it exists)</returns>
        public object GetByIndex(int index)
        {
            string fieldName;
            fieldsByIndex.TryGetValue(index, out fieldName);
            return this[fieldName];
        }

        /// <summary>
        ///     Remove the field by it's index
        /// </summary>
        /// <param name="index">Index of the field to remove</param>
        public void DeleteByIndex(int index)
        {
            string fieldName;
            fieldsByIndex.TryGetValue(index, out fieldName);
            this[fieldName] = null;
        }

        /// <summary>
        ///     Getter function, required for <see cref="ColyseusReferenceTracker.GarbageCollection" />
        /// </summary>
        /// <returns>
        ///     <see cref="fieldChildTypes" />
        /// </returns>
        public Dictionary<string, System.Type> GetFieldChildTypes()
        {
            // This is required for "garbage collection" inside ReferenceTracker.
            return fieldChildTypes;
        }

        /// <summary>
        ///     Decode incoming data
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it"><see cref="Iterator" /> used to decode. If null, will create a new one</param>
        /// <param name="refs">
        ///     <see cref="ColyseusReferenceTracker" /> for all refs found through the decoding process. If null, will
        ///     create a new one
        /// </param>
        /// <exception cref="Exception">If no decoding fails</exception>
        public void Decode(byte[] bytes, Iterator it = null, ColyseusReferenceTracker refs = null)
        {
            ColyseusDecoder decode = ColyseusDecoder.GetInstance();

            if (it == null)
            {
                it = new Iterator();
            }

            if (refs == null)
            {
                refs = new ColyseusReferenceTracker();
            }

            int totalBytes = bytes.Length;

            int refId = 0;
            IRef _ref = this;

            this.refs = refs;
            refs.Add(refId, _ref);

            List<DataChange> allChanges = new List<DataChange>();

            while (it.Offset < totalBytes)
            {
                byte _byte = bytes[it.Offset++];

                if (_byte == (byte) SPEC.SWITCH_TO_STRUCTURE)
                {
                    refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));
                    _ref = refs.Get(refId);

                    //
                    // Trying to access a reference that haven't been decoded yet.
                    //
                    if (_ref == null)
                    {
                        throw new Exception("refId not found: " + refId);
                    }

                    // create empty list of changes for this refId.
                    //allChanges = new List<DataChange>();
                    //allChanges[(object) refId] = allChanges;

                    continue;
                }

                bool isSchema = _ref is Schema;

                byte operation = (byte) (isSchema
                    ? (_byte >> 6) << 6 // "compressed" index + operation
                    : _byte); // "uncompressed" index + operation (array/map items)

                if (operation == (byte) OPERATION.CLEAR)
                {
                    ((ISchemaCollection) _ref).Clear(ref allChanges, ref refs);
                    continue;
                }

                int fieldIndex;
                string fieldName = null;
                string fieldType = null;

                System.Type childType = null;

                if (isSchema)
                {
                    fieldIndex = _byte % (operation == 0 ? 255 : operation); // FIXME: JS allows (0 || 255)
                    ((Schema) _ref).fieldsByIndex.TryGetValue(fieldIndex, out fieldName);

                    // fieldType = ((Schema)_ref).fieldTypes[fieldName];
                    ((Schema) _ref).fieldTypes.TryGetValue(fieldName ?? "", out fieldType);
                    ((Schema) _ref).fieldChildTypes.TryGetValue(fieldName ?? "", out childType);
                }
                else
                {
                    fieldName = ""; // FIXME

                    fieldIndex = Convert.ToInt32(decode.DecodeNumber(bytes, it));
                    if (((ISchemaCollection) _ref).HasSchemaChild)
                    {
                        fieldType = "ref";
                        childType = ((ISchemaCollection) _ref).GetChildType();
                    }
                    else
                    {
                        fieldType = ((ISchemaCollection) _ref).ChildPrimitiveType;
                    }
                }

                object value = null;
                object previousValue = null;
                object dynamicIndex = null;

                if (!isSchema)
                {
                    previousValue = _ref.GetByIndex(fieldIndex);

                    if ((operation & (byte) OPERATION.ADD) == (byte) OPERATION.ADD)
                    {
                        // MapSchema dynamic index.
                        dynamicIndex = ((ISchemaCollection) _ref).GetItems() is OrderedDictionary
                            ? (object) decode.DecodeString(bytes, it)
                            : fieldIndex;

                        ((ISchemaCollection) _ref).SetIndex(fieldIndex, dynamicIndex);
                    }
                    else
                    {
                        dynamicIndex = ((ISchemaCollection) _ref).GetIndex(fieldIndex);
                    }
                }
                else if (fieldName != null) // FIXME: duplicate check
                {
                    previousValue = ((Schema) _ref)[fieldName];
                }

                //
                // Delete operations
                //
                if ((operation & (byte) OPERATION.DELETE) == (byte) OPERATION.DELETE)
                {
                    if (operation != (byte) OPERATION.DELETE_AND_ADD)
                    {
                        _ref.DeleteByIndex(fieldIndex);
                    }

                    // Flag `refId` for garbage collection.
                    if (previousValue != null && previousValue is IRef)
                    {
                        refs.Remove(((IRef) previousValue).__refId);
                    }

                    value = null;
                }

                if (fieldName == null)
                {
                    //
                    // keep skipping next bytes until reaches a known structure
                    // by local decoder.
                    //
                    Iterator nextIterator = new Iterator {Offset = it.Offset};

                    while (it.Offset < totalBytes)
                    {
                        if (decode.SwitchStructureCheck(bytes, it))
                        {
                            nextIterator.Offset = it.Offset + 1;
                            if (refs.Has(Convert.ToInt32(decode.DecodeNumber(bytes, nextIterator))))
                            {
                                break;
                            }
                        }

                        it.Offset++;
                    }

                    continue;
                }

                if (operation == (byte) OPERATION.DELETE)
                {
                    //
                    // FIXME: refactor me.
                    // Don't do anything.
                    //
                }
                else if (fieldType == "ref")
                {
                    var __refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));
                    value = refs.Get(__refId);

                    if (operation != (byte) OPERATION.REPLACE)
                    {
                        System.Type concreteChildType = GetSchemaType(bytes, it, childType);

                        if (value == null)
                        {
                            value = CreateTypeInstance(concreteChildType);
                            ((IRef)value).__refId = __refId;

                            if (previousValue != null)
                            {
                                ((Schema) value).MoveEventHandlers((Schema) previousValue);

                                if (
                                    ((IRef) previousValue).__refId > 0 &&
                                    __refId != ((IRef) previousValue).__refId
                                )
                                {
                                    refs.Remove(((IRef) previousValue).__refId);
                                }
                            }
                        }

                        refs.Add(__refId, (IRef) value, value != previousValue);
                    }
                }
                else if (childType == null)
                {
                    // primitive values
                    value = decode.DecodePrimitiveType(fieldType, bytes, it);
                }
                else
                {
                    var __refId = Convert.ToInt32(decode.DecodeNumber(bytes, it));

                    ISchemaCollection valueRef = refs.Has(__refId)
                        ? (ISchemaCollection) previousValue
                        : (ISchemaCollection) Activator.CreateInstance(childType);

                    value = valueRef.Clone();
					((ISchemaCollection)value).__refId = __refId;

                    // keep reference to nested childPrimitiveType.
                    string childPrimitiveType;
                    ((Schema) _ref).fieldChildPrimitiveTypes.TryGetValue(fieldName, out childPrimitiveType);
                    ((ISchemaCollection) value).ChildPrimitiveType = childPrimitiveType;

                    if (previousValue != null)
                    {
                        ((ISchemaCollection) value).MoveEventHandlers((ISchemaCollection) previousValue);

                        if (
                            ((IRef) previousValue).__refId > 0 &&
                            __refId != ((IRef) previousValue).__refId
                        )
                        {
                            refs.Remove(((IRef) previousValue).__refId);

                            IDictionary items = ((ISchemaCollection) previousValue).GetItems();

                            foreach (object key in items.Keys)
                            {
                                allChanges.Add(new DataChange
                                {
                                    RefId = __refId,
                                    DynamicIndex = key,
                                    Op = (byte) OPERATION.DELETE,
                                    Value = null,
                                    PreviousValue = items[key]
                                });
                            }
                        }
                    }

                    refs.Add(__refId, (IRef) value, valueRef != previousValue);
                }

                if (value != null)
                {
                    if (_ref is Schema)
                    {
                        ((Schema) _ref)[fieldName] = value;
                    }
                    else if (_ref is ISchemaCollection)
                    {
                        ((ISchemaCollection) _ref).SetByIndex(fieldIndex, dynamicIndex, value);
                    }
                }

                if (previousValue != value)
                {
                    allChanges.Add(new DataChange
                    {
                        RefId = refId,
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

        public bool HasCallbacks()
		{
            return __callbacks != null;
		}

        /// <summary>
        ///     Take all of the changes that have occurred and apply them in order to the <see cref="Schema" />
        /// </summary>
        /// <param name="allChanges">Dictionary of the changes to apply</param>
        protected void TriggerChanges(ref List<DataChange> allChanges)
        {
            var uniqueRefIds = new HashSet<int>();

            foreach (DataChange change in allChanges)
            {
                var refId = change.RefId;
                var _ref = refs.Get(refId);

                //
                // trigger onRemove on child structure.
                //
                if (
                    (change.Op & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE &&
                    change.PreviousValue is Schema
                )
                {
                    ((Schema)change.PreviousValue).__callbacks?.InvokeOnRemove();
                }

                // no callbacks defined, skip this structure!
                if (!_ref.HasCallbacks())
                {
                    continue;
                }

                if (_ref is Schema)
				{
                    var __callbacks = ((Schema)_ref).__callbacks;

                    if (!uniqueRefIds.Contains(refId))
					{
						try
						{
                            // trigger onChange
                            __callbacks.InvokeOnChange();

						}
						catch (Exception e)
						{
                            UnityEngine.Debug.LogError(e.Message);
						}
					}

                    if (__callbacks.HasPropertyCallback(change.Field))
					{
						((Schema)_ref).TriggerFieldChange(change);
                    }
				}
                else
                {
                    ISchemaCollection container = (ISchemaCollection) _ref;                    

                    if (change.Op == (byte) OPERATION.ADD &&
                        (
                            change.PreviousValue == null ||
                            change.PreviousValue == container.GetTypeDefaultValue()
                        ))
                    {
                        UnityEngine.Debug.Log("InvokeOnAdd => " + change.DynamicIndex+ " : " + change.Value);
                        container.InvokeOnAdd(change.Value, change.DynamicIndex);
                    }
                    else if (change.Op == (byte) OPERATION.DELETE)
                    {
                        //
                        // FIXME: `previousValue` should always be available.
                        // ADD + DELETE operations are still encoding DELETE operation.
                        //
                        if (change.PreviousValue != container.GetTypeDefaultValue())
                        {
                            container.InvokeOnRemove(change.PreviousValue, change.DynamicIndex ?? change.Field);
                        }
                    }
                    else if (change.Op == (byte) OPERATION.DELETE_AND_ADD)
                    {
                        if (change.PreviousValue != container.GetTypeDefaultValue())
                        {
                            container.InvokeOnRemove(change.PreviousValue, change.DynamicIndex);
                        }

                        container.InvokeOnAdd(change.Value, change.DynamicIndex);
                    }

                    //
                    // FIXME: this implementation differs from other languages
                    // "change.Value != null" is needed here because OnChange.Invoke crashes when casting (T)null.
                    //
                    if (change.Value != change.PreviousValue && change.Value != null)
                    {
                        container.InvokeOnChange(change.Value, change.DynamicIndex ?? change.Field);
                    }
                }

                uniqueRefIds.Add(refId);
            }
        }

        protected virtual void TriggerFieldChange(DataChange change)
		{
            //
            // This method is going to be overwriten by schema-codegen.
            //
        }

        /// <summary>
        ///     Determine what type of <see cref="Schema" /> this is
        /// </summary>
        /// <param name="bytes">Incoming data</param>
        /// <param name="it">
        ///     The <see cref="Iterator" /> used to <see cref="ColyseusDecoder.DecodeNumber" /> the <paramref name="bytes" />
        /// </param>
        /// <param name="defaultType">
        ///     The default <see cref="Schema" /> type, if one cant be determined from the
        ///     <paramref name="bytes" />
        /// </param>
        /// <returns>The parsed <see cref="System.Type" /> if found, <paramref name="defaultType" /> if not</returns>
        protected System.Type GetSchemaType(byte[] bytes, Iterator it, System.Type defaultType)
        {
            System.Type type = defaultType;

            if (it.Offset < bytes.Length && bytes[it.Offset] == (byte) SPEC.TYPE_ID)
            {
                it.Offset++;
                int typeId = Convert.ToInt32(ColyseusDecoder.GetInstance().DecodeNumber(bytes, it));
                type = ColyseusContext.GetInstance().Get(typeId);
            }

            return type;
        }

        /// <summary>
        ///     Create an instance of the provided <paramref name="type" />
        /// </summary>
        /// <param name="type">The <see cref="System.Type" /> to create an instance of</param>
        /// <returns></returns>
        protected object CreateTypeInstance(System.Type type)
        {
            return Activator.CreateInstance(type);
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
    }
}
