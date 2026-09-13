using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Colyseus.Schema;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.Fixtures.CollectionFixtures;

namespace Colyseus.Tests
{
	/// <summary>
	///     foreach / LINQ over ArraySchema and MapSchema. Maps enumerate
	///     KeyValuePairs in INSERTION order (JS Map order — game code tie-breaks
	///     on it), and the struct enumerators allocate nothing.
	/// </summary>
	[TestFixture]
	public class SchemaCollectionEnumerationTest
	{
		/// <summary>b, c, a after a remove + re-add ("a" moves to the end) and a replace ("b" keeps its place).</summary>
		private static MapSchema<int> Churned()
		{
			var m = new MapSchema<int>();
			m["b"] = 2;
			m["a"] = 1;
			m["c"] = 3;
			m.Remove("a");
			m["a"] = 10;
			m["b"] = 20;
			return m;
		}

		[Test]
		public void ArraySchemaYieldsItemsInOrder()
		{
			var a = new ArraySchema<int>(new List<int> { 3, 1, 2 });
			var seen = new List<int>();
			foreach (var v in a) { seen.Add(v); }

			CollectionAssert.AreEqual(new[] { 3, 1, 2 }, seen);
			CollectionAssert.AreEqual(new[] { 3, 2 }, a.Where(v => v > 1).ToArray());
			Assert.AreEqual(6, a.Sum());
			IReadOnlyList<int> list = a;
			Assert.AreEqual(1, list[1]);
		}

		[Test]
		public void MapSchemaYieldsPairsInInsertionOrder()
		{
			var m = Churned();
			var seen = new List<string>();
			foreach (var kv in m) { seen.Add($"{kv.Key}={kv.Value}"); }
			CollectionAssert.AreEqual(new[] { "b=20", "c=3", "a=10" }, seen);

			seen.Clear();
			foreach (var (key, value) in m) { seen.Add(key + value); }
			CollectionAssert.AreEqual(new[] { "b20", "c3", "a10" }, seen);
		}

		[Test]
		public void TypedKeysAndValuesViews()
		{
			var m = Churned();
			var keys = new List<string>();
			foreach (var k in m.Keys) { keys.Add(k); }
			var values = new List<int>();
			foreach (var v in m.Values) { values.Add(v); }

			CollectionAssert.AreEqual(new[] { "b", "c", "a" }, keys);
			CollectionAssert.AreEqual(new[] { 20, 3, 10 }, values);
			Assert.AreEqual(3, m.Keys.Count);
			Assert.AreEqual(3, m.Values.Count);
		}

		[Test]
		public void KeysAndValuesStillServeNonGenericCallers()
		{
			var m = Churned();
			ICollection legacyValues = m.Values;
			ICollection legacyKeys = m.Keys;
			var boxed = new List<object>();
			foreach (object o in legacyValues) { boxed.Add(o); }
			CollectionAssert.AreEqual(new object[] { 20, 3, 10 }, boxed);

			var copy = new object[3];
			legacyKeys.CopyTo(copy, 0);
			CollectionAssert.AreEqual(new object[] { "b", "c", "a" }, copy);

			// the untyped foreach people wrote against the old ICollection still compiles
			int sum = 0;
			foreach (int v in m.Values) { sum += v; }
			Assert.AreEqual(33, sum);
		}

		[Test]
		public void Linq()
		{
			var m = Churned();
			CollectionAssert.AreEqual(new[] { "b", "a" }, m.Where(kv => kv.Value > 5).Select(kv => kv.Key).ToArray());
			CollectionAssert.AreEqual(new[] { 20, 10 }, m.Values.Where(v => v > 5).ToList());
			CollectionAssert.AreEqual(new[] { "a", "b", "c" }, m.Keys.OrderBy(k => k).ToArray());
			Assert.AreEqual(10, m.ToDictionary(kv => kv.Key, kv => kv.Value)["a"]);

			IReadOnlyDictionary<string, int> ro = m;
			Assert.IsTrue(ro.ContainsKey("c"));
			Assert.IsTrue(ro.TryGetValue("a", out var a) && a == 10);
			CollectionAssert.AreEqual(new[] { "b", "c", "a" }, ro.Keys.ToArray());
		}

