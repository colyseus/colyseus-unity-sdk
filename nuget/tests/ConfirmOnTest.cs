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
	///     ConfirmOn — declarative settlement wired by predict.DefineEvent off the
	///     callbacks face (mirror of sdk test/predict-confirm-on.test.ts). The
	///     fake drives add/remove/listen by hand; no decoder bytes.
	/// </summary>
	[TestFixture]
	public class ConfirmOnTest
	{
		private static (FakeCallbacks cb, Colyseus.Predict.Predict predict) Make(string sessionId = "me")
		{
			var (cb, _, predict) = MakePredict(sessionId: () => sessionId);
			return (cb, predict);
		}

		private static SchemaTest.Predict.Crate Crate(int refId, bool alive = true, string owner = null)
			=> new SchemaTest.Predict.Crate { __refId = refId, alive = alive, owner = owner };

		[Test]
		public void FieldFlipConfirmsByCollectionKey()
		{
			var (cb, predict) = Make();
			var log = new List<string>();
			var breaks = predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "crates", Field = "alive", Equals = false },
				OnConfirm = id => log.Add($"C:{id}"),
				OnUnpredicted = key => log.Add($"U:{key}"),
			});

			// an already-flipped child at bind time is history, not a settle signal
			var dead = Crate(1, alive: false);
			cb.Existing["crates"] = new List<(Schema.Schema, object)> { (dead, "c0") };
			var c1 = Crate(2);
			var c2 = Crate(3);
			cb.Add("crates", c1, "c1");
			cb.Add("crates", c2, "c2");
			Assert.AreEqual(0, log.Count);

			breaks.Predict("c1");
			cb.Push(c1, "alive", false);             // ours → confirmed by key
			cb.Push(c2, "alive", false);             // nobody predicted → unpredicted
			cb.Push(c1, "hp", 3f);                   // another field: no signal
			CollectionAssert.AreEqual(new[] { "C:c1", "U:c2" }, log);
			Assert.IsFalse(breaks.Has("c1"));

			// a decoder re-fire for one ref attaches no second listener
			int before = cb.ListenerCount;
			cb.Add("crates", c1, "c1");
			Assert.AreEqual(before, cb.ListenerCount);

			// removal drops the child's listener; dispose drops the rest
			cb.Remove("crates", c1, "c1");
			Assert.AreEqual(before - 1, cb.ListenerCount);
			breaks.Dispose();
			Assert.AreEqual(0, cb.ListenerCount);
			cb.Push(c2, "alive", true);
			cb.Push(c2, "alive", false);
			Assert.AreEqual(2, log.Count, "detached: no further settlement");
		}

		[Test]
		public void EqualsComparesNumbersAcrossWidths()
		{
			var (cb, predict) = Make();
			int confirmed = 0;
			var empty = predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "crates", Field = "hp", Equals = 0 },   // int vs the float field
				OnConfirm = _ => confirmed++,
			});
			var c = Crate(1);
			cb.Add("crates", c, "c1");
			empty.Predict("c1");
			cb.Push(c, "hp", 0f);
			Assert.AreEqual(1, confirmed);
		}

		[Test]
		public void RemoveConfirmsTheRemovedKey()
		{
			var (cb, predict) = Make();
			var log = new List<string>();
			var eaten = predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "bananas", Event = "remove" },
				OnConfirm = id => log.Add($"C:{id}"),
				OnUnpredicted = key => log.Add($"U:{key}"),
			});
			var b = Crate(1);
			eaten.Predict("b1");
			cb.Remove("bananas", b, "b1");
			cb.Remove("bananas", b, "b2");
			CollectionAssert.AreEqual(new[] { "C:b1", "U:b2" }, log);
		}

		[Test]
		public void AddSettlesKeylessAndOnlyForMine()
		{
			var (cb, predict) = Make("me");
			var log = new List<string>();
			var drops = predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "bananas", Event = "add", Mine = "owner" },
				OnConfirm = id => log.Add($"C:{id}"),
				OnUnpredicted = key => log.Add($"U:{key}"),
			});

			// existing children never settle (immediate = false)
			cb.Existing["bananas"] = new List<(Schema.Schema, object)> { (Crate(1, owner: "me"), "old") };
			drops.Predict("pending-drop");
			cb.Add("bananas", Crate(2, owner: "them"), "b1");   // a remote player's spawn
			Assert.AreEqual(0, log.Count);
			cb.Add("bananas", Crate(3, owner: "me"), "b2");     // OUR arrival settles the pending drop
			CollectionAssert.AreEqual(new[] { "C:pending-drop" }, log);
			Assert.IsFalse(drops.Has());
		}

		[Test]
		public void AddWithoutMineSettlesOnAnyArrival()
		{
			var (cb, predict) = Make(null);
			int confirmed = 0;
			var drops = predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "bananas", Event = "add" },
				OnConfirm = _ => confirmed++,
			});
			drops.Predict("x");
			cb.Add("bananas", Crate(1, owner: "them"), "b1");
			Assert.AreEqual(1, confirmed);
		}

		[Test]
		public void MalformedBindingThrowsAtDefineTime()
		{
			var (_, predict) = Make();
			Assert.Throws<Exception>(() => predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "crates" },
			}));
			Assert.Throws<Exception>(() => predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Event = "add" },
			}));
		}

		[Test]
		public void PredictDisposeTearsDownTheWiring()
		{
			var (cb, predict) = Make();
			predict.DefineEvent(new PredictedEventChannelOptions<string>
			{
				ConfirmOn = new ConfirmOn { Collection = "crates", Field = "alive", Equals = false },
			});
			cb.Add("crates", Crate(1), "c1");
			Assert.AreEqual(1, cb.ListenerCount);
			predict.Dispose();
			Assert.AreEqual(0, cb.ListenerCount);
		}
	}
}
