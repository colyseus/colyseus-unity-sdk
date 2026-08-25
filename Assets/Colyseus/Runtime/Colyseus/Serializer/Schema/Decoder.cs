using System;
using System.Collections.Generic;

namespace Colyseus.Schema
{
    public delegate void TriggerChangesDelegate(ref List<DataChange> changes);

    public class Decoder<T> where T : Schema
    {
        public ReferenceTracker Refs = new ReferenceTracker();
        public TypeContext Context = new TypeContext();
        public T State;

        public TriggerChangesDelegate TriggerChanges;
		protected List<DataChange> AllChanges = new List<DataChange>();

		/// <summary>
		///     DynamicTypeDefinitions keyed by typeId, populated during Handshake when T is DynamicSchema.
		/// </summary>
		internal Dictionary<float, DynamicTypeDefinition> DynamicDefinitions;

		/// <summary>
		///     Maps collection refIds to their child element typeId (for DynamicSchema support).
		/// </summary>
		private Dictionary<int, float> _collectionChildTypeId;

		/// <summary>
		///     Non-null only while <see cref="DecodeResync" /> is in progress — its
		///     non-nullness IS the resync-mode flag (see PORTING_RESYNC.md in
		///     @colyseus/schema). Maps collection refId → visited entry identities
		///     (map string keys / boxed array indexes).
		/// </summary>
		internal Dictionary<int, HashSet<object>> ResyncVisited;
		internal bool ResyncDamaged;

		public Decoder()
		{
            State = Activator.CreateInstance<T>();
            Refs.Add(0, State);
        }

		/// <summary>
		///     Full-snapshot reconciliation ("resync") decode.
		///
		///     Behaves exactly like <see cref="Decode" />, plus: every collection
		///     entry the payload does NOT mention is removed through the regular
		///     DELETE path — OnRemove callbacks fire with the real previous value
		///     and released refs are garbage-collected. Use it to apply a
		///     rejoin/reconnect full state over an existing decoded tree.
		///
		///     ONLY valid for full-snapshot payloads. Calling it on an incremental
		///     patch would prune everything the patch doesn't touch.
		/// </summary>
		public void DecodeResync(byte[] bytes, Iterator it = null)
		{
			ResyncVisited = new Dictionary<int, HashSet<object>>();
			ResyncDamaged = false;
			try
			{
				Decode(bytes, it);
			}
			finally
			{
				ResyncVisited = null;
			}
		}

		/// <summary>
		///     Decode incoming data
		/// </summary>
		/// <param name="bytes">The incoming data</param>
		/// <param name="it"><see cref="Iterator" /> used to  If null, will create a new one</param>
		/// <param name="refs">
		///     <see cref="ReferenceTracker" /> for all refs found through the decoding process. If null, will
		///     create a new one
		/// </param>
		/// <exception cref="Exception">If no decoding fails</exception>
		public void Decode(byte[] bytes, Iterator it = null)
		{
			if (it == null)
			{
				it = new Iterator();
			}

			int refId = 0;
			IRef _ref = State;

			AllChanges.Clear();

			int totalBytes = bytes.Length;

			while (it.Offset < totalBytes)
			{
				if (bytes[it.Offset] == (byte)SPEC.SWITCH_TO_STRUCTURE)
				{
					it.Offset++;

					refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));

					if (_ref is IArraySchema) { ((IArraySchema)_ref).OnDecodeEnd(); }

					IRef nextRef = Refs.Get(refId);

					//
					// Trying to access a reference that haven't been decoded yet.
					//
					if (nextRef == null)
					{
						ColyseusContext.Logger.Log("@colyseus/schema: refId not found: " + refId);
						SkipCurrentStructure(bytes, it, totalBytes);
					}
					else
					{
						_ref = nextRef;
					}

					continue;
				}

				bool isSchemaDefinitionMismatch;

				if (_ref is Schema)
				{
					isSchemaDefinitionMismatch = !DecodeSchema(bytes, it, (Schema)_ref);
				}
				else if (_ref is IMapSchema)
				{
					isSchemaDefinitionMismatch = !DecodeMapSchema(bytes, it, (IMapSchema)_ref);
				}
				else
				{
					isSchemaDefinitionMismatch = !DecodeArraySchema(bytes, it, (IArraySchema)_ref);
				}

				if (isSchemaDefinitionMismatch)
				{
					SkipCurrentStructure(bytes, it, totalBytes);
					continue;
				}
			}

			if (_ref is IArraySchema) { ((IArraySchema)_ref).OnDecodeEnd(); }

