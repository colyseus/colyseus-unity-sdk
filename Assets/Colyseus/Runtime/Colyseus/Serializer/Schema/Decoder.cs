using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Colyseus.Schema
{
    public delegate void TriggerChangesDelegate(ref List<DataChange> changes);

    public class Decoder<T> where T : Schema
    {
        public ColyseusReferenceTracker Refs = new ColyseusReferenceTracker();
        public TypeContext Context = new TypeContext();
        public T State;

        public TriggerChangesDelegate TriggerChanges;
		protected List<DataChange> AllChanges = new List<DataChange>();

		public Decoder()
		{
            State = Activator.CreateInstance<T>();
        }

        /// <summary>
        ///     Decode incoming data
        /// </summary>
        /// <param name="bytes">The incoming data</param>
        /// <param name="it"><see cref="Iterator" /> used to  If null, will create a new one</param>
        /// <param name="refs">
        ///     <see cref="ColyseusReferenceTracker" /> for all refs found through the decoding process. If null, will
        ///     create a new one
        /// </param>
        /// <exception cref="Exception">If no decoding fails</exception>
        public void Decode(byte[] bytes, Iterator it = null)
        {
            if (it == null)
            {
                it = new Iterator();
            }

            int totalBytes = bytes.Length;

            int refId = 0;
            IRef _ref = State;

            Refs.Add(refId, _ref);

			AllChanges.Clear();

			while (it.Offset < totalBytes)
            {
                byte _byte = bytes[it.Offset++];

                if (_byte == (byte)SPEC.SWITCH_TO_STRUCTURE)
                {
                    refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
                    _ref = Refs.Get(refId);

                    //
                    // Trying to access a reference that haven't been decoded yet.
                    //
                    if (_ref == null)
                    {
                        throw new Exception("refId not found: " + refId);
                    }

                    // create empty list of changes for this refId.
                    //AllChanges = new List<DataChange>();
                    //AllChanges[(object) refId] = allChanges;

                    continue;
                }

                bool isSchema = _ref is Schema;

                byte operation = (byte)(isSchema
                    ? (_byte >> 6) << 6 // "compressed" index + operation
                    : _byte); // "uncompressed" index + operation (array/map items)

                if (operation == (byte)OPERATION.CLEAR)
                {
					((ISchemaCollection)_ref).Clear(ref AllChanges, ref Refs);
                    continue;
                }

                int fieldIndex;
                string fieldName = null;
                string fieldType = null;

                System.Type childType = null;

                if (isSchema)
                {
                    fieldIndex = _byte % (operation == 0 ? 255 : operation); // FIXME: JS allows (0 || 255)
                    ((Schema)_ref).fieldsByIndex.TryGetValue(fieldIndex, out fieldName);

                    // fieldType = ((Schema)_ref).fieldTypes[fieldName];
                    ((Schema)_ref).fieldTypes.TryGetValue(fieldName ?? "", out fieldType);
                    ((Schema)_ref).fieldChildTypes.TryGetValue(fieldName ?? "", out childType);
                }
                else
                {
                    fieldName = ""; // FIXME

                    fieldIndex = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
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
                        dynamicIndex = ((ISchemaCollection)_ref).GetItems() is OrderedDictionary
                            ? (object)Utils.Decode.DecodeString(bytes, it)
                            : fieldIndex;

                        ((ISchemaCollection)_ref).SetIndex(fieldIndex, dynamicIndex);
                    }
                    else
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
                        Refs.Remove(((IRef)previousValue).__refId);
                    }

                    value = null;
                }

                if (fieldName == null)
                {
                    //
                    // keep skipping next bytes until reaches a known structure
                    // by local decoder.
                    //
                    Iterator nextIterator = new Iterator { Offset = it.Offset };

                    while (it.Offset < totalBytes)
                    {
                        if (Utils.Decode.SwitchStructureCheck(bytes, it))
                        {
                            nextIterator.Offset = it.Offset + 1;
                            if (Refs.Has(Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, nextIterator))))
                            {
                                break;
                            }
                        }

                        it.Offset++;
                    }

                    continue;
                }

                if (operation == (byte)OPERATION.DELETE)
                {
                    //
                    // FIXME: refactor me.
                    // Don't do anything.
                    //
                }
                else if (fieldType == "ref")
                {
                    var __refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
                    value = Refs.Get(__refId);

                    if (operation != (byte)OPERATION.REPLACE)
                    {
                        System.Type concreteChildType = GetSchemaType(bytes, it, childType);

                        if (value == null)
                        {
                            value = CreateTypeInstance(concreteChildType);
                            ((IRef)value).__refId = __refId;

                            if (previousValue != null)
                            {
                                // ((Schema)value).MoveEventHandlers((Schema)previousValue);

                                if (
                                    ((IRef)previousValue).__refId > 0 &&
                                    __refId != ((IRef)previousValue).__refId
                                )
                                {
                                    Refs.Remove(((IRef)previousValue).__refId);
                                }
                            }
                        }

                        Refs.Add(__refId, (IRef)value, value != previousValue);
                    }
                }
                else if (childType == null)
                {
                    // primitive values
                    value = Utils.Decode.DecodePrimitiveType(fieldType, bytes, it);
                }
                else
                {
                    var __refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));

                    ISchemaCollection valueRef = Refs.Has(__refId)
						? (ISchemaCollection)previousValue ?? (ISchemaCollection)Refs.Get(__refId)
						: (ISchemaCollection)Activator.CreateInstance(childType);

                    value = valueRef.Clone();
                    ((ISchemaCollection)value).__refId = __refId;

                    // keep reference to nested childPrimitiveType.
                    string childPrimitiveType;
                    ((Schema)_ref).fieldChildPrimitiveTypes.TryGetValue(fieldName, out childPrimitiveType);
                    ((ISchemaCollection)value).ChildPrimitiveType = childPrimitiveType;

                    if (previousValue != null)
                    {
                        // ((ISchemaCollection)value).MoveEventHandlers((ISchemaCollection)previousValue);

                        if (
                            ((IRef)previousValue).__refId > 0 &&
                            __refId != ((IRef)previousValue).__refId
                        )
                        {
                            Refs.Remove(((IRef)previousValue).__refId);

                            var items = ((ISchemaCollection)previousValue).GetItems();

                            foreach (object key in items.Keys)
                            {
                                AllChanges.Add(new DataChange
                                {
                                    RefId = __refId,
                                    DynamicIndex = key,
                                    Op = (byte)OPERATION.DELETE,
                                    Value = null,
                                    PreviousValue = items[key]
                                });
                            }
                        }
                    }

                    Refs.Add(__refId, (IRef)value, valueRef != previousValue);
                }

                if (value != null)
                {
                    if (_ref is Schema)
                    {
                        ((Schema)_ref)[fieldName] = value;
                    }
                    else if (_ref is ISchemaCollection)
                    {
                        ((ISchemaCollection)_ref).SetByIndex(fieldIndex, dynamicIndex, value);
                    }
                }

                if (previousValue != value)
                {
                    AllChanges.Add(new DataChange
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

            TriggerChanges?.Invoke(ref AllChanges);

            Refs.GarbageCollection();
        }

        /// <summary>
        ///     Determine what type of <see cref="Schema" /> this is
        /// </summary>
        /// <param name="bytes">Incoming data</param>
        /// <param name="it">
        ///     The <see cref="Iterator" /> used to <see cref="Decoder.DecodeNumber" /> the <paramref name="bytes" />
        /// </param>
        /// <param name="defaultType">
        ///     The default <see cref="Schema" /> type, if one cant be determined from the
        ///     <paramref name="bytes" />
        /// </param>
        /// <returns>The parsed <see cref="System.Type" /> if found, <paramref name="defaultType" /> if not</returns>
        protected System.Type GetSchemaType(byte[] bytes, Iterator it, System.Type defaultType)
        {
            System.Type type = defaultType;

            if (it.Offset < bytes.Length && bytes[it.Offset] == (byte)SPEC.TYPE_ID)
            {
                it.Offset++;
                int typeId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
                type = Context.Get(typeId);
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

        internal void Teardown()
		{
            Refs.Clear();
        }

	}
}