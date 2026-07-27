using System;
using System.Collections.Generic;

namespace Colyseus.Predict
{
	/// <summary>One member of the simulated world.</summary>
	public class SimPart
	{
		/// <summary>Pose-key prefix, e.g. "paddle" → "paddle.x". Must be unique.</summary>
		public string Name;
		/// <summary>
		///     Decoded truth instance. Null makes the part OPAQUE: the store
		///     never touches it and your <see cref="SimReconcilerOptions{I}.Adopt" />
		///     restores it from whatever authority you have.
		/// </summary>
		public Schema.Schema Source;
		/// <summary>
		///     App object for an opaque part; ignored when <see cref="Source" />
		///     is set (a bound part's live object is its mirror).
		/// </summary>
		public object Opaque;
		/// <summary>
		///     Fields to mirror and expose as poses. Null = every scalar field of
		///     the source schema. Ignored for opaque parts.
		/// </summary>
		public IReadOnlyList<string> Fields;
	}

	/// <summary>Options for <see cref="SimReconciler{I}" />.</summary>
	public class SimReconcilerOptions<I> : RollbackOptions
	{
		public IReadOnlyList<SimPart> Parts;
		/// <summary>
		///     Deterministic step over the WHOLE world, shared with the server.
		///     Fetch parts with <see cref="SimWorld.Part" /> and mutate in place.
		/// </summary>
		public Action<StepContext, SimWorld, I> Step;
		/// <summary>
		///     Restore the OPAQUE parts from authority before each replay. Bound
		///     parts are pulled from their sources first, unconditionally.
		///     Required when no part is bound — otherwise there is no restore
		///     point at all.
		/// </summary>
		public Action<SimWorld> Adopt;
	}

	/// <summary>
	///     The live world handed to the step: part name → its mirror (bound) or
	///     your own object (opaque).
	/// </summary>
	public class SimWorld
	{
		private readonly Dictionary<string, object> parts = new Dictionary<string, object>();

		internal void Register(string name, object live) => parts[name] = live;

		/// <summary>The live object for `name`, or null when unknown.</summary>
		public object Part(string name) => parts.TryGetValue(name, out var p) ? p : null;

		/// <summary>Typed convenience for a bound part's mirror.</summary>
		public T Part<T>(string name) where T : class => Part(name) as T;
	}

	/// <summary>
	///     SimReconciler — the COMPOSITE face of the same rollback engine that
	///     drives <see cref="Reconciler{S,I}" /> (port of
	///     predict/simReconciler.ts). Where the flat reconciler predicts ONE
	///     entity's scalar fields, this predicts a WORLD of parts and reads back
	///     a pose keyed "&lt;part&gt;.&lt;field&gt;".
	///
	///     The engine — catch-up, reconcile, error rebase, snap, drift, memos,
	///     epoch follow — is inherited verbatim; only the four state hooks differ.
	///     Notably <see cref="TruthMatchesAt" /> stays false: a composite sim has
	///     no wire-precision short-circuit and always adopts.
	///
	///     NOT ported (see PORTING.md): custom pose/interpolate overlays, and the
	///     boundRegistrations hook into Predict.Value — read poses through
	///     <see cref="Value" />.
	/// </summary>
	public class SimReconciler<I> : RollbackController
	{
		/// <summary>A bound part: its source, its mirror, and the fields it poses.</summary>
		private class Bound
		{
			public string Name;
			public Schema.Schema Source;
			public Schema.Schema Mirror;
			public List<string> Fields;
			public List<string> NumericFields;
		}

		private readonly Action<StepContext, SimWorld, I> step;
		private readonly Action<SimWorld> adopt;
		private readonly List<Bound> bound = new List<Bound>();
		private readonly List<string> poseKeys = new List<string>();
		private readonly Dictionary<string, (Bound part, string field)> poseOf =
			new Dictionary<string, (Bound, string)>();

		/// <summary>The world the step mutates. Also readable for game logic.</summary>
		public SimWorld World { get; } = new SimWorld();

