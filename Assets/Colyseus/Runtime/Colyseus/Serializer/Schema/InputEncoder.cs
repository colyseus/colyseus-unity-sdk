using System;
using System.Collections.Generic;
using System.Linq;

namespace Colyseus.Schema
{
	/// <summary>
	///     Bound single-struct encoder for client→server input packets. Port of
	///     @colyseus/schema <c>src/input/InputEncoder.ts</c> (flat primitive
	///     fields only).
	///
	///     <para>
	///     Delta tracking differs from the JS reference by design: the reference
	///     uses setter-populated ChangeTrees; this port diffs the instance
	///     against the last-sent snapshot (seeded from construction defaults, so
	///     an unassigned field is not dirty). Benign divergence: re-assigning an
	///     identical value emits nothing — the server decodes to the same state.
	///     </para>
	///
	///     <para>
	///     "reliable" mode: one delta per <see cref="Encode()" /> — only changed
	///     fields, empty when nothing changed. "unreliable" mode: ring of the
	///     last <see cref="HistorySize" /> deltas in one packet:
	///     <c>[baseSeq][len][slot]…</c> oldest→newest; a no-change tick still
	///     pushes an empty carry-forward slot.
	///     </para>
	/// </summary>
	public class InputEncoder
	{
		// A field op packs as `operation | index`, so index 63 would emit 255 —
		// the byte a decoder reads as SWITCH_TO_STRUCTURE. The server rejects
		// the 64th field where the schema is defined; mirror it here, since
		// this encoder assembles the op byte itself.
		private const int MAX_FIELDS = 63;

		public readonly Schema Instance;
		public readonly string Mode;
		public readonly int HistorySize;

		/// <summary>
		///     Framework input seq of the most recent tick (unreliable mode):
		///     monotonic, ++ per <see cref="Encode()" />, kept across
		///     <see cref="Reset" />. 0 in reliable mode.
		/// </summary>
		public int Seq { get; private set; }

		// field indexes in wire order
		private readonly int[] fieldIndexes;

		// last-sent snapshot per field index, seeded from construction
		// defaults; null → snapshot mode: next encode emits every populated
		// field (after Reset()).
		private Dictionary<int, object> baseline;

		// unreliable ring (oldest→newest via head/count arithmetic)
		private readonly List<byte>[] slots;
		private int slotHead;
		private int slotCount;

		public InputEncoder(Schema instance, string mode = "reliable", int historySize = 3)
		{
			Instance = instance;
			Mode = mode ?? "reliable";
			HistorySize = Mode == "unreliable" ? Math.Max(1, historySize) : 1;

			fieldIndexes = instance.fieldsByIndex.Keys.OrderBy(i => i).ToArray();
			foreach (var index in fieldIndexes)
			{
				if (index >= MAX_FIELDS)
				{
					throw new Exception(
						$"InputEncoder: field '{instance.fieldsByIndex[index]}' is at index {index}; a Schema may only have {MAX_FIELDS} fields.");
				}

				var fieldType = instance.fieldTypes[instance.fieldsByIndex[index]];
				if (fieldType == "ref" || fieldType == "array" || fieldType == "map")
				{
					throw new Exception(
						$"InputEncoder: non-primitive field '{instance.fieldsByIndex[index]}' is not supported.");
				}
			}

			if (Mode == "unreliable")
			{
				slots = new List<byte>[HistorySize];
			}

			// diff against construction defaults from the start — an unassigned
			// field is not dirty (the JS ChangeTree behaves the same way)
			baseline = new Dictionary<int, object>();
			foreach (var index in fieldIndexes)
			{
				baseline[index] = instance[instance.fieldsByIndex[index]];
			}
		}

		/// <summary>Encode the bound instance's delta (see class doc for the shape per mode).</summary>
		public byte[] Encode()
		{
			var body = ProduceDelta();
			return Mode == "reliable" ? body.ToArray() : PushAndEmitRing(body);
		}

