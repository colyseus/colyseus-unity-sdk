using System;
using System.Collections.Generic;
using NUnit.Framework;
using Colyseus.Predict;
using Colyseus.Schema;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.Fixtures.CollectionFixtures;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     float32 fields between predicted steps. The server (JS) keeps a
	///     float32 field as a double in memory and only the wire rounds it, so a
	///     predicted replay must carry doubles step to step to reproduce it. A
	///     float32 field declared <c>double</c> gives the reconciler that
	///     double-backed mirror; the truth compare still rounds to the wire.
	/// </summary>
	[TestFixture]
	public class Float32PrecisionTest
	{
		private const double Speed = 3;
		private static readonly double[] Turns = { 0.375, 0.25, -0.5, 0.125, 0.75, -0.25, 0.5, 0.375, -0.125, 0.25, 0.625, -0.375 };

		[Test]
		public void Float32DecodesIntoDoubleFieldsAndCollections()
		{
			var decoder = new Decoder<WState>();
			decoder.Decode(Wide);
			var s = decoder.State;
			CollectionAssert.AreEqual(WideValues, new[] { s.f, s.nums[0], s.nums[1], s.tags["k"] });
			Assert.IsInstanceOf<double>(s["f"]);
		}

		/// <summary>The double-precision reference: what the server computes for the same commands.</summary>
		private static (double[] x, double[] dir) Reference(double dt)
		{
			var x = new double[Turns.Length + 1];
			var dir = new double[Turns.Length + 1];
			for (int i = 0; i < Turns.Length; i++)
			{
				dir[i + 1] = dir[i] + Turns[i] * dt;
				x[i + 1] = x[i] + Math.Cos(dir[i + 1]) * Speed * dt;
			}
			return (x, dir);
		}

		private static void Send(InputHandle input, int i)
		{
			((TurnInput)input.Data).turn = Turns[i];
			input.Send();
		}

		[Test]
		public void FloatDeclaredFloat32RoundsEveryPredictedStep()
		{
			var input = MakeHandle(new TurnInput());
			var recon = new Reconciler<HeadingF, TurnInput>(new HeadingF { __refId = 1 }, new ReconcilerOptions<HeadingF, TurnInput>
			{
				Input = input,
				StepMs = 50,
				Step = (ctx, s, cmd) =>
				{
					s.dir = (float)(s.dir + cmd.turn * ctx.Dt);
					s.x += Math.Cos(s.dir) * Speed * ctx.Dt;
				},
			});
			for (int i = 0; i < Turns.Length; i++) { Send(input, i); }

			var (x, _) = Reference(0.05);
			Assert.AreNotEqual(x[Turns.Length], recon.State.x, "a float mirror drifts from the server's doubles");
		}

		[Test]
		public void DoubleBackedFloat32ReplaysBitExactAndReconcilesAtWirePrecision()
		{
			var input = MakeHandle(new TurnInput());
			var truth = new HeadingD { __refId = 1 };
			var recon = new Reconciler<HeadingD, TurnInput>(truth, new ReconcilerOptions<HeadingD, TurnInput>
			{
				Input = input,
				StepMs = 50,
				Step = (ctx, s, cmd) =>
				{
					s.dir += cmd.turn * ctx.Dt;
					s.x += Math.Cos(s.dir) * Speed * ctx.Dt;
				},
			});
			var (x, dir) = Reference(0.05);

			// an ack whose dir the wire actually rounds
			int acked = 3;
			while ((float)dir[acked] == dir[acked]) { acked++; }
			for (int i = 0; i < Turns.Length; i++) { Send(input, i); }
			Assert.AreEqual(x[Turns.Length], recon.State.x);
			Assert.AreEqual(dir[Turns.Length], recon.State.dir);

			// The decoded truth at the ack is wire-rounded: "number" by the codec's dynamic rule, float32 by fround.
			truth.x = Reconciler<HeadingD, TurnInput>.QuantizeAutoNumber(x[acked]);
			truth.dir = (float)dir[acked];
			input.AckInput(acked);
			recon.Tick(1000);

			Assert.AreEqual(1, recon.ReconcileSeq);
			Assert.AreEqual(0, recon.LastCorrectionMag, "matches at wire precision — no adopt");
			Assert.AreEqual(x[Turns.Length], recon.State.x, "the predicted doubles survive the reconcile");
			Assert.AreEqual(dir[Turns.Length], recon.State.dir);

			// a genuine mismatch still adopts + replays
			truth.dir = (float)(dir[acked + 1] + 0.01);
			input.AckInput(acked + 1);
			recon.Tick(1016);
			Assert.Greater(recon.LastCorrectionMag, 0);
		}
	}
}