		public SimReconciler(SimReconcilerOptions<I> opts) : base(opts)
		{
			step = opts.Step ?? throw new Exception("SimReconciler: step required");
			adopt = opts.Adopt;

			if (opts.Parts == null || opts.Parts.Count == 0)
			{
				throw new Exception("SimReconciler: at least one part is required.");
			}

			foreach (var part in opts.Parts)
			{
				if (string.IsNullOrEmpty(part.Name))
				{
					throw new Exception("SimReconciler: every part needs a name.");
				}
				if (part.Source == null)
				{
					// Opaque: the store never reads or writes it.
					World.Register(part.Name, part.Opaque);
					continue;
				}
				bound.Add(BindPart(part));
			}

			// Without a bound part there is nothing to restore from, so an adopt
			// callback is the only possible restore point.
			if (bound.Count == 0 && adopt == null)
			{
				throw new Exception(
					"SimReconciler: no part is bound to a source, so `adopt` is required — " +
					"otherwise a replay has no state to roll back to.");
			}
		}

		private Bound BindPart(SimPart part)
		{
			var declared = part.Fields;
			if (declared == null)
			{
				var derived = new List<string>();
				foreach (var pair in part.Source.fieldTypes)
				{
					if (IsScalarType(pair.Value)) { derived.Add(pair.Key); }
				}
				declared = derived;
			}
			if (declared.Count == 0)
			{
				throw new Exception($"SimReconciler: bound part '{part.Name}' has no scalar fields.");
			}

			// Same as the flat face: the predicted state is a same-type schema
			// mirror, so a step can write `paddle.vy = …` against a real instance.
			var mirror = (Schema.Schema)Activator.CreateInstance(part.Source.GetType());
			var b = new Bound
			{
				Name = part.Name,
				Source = part.Source,
				Mirror = mirror,
				Fields = new List<string>(declared),
				NumericFields = new List<string>(),
			};

			foreach (var f in b.Fields)
			{
				var value = part.Source[f];
				mirror[f] = value;
				string key = part.Name + "." + f;
				poseOf[key] = (b, f);
				if (IsNumeric(value))
				{
					b.NumericFields.Add(f);
					poseKeys.Add(key);
					prev[key] = Convert.ToDouble(value);
					error[key] = 0;
				}
			}

			World.Register(part.Name, mirror);
			return b;
		}

		private static bool IsScalarType(string fieldType)
		{
			switch (fieldType)
			{
				case "ref":
				case "array":
				case "map":
				case "string":
					return false;
				default:
					return true;
			}
		}

		private static bool IsNumeric(object value) =>
			value is float || value is double || value is byte || value is sbyte
			|| value is short || value is ushort || value is int || value is uint
			|| value is long || value is ulong;

		private double ReadPose(string key)
		{
			if (!poseOf.TryGetValue(key, out var slot)) { return double.NaN; }
			var v = slot.part.Mirror[slot.field];
			return IsNumeric(v) ? Convert.ToDouble(v) : double.NaN;
		}

		/// <summary>
		///     Rendered pose for "&lt;part&gt;.&lt;field&gt;": the predicted value
		///     interpolated between the two latest steps plus the decaying
		///     correction offset. NaN for an unknown key.
		/// </summary>
		public double Value(string poseKey)
		{
			double current = ReadPose(poseKey);
			if (double.IsNaN(current)) { return double.NaN; }
			double smoothed = current + GetError(poseKey);
			double p = prev.TryGetValue(poseKey, out var pv) ? pv : smoothed;
			return p + (smoothed - p) * RenderAlpha();
		}

		// --- RollbackController hooks -----------------------------------------

		protected override IReadOnlyList<string> SmoothedFields() => poseKeys;

		protected override double ReadCurrent(string field) => ReadPose(field);

		protected override void ApplyStep(object command)
		{
			step(stepCtx, World, (I)command);
		}

		protected override void SnapshotPrev()
		{
			foreach (var key in poseKeys) { prev[key] = ReadPose(key) + GetError(key); }
		}

		/// <summary>
		///     Pull every mirror back from its source, then let the app restore
		///     the opaque parts. Bound parts are unconditional: unlike the flat
		///     face there is no per-field wire-precision comparison to skip on.
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
