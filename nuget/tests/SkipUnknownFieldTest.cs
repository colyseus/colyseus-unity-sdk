using NUnit.Framework;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>
	///     A field the server added but the client doesn't know is skipped by scanning ahead for the next
	///     SWITCH_TO_STRUCTURE (0xFF) that leads to a known ref. A 0xFF inside the skipped value is data,
	///     and probing what follows it must not throw — the JS decoder treats that as "keep scanning".
	/// </summary>
	[TestFixture]
	public class SkipUnknownFieldTest
	{
		// encodeAll() of MyRoomState with `currentTurn = "a"` plus a server-added `@type("number") hp = 255` (cc ff),
		// from @colyseus/schema 5.0
		[Test]
		public void SkipsFieldEndingThePayloadInFF()
		{
			byte[] bytes = { 128, 1, 130, 161, 97, 131, 204, 255 };

			var decoder = new Decoder<MyRoomState>();
			decoder.Decode(bytes);

			Assert.AreEqual("a", decoder.State.currentTurn);
		}

		[Test]
		public void SkipsFieldWhoseFFIsFollowedByANonNumber()
		{
			// ...then a server-added `@type("number") mana = 1` (0x84 after the 0xFF)
			byte[] bytes = { 128, 1, 130, 161, 97, 131, 204, 255, 132, 1 };

			var decoder = new Decoder<MyRoomState>();
			decoder.Decode(bytes);

			Assert.AreEqual("a", decoder.State.currentTurn);
		}

		[Test]
		public void ResumesAtTheNextStructureAfterTheSkippedField()
		{
			// ...then players["p1"] = { x: 3, y: 4, isBot: true }
			byte[] bytes = { 128, 1, 130, 161, 97, 131, 204, 255, 255, 1, 128, 0, 162, 112, 49, 2, 255, 2, 128, 3, 129, 4, 130, 1 };

			var decoder = new Decoder<MyRoomState>();
			decoder.Decode(bytes);

			var p1 = decoder.State.players["p1"];
			Assert.AreEqual(3f, p1.x);
			Assert.AreEqual(4f, p1.y);
			Assert.IsTrue(p1.isBot);
		}
	}
}
