using System;
using System.Collections.Generic;
using System.Reflection;

namespace Colyseus.Predict
{
	/// <summary>Options for <see cref="SimReconciler{W,I}" />.</summary>
	public class SimReconcilerOptions<W, I> : RollbackOptions where W : class
	{
		/// <summary>
		///     Your world handle — a small class whose public fields reach the
		///     simulated state. Fields holding a DECODED schema instance
		///     (<c>paddle = state.players[sid]</c>, <c>puck = state.puck</c>) are
		///     auto-bound: replaced IN PLACE by mirrors that the controller seeds,
		///     re-adopts on every ack, and poses as
		///     <c>"&lt;field&gt;.&lt;schemaField&gt;"</c>. Every other field is
		///     opaque and untouched.
		///
		///     Set once and passed to every callback, never swapped — so the
		///     object you hand in IS the one your step mutates, and reading
		///     <c>World.paddle</c> after construction gives you the mirror.
		/// </summary>
		public W World;
		/// <summary>
		///     Deterministic input-application step, SHARED with the server. Apply
		///     <c>command</c> to <c>world</c> and advance it by <c>ctx.Dt</c>.
		///     Parameter order matches <see cref="Reconciler{S,I}" />'s
		///     <c>Step(ctx, state, command)</c> — world ≈ state.
		/// </summary>
		public Action<StepContext, W, I> Step;
		/// <summary>
		///     Adopt the server's truth into the world's OPAQUE fields. Called on
		///     every ack, BEFORE the unacked inputs replay on top and AFTER the
		///     bound fields' auto-adopt — so it may derive from just-adopted
		///     mirrors.
		///
		///     Optional when bound fields cover the world; REQUIRED when nothing
		///     is bound, since there would be no restore point at all.
		/// </summary>
		public Action<W> Adopt;
	}

	/// <summary>
	///     SimReconciler — the COMPOSITE face of the same rollback engine that
	///     drives <see cref="Reconciler{S,I}" /> (port of
	///     predict/simReconciler.ts). Where the flat reconciler predicts ONE
	///     entity's scalar fields, this predicts a WORLD of parts and reads back
	///     a pose keyed "&lt;field&gt;.&lt;schemaField&gt;".
	///
	///     The engine — catch-up, reconcile, error rebase, snap, drift, memos,
	///     epoch follow — is inherited verbatim; only the state hooks differ.
	///     Notably <see cref="TruthMatchesAt" /> stays false: a composite sim has
	///     no wire-precision short-circuit and always adopts, which is why the
	///     reference expects a little float noise in the correction rather than
	///     an exact zero.
	///
	///     Bound fields register into <c>Predict.Value</c>, so the render layer
	///     reads them the same way it reads any other entity —
	///     <c>predict.Value(state.puck, "x")</c> — and the
	///     "field.schemaField" pose key stays an internal detail.
	///
	///     NOT ported (see PORTING.md): the custom `pose`/`interpolate` overlays
	///     that give OPAQUE parts render smoothing. Those have no decoded
	///     instance to key on, so <see cref="Value" /> remains the only way to
	///     read them.
	/// </summary>
	public class SimReconciler<W, I> : RollbackController where W : class
	{
		/// <summary>A bound field: its source, the mirror that replaced it, its poses.</summary>
		private class Bound
		{
			/// <summary>World field this entry came from — the prefix of its pose keys.</summary>
			public string Name;
			public Schema.Schema Source;
			public Schema.Schema Mirror;
			public List<string> Fields;
		}

		private readonly Action<StepContext, W, I> step;
		private readonly Action<W> adopt;
		private readonly List<Bound> bound = new List<Bound>();
		private readonly List<string> poseKeys = new List<string>();
		private readonly Dictionary<string, (Bound part, string field)> poseOf =
			new Dictionary<string, (Bound, string)>();

		/// <summary>
		///     The world the step mutates — the same object you passed in, with
		///     bound fields already replaced by their mirrors.
		/// </summary>
		public W World { get; }

		public SimReconciler(SimReconcilerOptions<W, I> opts) : base(opts)
		{
			step = opts.Step ?? throw new Exception("SimReconciler: step required");
			adopt = opts.Adopt;
			World = opts.World ?? throw new Exception("SimReconciler: world required");

			// Public instance fields only: a world handle is meant to be a small
			// plain class, and keeping the rule simple keeps "what gets bound"
			// obvious at the call site.
			foreach (var f in World.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				if (!(f.GetValue(World) is Schema.Schema source)) { continue; }   // opaque
				bound.Add(BindField(f, source));
			}

			// Without a bound field there is nothing to restore from, so an adopt
			// callback is the only possible restore point.
			if (bound.Count == 0 && adopt == null)
			{
				throw new Exception(
					"SimReconciler: no field of the world holds a decoded schema instance, " +
					"so `Adopt` is required — otherwise a replay has no state to roll back to.");
			}
		}

