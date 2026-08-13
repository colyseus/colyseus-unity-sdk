using System.Collections.Generic;
using NUnit.Framework;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>
	///     `"number"` precision on the untyped path: <see cref="DynamicSchema" /> and the `object`-element
	///     collections it builds for primitive children.
	/// </summary>
	/// <remarks>
	///     A generated schema decides the width by its declared field type, but a dynamic state has no
	///     declared types to consult - its values are boxed straight into a `Dictionary&lt;string, object&gt;`.
	///     So it takes the widest reading of every `"number"`, which is also what the server means by the
	///     type: in JS every `"number"` is a float64. Narrowing here would discard precision that nothing
	///     downstream asked to lose.
	/// </remarks>
	[TestFixture]
	public class DynamicNumberPrecisionTest
	{
		/// <summary>A real epoch millisecond — needs the 8-byte float64 payload.</summary>
		private const double EpochMs = 1786595532328d;

		/// <summary>The same instant in seconds — a uint32 on the wire, and already past float32.</summary>
		private const uint EpochSeconds = 1786595532;

		private static Decoder<DynamicSchema> DecoderFor(params ReflectionField[] fields)
		{
			var definition = new DynamicTypeDefinition { TypeId = 0 };
			for (int i = 0; i < fields.Length; i++)
			{
				definition.ParseFieldType(fields[i], i);
			}

			var decoder = new Decoder<DynamicSchema>();
			decoder.DynamicDefinitions = new Dictionary<float, DynamicTypeDefinition> { [0] = definition };
			decoder.State.Definition = definition;
			return decoder;
		}

		private static ReflectionField Number(string name)
		{
			return new ReflectionField { name = name, type = "number", referencedType = -1 };
		}

		private static ReflectionField NumberArray(string name)
		{
			return new ReflectionField { name = name, type = "array", referencedType = -1, childPrimitive = "number" };
		}

		private static byte[] Float64Payload(double value)
		{
			var bytes = new byte[9];
			bytes[0] = 0xcb;
			System.BitConverter.GetBytes(value).CopyTo(bytes, 1);
			return bytes;
		}

		private static byte[] Uint32Payload(uint value)
		{
			var bytes = new byte[5];
			bytes[0] = 0xce;
			bytes[1] = (byte)(value & 0xff);
			bytes[2] = (byte)((value >> 8) & 0xff);
			bytes[3] = (byte)((value >> 16) & 0xff);
			bytes[4] = (byte)((value >> 24) & 0xff);
			return bytes;
		}

		private static byte[] Field(int index, byte[] payload)
		{
			var bytes = new List<byte> { (byte)(0x80 | index) };
			bytes.AddRange(payload);
			return bytes.ToArray();
		}

		[Test]
		public void KeepsFloat64Precision()
		{
			var decoder = DecoderFor(Number("at"));

			decoder.Decode(Field(0, Float64Payload(EpochMs)));

			Assert.IsInstanceOf<double>(decoder.State["at"]);
			Assert.AreEqual(EpochMs, decoder.State.Get<double>("at"), "the float64 payload must survive intact");
		}

		/// <summary>
		///     The float64 payload is not the only lossy case, and this one is easy to miss: epoch SECONDS
		///     fit a uint32, which float32 cannot represent exactly either - the value lands ~128s off.
		/// </summary>
		[Test]
		public void KeepsIntegerPrecisionBeyondFloat32()
		{
			var decoder = DecoderFor(Number("at"));

			decoder.Decode(Field(0, Uint32Payload(EpochSeconds)));

			Assert.AreEqual(EpochSeconds, decoder.State.Get<double>("at"));
			Assert.AreNotEqual((double)EpochSeconds, (double)(float)EpochSeconds, "sanity: float32 really cannot hold this");
		}

		/// <summary>
		///     The destination decides the width, never the payload — so a field's boxed type cannot flip
		///     between patches just because the server picked a narrower encoding for a smaller value.
		/// </summary>
		[Test]
		public void BoxedTypeDoesNotFlipBetweenPatches()
		{
			var decoder = DecoderFor(Number("at"));

			decoder.Decode(Field(0, new byte[] { 0xca, 0x00, 0x00, 0x80, 0x3f })); // float32 1.0
			Assert.IsInstanceOf<double>(decoder.State["at"]);
			Assert.AreEqual(1d, decoder.State.Get<double>("at"));

			decoder.Decode(Field(0, Float64Payload(EpochMs)));
			Assert.IsInstanceOf<double>(decoder.State["at"]);
			Assert.AreEqual(EpochMs, decoder.State.Get<double>("at"));
		}

		/// <summary>
		///     `Get&lt;T&gt;` converts, so code that wants a float still gets one — the wider storage is not a
		///     breaking change for the documented accessor.
		/// </summary>
		[Test]
		public void GetStillNarrowsOnRequest()
		{
			var decoder = DecoderFor(Number("at"));

			decoder.Decode(Field(0, Float64Payload(EpochMs)));

			Assert.AreEqual((float)EpochMs, decoder.State.Get<float>("at"));
		}

		/// <summary>
		///     A primitive child collection of a dynamic state is an `ArraySchema&lt;object&gt;`, whose elements
		///     box just as freely as a field does.
		/// </summary>
		[Test]
		public void KeepsPrecisionInsidePrimitiveCollections()
		{
			var decoder = DecoderFor(NumberArray("nums"));

			var bytes = new List<byte>();
			bytes.AddRange(Field(0, new byte[] { 1 }));   // "nums" → refId 1
			bytes.AddRange(new byte[] { 255, 1 });        // SWITCH_TO_STRUCTURE refId 1
			bytes.Add((byte)OPERATION.ADD);
			bytes.Add(0);                                 // index 0
			bytes.AddRange(Float64Payload(EpochMs));

			decoder.Decode(bytes.ToArray());

			var nums = decoder.State.Get<ArraySchema<object>>("nums");
			Assert.AreEqual(1, nums.Count);
			Assert.IsInstanceOf<double>(nums[0]);
			Assert.AreEqual(EpochMs, (double)nums[0]);
		}
	}
}
