using NUnit.Framework;
using Colyseus.Predict;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     Lag compensation's render delay follows the Predict's default-profile
	///     lerp delay (Predictor.ts canonicalDelay/bindInputRenderDelay): a live
	///     binding, independent of which instances happen to be attached, with an
	///     explicit handle value still winning.
	/// </summary>
	[TestFixture]
	public class PredictRenderDelayTest
	{
		private static Reconciler<HeadingD, TurnInput> Spawn(Colyseus.Predict.Predict predict, InputHandle input)
			=> predict.Reconciler(new HeadingD { __refId = 99 }, new ReconcilerOptions<HeadingD, TurnInput>
			{
				Input = input,
				StepMs = 50,
				Step = (ctx, s, cmd) => { },
			});

		private static HeadingD Ent(int refId) => new HeadingD { __refId = refId };

		[Test]
		public void ReconcilerCreatedBeforeAnyAttachBindsTheDefaultDelay()
		{
			var (_, _, predict) = MakePredict();
			var input = MakeHandle(new TurnInput());
			Spawn(predict, input);
			Assert.AreEqual(100, input.RenderDelay);
		}

		[Test]
		public void AttachedSlotDelaysDoNotLeakIntoTheBinding()
		{
			foreach (var order in new[] { new[] { 50.0, 150.0 }, new[] { 150.0, 50.0 } })
			{
				var (_, _, predict) = MakePredict();
				predict.Attach(Ent(1), new AttachConfig { ["x"] = new PredictFieldOptions { Mode = PredictMode.Lerp, Delay = order[0] } });
				predict.Attach(Ent(2), new AttachConfig { ["x"] = new PredictFieldOptions { Mode = PredictMode.Lerp, Delay = order[1] } });
				var input = MakeHandle(new TurnInput());
				Spawn(predict, input);
				Assert.AreEqual(100, input.RenderDelay, $"attach order {order[0]}, {order[1]}");
			}
		}

		[Test]
		public void BindingFollowsTheDefaultDelayLive()
		{
			var (_, _, predict) = MakePredict(new PredictGetOptions { Delay = 80 });
			var input = MakeHandle(new TurnInput());
			Spawn(predict, input);
			Assert.AreEqual(80, input.RenderDelay);

			predict.SetDefaults(new PredictGetOptions { Delay = 60 });
			Assert.AreEqual(60, input.RenderDelay, "re-evaluated on read, like the JS provider on every send");
		}

		[Test]
		public void SimBindsTheSameWay()
		{
			var (_, _, predict) = MakePredict(new PredictGetOptions { Delay = 70 });
			var input = MakeHandle(new TurnInput());
			predict.Sim(new SimReconcilerOptions<OpaqueWorld, TurnInput>
			{
				Input = input,
				StepMs = 50,
				World = new OpaqueWorld(),
				Step = (ctx, w, cmd) => { },
				Adopt = w => { },
			});
			Assert.AreEqual(70, input.RenderDelay);
		}

		[Test]
		public void ExplicitHandleValueWins()
		{
			var (_, _, predict) = MakePredict();
			var input = new InputHandle(new TurnInput(), new Colyseus.Schema.InputEncoder(new TurnInput()), false, false, 40, null,
				null, null, null, () => new StubConnection(), () => null);
			Spawn(predict, input);
			Assert.AreEqual(40, input.RenderDelay);

			var set = MakeHandle(new TurnInput());
			set.RenderDelay = 25;
			Spawn(predict, set);
			predict.SetDefaults(new PredictGetOptions { Delay = 60 });
			Assert.AreEqual(25, set.RenderDelay);
		}

		public class OpaqueWorld { public double x; }
	}
}