		private Bound BindField(FieldInfo field, Schema.Schema source)
		{
			// Same as the flat face: the predicted state is a same-type schema
			// mirror, so a step writes `world.paddle.vy = …` against a real
			// instance rather than a dictionary.
			var mirror = (Schema.Schema)Activator.CreateInstance(source.GetType());
			var b = new Bound { Name = field.Name, Source = source, Mirror = mirror, Fields = new List<string>() };

			foreach (var pair in source.fieldTypes)
			{
				if (!FieldKinds.IsScalarType(pair.Value)) { continue; }
				string name = pair.Key;
				var value = source[name];
				mirror[name] = value;
				b.Fields.Add(name);

				string key = field.Name + "." + name;
				poseOf[key] = (b, name);
				if (FieldKinds.IsNumericValue(value))
				{
					poseKeys.Add(key);
					prev[key] = Convert.ToDouble(value);
					error[key] = 0;
				}
			}

			if (b.Fields.Count == 0)
			{
				throw new Exception(
					$"SimReconciler: bound field '{field.Name}' has no scalar schema fields.");
			}

			// In place, exactly like the JS reference: the caller's own object now
			// points at the mirror, so `World.paddle` is what the step mutates.
			field.SetValue(World, mirror);
			return b;
		}

		private double ReadPose(string key)
		{
			if (!poseOf.TryGetValue(key, out var slot)) { return double.NaN; }
			var v = slot.part.Mirror[slot.field];
			return FieldKinds.IsNumericValue(v) ? Convert.ToDouble(v) : double.NaN;
		}

		/// <summary>
		///     Rendered pose for "&lt;field&gt;.&lt;schemaField&gt;": the predicted
		///     value interpolated between the two latest steps plus the decaying
		///     correction offset. NaN for an unknown key.
		/// </summary>
		public override double Value(string poseKey)
		{
			double current = ReadPose(poseKey);
			if (double.IsNaN(current)) { return double.NaN; }
			double smoothed = current + GetError(poseKey);
			double p = prev.TryGetValue(poseKey, out var pv) ? pv : smoothed;
			return p + (smoothed - p) * RenderAlpha();
		}

		/// <summary>Every pose key this world exposes — useful when one reads NaN.</summary>
		public IReadOnlyList<string> PoseKeys => poseKeys;

		/// <summary>
		///     Composite face: each bound field keeps its ORIGINAL decoded instance
		///     as the source (not the mirror that replaced it), so
		///     <c>Predict.Value(state.puck, "x")</c> resolves without the caller
		///     ever naming "puck.x".
		/// </summary>
		public override IReadOnlyList<BoundRegistration> BoundRegistrations
		{
			get
			{
				var outList = new List<BoundRegistration>();
				foreach (var b in bound)
				{
					var fields = new List<string>();
					var keys = new List<string>();
					foreach (var f in b.Fields)
					{
						string key = b.Name + "." + f;
						if (poseOf.ContainsKey(key) && FieldKinds.IsNumericValue(b.Mirror[f]))
						{
							fields.Add(f);
							keys.Add(key);
						}
					}
					if (fields.Count > 0)
					{
						outList.Add(new BoundRegistration {
							Source = b.Source, Fields = fields, PoseKeys = keys,
						});
					}
				}
				return outList;
			}
		}

		// --- RollbackController hooks -----------------------------------------

		protected override IReadOnlyList<string> SmoothedFields() => poseKeys;

		protected override double ReadCurrent(string field) => ReadPose(field);

		protected override void ApplyStep(object command) => step(stepCtx, World, (I)command);

		protected override void SnapshotPrev()
		{
			foreach (var key in poseKeys) { prev[key] = ReadPose(key) + GetError(key); }
		}

		/// <summary>
		///     Pull every mirror back from its source, then let the app restore the
		///     opaque fields. Bound fields are unconditional: unlike the flat face
		///     there is no per-field wire-precision comparison to skip on.
		/// </summary>
		protected override void AdoptTruth()
		{
			foreach (var b in bound)
			{
				foreach (var f in b.Fields) { b.Mirror[f] = b.Source[f]; }
			}
			adopt?.Invoke(World);
		}

		protected override void ReseedState()
		{
			AdoptTruth();
			foreach (var key in poseKeys)
			{
				prev[key] = ReadPose(key);
				error[key] = 0;
			}
		}
	}
}
