using System.Collections.Generic;
using NUnit.Framework;
using Colyseus.Predict;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     ctx.Memo / ctx.TryMemo against the JS MemoStore (rollback.ts): the live
	///     step computes and freezes; a replay returns the frozen value WITHOUT
	///     computing, or reports "nothing stored" — never recomputes.
	/// </summary>
	[TestFixture]
	public class PredictMemoTest
	{
		private class Harness
		{
			public readonly InputHandle Input = MakeHandle(new TurnInput());
			public readonly HeadingD Truth = new HeadingD { __refId = 1 };
			public Reconciler<HeadingD, TurnInput> Recon;

			public Harness(System.Action<StepContext, HeadingD, TurnInput> step)
			{
				Recon = new Reconciler<HeadingD, TurnInput>(Truth, new ReconcilerOptions<HeadingD, TurnInput>
				{
					Input = Input, StepMs = 50, Step = step,
				});
			}

			/// <summary>Send n inputs, then ack the first against mismatching truth — seqs 2..n replay.</summary>
			public void SendThenForceReplay(int n)
			{
				for (int i = 0; i < n; i++) { Input.Send(); }
				Truth.x = 100;
				Input.AckInput(1);
				Recon.Tick(1000);
			}
		}

		[Test]
		public void ValueTypeMemoOfZeroReplaysAsPresent()
		{
			int computes = 0;
			var replayed = new List<(int tick, bool found, int value)>();
			var h = new Harness((ctx, s, cmd) =>
			{
				s.x += 1;
				bool found = ctx.TryMemo("zero", () => { computes++; return 0; }, out int z);
				if (ctx.IsReplay) { replayed.Add((ctx.Tick, found, z)); }
			});
			h.SendThenForceReplay(3);

			Assert.AreEqual(3, computes, "computed once per live step, never on replay");
			CollectionAssert.AreEqual(new[] { (2, true, 0), (3, true, 0) }, replayed);
		}

		[Test]
		public void ReplayWithNothingStoredReportsAbsentWithoutComputing()
		{
			int computes = 0;
			var replayed = new List<(bool found, int value)>();
			var h = new Harness((ctx, s, cmd) =>
			{
				s.x += 1;
				// live x stays small, so only the replay (on adopted x = 100) reaches this memo
				if (s.x > 50)
				{
					bool found = ctx.TryMemo(() => { computes++; return 7; }, out int v);
					replayed.Add((found, v));
					Assert.AreEqual(0, ctx.Memo(() => 7), "Memo<T> keeps returning default when absent");
				}
			});
			h.SendThenForceReplay(3);

			Assert.AreEqual(0, computes);
			CollectionAssert.AreEqual(new[] { (false, 0), (false, 0) }, replayed);
		}

		[Test]
		public void NullIsAMemoizedValue()
		{
			var replayed = new List<(bool found, string value)>();
			var h = new Harness((ctx, s, cmd) =>
			{
				s.x += 1;
				bool found = ctx.TryMemo("miss", () => (string)null, out var hit);
				if (ctx.IsReplay) { replayed.Add((found, hit)); }
			});
			h.SendThenForceReplay(2);

			CollectionAssert.AreEqual(new[] { (true, (string)null) }, replayed);
		}
	}
}