			// resync mode: prune everything the snapshot didn't visit. Runs
			// before TriggerChanges (DELETE changes fire OnRemove with the real
			// PreviousValue) and before GC (Refs.Remove feeds deletedRefs).
			if (ResyncVisited != null) { ResyncSweep(); }

			TriggerChanges?.Invoke(ref AllChanges);

			Refs.GarbageCollection();
		}

		//
		// keep skipping next bytes until reaches a known structure
		// by local decoder.
		//
		protected void SkipCurrentStructure(byte[] bytes, Iterator it, int totalBytes)
		{
			// a skipped range can swallow other structures' ops — resync
			// visited data is no longer trustworthy
			if (ResyncVisited != null) { ResyncDamaged = true; }

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
		}

		// ─── resync bookkeeping (guarded by `ResyncVisited != null` at call sites) ───

		private HashSet<object> ResyncVisitedFor(int refId)
		{
			if (!ResyncVisited.TryGetValue(refId, out var set))
			{
				ResyncVisited[refId] = set = new HashSet<object>();
			}
			return set;
		}

		/// <summary>
		///     Release a replaced occupant: full-sync emits plain ADD (never
		///     DELETE_AND_ADD), so an entry whose instance changed while this
		///     client was off the wire would otherwise leak its previous ref.
		///     Resync-only — a live patch's plain ADD can be a positional rewrite
		///     where the occupant moved and is still alive.
		/// </summary>
		private void ResyncReleaseReplaced(IRef _ref, byte operation, object identity, object previousValue, object value)
		{
			if (previousValue is IRef previousRef && operation == (byte)OPERATION.ADD && previousValue != value)
			{
				Refs.Remove(previousRef.__refId);
				AllChanges.Add(new DataChange
				{
					RefId = _ref.__refId,
					Op = (byte)OPERATION.DELETE,
					Field = null,
					DynamicIndex = identity,
					Value = null,
					PreviousValue = previousValue
				});
			}
		}

		private void ResyncSweep()
		{
			if (ResyncDamaged)
			{
				ColyseusContext.Logger.Log("@colyseus/schema: resync sweep skipped — parts of the payload could not be decoded. Stale entries may persist until the next resync.");
				return;
			}
			SweepSchema(State, new HashSet<int>());
		}

		private void SweepSchema(Schema schema, HashSet<int> seen)
		{
			if (!seen.Add(schema.__refId)) { return; }

			// virtual metadata accessors — one walker covers DynamicSchema too
			foreach (var entry in schema.fieldsByIndex)
			{
				schema.fieldTypes.TryGetValue(entry.Value, out var fieldType);
				if (fieldType != "ref" && fieldType != "array" && fieldType != "map") { continue; }

				object value = schema.GetByIndex(entry.Key);
				if (value == null) { continue; }

				if (fieldType == "ref")
				{
					SweepSchema((Schema)value, seen);
				}
				else
				{
					SweepCollection((ISchemaCollection)value, seen);
				}
			}
		}

		private void SweepCollection(ISchemaCollection coll, HashSet<int> seen)
		{
			if (!seen.Add(coll.__refId)) { return; }

			// absent = the collection never appeared in the payload at all — it
			// is not part of full-sync (@transient, view-invisible) and must be
			// left alone. An empty set means "present with zero entries".
			if (!ResyncVisited.TryGetValue(coll.__refId, out var visited)) { return; }

			void Prune(object value, object identity)
			{
				AllChanges.Add(new DataChange
				{
					RefId = coll.__refId,
					Op = (byte)OPERATION.DELETE,
					Field = null,
					DynamicIndex = identity,
					Value = null,
					PreviousValue = value
				});
				if (value is IRef childRef)
				{
					Refs.Remove(childRef.__refId);
				}
			}

			// recurse so nested collections of retained entries sweep too; swept
			// subtrees are left to the GC's transitive walk instead (sweeping
			// them directly would double-decrement shared children)
			void Keep(object value)
			{
				if (value is Schema childSchema) { SweepSchema(childSchema, seen); }
			}

			if (coll is IMapSchema map)
			{
				map.ResyncPrune(visited, Prune, Keep);
			}
			else if (coll is IArraySchema arr)
			{
				arr.ResyncPrune(visited, Prune, Keep);
			}
		}

		/// <summary>
		///     <c>typeof(double)</c> when a <c>"number"</c> can be delivered at full width to this
		///     destination; <c>null</c> when it must stay <see cref="float" />, as every generated schema
		///     mapping <c>"number"</c> to <see cref="float" /> requires.
		/// </summary>
		/// <remarks>
		///     The destination governs, never the payload — so a value decodes to the same C# type
		///     whatever encoding the server picked for it, patch after patch.
		/// </remarks>
		private static System.Type DoubleTargetOrNull(IRef _ref, int fieldIndex, string fieldType)
		{
			if (fieldType != "number") { return null; }

			bool wide = _ref is Schema schema
				? schema.AcceptsWideNumber(fieldIndex)
				: _ref is ISchemaCollection collection && AcceptsWideNumber(collection);

			return wide ? typeof(double) : null;
		}

		/// <summary>
		///     Whether this collection's elements can hold a <see cref="double" />. <c>SetByIndex</c> casts
		///     with <c>(T)value</c>, and unboxing converts neither way: an <c>object</c> element (what an
		///     untyped state builds) takes anything, a <c>double</c> one is where the boxed float throws,
		///     and <c>ArraySchema&lt;float&gt;</c> must keep receiving floats.
		/// </summary>
		private static bool AcceptsWideNumber(ISchemaCollection collection)
		{
			if (collection.HasSchemaChild) { return false; }

			System.Type element = collection.GetChildType();
			return element == typeof(object) || element == typeof(double);
		}

		protected void DecodeValue(byte[] bytes, Iterator it, IRef _ref, int fieldIndex, string fieldType, System.Type childType, byte operation, out object value, out object previousValue)
		{
			previousValue = _ref.GetByIndex(fieldIndex);

			//
			// Delete operations
			//
			if ((operation & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE)
			{
				// Flag `refId` for garbage collection.
				if (previousValue != null && previousValue is IRef)
				{
					Refs.Remove(((IRef)previousValue).__refId);
				}

				if (operation != (byte)OPERATION.DELETE_AND_ADD)
				{
					_ref.DeleteByIndex(fieldIndex);
				}

				value = null;
			}

			if (operation == (byte)OPERATION.DELETE)
			{
				//
				// FIXME: refactor me.
				// Don't do anything.
				//
				value = null;

			}
			else if (fieldType == "ref")
			{
				var __refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
				value = Refs.Get(__refId);

				if ((operation & (byte)OPERATION.ADD) == (byte)OPERATION.ADD)
				{
					float resolvedTypeId;
					System.Type concreteChildType = GetSchemaType(bytes, it, childType, out resolvedTypeId);
					if (value == null)
					{
						value = CreateTypeInstance(concreteChildType);
						((IRef)value).__refId = __refId;
					}

					// Assign DynamicTypeDefinition for DynamicSchema instances
					if (DynamicDefinitions != null && value is DynamicSchema dynamicValue && dynamicValue.Definition == null)
					{
						if (resolvedTypeId < 0)
						{
							// No TYPE_ID in stream, determine from parent context
							if (_ref is DynamicSchema parentDs &&
								parentDs.Definition != null &&
								parentDs.Definition.FieldsByIndex.TryGetValue(fieldIndex, out var fn) &&
								parentDs.Definition.FieldReferencedTypes.TryGetValue(fn, out var parentTypeId))
							{
								resolvedTypeId = parentTypeId;
							}
							else if (_ref is ISchemaCollection &&
								_collectionChildTypeId != null &&
								_collectionChildTypeId.TryGetValue(_ref.__refId, out var colTypeId))
							{
								resolvedTypeId = colTypeId;
							}
						}

						if (resolvedTypeId >= 0 && DynamicDefinitions.TryGetValue(resolvedTypeId, out var def))
						{
							dynamicValue.Definition = def;
						}
					}

					Refs.Add(__refId, (IRef)value, (
						value != previousValue ||  // increment ref count if value has changed
						(operation == (byte)OPERATION.DELETE_AND_ADD && value == previousValue) // increment ref count if the same instance is being added again
					));
				}

			}
			else if (fieldType == "quantized")
			{
				// Quantized scalar: read the unsigned int of the descriptor's
				// wire width, then dequantize — the instance only ever holds
				// the wire-exact double (see Utils.Quantize).
				var schemaRef = (Schema)_ref;
				var desc = schemaRef.fieldQuantizedDescriptors[schemaRef.fieldsByIndex[fieldIndex]];

				uint q = desc.Bits == 8 ? Utils.Decode.DecodeUint8(bytes, it)
					: desc.Bits == 16 ? Utils.Decode.DecodeUint16(bytes, it)
					: Utils.Decode.DecodeUint32(bytes, it);

				value = Utils.Quantize.Dequantize(desc, q);
			}
			else if (childType == null)
			{
				// primitive values
				value = Utils.Decode.DecodePrimitiveType(fieldType, bytes, it, DoubleTargetOrNull(_ref, fieldIndex, fieldType));
			}
			else
			{
				var __refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));

				// resync bookkeeping: mark the collection present in the payload —
				// even with zero entries — so the sweep knows it participated
				if (ResyncVisited != null && !ResyncVisited.ContainsKey(__refId))
				{
					ResyncVisited[__refId] = new HashSet<object>();
				}

				ISchemaCollection valueRef = Refs.Has(__refId)
					? (ISchemaCollection)previousValue ?? (ISchemaCollection)Refs.Get(__refId)
					: (ISchemaCollection)Activator.CreateInstance(childType);

				value = valueRef.Clone();
				((ISchemaCollection)value).__refId = __refId;

				// keep reference to nested childPrimitiveType.
				string childPrimitiveType;
				((Schema)_ref).fieldsByIndex.TryGetValue(fieldIndex, out var fieldName);
				((Schema)_ref).fieldChildPrimitiveTypes.TryGetValue(fieldName, out childPrimitiveType);
				((ISchemaCollection)value).ChildPrimitiveType = childPrimitiveType;

				// Track child typeId for DynamicSchema collections
				if (DynamicDefinitions != null && _ref is DynamicSchema parentDs2 &&
					parentDs2.Definition != null && fieldName != null &&
					parentDs2.Definition.FieldReferencedTypes.TryGetValue(fieldName, out var collChildTypeId))
				{
					if (_collectionChildTypeId == null) _collectionChildTypeId = new Dictionary<int, float>();
					_collectionChildTypeId[__refId] = collChildTypeId;
				}

				if (previousValue != null)
				{
					if (
						((IRef)previousValue).__refId > 0 &&
						__refId != ((IRef)previousValue).__refId
					)
					{
						((ISchemaCollection)previousValue).ForEach((key, value) => {
							if (value is IRef) {
								Refs.Remove(((IRef)value).__refId);
							}

							AllChanges.Add(new DataChange
							{
								RefId = __refId,
								DynamicIndex = key,
								Op = (byte)OPERATION.DELETE,
								Value = null,
								PreviousValue = value
							});
						});
					}
				}

				Refs.Add(__refId, (IRef)value, (
					valueRef != previousValue || // increment ref count if value has changed
					(operation == (byte)OPERATION.DELETE_AND_ADD && valueRef == previousValue) // increment ref count if the same instance is being added again
				));
			}
		}

		protected bool DecodeSchema(byte[] bytes, Iterator it, Schema refSchema)
		{
			byte firstByte = bytes[it.Offset++];
			byte operation = (byte) ((firstByte >> 6) << 6); // "compressed" index + operation

			int fieldIndex = firstByte % (operation == 0 ? 255 : operation); // FIXME: JS allows (0 || 255);

			refSchema.fieldsByIndex.TryGetValue(fieldIndex, out var fieldName);
			refSchema.fieldTypes.TryGetValue(fieldName ?? "", out var fieldType);
			refSchema.fieldChildTypes.TryGetValue(fieldName ?? "", out var childType);

			if (fieldName == null)
			{
				return false;
			}

			DecodeValue(
				bytes,
				it,
				refSchema,
				fieldIndex,
				fieldType,
				childType,
				operation,
				out var value,
				out var previousValue
			);

			if (value != null)
			{
				refSchema[fieldName] = value;
			}

			if (previousValue != value)
			{
				AllChanges.Add(new DataChange
				{
					RefId = refSchema.__refId,
					Op = operation,
					Field = fieldName,
					Value = value,
					PreviousValue = previousValue
				});
			}

			return true;
		}

		protected bool DecodeMapSchema (byte[] bytes, Iterator it, IMapSchema refMap)
		{
			byte operation = bytes[it.Offset++];

			if (operation == (byte)OPERATION.CLEAR)
			{
				refMap.Clear(AllChanges, Refs);
				return true;
			}

			int fieldIndex = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
			string fieldType;
			System.Type childType = null;

			if (refMap.HasSchemaChild)
			{
				fieldType = "ref";
				childType = refMap.GetChildType();
			}
			else
			{
				fieldType = refMap.ChildPrimitiveType;
			}

			string dynamicIndex;

			if ((operation & (byte)OPERATION.ADD) == (byte)OPERATION.ADD)
			{
				// MapSchema dynamic index.
				dynamicIndex = Utils.Decode.DecodeString(bytes, it);
				refMap.SetIndex(fieldIndex, dynamicIndex);
			}
			else
			{
				dynamicIndex = (string)refMap.GetIndex(fieldIndex);
			}

			DecodeValue(
				bytes,
				it,
				refMap,
				fieldIndex,
				fieldType,
				childType,
				operation,
				out var value,
				out var previousValue
			);

			// resync bookkeeping — record even when the value is unchanged
			// (the change list can't serve as the record: its pushes are gated)
			if (ResyncVisited != null)
			{
				ResyncVisitedFor(refMap.__refId).Add(dynamicIndex);
				ResyncReleaseReplaced(refMap, operation, dynamicIndex, previousValue, value);
			}

			if (value != null)
			{
				refMap.SetByIndex(fieldIndex, dynamicIndex, value);
			}

			if (previousValue != value)
			{
				AllChanges.Add(new DataChange
				{
					RefId = refMap.__refId,
					Op = operation,
					Field = null,
					DynamicIndex = dynamicIndex,
					Value = value,
					PreviousValue = previousValue
				});
			}
			return true;
		}

		protected bool DecodeArraySchema(byte[] bytes, Iterator it, IArraySchema refArray)
		{
			byte operation = bytes[it.Offset++];
			int index;

			if (operation == (byte)OPERATION.CLEAR)
			{
				refArray.Clear(AllChanges, Refs);
				return true;

			}
			else if (operation == (byte)OPERATION.REVERSE)
			{
				refArray.Reverse();
				return true;

			}
			else if (operation == (byte)OPERATION.DELETE_BY_REFID)
			{
				int refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
				object itemByRefId = Refs.Get(refId);

				// stale DELETE — refId unknown to this decoder (mid-tick joiner)
				if (itemByRefId == null) { return true; }

				// ref-count decrement — must run even when the item is absent from
				// THIS array (view churn); this branch never reaches DecodeValue()
				Refs.Remove(refId);

				int i = 0;
				index = -1;
				foreach (var item in refArray.GetItems())
				{
					if (item == itemByRefId)
					{
						index = i;
						break;
					}
					i++;
				}

				if (index == -1) { return true; }

				refArray.DeleteByIndex(index);
				AllChanges.Add(new DataChange
				{
					RefId = refArray.__refId,
					Op = (byte) OPERATION.DELETE,
					Field = "",
					DynamicIndex = index,
					Value = null,
					PreviousValue = itemByRefId
				});
				return true;

			}
			else if (operation == (byte)OPERATION.ADD_BY_REFID)
			{
				int refId = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
				IRef itemByRefId = Refs.Get(refId);

				index = -1;
				if (itemByRefId != null)
				{
					int i = 0;
					foreach (var item in refArray.GetItems())
					{
						if (item == itemByRefId)
						{
							index = i;
							break;
						}
						i++;
					}
				}

				if (index == -1)
				{
					index = refArray.Count;
				}

			}
			else
			{
				index = Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
			}

			string fieldType;
			System.Type childType = null;

			if (refArray.HasSchemaChild)
			{
				fieldType = "ref";
				childType = refArray.GetChildType();
			}
			else
			{
				fieldType = refArray.ChildPrimitiveType;
			}

			DecodeValue(
				bytes,
				it,
				refArray,
				index,
				fieldType,
				childType,
				operation,
				out var value,
				out var previousValue
			);

			// resync bookkeeping — identity is the resolved client-side index
			// (ADD_BY_REFID resolves above, so visited indexes may be sparse)
			if (ResyncVisited != null)
			{
				ResyncVisitedFor(refArray.__refId).Add(index);
				ResyncReleaseReplaced(refArray, operation, index, previousValue, value);
			}

			if (value != null && value != previousValue)
			{
				// resync snapshot ADDs are positional overwrites, not inserts
				refArray.SetByIndex(index, value,
					(ResyncVisited != null && operation == (byte)OPERATION.ADD)
						? (byte)OPERATION.REPLACE
						: operation);
			}

			if (previousValue != value)
			{
				AllChanges.Add(new DataChange
				{
					RefId = refArray.__refId,
					Op = operation,
					Field = null,
					DynamicIndex = index,
					Value = value,
					PreviousValue = previousValue
				});
			}
			return true;
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
		protected System.Type GetSchemaType(byte[] bytes, Iterator it, System.Type defaultType, out float resolvedTypeId)
        {
            System.Type type = defaultType;
            resolvedTypeId = -1;

            if (it.Offset < bytes.Length && bytes[it.Offset] == (byte)SPEC.TYPE_ID)
            {
                it.Offset++;
                resolvedTypeId = (float)Convert.ToInt32(Utils.Decode.DecodeNumber(bytes, it));
                type = Context.Get(resolvedTypeId);
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