using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>Read / ReadAt — batched Value / ValueAt, one reckon integration per batch.</summary>
	[TestFixture]
	public class PredictReadTest
	{
		[Test]
		public void ReadAtIntegratesOncePerBatch()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var ball = new SchemaTest.Predict.ReckonBall { x = 100, vx = 50 };
				var (_, clock, predict) = MakePredict();
				int stepCalls = 0;
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall>
				{
					Fields = new[] { "x", "vx" },
					Step = (s, dt, elapsed) => { stepCalls++; s.x += s.vx * (float)dt; },
					SmoothMs = 0,
					Substep = 50,
				});
				clock.Sample(1000, -1);

				// singles: one integration each (150ms → 3 substeps apiece)
				stepCalls = 0;
				double x = predict.ValueAt(ball, "x", 1150);
				double vx = predict.ValueAt(ball, "vx", 1150);
				Assert.AreEqual(6, stepCalls);
				Assert.AreEqual(107.5, x, 1e-9);
				Assert.AreEqual(50, vx, 1e-9);

				// the batch: one walk for both fields
				stepCalls = 0;
				var scratch = new double[2];
				var read = predict.ReadAt(ball, new[] { "x", "vx" }, 1150, scratch);
				Assert.AreSame(scratch, read);
				Assert.AreEqual(3, stepCalls);
				CollectionAssert.AreEqual(new[] { x, vx }, read);

				// a too-small buffer is replaced; past clamps to the snapshot, like ValueAt
				var fresh = predict.ReadAt(ball, new[] { "x" }, 900, new double[0]);
				Assert.AreEqual(1, fresh.Length);
				Assert.AreEqual(100, fresh[0], 1e-9);
			}
		}

		[Test]
		public void ReadMatchesValueAndFallsBackForUntrackedFields()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var ent = new SchemaTest.Predict.PassiveEnt { a = 10, b = 7 };
				var (cb, _, predict) = MakePredict();
				predict.Attach(ent, new AttachConfig { ["a"] = PredictMode.Lerp });

				now = 1050; cb.Push(ent, "a", 20f);
				now = 1130; predict.Tick(now);
				var read = predict.Read(ent, new[] { "a", "b" });
				Assert.AreEqual(predict.Value(ent, "a"), read[0]);
				Assert.AreEqual(16, read[0], 1e-12);
				Assert.AreEqual(7, read[1], "untracked → the live value");
				// time is ignored off the reckon path, like ValueAt
				CollectionAssert.AreEqual(read, predict.ReadAt(ent, new[] { "a", "b" }, 5000));
			}
		}
	}
}