		/// <summary>
		///     Reset: drops the ring and the diff baseline, so the next
		///     <see cref="Encode()" /> emits a full snapshot of populated fields.
		///     <see cref="Seq" /> is kept (monotonic across reset).
		/// </summary>
		public void Reset()
		{
			baseline = null;
			slotHead = 0;
			slotCount = 0;
		}

		/// <summary>Copy the bound instance's field values into <paramref name="target" /> (same-type instance).</summary>
		public void CopyInto(Schema target)
		{
			foreach (var index in fieldIndexes)
			{
				var name = Instance.fieldsByIndex[index];
				target[name] = Instance[name];
			}
		}

		private List<byte> ProduceDelta()
		{
			var output = new List<byte>();
			bool snapshot = baseline == null; // post-reset
			if (snapshot) { baseline = new Dictionary<int, object>(); }

			foreach (var index in fieldIndexes)
			{
				var name = Instance.fieldsByIndex[index];
				var current = Instance[name];
				bool changed = snapshot
					? current != null                                        // snapshot: every populated field
					: !Equals(current, baseline.TryGetValue(index, out var prev) ? prev : null);
				if (!changed) { continue; }

				output.Add((byte)(0x80 | index)); // ADD|fieldIndex — the schema field op
				EncodeValue(output, Instance.fieldTypes[name], name, current);
				baseline[index] = current;
			}
			return output;
		}

		private void EncodeValue(List<byte> output, string fieldType, string fieldName, object value)
		{
			switch (fieldType)
			{
				case "number": Utils.Encode.Number(output, Convert.ToDouble(value)); break;
				case "string": Utils.Encode.String(output, (string)value); break;
				case "boolean": Utils.Encode.Boolean(output, (bool)value); break;
				case "int8": Utils.Encode.Int8(output, Convert.ToSByte(value)); break;
				case "uint8": Utils.Encode.Uint8(output, Convert.ToByte(value)); break;
				case "int16": Utils.Encode.Int16(output, Convert.ToInt16(value)); break;
				case "uint16": Utils.Encode.Uint16(output, Convert.ToUInt16(value)); break;
				case "int32": Utils.Encode.Int32(output, Convert.ToInt32(value)); break;
				case "uint32": Utils.Encode.Uint32(output, Convert.ToUInt32(value)); break;
				case "int64": Utils.Encode.Int64(output, Convert.ToInt64(value)); break;
				case "uint64": Utils.Encode.Uint64(output, Convert.ToUInt64(value)); break;
				case "float32": Utils.Encode.Float32(output, Convert.ToSingle(value)); break;
				case "float64": Utils.Encode.Float64(output, Convert.ToDouble(value)); break;
				case "quantized":
				{
					var desc = Instance.fieldQuantizedDescriptors[fieldName];
					uint q = Utils.Quantize.QuantizeValue(desc, Convert.ToDouble(value));
					if (desc.Bits == 8) { Utils.Encode.Uint8(output, (byte)q); }
					else if (desc.Bits == 16) { Utils.Encode.Uint16(output, (ushort)q); }
					else { Utils.Encode.Uint32(output, q); }
					break;
				}
				default:
					throw new Exception($"InputEncoder: unsupported field type '{fieldType}'");
			}
		}

		private byte[] PushAndEmitRing(List<byte> body)
		{
			// push EVERY tick (even empty) so ring seqs stay consecutive: the
			// packet carries one base seq; the decoder derives slot seqs by position
			Seq++;
			slots[slotHead] = body;
			slotHead = (slotHead + 1) % HistorySize;
			if (slotCount < HistorySize) { slotCount++; }

			// [baseSeq][len][slot]… oldest→newest
			var output = new List<byte>();
			int baseSeq = Seq - slotCount + 1;
			Utils.Encode.Number(output, baseSeq);
			int oldest = (slotHead - slotCount + HistorySize) % HistorySize;
			for (int i = 0; i < slotCount; i++)
			{
				var slot = slots[(oldest + i) % HistorySize];
				Utils.Encode.Number(output, slot.Count);
				output.AddRange(slot);
			}
			return output.ToArray();
		}
	}
}
