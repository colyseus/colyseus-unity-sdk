using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Colyseus.Schema;
using Colyseus.Tests.Resend;
using static Colyseus.Tests.Fixtures.ResendFixtures;

namespace Colyseus.Tests
{
	/// <summary>
	///     The patch after a client's full state re-sends ops that state already
	///     holds. They must decode as unchanged — no re-inserted array entries, no
	///     re-fired OnAdd / Listen — exactly as the JS decoder does. The expected
	///     logs are the JS Callbacks' own, recorded by
	///     Fixtures/generate-resend-fixtures.ts.
	/// </summary>
	[TestFixture]
	public class ResendAfterFullStateTest
	{
		private static string N(object v) => v is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : v?.ToString();

		/// <summary>Group a log into its "--" sections, each order-insensitive.</summary>
		private static List<string> Sections(IEnumerable<string> log)
		{
			var sections = new List<string>();
			var current = new List<string>();
			void Flush() { current.Sort(StringComparer.Ordinal); sections.Add(string.Join(" | ", current)); current.Clear(); }
			foreach (var line in log)
			{
				if (line.StartsWith("--")) { Flush(); }
				current.Add(line);
			}
			Flush();
			return sections;
		}

		private static string Show(RState s) =>
			$"label={s.label} score={N(s.score)} ratio={N(s.ratio)} order=[{string.Join(",", s.order)}] nums=[{string.Join(",", s.nums.Select(v => N(v)))}] " +
			$"units=[{string.Join(",", s.units.Select(u => u.name))}] names=[{string.Join(",", s.names.Select(kv => $"{kv.Key}:{kv.Value}"))}]";

		private static string Show(RStateF s) =>
			$"label={s.label} score={N(s.score)} ratio={N(s.ratio)} order=[{string.Join(",", s.order)}] nums=[{string.Join(",", s.nums.Select(v => N(v)))}] " +
			$"units=[{string.Join(",", s.units.Select(u => u.name))}] names=[{string.Join(",", s.names.Select(kv => $"{kv.Key}:{kv.Value}"))}]";

		private static List<string> Register(Decoder<RState> decoder)
		{
			var log = new List<string>();
			var cb = Callbacks.Get(decoder);
			cb.Listen(s => s.label, (cur, prev) => log.Add($"label={cur}"), false);
			cb.Listen(s => s.score, (cur, prev) => log.Add($"score={N(cur)}"), false);
			cb.Listen(s => s.ratio, (cur, prev) => log.Add($"ratio={N(cur)}"), false);
			cb.OnAdd(s => s.order, (i, v) => log.Add($"order+{i}={v}"), false);
			cb.OnRemove(s => s.order, (i, v) => log.Add($"order-{i}={v}"));
			cb.OnAdd(s => s.nums, (i, v) => log.Add($"nums+{i}={N(v)}"), false);
			cb.OnRemove(s => s.nums, (i, v) => log.Add($"nums-{i}={N(v)}"));
			cb.OnAdd(s => s.units, (i, u) => log.Add($"units+{i}={u.name}"), false);
			cb.OnRemove(s => s.units, (i, u) => log.Add($"units-{i}={u.name}"));
			cb.OnAdd(s => s.names, (k, v) => log.Add($"names+{k}={v}"), false);
			cb.OnRemove(s => s.names, (k, v) => log.Add($"names-{k}={v}"));
			return log;
		}

		private static List<string> Register(Decoder<RStateF> decoder)
		{
			var log = new List<string>();
			var cb = Callbacks.Get(decoder);
			cb.Listen(s => s.label, (cur, prev) => log.Add($"label={cur}"), false);
			cb.Listen(s => s.score, (cur, prev) => log.Add($"score={N(cur)}"), false);
			cb.Listen(s => s.ratio, (cur, prev) => log.Add($"ratio={N(cur)}"), false);
			cb.OnAdd(s => s.order, (i, v) => log.Add($"order+{i}={v}"), false);
			cb.OnRemove(s => s.order, (i, v) => log.Add($"order-{i}={v}"));
			cb.OnAdd(s => s.nums, (i, v) => log.Add($"nums+{i}={N(v)}"), false);
			cb.OnRemove(s => s.nums, (i, v) => log.Add($"nums-{i}={N(v)}"));
			cb.OnAdd(s => s.units, (i, u) => log.Add($"units+{i}={u.name}"), false);
			cb.OnRemove(s => s.units, (i, u) => log.Add($"units-{i}={u.name}"));
			cb.OnAdd(s => s.names, (k, v) => log.Add($"names+{k}={v}"), false);
			cb.OnRemove(s => s.names, (k, v) => log.Add($"names-{k}={v}"));
			return log;
		}

		private static (List<string> log, string state) Joiner(byte[] fullState)
		{
			var decoder = new Decoder<RState>();
			var log = Register(decoder);
			foreach (var (name, frame) in new[] { ("full", fullState), ("tick1", Tick1), ("tick2", Tick2) })
			{
				log.Add("--" + name);
				decoder.Decode(frame);
			}
			return (log, Show(decoder.State));
		}

		[Test]
		public void FirstClientDecodesTheResentPatchAsUnchanged()
		{
			var (log, state) = Joiner(FullA);
			CollectionAssert.AreEqual(Sections(LogA), Sections(log));
			Assert.AreEqual(StateAfterTicks, state);
		}

		[Test]
		public void JoinerDecodesTheResentPatchAsUnchanged()
		{
			var (log, state) = Joiner(FullB);
			CollectionAssert.AreEqual(Sections(LogB), Sections(log));
			Assert.AreEqual(StateAfterTicks, state);
		}

		[Test]
		public void FloatFieldsAndNullCollectionsDecodeTheResentPatchAsUnchanged()
		{
			var decoder = new Decoder<RStateF>();
			var log = Register(decoder);
			foreach (var (name, frame) in new[] { ("full", FullB), ("tick1", Tick1), ("tick2", Tick2) })
			{
				log.Add("--" + name);
				decoder.Decode(frame);
			}
			CollectionAssert.AreEqual(Sections(LogB), Sections(log));
			Assert.AreEqual(StateAfterTicks, Show(decoder.State));
		}

		[Test]
		public void ResyncKeepsSurvivingPrimitivesQuiet()
		{
			var decoder = new Decoder<RState>();
			decoder.Decode(ResyncJoin);
			var log = Register(decoder);
			decoder.DecodeResync(ResyncSnapshot);
			CollectionAssert.AreEquivalent(ResyncLog, log);
			Assert.AreEqual(ResyncState, Show(decoder.State));
		}
	}
}
