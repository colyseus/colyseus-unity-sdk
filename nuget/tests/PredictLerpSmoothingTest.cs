using System;
using System.Collections.Generic;
using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>
	///     Lerp + <c>SmoothMs</c> — the display-only output spring on the lerp
	///     result (mirror of sdk test/predict-lerp-smoothing.test.ts). Default 0
	///     (off): the output stays the raw interpolant, bit-identical to a
	///     spring-less lerp. Armed, it keeps rendered velocity continuous,
	///     trailing the raw output by speed × SmoothMs during motion —
	///     frame-rate independently (exact first-order-hold step).
	///
	///     The reference's fields-array / constructor-defaults spellings don't
	///     exist on this port; the per-field map is the one config surface, so
	///     those cases collapse into the trail test. The setDefaults mode-flip
	///     case maps to "damped with SmoothMs unset uses its own 50 default".
	/// </summary>
	[TestFixture]
	public class PredictLerpSmoothingTest
	{
		/// <summary>Listener stub — tests push samples directly, no decoder bytes.</summary>
		private class FakeCallbacks : IPredictCallbacks
		{
			private readonly Dictionary<string, Action<object>> listeners = new Dictionary<string, Action<object>>();

			public Action Listen(Schema.Schema instance, string field, Action<object> handler, bool immediate)
			{
				listeners[field] = handler;
				if (immediate) { handler(instance[field]); }
				return () => listeners.Remove(field);
			}

			public Action OnAdd(string collection, Action<Schema.Schema, object> handler) => () => { };
			public Action OnRemove(string collection, Action<Schema.Schema, object> handler) => () => { };

			public void Push(string field, double value) => listeners[field](value);
		}

		private static (SchemaTest.Predict.PassiveEnt ent, FakeCallbacks cb, Colyseus.Predict.Predict predict) Setup(float x0 = 10)
		{
			var ent = new SchemaTest.Predict.PassiveEnt { a = x0 };
			var cb = new FakeCallbacks();
			var predict = new Colyseus.Predict.Predict(cb, new RoomClock());
			return (ent, cb, predict);
		}

		[Test]
		public void SmoothMsOmittedIsBitIdenticalToExplicitZero()
		{
			// The GOTCHA this guards: 50 is the damped/extrapolate default —
			// lerp must NOT silently spring with it.
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (ent, cb, p) = Setup();
				var (_, cb0, p0) = Setup();
				p.Attach(ent, new AttachConfig { ["a"] = PredictMode.Lerp });
				p0.Attach(ent, new AttachConfig { ["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, SmoothMs = 0 } });

				now = 1050; cb.Push("a", 20); cb0.Push("a", 20);
				now = 1130; p.Tick(now); p0.Tick(now);        // target 1030 → u = 0.6
				double v = p.Value(ent, "a");
				Assert.AreEqual(16, v, 1e-12, "mid-segment interpolant");
				Assert.AreEqual(p0.Value(ent, "a"), v, "default ≡ explicit 0");

				now = 1145; cb.Push("a", 35); cb0.Push("a", 35);
				now = 1170; p.Tick(now); p0.Tick(now);
				Assert.AreEqual(p0.Value(ent, "a"), p.Value(ent, "a"), "stays identical across frames");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void SmoothMsTrailsTheRawOutputDuringMotion()
		{
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (entRaw, cbRaw, raw) = Setup();
				var (entSm, cbSm, sm) = Setup();
				raw.Attach(entRaw, new AttachConfig { ["a"] = PredictMode.Lerp });
				sm.Attach(entSm, new AttachConfig { ["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, SmoothMs = 30 } });

				for (now = 1050; now <= 1400; now += 50)
				{
					double x = 10 + (now - 1000) / 5;
					cbRaw.Push("a", x); cbSm.Push("a", x);
				}
				for (now = 1000; now <= 1400; now += 10)
				{
					raw.Tick(now); sm.Tick(now);
					raw.Value(entRaw, "a"); sm.Value(entSm, "a");
				}
				now -= 10;
				double vRaw = raw.Value(entRaw, "a");
				double vSm = sm.Value(entSm, "a");
				Assert.Greater(vRaw, 10, "raw is moving");
				Assert.Less(vSm, vRaw, "spring trails the raw output");
				Assert.Greater(vSm, vRaw - 15, "by a bounded distance, not stuck");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void SteadyMoverTrailsBySpeedTimesSmoothMsAtAnyFrameRate()
		{
			// 200 u/s stream, SmoothMs 25 → trail = 200 × 0.025 = 5 u. The exact
			// first-order-hold step makes it hold at ANY tick cadence.
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				double Run(double tickMs)
				{
					now = 1000;
					var (ent, cb, p) = Setup();
					p.Attach(ent, new AttachConfig { ["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, SmoothMs = 25 } });
					double v = 10;
					for (now = 1000 + tickMs; now <= 2500; now += tickMs)
					{
						if (now % 50 == 0) { cb.Push("a", 10 + (now - 1000) / 5); }
						p.Tick(now);
						v = p.Value(ent, "a");
					}
					double rawAt2500 = 10 + (2500 - 100 - 1000) / 5;   // target = now − delay(100)
					return rawAt2500 - v;
				}

				Assert.AreEqual(5, Run(10), 1e-6, "trail = speed × SmoothMs");
				Assert.AreEqual(5, Run(25), 1e-6, "same trail at a coarser tick");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void SnapTeleportPopsTheSpring()
		{
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (ent, cb, p) = Setup();
				p.Attach(ent, new AttachConfig
				{
					["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, Snap = 4, SmoothMs = 30 },
				});

				now = 1050; cb.Push("a", 10.2);           // establish cadence
				now = 3000; cb.Push("a", 60);             // teleport
				now = 3060; p.Tick(now);
				Assert.AreEqual(60, p.Value(ent, "a"), 1e-9, "spring state popped with the ring");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void DampedUnsetSmoothMsKeepsItsOwnDefault()
		{
			// Lerp's 0 default must not leak into damped: unset SmoothMs on a
			// damped field chases with the 50ms default, not frozen at 0.
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (ent, cb, p) = Setup();
				p.Attach(ent, new AttachConfig { ["a"] = PredictMode.Damped });

				now = 1050; cb.Push("a", 60);
				now = 1110; p.Tick(now);
				double v = p.Value(ent, "a");
				Assert.Greater(v, 10, "damped is chasing — SmoothMs 50 intact");
				Assert.Less(v, 60, "still mid-glide");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void DampedExplicitZeroSnapsToTheLatestValue()
		{
			// The old rate-form k=0 froze the output — 0 now means snap.
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (ent, cb, p) = Setup();
				p.Attach(ent, new AttachConfig
				{
					["a"] = new PredictFieldOptions { Mode = PredictMode.Damped, SmoothMs = 0 },
				});

				now = 1050; cb.Push("a", 60);
				now = 1110; p.Tick(now);
				Assert.AreEqual(60, p.Value(ent, "a"), "SmoothMs 0 = snap");
			}
			finally { RoomClock.GetNow = originalNow; }
		}

		[Test]
		public void SameFrameReReadsReturnTheSameValue()
		{
			double now = 1000;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (ent, cb, p) = Setup();
				p.Attach(ent, new AttachConfig
				{
					["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, SmoothMs = 30 },
				});

				now = 1050; cb.Push("a", 20);
				now = 1130; p.Tick(now);
				double v1 = p.Value(ent, "a");
				Assert.AreEqual(v1, p.Value(ent, "a"), "spring advances once per frame");
			}
			finally { RoomClock.GetNow = originalNow; }
		}
	}
}
