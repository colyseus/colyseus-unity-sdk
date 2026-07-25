using System;
using System.Collections.Generic;
using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>
	///     Phase 4 — Predict layer.
	///
	///     Scenarios mirror colyseus-0.18 PORTING/generate-predict-fixtures.cts
	///     (self-verified against the JS reference). Contract:
	///     PORTING/sdk-ports-predict-layer.md.
	/// </summary>
	[TestFixture]
	public class PredictTest
	{
		private class StubConnection : Connection
		{
			public StubConnection() : base("ws://localhost", null)
			{
				IsOpen = true;
			}

			public override System.Threading.Tasks.Task Send(byte[] data) =>
				System.Threading.Tasks.Task.CompletedTask;
		}

		private static InputHandle MakeHandle(Schema.Schema input)
		{
			var encoder = new InputEncoder(input);
			var stub = new StubConnection();
			return new InputHandle(input, encoder, false, false, 0, null,
				null, null, null, () => stub, () => null);
		}

		[Test]
		public void ReconcilerCoreTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var truth = new SchemaTest.Predict.ReconState { x = 0, vx = 0 };
				var command = new SchemaTest.Predict.AccelInput();
				var handle = MakeHandle(command);
				var me = new Reconciler<SchemaTest.Predict.ReconState, SchemaTest.Predict.AccelInput>(truth,
					new ReconcilerOptions<SchemaTest.Predict.ReconState, SchemaTest.Predict.AccelInput>
					{
						Input = handle,
						Fields = new[] { "x", "vx" },
						Step = (ctx, s, cmd) => { s.vx += cmd.ax * (float)ctx.Dt; s.x += s.vx * (float)ctx.Dt; },
						Smoothing = 0,
						StepMs = 50,
					});

				// server mirror: the SAME deterministic step applied to truth
				void ServerStep(float ax)
				{
					truth.vx += ax * 0.05f;
					truth.x += truth.vx * 0.05f;
				}

				now = 0; me.Tick(now);
				// fixture trajectory (float32 fields — the C# schema "number" is
				// float, so the math runs in f32; the JS fixture used f64 doubles.
				// The trajectory values 0.025.. are exactly representable ops here.
				var sent = new List<float>();
				for (int i = 1; i <= 6; i++)
				{
					now = i * 50; me.Tick(now);
					float ax = i <= 3 ? 10 : -5;
					sent.Add(ax);
					command.ax = ax;
					handle.Send();
					if (i >= 3)
					{
						ServerStep(sent[i - 3]);
						handle.AckInput(i - 2);
						me.Tick(now);
					}
					Assert.AreEqual(0, me.LastCorrectionMag, $"reconcile {i} should match exactly");
				}
				Assert.AreEqual(4, me.ReconcileSeq);
				Assert.AreEqual(0.75f, me.State.vx);

				// divergent truth: server-side teleport the client didn't predict
				ServerStep(sent[3]);
				truth.x += 100;
				handle.AckInput(5);
				now = 350; me.Tick(now);
				Assert.AreEqual(100, me.LastCorrectionMag, 1e-4);
				Assert.AreEqual(truth.x + 0.75f * 0.05f /* replayed input 6 */, me.State.x, 1e-4);
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}

		[Test]
		public void ReconcilerMemoEpochTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var truth = new SchemaTest.Predict.ReconState { x = 0 };
				var command = new SchemaTest.Predict.AccelInput();
				var handle = MakeHandle(command);
				int computeRuns = 0;
				var me = new Reconciler<SchemaTest.Predict.ReconState, SchemaTest.Predict.AccelInput>(truth,
					new ReconcilerOptions<SchemaTest.Predict.ReconState, SchemaTest.Predict.AccelInput>
					{
						Input = handle,
						Fields = new[] { "x" },
						Step = (ctx, s, cmd) =>
						{
							// memo: computed once live, frozen on replay
							object bonus = ctx.Memo<object>(() =>
							{
								computeRuns++;
								return cmd.ax >= 2 ? (object)5f : null;
							});
							s.x += cmd.ax + (bonus is float b ? b : 0);
						},
						Smoothing = 0,
						StepMs = 50,
					});

				now = 0; me.Tick(now);
				command.ax = 1; handle.Send();
				command.ax = 2; handle.Send();   // memoizes 5
				command.ax = 1; handle.Send();
				Assert.AreEqual(9f, me.State.x);
				Assert.AreEqual(3, computeRuns);

				// ack 1 with matching truth → adopt + replay 2..3; memo frozen
				truth.x = 1;
				handle.AckInput(1);
				now = 50; me.Tick(now);
				Assert.AreEqual(9f, me.State.x);
				Assert.AreEqual(3, computeRuns);

				// epoch follow: handle reset → controller self-resets from truth
				truth.x = 42;
				handle.Reset();
				now = 100; me.Tick(now);
				Assert.AreEqual(42f, me.State.x);
				Assert.AreEqual(0, me.PendingCount);
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}

		private static (Decoder<T> decoder, StateCallbackStrategy<T> callbacks, RoomClock clock, Colyseus.Predict.Predict predict)
			MakePassive<T>() where T : Schema.Schema, new()
		{
			var decoder = new Decoder<T>();
			var callbacks = Schema.Callbacks.Get(decoder);
			var clock = new RoomClock();
			var predict = new Colyseus.Predict.Predict(new PredictCallbacks<T>(callbacks), clock);
			return (decoder, callbacks, clock, predict);
		}

		[Test]
		public void PassiveSmoothingTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (decoder, _, clock, predict) = MakePassive<SchemaTest.Predict.PassiveEnt>();
				clock.SetPatchInterval(50);
				var ent = decoder.State;

				predict.Track(ent, "a", new PredictFieldOptions { Mode = PredictMode.Lerp });
				predict.Track(ent, "b", new PredictFieldOptions { Mode = PredictMode.Damped });
				predict.Track(ent, "c", new PredictFieldOptions { Mode = PredictMode.Extrapolate, Damping = 0 });
				predict.Track(ent, "d", new PredictFieldOptions { Mode = PredictMode.Raw });
				predict.Track(ent, "yaw", new PredictFieldOptions { Mode = PredictMode.Lerp, Angle = true });

				void Patch(double sNow, byte[] bytes)
				{
					now = sNow;
					clock.Sample(sNow, -1);   // offset 0 → serverNow == now
					decoder.Decode(bytes, new Iterator { Offset = 0 });
				}

				Patch(1000, new byte[] { 128, 10, 129, 10, 130, 10, 131, 10, 132, 3 });
				Patch(1050, new byte[] { 128, 20, 129, 20, 130, 20, 131, 20, 132, 253 }); // yaw -3 (±π seam)
				Patch(1100, new byte[] { 128, 30, 129, 30, 130, 30, 131, 30 });

				// fixture reads @1150: lerp(a)=20, raw(d)=30, extrapolate(c)=40
				now = 1150; predict.Tick(now);
				Assert.AreEqual(20, predict.Value(ent, "a"));
				Assert.AreEqual(30, predict.Value(ent, "d"));
				Assert.AreEqual(40, predict.Value(ent, "c"));
				Assert.Greater(Math.Abs(predict.Value(ent, "yaw")), 3.0); // stayed on the seam

				now = 1175; predict.Tick(now);
				Assert.AreEqual(25, predict.Value(ent, "a"));
				Assert.AreEqual(45, predict.Value(ent, "c"));

				// idle-resume gap collapse: a idle 1100→1400 then 40; synthetic
				// held sample (1350, 30) → 31 / 35 / 40
				Patch(1400, new byte[] { 128, 40 });
				now = 1455; predict.Tick(now);
				Assert.AreEqual(31, predict.Value(ent, "a"));
				now = 1475; predict.Tick(now);
				Assert.AreEqual(35, predict.Value(ent, "a"));
				now = 1500; predict.Tick(now);
				Assert.AreEqual(40, predict.Value(ent, "a"));
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}

		[Test]
		public void ReckonValueAtTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var (decoder, _, clock, predict) = MakePassive<SchemaTest.Predict.ReckonBall>();
				var ball = decoder.State;

				predict.TrackReckon(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall>
				{
					Fields = new[] { "x" },
					Step = (s, dt, elapsed) => { s.x += s.vx * (float)dt; },
					Smoothing = 0,   // raw projection (exp()-free)
					Substep = 10,
				});

				// patch: x=100 vx=50 stamped sNow=1000 (offset 0 → serverNow == now)
				now = 1000;
				clock.Sample(1000, -1);
				decoder.Decode(new byte[] { 128, 100, 129, 50 }, new Iterator { Offset = 0 });

				// fixture: 1000→100, 1050→102.5, 1100→105, 1200→110
				foreach (var (sNow, expected) in new[] { (1000.0, 100.0), (1050.0, 102.5), (1100.0, 105.0), (1200.0, 110.0) })
				{
					now = sNow;
					predict.Tick(now);
					Assert.AreEqual(expected, predict.Value(ball, "x"), 1e-9);
				}

				// valueAt: arbitrary instant raw; past clamps to the snapshot
				Assert.AreEqual(107.5, predict.ValueAt(ball, "x", 1150), 1e-9);
				Assert.AreEqual(100, predict.ValueAt(ball, "x", 900), 1e-9);
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}

		[Test]
		public void EventChannelSettlementTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var clock = new RoomClock();
				now = 1000;
				clock.Sample(1000, -1);

				var log = new List<string>();
				int unpredicted = 0;
				var chan = new PredictedEventChannel<string>(new PredictedEventChannelOptions<string>
				{
					OnPredict = p => log.Add($"P:{p}"),
					OnConfirm = p => log.Add($"C:{p}"),
					OnReject = p => log.Add($"R:{p}"),
					OnUnpredicted = k => unpredicted++,
					GraceTicks = 3,
				}, clock);

				chan.Predict("goal-a");
				Assert.AreEqual(1, chan.PendingCount);
				Assert.AreEqual(1, chan.Confirm("goal-a"));
				Assert.AreEqual(0, chan.Confirm("goal-b"));
				Assert.AreEqual(1, unpredicted);

				// pending dedupe
				chan.Predict("kill-1");
				chan.Predict("kill-1");
				Assert.AreEqual(1, chan.PendingCount);

				// sim-born entry with grace auto-reject
				int acked = 10;
				((IPredictSink<string>)chan).PredictFromSim(12, "kill-2", () => acked);
				Assert.AreEqual(2, chan.PendingCount);
				acked = 14;
				chan.Prune();
				Assert.IsTrue(chan.Has("kill-2"));
				acked = 15;   // >= 12 + 3 → auto-reject
				chan.Prune();
				Assert.IsFalse(chan.Has("kill-2"));

				// UI-born TTL: rtt 0 → 600ms
				now = 1601;
				chan.Prune();
				Assert.AreEqual(0, chan.PendingCount);

				CollectionAssert.AreEqual(
					new[] { "P:goal-a", "C:goal-a", "P:kill-1", "P:kill-2", "R:kill-2", "R:kill-1" },
					log);
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}

		private class Rocket
		{
			public string Owner;
			public double BornMs;
		}

		[Test]
		public void SpawnsCorrelationTest()
		{
			double now = 0;
			var originalNow = RoomClock.GetNow;
			RoomClock.GetNow = () => now;
			try
			{
				var clock = new RoomClock();
				now = 1000;
				clock.Sample(1000, -1);

				var rejected = new List<int>();
				var store = new PredictedSpawns<Rocket, double[]>(new PredictedSpawnsOptions<Rocket, double[]>
				{
					Owned = s => s.Owner == "me",
					SpawnTime = s => s.BornMs,
					Step = (l, dt) => l[0] += 10 * dt,
					OnReject = (l, id) => rejected.Add(id),
				}, clock);

				var local = new double[] { 0 };
				var h1 = store.Spawn(local);
				Assert.AreEqual(1, h1.Id);

				// pending local steps on the serverNow axis: 1000→1100 = dt 0.1 → +1
				store.Tick(0);
				now = 1100;
				store.Tick(0);
				Assert.AreEqual(1, local[0], 1e-9);

				// fifo match + lead = bornMs − at = 1080 − 1000
				var server1 = new Rocket { Owner = "me", BornMs = 1080 };
				store.HandleAdd(server1);
				var e1 = store.EntryFor(server1);
				Assert.AreEqual(1, e1.Id);
				Assert.IsTrue(e1.Confirmed);
				Assert.AreEqual(80, e1.LeadMs);

				// foreign entity: never consumes a prediction
				var server2 = new Rocket { Owner = "them", BornMs = 1090 };
				store.HandleAdd(server2);
				Assert.AreEqual(0, store.EntryFor(server2).LeadMs);
				Assert.IsNull(store.EntryFor(server2).Local);

				// mispredict prune: unmatched pending, TTL 600
				var h2 = store.Spawn(new double[] { 0 });
				now = 1701;
				store.Prune();
				CollectionAssert.AreEqual(new[] { h2.Id }, rejected);
				Assert.IsFalse(store.Alive(h2.Id));

				// remove drops the confirmed entry
				store.HandleRemove(server1);
				Assert.IsFalse(store.Alive(1));
				Assert.AreEqual(1, store.Size);
			}
			finally
			{
				RoomClock.GetNow = originalNow;
			}
		}
	}
}
