using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Colyseus.Predict;
using Colyseus.Tests.CollectionDefaults;
using static Colyseus.Tests.PredictTestSupport;

namespace Colyseus.Tests
{
	/// <summary>
	///     The fields-list shorthand — JS <c>attachAll(key, { fields, mode, snap,
	///     smoothMs, … })</c> — as <see cref="AttachOptions" />, and
	///     <see cref="PredictGetOptions.Name" />.
	/// </summary>
	[TestFixture]
	public class PredictAttachOptionsTest
	{
		/// <summary>Drive two predicts through the same samples; return each one's Value() per frame.</summary>
		private static (double[] a, double[] b) Compare(Action<Colyseus.Predict.Predict> attachA, Action<Colyseus.Predict.Predict> attachB)
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (cbA, _, pA) = MakePredict();
				var (cbB, _, pB) = MakePredict();
				attachA(pA);
				attachB(pB);
				var ea = new HeadingD { __refId = 1, x = 10, dir = 1 };
				var eb = new HeadingD { __refId = 1, x = 10, dir = 1 };
				cbA.Add("ents", ea, "e");
				cbB.Add("ents", eb, "e");

				var outA = new double[8];
				var outB = new double[8];
				for (int i = 0; i < 8; i++)
				{
					now += 30;
					if (i % 2 == 0)
					{
						cbA.Push(ea, "x", 10.0 + i * 4); cbB.Push(eb, "x", 10.0 + i * 4);
						cbA.Push(ea, "dir", 3.0 - i); cbB.Push(eb, "dir", 3.0 - i);
					}
					pA.Tick(now); pB.Tick(now);
					outA[i] = pA.Value(ea, "x") + 1000 * pA.Value(ea, "dir");
					outB[i] = pB.Value(eb, "x") + 1000 * pB.Value(eb, "dir");
				}
				return (outA, outB);
			}
		}

		[Test]
		public void ShorthandMatchesTheDictionaryForm()
		{
			var (a, b) = Compare(
				p => p.AttachAll("ents", new AttachOptions { Fields = new[] { "x", "dir" }, Mode = PredictMode.Lerp, Delay = 50, Snap = 20, Angle = true }),
				p =>
				{
					var o = new PredictFieldOptions { Mode = PredictMode.Lerp, Delay = 50, Snap = 20, Angle = true };
					p.AttachAll("ents", new AttachConfig { ["x"] = o, ["dir"] = o });
				});
			CollectionAssert.AreEqual(b, a);
		}

		[Test]
		public void ShorthandDampedWithSmoothMsMatchesTheDictionaryForm()
		{
			var (a, b) = Compare(
				p => p.AttachAll("ents", new AttachOptions { Fields = new[] { "x" }, Mode = PredictMode.Damped, SmoothMs = 40 }),
				p => p.AttachAll("ents", new AttachConfig { ["x"] = new PredictFieldOptions { Mode = PredictMode.Damped, SmoothMs = 40 } }));
			CollectionAssert.AreEqual(b, a);
		}

		[Test]
		public void OmittedModeFollowsThePredictDefault()
		{
			double now = 1000;
			using (FreezeClock(() => now))
			{
				var (cb, _, predict) = MakePredict(new PredictGetOptions { Mode = PredictMode.Raw });
				var ent = new HeadingD { __refId = 1, x = 10 };
				predict.Attach(ent, new AttachOptions { Fields = new[] { "x" } });
				now = 1050; cb.Push(ent, "x", 20.0);
				now = 1100; cb.Push(ent, "x", 30.0);
				predict.Tick(1130);
				Assert.AreEqual(30, predict.Value(ent, "x"), "raw = latest sample");
			}
		}

		[Test]
		public void ReckonModeUsesTheStepFromOptionsOrThePredict()
		{
			var (_, _, predict) = MakePredict();
			var ent = new HeadingD { __refId = 1, x = 10 };
			predict.Attach(ent, new AttachOptions
			{
				Fields = new[] { "x" }, Mode = PredictMode.Reckon, Substep = 16,
				Step = (s, dt, elapsed) => ((HeadingD)s).x += 1,
			});
			// no server time yet: ValueAt(t) reckons t ms forward = two 16ms substeps
			Assert.AreEqual(12, predict.ValueAt(ent, "x", 32));

			var (_, _, inherited) = MakePredict(new PredictGetOptions { Mode = PredictMode.Reckon, Step = (s, dt, elapsed) => ((HeadingD)s).x += 2 });
			var ent2 = new HeadingD { __refId = 2, x = 10 };
			inherited.Attach(ent2, new AttachOptions { Fields = new[] { "x" } });
			Assert.AreEqual(14, inherited.ValueAt(ent2, "x", 32));
		}

		[Test]
		public void ReckonWithoutAnyStepThrows()
		{
			var (_, _, predict) = MakePredict();
			var ex = Assert.Throws<Exception>(() =>
				predict.Attach(new HeadingD { __refId = 1 }, new AttachOptions { Fields = new[] { "x" }, Mode = PredictMode.Reckon }));
			StringAssert.Contains("Step", ex.Message);
		}

		[Test]
		public void UndeclaredFieldsAreDroppedAndAnEmptyMatchWarns()
		{
			using (CaptureLogs(out var logger))
			{
				var (_, _, predict) = MakePredict();
				var ent = new HeadingD { __refId = 1, x = 10 };
				predict.Attach(ent, new AttachOptions { Fields = new[] { "nope" }, Mode = PredictMode.Lerp });
				Assert.AreEqual(1, logger.Warnings.Count);
				predict.Attach(ent, new AttachOptions { Fields = new[] { "x", "nope" }, Mode = PredictMode.Raw });
				Assert.AreEqual(1, logger.Warnings.Count, "a partial match is fine");
			}
		}

		[Test]
		public void NameRoundTripsAndDefaultsToAnAutoLabel()
		{
			var (_, _, named) = MakePredict(new PredictGetOptions { Name = "heroes" });
			Assert.AreEqual("heroes", named.Name);

			var (_, _, a) = MakePredict();
			var (_, _, b) = MakePredict();
			StringAssert.IsMatch(@"^predict#\d+$", a.Name);
			Assert.AreNotEqual(a.Name, b.Name);

			named.SetDefaults(new PredictGetOptions { Name = "other", Delay = 10 });
			Assert.AreEqual("heroes", named.Name, "construction-time only, like JS");
		}
	}
}
