using NUnit.Framework;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     An attach that matches NO field says so — once per class + config
	///     shape — while partial drops stay silent, as the reference's own test
	///     asserts (one config covers a heterogeneous collection).
	/// </summary>
	[TestFixture]
	public class PredictAttachWarningsTest
	{
		[Test]
		public void AttachMatchingNothingWarnsOnce()
		{
			using (CaptureLogs(out var log))
			{
				var (_, _, predict) = MakePredict();
				var a = new SchemaTest.Predict.SimPaddle { __refId = 1 };
				var b = new SchemaTest.Predict.SimPaddle { __refId = 2 };
				var config = new AttachConfig { ["nonsense"] = PredictMode.Lerp };

				predict.Attach(a, config);
				Assert.AreEqual(1, log.Warnings.Count);
				StringAssert.Contains("SimPaddle", log.Warnings[0]);
				StringAssert.Contains("[nonsense]", log.Warnings[0]);
				StringAssert.Contains("available: [x, y]", log.Warnings[0]);

				// same class + shape (an AttachAll over a collection): silent
				predict.Attach(b, config);
				Assert.AreEqual(1, log.Warnings.Count);

				// a different shape on the same class is a different mistake
				predict.Attach(a, new AttachConfig { ["other"] = PredictMode.Lerp });
				Assert.AreEqual(2, log.Warnings.Count);
			}
		}

		[Test]
		public void PartialDropsStaySilent()
		{
			using (CaptureLogs(out var log))
			{
				var (_, _, predict) = MakePredict();
				var paddle = new SchemaTest.Predict.SimPaddle();
				predict.Attach(paddle, new AttachConfig { ["x"] = PredictMode.Lerp, ["bogus"] = PredictMode.Lerp });
				Assert.AreEqual(0, log.Warnings.Count);
			}
		}

		[Test]
		public void StringKeyIsDroppedNotTracked()
		{
			using (CaptureLogs(out var log))
			{
				var (_, _, predict) = MakePredict();
				var paddle = new SchemaTest.Predict.SimPaddle { team = "left" };
				predict.Attach(paddle, new AttachConfig { ["team"] = PredictMode.Lerp });
				Assert.AreEqual(1, log.Warnings.Count);
				// nothing tracked: Value() is the raw fallback (a string → 0)
				Assert.AreEqual(0, predict.Value(paddle, "team"));
			}
		}

		[Test]
		public void ReckonAttachMatchingNothingWarns()
		{
			using (CaptureLogs(out var log))
			{
				var (_, _, predict) = MakePredict();
				var ball = new SchemaTest.Predict.ReckonBall();
				predict.Attach(ball, new ReckonOptions<SchemaTest.Predict.ReckonBall>
				{
					Fields = new[] { "nope" },
					Step = (s, dt, e) => { },
				});
				Assert.AreEqual(1, log.Warnings.Count);
				StringAssert.Contains("ReckonBall", log.Warnings[0]);
			}
		}
	}
}
