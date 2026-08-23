using System;
using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     Predict.Get(room, options) / SetDefaults / Mode — room-wide defaults an
	///     attach inherits when it omits an option. Each attach SNAPSHOTS what it
	///     resolved, so a later SetDefaults never moves an attached slot.
	/// </summary>
	[TestFixture]
	public class PredictGetOptionsTest
	{
		private static (SchemaTest.Predict.PassiveEnt ent, FakeCallbacks cb, Colyseus.Predict.Predict predict)
			Setup(PredictGetOptions opts = null, float a0 = 10)
		{
			var (cb, _, predict) = MakePredict(opts);
			return (new SchemaTest.Predict.PassiveEnt { a = a0 }, cb, predict);
		}

		[Test]
		public void ModeDefaultsToLerpWhenNoOptionsAreGiven()
		{
			var (_, _, predict) = Setup();
			Assert.AreEqual(PredictMode.Lerp, predict.Mode);
		}

		[Test]
		public void AttachWithoutModeFollowsThePredictDefault()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ent, cb, predict) = Setup(new PredictGetOptions { Mode = PredictMode.Raw });
				Assert.AreEqual(PredictMode.Raw, predict.Mode);
				predict.Attach(ent, new AttachConfig { ["a"] = new PredictFieldOptions() });

				now = 1050; cb.Push(ent, "a", 20f);
				now = 1100; cb.Push(ent, "a", 30f);
				now = 1130; predict.Tick(now);
				// raw = the latest sample; a lerp would sit mid-segment at now − 100ms
				Assert.AreEqual(30, predict.Value(ent, "a"));
			}
		}

		[Test]
		public void DelayDefaultFlowsIntoALerpAttach()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ent, cb, predict) = Setup(new PredictGetOptions { Delay = 50 });
				var (ent0, cb0, predict0) = Setup();
				predict.Attach(ent, new AttachConfig { ["a"] = PredictMode.Lerp });
				predict0.Attach(ent0, new AttachConfig { ["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, Delay = 50 } });

				now = 1050; cb.Push(ent, "a", 20f); cb0.Push(ent0, "a", 20f);
				now = 1130; predict.Tick(now); predict0.Tick(now);   // target 1080 → past the newest sample
				Assert.AreEqual(20, predict.Value(ent, "a"), 1e-12);
				Assert.AreEqual(predict0.Value(ent0, "a"), predict.Value(ent, "a"), "inherited ≡ explicit");
			}
		}

		[Test]
		public void SetDefaultsAfterAttachDoesNotMoveTheSlot()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ent, cb, predict) = Setup(new PredictGetOptions { Mode = PredictMode.Raw });
				predict.Attach(ent, new AttachConfig { ["a"] = new PredictFieldOptions() });

				predict.SetDefaults(new PredictGetOptions { Mode = PredictMode.Damped });
				Assert.AreEqual(PredictMode.Damped, predict.Mode);

				now = 1050; cb.Push(ent, "a", 20f);
				now = 1100; cb.Push(ent, "a", 30f);
				now = 1130; predict.Tick(now);
				Assert.AreEqual(30, predict.Value(ent, "a"), "the attached slot stayed raw");

				// ...while a NEW attach takes the new default
				var other = new SchemaTest.Predict.PassiveEnt { a = 10, __refId = 2 };
				predict.Attach(other, new AttachConfig { ["a"] = new PredictFieldOptions() });
				now = 1150; cb.Push(other, "a", 30f);
				now = 1160; predict.Tick(now);
				double v = predict.Value(other, "a");
				Assert.Greater(v, 10, "damped chases");
				Assert.Less(v, 30, "...but has not arrived in one frame");
			}
		}

		[Test]
		public void SetDefaultsTouchesOnlyThePresentOptions()
		{
			var (_, _, predict) = Setup(new PredictGetOptions { Mode = PredictMode.Extrapolate });
			predict.SetDefaults(new PredictGetOptions { Delay = 30 });
			Assert.AreEqual(PredictMode.Extrapolate, predict.Mode);
			predict.SetDefaults(null);
			Assert.AreEqual(PredictMode.Extrapolate, predict.Mode);
		}

		[Test]
		public void SmoothMsDefaultStaysNullForLerp()
		{
			// The GOTCHA this guards: 50 is the damped/extrapolate default — a
			// Predict that never set SmoothMs must NOT spring its lerps with it.
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ent, cb, p) = Setup(new PredictGetOptions { Mode = PredictMode.Lerp });
				var (ent0, cb0, p0) = Setup();
				p.Attach(ent, new AttachConfig { ["a"] = new PredictFieldOptions() });
				p0.Attach(ent0, new AttachConfig { ["a"] = new PredictFieldOptions { Mode = PredictMode.Lerp, SmoothMs = 0 } });

				now = 1050; cb.Push(ent, "a", 20f); cb0.Push(ent0, "a", 20f);
				now = 1130; p.Tick(now); p0.Tick(now);        // target 1030 → u = 0.6
				Assert.AreEqual(16, p.Value(ent, "a"), 1e-12, "mid-segment interpolant");
				Assert.AreEqual(p0.Value(ent0, "a"), p.Value(ent, "a"), "unset ≡ explicit 0");
			}
		}

		[Test]
		public void SmoothMsDefaultArmsTheLerpSpring()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (entRaw, cbRaw, raw) = Setup();
				var (entSm, cbSm, sm) = Setup(new PredictGetOptions { SmoothMs = 30 });
				raw.Attach(entRaw, new AttachConfig { ["a"] = PredictMode.Lerp });
				sm.Attach(entSm, new AttachConfig { ["a"] = PredictMode.Lerp });

				for (now = 1050; now <= 1400; now += 50)
				{
					float x = 10 + (float)(now - 1000) / 5;
					cbRaw.Push(entRaw, "a", x); cbSm.Push(entSm, "a", x);
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
				Assert.Less(vSm, vRaw, "the inherited spring trails the raw output");
			}
		}

		// --- Reckon inheritance ------------------------------------------------

		private static (SchemaTest.Predict.ReckonBall ball, FakeCallbacks cb, RoomClock clock, Colyseus.Predict.Predict predict)
			ReckonSetup(PredictGetOptions opts)
		{
			var (cb, clock, predict) = MakePredict(opts);
			return (new SchemaTest.Predict.ReckonBall { x = 100, vx = 50 }, cb, clock, predict);
		}

		[Test]
		public void ReckonAttachInheritsStepFromThePredict()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ball, _, clock, predict) = ReckonSetup(new PredictGetOptions
				{
					Mode = PredictMode.Reckon,
					Step = (s, dt, elapsed) => { var b = (SchemaTest.Predict.ReckonBall)s; b.x += b.vx * (float)dt; },
					SmoothMs = 0,   // raw projection (exp()-free)
					Substep = 10,
				});
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall> { Fields = new[] { "x" } });

				clock.Sample(1000, -1);   // offset 0 → serverNow == now
				// same trajectory as ReckonValueAtTest: 1000→100, 1100→105, 1200→110
				foreach (var (sNow, expected) in new[] { (1000.0, 100.0), (1100.0, 105.0), (1200.0, 110.0) })
				{
					now = sNow;
					predict.Tick(now);
					Assert.AreEqual(expected, predict.Value(ball, "x"), 1e-9);
				}
				Assert.AreEqual(107.5, predict.ValueAt(ball, "x", 1150), 1e-9);
			}
		}

		[Test]
		public void PerAttachStepWinsOverThePredictDefault()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (ball, _, clock, predict) = ReckonSetup(new PredictGetOptions
				{
					Step = (s, dt, elapsed) => Assert.Fail("the inherited step must not run"),
					SmoothMs = 0,
				});
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall>
				{
					Fields = new[] { "x" },
					Step = (s, dt, elapsed) => { s.x += s.vx * (float)dt; },
				});
				clock.Sample(1000, -1);
				now = 1100; predict.Tick(now);
				Assert.AreEqual(105, predict.Value(ball, "x"), 1e-3);   // f32 scratch, 16ms substeps
			}
		}

		[Test]
		public void ReckonAttachWithoutAnyStepThrows()
		{
			var (ball, _, _, predict) = ReckonSetup(null);
			var ex = Assert.Throws<Exception>(() =>
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall> { Fields = new[] { "x" } }));
			StringAssert.Contains("requires a 'Step' function", ex.Message);
		}

		[Test]
		public void ReckonAttachWithoutFieldsThrows()
		{
			var (ball, _, _, predict) = ReckonSetup(null);
			var ex = Assert.Throws<Exception>(() =>
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall> { Step = (s, dt, e) => { } }));
			StringAssert.Contains("requires `Fields`", ex.Message);
		}
	}
}
