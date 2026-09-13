using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Colyseus.Predict;
using Colyseus.Schema;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.Fixtures.CollectionFixtures;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     Default-initialized collections (codegen's <c>= new MapSchema&lt;T&gt;()</c>)
	///     against real encoder bytes, including a StateView-filtered map that
	///     is absent from the full state. The expected event log is the JS
	///     Callbacks' own, recorded by Fixtures/generate-collection-fixtures.ts.
	/// </summary>
	[TestFixture]
	public class CollectionDefaultsTest
	{
		private static string Sz(MapSchema<DHero> m) => m == null ? "null" : m.Count.ToString();

		/// <summary>Group a log into its "--" sections, each order-insensitive (JS dispatches handlers last-registered-first).</summary>
		private static List<string> Sections(IEnumerable<string> log)
		{
			var sections = new List<string>();
			var current = new List<string>();
			void Flush() { current.Sort(StringComparer.Ordinal); sections.Add(string.Join(" | ", current)); current.Clear(); }
			foreach (var line in log)
			{
				if (line.StartsWith("--")) { Flush(); current.Add(line); }
				else { current.Add(line); }
			}
			Flush();
			return sections;
		}

		private static (Decoder<DState> decoder, List<string> log, MapSchema<DHero> initial) ReplayViewScenario()
		{
			var decoder = new Decoder<DState>();
			var cb = Callbacks.Get(decoder);
			var log = new List<string>();
			var initial = decoder.State.heroes;

			// A: typed expression overloads, before any bytes
			cb.OnAdd(s => s.heroes, (k, h) => log.Add($"A.add:{k}:{h.hp}"));
			cb.OnRemove(s => s.heroes, (k, h) => log.Add($"A.remove:{k}:{h.hp}"));
			cb.Listen(s => s.heroes, (cur, prev) => log.Add($"L:{Sz(cur)}:{Sz(prev)}"));
			cb.Listen(s => s.tick, (cur, prev) => log.Add($"tick:{cur}"));
			cb.OnAdd(s => s.open, (k, h) => log.Add($"open.add:{k}:{h.hp}"));

			log.Add("--full");
			decoder.Decode(ViewFull);

			// B: string overloads, after the full state — heroes is still the unsynced default
			cb.OnAdd<DHero>("heroes", (string k, DHero h) => log.Add($"B.add:{k}:{h.hp}"));
			cb.OnRemove<DHero>("heroes", (string k, DHero h) => log.Add($"B.remove:{k}:{h.hp}"));

			for (int i = 0; i < ViewPatches.Length; i++)
			{
				log.Add($"--P{i}");
				decoder.Decode(ViewPatches[i]);
				if (i == 0)
				{
					// C: once heroes is live — immediate replay of what's there
					cb.OnAdd(s => s.heroes, (k, h) => log.Add($"C.add:{k}:{h.hp}"));
					cb.OnRemove(s => s.heroes, (k, h) => log.Add($"C.remove:{k}:{h.hp}"));
				}
			}
			return (decoder, log, initial);
		}

		[Test]
		public void CallbacksRegisteredBeforeTheCollectionArrivesMatchTheJsLog()
		{
			var (decoder, log, _) = ReplayViewScenario();

			CollectionAssert.AreEqual(Sections(ViewLog), Sections(log));
			Assert.AreEqual(2, decoder.State.tick);
			Assert.AreEqual(31, decoder.State.open["o1"].hp);
			Assert.IsTrue(decoder.State.heroes.ContainsKey("a"));
			Assert.AreEqual(1, decoder.State.heroes.Count);
		}

		[Test]
		public void DefaultInstanceIsReplacedOnArrivalLikeJs()
		{
			var (decoder, _, initial) = ReplayViewScenario();
			Assert.AreEqual(ViewReplacedOnArrival, !ReferenceEquals(initial, decoder.State.heroes));
		}

		[Test]
		public void WaitingRegistrationsDoNotLeakOrDoubleRegister()
		{
			var (decoder, _, _) = ReplayViewScenario();
			var callbacks = decoder.Refs.callbacks;

			// the "heroes" waiters unhooked themselves on arrival; only the Listen stays
			Assert.AreEqual(1, callbacks[0]["heroes"].Count);
			var heroes = callbacks[decoder.State.heroes.__refId];
			Assert.AreEqual(3, heroes[OPERATION.ADD].Count, "A + B + C");
			Assert.AreEqual(3, heroes[OPERATION.DELETE].Count, "A + B + C");
		}

		[Test]
		public void UnsubscribingBeforeArrivalNeverFires()
		{
			var decoder = new Decoder<DState>();
			var cb = Callbacks.Get(decoder);
			int fired = 0;
			var off = cb.OnAdd(s => s.heroes, (k, h) => fired++);
			decoder.Decode(ViewFull);
			off();
			foreach (var patch in ViewPatches) { decoder.Decode(patch); }

			Assert.AreEqual(0, fired);
			Assert.IsFalse(decoder.Refs.callbacks[0].TryGetValue("heroes", out var waiters) && waiters.Count > 0);
		}

		[Test]
		public void UnsentChildCollectionDoesNotReleaseTheRoot()
		{
			var decoder = new Decoder<DState>();
			decoder.Decode(ViewFull);
			for (int i = 0; i <= 2; i++) { decoder.Decode(ViewPatches[i]); }   // P2: "a" (with its never-sent loot) leaves

			Assert.AreSame(decoder.State, decoder.Refs.Get(0));
			Assert.AreEqual(1, decoder.Refs.refCounts[0]);
			Assert.IsTrue(decoder.Refs.Has(decoder.State.open.__refId), "root's live children stay tracked");

			decoder.Decode(ViewPatches[3]);
			Assert.AreEqual(2, decoder.State.tick);
			Assert.AreEqual(31, decoder.State.open["o1"].hp);
		}

		[Test]
		public void PredictAttachAllBeforeTheCollectionArrives()
		{
			var decoder = new Decoder<DState>();
			var predict = new Colyseus.Predict.Predict(new PredictCallbacks<DState>(Callbacks.Get(decoder)), new RoomClock());
			predict.AttachAll("heroes", new AttachConfig { ["x"] = PredictMode.Raw });

			decoder.Decode(ViewFull);
			decoder.Decode(ViewPatches[0]);

			// Raw reads the last SAMPLE; an untracked field falls back to the live value
			var a = decoder.State.heroes["a"];
			a.x = 999;
			Assert.AreEqual(15, predict.Value(a, "x"), "attached when the view delivered it");
		}
	}
}