		[Test]
		public void ExistingMembersKeepWorking()
		{
			var m = Churned();
			var viaForEach = new List<string>();
			m.ForEach((string k, int v) => viaForEach.Add(k));
			CollectionAssert.AreEqual(new[] { "b", "c", "a" }, viaForEach);
			Assert.AreEqual(3, m.Count);
			Assert.AreEqual(20, m["b"]);
			Assert.AreEqual(0, m["missing"], "missing key reads as default, as before");
			Assert.IsTrue(m.ContainsKey((object)"a"));
			Assert.IsTrue(m.TryGetValue("c", out var c) && c == 3);

			var withInitializer = new MapSchema<string> { { "x", "1" }, { "y", "2" } };
			CollectionAssert.AreEqual(new[] { "x", "y" }, withInitializer.Keys.ToArray());
		}

		[Test]
		public void ModifyingDuringEnumerationThrows()
		{
			var m = Churned();
			Assert.Throws<InvalidOperationException>(() => { foreach (var kv in m) { m.Remove(kv.Key); } });
		}

		[Test]
		public void CompactionKeepsOrderAcrossHeavyChurn()
		{
			var m = new MapSchema<int>();
			for (int i = 0; i < 100; i++) { m[i.ToString()] = i; }
			for (int i = 0; i < 100; i += 2) { m.Remove(i.ToString()); }
			for (int i = 100; i < 110; i++) { m[i.ToString()] = i; }

			var expected = Enumerable.Range(0, 100).Where(i => i % 2 == 1).Concat(Enumerable.Range(100, 10)).ToArray();
			CollectionAssert.AreEqual(expected, m.Values.ToArray());
			CollectionAssert.AreEqual(expected.Select(i => i.ToString()), m.Keys.ToArray());
			Assert.AreEqual(expected.Length, m.Count);
			Assert.AreEqual(51, m["51"]);
		}

		[Test]
		public void DecodedMapFollowsTheServerInsertionOrder()
		{
			var decoder = new Decoder<DState>();
			decoder.Decode(ViewFull);
			for (int i = 0; i < ViewPatches.Length; i++)
			{
				decoder.Decode(ViewPatches[i]);
				Assert.AreEqual(ViewKeyOrder[i], string.Join(",", decoder.State.heroes.Keys), $"after P{i}");
			}
		}

		[Test]
		public void EnumerationDoesNotAllocate()
		{
			var m = Churned();
			var a = new ArraySchema<int>(new List<int> { 1, 2, 3 });
			int Walk()
			{
				int n = 0;
				foreach (var kv in m) { n += kv.Value; }
				foreach (var k in m.Keys) { n += k.Length; }
				foreach (var v in m.Values) { n += v; }
				foreach (var v in a) { n += v; }
				return n;
			}
			Walk(); // warm-up: JIT + the cached Keys/Values views

			long before = GC.GetAllocatedBytesForCurrentThread();
			int total = 0;
			for (int i = 0; i < 100; i++) { total += Walk(); }
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

			Assert.AreEqual(0, allocated);
			Assert.AreEqual(100 * (33 + 3 + 33 + 6), total);
		}

#pragma warning disable CS0618
		[Test]
		public void LegacyItemsViewStillReadsAndWrites()
		{
			var m = Churned();
			var keys = new List<string>();
			foreach (DictionaryEntry e in m.items) { keys.Add((string)e.Key); }
			CollectionAssert.AreEqual(new[] { "b", "c", "a" }, keys);
			Assert.AreEqual(3, m.items.Count);
			Assert.AreEqual(3, m.items["c"]);
			Assert.IsTrue(m.items.Contains("a"));
			m.items["d"] = 4;
			Assert.AreEqual(4, m["d"]);
		}

		/// <summary>The old OrderedDictionary took an int key as a position — `items[i]` loops must keep working.</summary>
		[Test]
		public void LegacyItemsIntKeyIsAPosition()
		{
			var m = Churned(); // live order b, c, a with a tombstone left by the remove
			var byPosition = new List<int>();
			for (int i = 0; i < m.Count; i++) { byPosition.Add((int)m.items[i]); }
			CollectionAssert.AreEqual(new[] { 20, 3, 10 }, byPosition);

			m.items[1] = 30;
			Assert.AreEqual(30, m["c"]);
			Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = m.items[3]; });
		}
#pragma warning restore CS0618
	}
}
