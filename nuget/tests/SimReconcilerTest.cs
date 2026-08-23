using System;
using System.Collections.Generic;
using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     SimReconciler — the COMPOSITE face: world binding, mirror creation,
	///     auto-adopt, pose registration (mirror of colyseus-haxe
	///     tests/SimReconcilerTestCase.hx).
	/// </summary>
	[TestFixture]
	public class SimReconcilerTest
	{
		/// <summary>
		///     The world handle: a plain class with PUBLIC FIELDS (the binding rule).
		///     Both fields are replaced in place by mirrors at construction.
		/// </summary>
		private class World
		{
			public SchemaTest.Predict.SimPaddle paddle;
			public SchemaTest.Predict.SimPuck puck;
		}

		/// <summary>No field holds a schema instance, so nothing binds.</summary>
		private class OpaqueWorld
		{
			public double n;
		}

		private class LabelWorld
		{
			public SchemaTest.Predict.SimLabel label;
			public SchemaTest.Predict.SimPuck puck;
		}

		/// <summary>Counts what a step declared through ctx.Predict.</summary>
		private class CountingSink : IPredictSink<float>
		{
			public readonly List<int> Seqs = new List<int>();
			public void PredictFromSim(int seq, float payload, Func<int> acked) => Seqs.Add(seq);
		}

		/// <summary>
		///     Distinct refIds: Predict's slot map keys on them, and a hand-built
		///     schema (the mirror included) otherwise shares refId 0.
		/// </summary>
		private static (SchemaTest.Predict.SimPaddle paddle, SchemaTest.Predict.SimPuck puck) MakeParts()
		{
			var paddle = new SchemaTest.Predict.SimPaddle { x = 1, y = 2, team = "left", __refId = 7 };
			var puck = new SchemaTest.Predict.SimPuck { x = 10, vx = 3, __refId = 8 };
			return (paddle, puck);
		}

		/// <summary>puck accelerates, paddle drifts — deterministic, shared with "the server".</summary>
		private static void StepWorld(StepContext ctx, World w, SchemaTest.Predict.AccelInput cmd)
		{
			w.puck.vx += cmd.ax * (float)ctx.Dt;
			w.puck.x += w.puck.vx * (float)ctx.Dt;
			w.paddle.x += cmd.ax * (float)ctx.Dt;
		}

		private static void ServerStep(SchemaTest.Predict.SimPaddle paddle, SchemaTest.Predict.SimPuck puck, float ax)
		{
			puck.vx += ax * 0.05f;
			puck.x += puck.vx * 0.05f;
			paddle.x += ax * 0.05f;
		}

		private class Rig
		{
			public SchemaTest.Predict.SimPaddle Paddle;
			public SchemaTest.Predict.SimPuck Puck;
			public World World;
			public SchemaTest.Predict.AccelInput Command;
			public InputHandle Handle;
			public SimReconcilerOptions<World, SchemaTest.Predict.AccelInput> Options;
			public SimReconciler<World, SchemaTest.Predict.AccelInput> Sim;
		}

		/// <summary>Parts, world, input handle and options; <c>Sim</c> is built unless the test wants predict.Sim.</summary>
		private static Rig MakeRig(double smoothMs = 0, Action<StepContext, World, SchemaTest.Predict.AccelInput> step = null, bool build = true)
		{
			var (paddle, puck) = MakeParts();
			var rig = new Rig
			{
				Paddle = paddle, Puck = puck,
				World = new World { paddle = paddle, puck = puck },
				Command = new SchemaTest.Predict.AccelInput(),
			};
			rig.Handle = MakeHandle(rig.Command);
			rig.Options = new SimReconcilerOptions<World, SchemaTest.Predict.AccelInput>
			{
				Input = rig.Handle, World = rig.World, Step = step ?? StepWorld, SmoothMs = smoothMs, StepMs = 50,
			};
			if (build) { rig.Sim = new SimReconciler<World, SchemaTest.Predict.AccelInput>(rig.Options); }
			return rig;
		}

		// --- Binding -----------------------------------------------------------

		[Test]
		public void BindingReplacesWorldFieldsWithSeededMirrors()
		{
			var rig = MakeRig();
			var (paddle, puck, world, sim) = (rig.Paddle, rig.Puck, rig.World, rig.Sim);

			// replaced IN PLACE — the caller's own object now points at the mirror
			Assert.AreSame(world, sim.World);
			Assert.AreNotSame(paddle, world.paddle);
			Assert.AreNotSame(puck, world.puck);
			Assert.AreEqual(typeof(SchemaTest.Predict.SimPaddle), world.paddle.GetType());
			Assert.AreEqual(typeof(SchemaTest.Predict.SimPuck), world.puck.GetType());

			// ...and seeded from the source
			Assert.AreEqual(1f, world.paddle.x);
			Assert.AreEqual(2f, world.paddle.y);
			Assert.AreEqual(10f, world.puck.x);
			Assert.AreEqual(3f, world.puck.vx);

			// pose keys: NUMERIC fields only, "<worldField>.<schemaField>", declaration order
			Assert.AreEqual("paddle.x,paddle.y,puck.x,puck.y,puck.vx,puck.vy", string.Join(",", sim.PoseKeys));
		}

		// --- String fields -----------------------------------------------------

		[Test]
		public void StringFieldsRideTheMirrorVerbatim()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var rig = MakeRig();
				var (paddle, puck, world, command, handle, sim) = (rig.Paddle, rig.Puck, rig.World, rig.Command, rig.Handle, rig.Sim);

				// seeded, not left at the class default
				Assert.AreEqual("left", world.paddle.team);

				// ...and re-adopted when the server changes it
				now = 0; sim.Tick(now);
				command.ax = 10; handle.Send();
				paddle.team = "right";
				ServerStep(paddle, puck, 10);
				handle.AckInput(1);
				now = 50; sim.Tick(now);
				Assert.AreEqual("right", world.paddle.team);

				// never posed: a string has no curve to error-correct
				CollectionAssert.DoesNotContain(sim.PoseKeys, "paddle.team");
				Assert.IsTrue(double.IsNaN(sim.Value("paddle.team")));
			}
		}

		/// <summary>
		///     A part whose only scalars are strings still binds — it is state
		///     worth restoring on rollback, it just contributes no poses.
		/// </summary>
		[Test]
		public void StringOnlyPartBindsWithoutPoses()
		{
			var (_, puck) = MakeParts();
			var label = new SchemaTest.Predict.SimLabel { team = "left", __refId = 9 };
			var world = new LabelWorld { label = label, puck = puck };
			var handle = MakeHandle(new SchemaTest.Predict.AccelInput());
			var sim = new SimReconciler<LabelWorld, SchemaTest.Predict.AccelInput>(
				new SimReconcilerOptions<LabelWorld, SchemaTest.Predict.AccelInput>
				{
					Input = handle, World = world, Step = (ctx, w, cmd) => { }, SmoothMs = 0, StepMs = 50,
				});

			Assert.AreNotSame(label, world.label);
			Assert.AreEqual("left", world.label.team);
			Assert.AreEqual("puck.x,puck.y,puck.vx,puck.vy", string.Join(",", sim.PoseKeys));
		}

		// --- Overlay routing ---------------------------------------------------

		/// <summary>
		///     predict.Value(decodedInstance, field) must read the reconciled pose.
		///     The registration carries the SOURCE, not the mirror that replaced it —
		///     easy to break, and the render layer would silently read raw state.
		/// </summary>
		[Test]
		public void PredictValueRoutesThroughTheReconciledPose()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var rig = MakeRig(build: false);
				var (paddle, command, handle) = (rig.Paddle, rig.Command, rig.Handle);
				var puck = rig.Puck;
				var (_, _, predict) = MakePredict();
				var sim = predict.Sim(rig.Options);

				now = 0; predict.Tick(now);
				command.ax = 10; handle.Send();
				// the pose interpolates between the two latest steps, so let the render
				// clock reach the step that Send just applied
				now = 50; predict.Tick(now);

				Assert.AreEqual(sim.Value("puck.x"), predict.Value(puck, "x"), 1e-9);
				Assert.AreEqual(sim.Value("paddle.x"), predict.Value(paddle, "x"), 1e-9);
				// the raw source never moved; the predicted pose did
				Assert.AreEqual(10f, puck.x);
				Assert.AreEqual(10.175, predict.Value(puck, "x"), 1e-5);
			}
		}

		// --- Adopt + replay ----------------------------------------------------

		/// <summary>
		///     The core rollback contract on the composite face: ack an older seq
		///     whose truth diverges, and the unacked inputs replay on top of it
		///     rather than the world snapping to the server value.
		/// </summary>
		[Test]
		public void DivergentAckAdoptsThenReplaysUnackedInputs()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var rig = MakeRig();
				var (paddle, puck, world, command, handle, sim) = (rig.Paddle, rig.Puck, rig.World, rig.Command, rig.Handle, rig.Sim);

				now = 0; sim.Tick(now);
				for (int i = 1; i <= 3; i++)
				{
					command.ax = 10;
					handle.Send();          // stepped eagerly on send
				}
				float predicted = world.puck.x;
				Assert.AreEqual(3, sim.PendingCount);

				// server processed input 1 only, and teleported the puck
				ServerStep(paddle, puck, 10);
				puck.x += 100;
				handle.AckInput(1);
				now = 50; sim.Tick(now);

				// inputs 2..3 replayed on top of the adopted truth
				Assert.AreEqual(2, sim.PendingCount);
				Assert.AreEqual(predicted + 100, world.puck.x, 1e-3);
				Assert.AreEqual(1, sim.ReconcileSeq);
				Assert.AreEqual(100, sim.LastCorrectionMag, 1e-3);
				// signed the same way as the flat face: predicted - truth
				Assert.AreEqual(-100, sim.LastCorrection["puck.x"], 1e-3);
			}
		}

		/// <summary>
		///     With smoothing armed the correction lands in the error term, so the
		///     RENDERED pose lags the raw predicted state instead of popping.
		/// </summary>
		[Test]
		public void CorrectionDecaysThroughTheErrorTermNotThePose()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var rig = MakeRig(smoothMs: 100);
				var (paddle, puck, world, command, handle, sim) = (rig.Paddle, rig.Puck, rig.World, rig.Command, rig.Handle, rig.Sim);

				now = 0; sim.Tick(now);
				command.ax = 10; handle.Send();

				ServerStep(paddle, puck, 10);
				puck.x += 100;
				handle.AckInput(1);
				now = 50; sim.Tick(now);

				// raw mirror jumped with the truth; the rendered pose has not caught up
				Assert.AreEqual(100, sim.LastCorrectionMag, 1e-3);
				Assert.Greater(Math.Abs(sim.Value("puck.x") - world.puck.x), 1);

				// ...and the gap decays away
				now = 1000; sim.Tick(now);
				Assert.AreEqual(world.puck.x, sim.Value("puck.x"), 0.05);
			}
		}

		// --- Adopt optionality -------------------------------------------------

		[Test]
		public void WorldWithNothingBoundRequiresAdopt()
		{
			var handle = MakeHandle(new SchemaTest.Predict.AccelInput());
			var ex = Assert.Throws<Exception>(() =>
				new SimReconciler<OpaqueWorld, SchemaTest.Predict.AccelInput>(
					new SimReconcilerOptions<OpaqueWorld, SchemaTest.Predict.AccelInput>
					{
						Input = handle, World = new OpaqueWorld(), Step = (ctx, w, cmd) => { }, StepMs = 50,
					}));
			StringAssert.Contains("`Adopt` is required", ex.Message);
		}

		[Test]
		public void WorldWithNothingBoundAcceptsAdopt()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var command = new SchemaTest.Predict.AccelInput();
				var handle = MakeHandle(command);
				double truth = 0;
				var w = new OpaqueWorld();
				var sim = new SimReconciler<OpaqueWorld, SchemaTest.Predict.AccelInput>(
					new SimReconcilerOptions<OpaqueWorld, SchemaTest.Predict.AccelInput>
					{
						Input = handle, World = w,
						Step = (ctx, world, cmd) => { world.n += cmd.ax; },
						Adopt = world => { world.n = truth; },
						StepMs = 50,
					});

				now = 0; sim.Tick(now);
				command.ax = 5; handle.Send();
				Assert.AreEqual(5.0, w.n);

				truth = 1;
				handle.AckInput(1);
				now = 50; sim.Tick(now);
				Assert.AreEqual(1.0, w.n);          // adopt ran; nothing left to replay
				Assert.AreEqual(0, sim.PoseKeys.Count);
			}
		}

		// --- ctx.Predict -------------------------------------------------------

		/// <summary>
		///     One-shot presentation must fire on the LIVE step only, never again
		///     when that same input replays under rollback.
		/// </summary>
		[Test]
		public void CtxPredictFiresOnceAcrossAReplay()
		{
			double now = 0;
			using (FreezeClock(() => now))
			{
				var sink = new CountingSink();
				var rig = MakeRig(step: (ctx, w, cmd) =>
				{
					StepWorld(ctx, w, cmd);
					ctx.Predict(sink, cmd.ax);
				});
				var (paddle, puck, command, handle, sim) = (rig.Paddle, rig.Puck, rig.Command, rig.Handle, rig.Sim);

				now = 0; sim.Tick(now);
				for (int i = 1; i <= 3; i++) { command.ax = 10; handle.Send(); }
				CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sink.Seqs);

				// ack 1 -> inputs 2..3 replay, and must NOT re-declare
				ServerStep(paddle, puck, 10);
				handle.AckInput(1);
				now = 50; sim.Tick(now);
				CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sink.Seqs);
			}
		}
	}
}
