using System;
using System.Collections.Generic;

namespace Colyseus.Predict
{
	/// <summary>Options for <see cref="Reconciler{S,I}" />.</summary>
	public class ReconcilerOptions<S, I> : RollbackOptions
	{
		/// <summary>
		///     Deterministic input-application step, SHARED with the server.
		///     Mutates <c>state</c> in place by applying <c>command</c> over
		///     <c>ctx.Dt</c>.
		/// </summary>
		public Action<StepContext, S, I> Step;
		/// <summary>
		///     Fields to mirror from the server on reconcile. Null = every scalar
		///     field of the schema (numbers, booleans, strings; declaration order).
		///     Numeric fields get smooth error correction; the rest copy verbatim.
		/// </summary>
		public IReadOnlyList<string> Fields;
	}

	/// <summary>
	///     Server-reconciled rollback for a locally-controlled entity whose
	///     truth is a flat scalar field list on ONE schema instance (port of
	///     predict/reconciler.ts). The predicted state is a plain
	///     dictionary-backed mirror exposed via <see cref="State" />; numeric
	///     fields get smooth error correction, booleans copy verbatim.
	/// </summary>
	public class Reconciler<S, I> : RollbackController where S : Schema.Schema
	{
		private readonly Dictionary<string, object> local = new Dictionary<string, object>();
		private readonly List<string> fields = new List<string>();
		private readonly List<string> numericFields = new List<string>();
		private readonly Action<StepContext, S, I> step;
		private readonly S instance;
		private readonly S mirror;

		// wire-precision history ring
		private readonly Func<double, double>[] wireRound;
		private readonly double[] history;
		private readonly double[] historySeq;
		private readonly int historySize;
		private readonly bool historyOn;

		public Reconciler(S instance, ReconcilerOptions<S, I> opts) : base(opts)
		{
			this.instance = instance;
			step = opts.Step ?? throw new Exception("Reconciler: step required");

			var declared = opts.Fields;
			if (declared == null)
			{
				var derived = new List<string>();
				foreach (var pair in instance.fieldTypes)
				{
					if (FieldKinds.IsScalarType(pair.Value)) { derived.Add(pair.Key); }
				}
				if (derived.Count == 0)
				{
					throw new Exception("Reconciler: no fields given and none derivable from the schema.");
				}
				declared = derived;
			}

			// the predicted state is a same-type schema MIRROR: step mutates a
			// real typed instance (state.vy = …)
			mirror = (S)Activator.CreateInstance(instance.GetType());

			bool scalarOnly = declared.Count > 0;
			wireRound = new Func<double, double>[declared.Count];
			int i = 0;
			foreach (var f in declared)
			{
				fields.Add(f);
				var value = instance[f];
				mirror[f] = value;
				if (FieldKinds.IsNumericValue(value))
				{
					numericFields.Add(f);
					prev[f] = Convert.ToDouble(value);
					error[f] = 0;
				}
				else if (!(value is bool))
				{
					scalarOnly = false;
				}
				wireRound[i++] = WireQuantizerOf(instance, f);
			}
			historyOn = scalarOnly;
			historySize = input.ReplayBufferSize > 0 ? input.ReplayBufferSize : 64;
			history = new double[historyOn ? historySize * fields.Count : 0];
			historySeq = new double[historyOn ? historySize : 0];
			for (int s = 0; s < historySeq.Length; s++) { historySeq[s] = -1; }
		}

		private static double AsScalar(object value)
		{
			if (value is bool b) { return b ? 1 : 0; }
			if (FieldKinds.IsNumericValue(value)) { return Convert.ToDouble(value); }
			return double.NaN;
		}

		/// <summary>
		///     Mirror of the codec's dynamic number wire rule — what value
		///     would the wire deliver for this float64?
		/// </summary>
		internal static double QuantizeAutoNumber(double v)
		{
			if (double.IsNaN(v)) { return 0; }
			if (double.IsInfinity(v)) { return v > 0 ? 9007199254740991.0 : -9007199254740991.0; }
			bool isInt32 = v == Math.Floor(v) && v >= -2147483648.0 && v <= 2147483647.0;
			if (!isInt32 && Math.Abs(v) <= 3.4028235e+38)
			{
				double f = (float)v;
				if (Math.Abs(Math.Abs(f) - Math.Abs(v)) < 1e-4) { return f; }
			}
			return v;
		}

		private static Func<double, double> WireQuantizerOf(S instance, string field)
		{
			instance.fieldTypes.TryGetValue(field, out var declaredType);
			switch (declaredType)
			{
				case "float32": return v => (float)v;
				case "number": return QuantizeAutoNumber;
				default: return v => v;
			}
		}

		/// <summary>
		///     The TRUE predicted state — a same-type schema mirror. Read for
		///     game logic; mutations must be replay-reproducible.
		/// </summary>
		public S State => mirror;

		/// <summary>
		///     Rendered value: the predicted state interpolated between the two
		///     latest steps plus the decaying correction offset. Numeric fields
		///     only; others return the current value.
		/// </summary>
		public override double Value(string field)
		{
			var current = mirror[field];
			if (!FieldKinds.IsNumericValue(current)) { return AsScalar(current); }
			double c = Convert.ToDouble(current);
			double smoothed = c + GetError(field);
			double p = prev.TryGetValue(field, out var pv) ? pv : smoothed;
			return p + (smoothed - p) * RenderAlpha();
		}

		/// <summary>
		///     Flat face: the pose key IS the field name, so the two lists match —
		///     kept separate to share one overlay path with the composite face.
		/// </summary>
		public override IReadOnlyList<BoundRegistration> BoundRegistrations =>
			numericFields.Count == 0
				? Array.Empty<BoundRegistration>()
				: new[] { new BoundRegistration {
					Source = instance, Fields = numericFields, PoseKeys = numericFields,
				} };

		// --- RollbackController hooks -----------------------------------------

		protected override IReadOnlyList<string> SmoothedFields() => numericFields;
		protected override double ReadCurrent(string field) => Convert.ToDouble(mirror[field]);

		protected override void ApplyStep(object command)
		{
			step(stepCtx, mirror, (I)command);
			if (historyOn)
			{
				int slot = stepCtx.Tick % historySize;
				int baseIdx = slot * fields.Count;
				for (int i = 0; i < fields.Count; i++)
				{
					history[baseIdx + i] = AsScalar(mirror[fields[i]]);
				}
				historySeq[slot] = stepCtx.Tick;
			}
		}

		protected override bool TruthMatchesAt(int acked)
		{
			if (!historyOn) { return false; }
			int slot = acked % historySize;
			if (historySeq[slot] != acked) { return false; }
			int baseIdx = slot * fields.Count;
			for (int i = 0; i < fields.Count; i++)
			{
				if (wireRound[i](history[baseIdx + i]) != AsScalar(instance[fields[i]])) { return false; }
			}
			return true;
		}

		protected override void SnapshotPrev()
		{
			foreach (var f in numericFields) { prev[f] = Convert.ToDouble(mirror[f]) + GetError(f); }
		}

		protected override void AdoptTruth()
		{
			foreach (var f in fields) { mirror[f] = instance[f]; }
		}

		protected override void ReseedState()
		{
			foreach (var f in fields) { mirror[f] = instance[f]; }
			foreach (var f in numericFields)
			{
				prev[f] = Convert.ToDouble(mirror[f]);
				error[f] = 0;
			}
			for (int s = 0; s < historySeq.Length; s++) { historySeq[s] = -1; }
		}
	}
}
