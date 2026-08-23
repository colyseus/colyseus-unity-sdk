using System;
using System.Collections.Generic;

namespace Colyseus.Predict
{
	/// <summary>
	///     Per-step context handed to a reconciler's step (port of
	///     predict/rollback.ts StepContext). One fixed dt drives both sides of
	///     the rollback. The instance is REUSED across steps.
	/// </summary>
	public class StepContext
	{
		/// <summary>Fixed step in SECONDS (1/tickRate).</summary>
		public double Dt;
		/// <summary>Fixed step in MILLISECONDS.</summary>
		public double DtMs;
		/// <summary>The input's seq — the index of the step being simulated.</summary>
		public int Tick;
		/// <summary>Physics sub-steps per fixed step (≥ 1).</summary>
		public int SubSteps = 1;
		public double SubDt;
		public double SubDtMs;
		/// <summary>
		///     True while re-simulating an already-applied input during
		///     rollback — one-shot presentation must branch on this.
		/// </summary>
		public bool IsReplay;
		/// <summary>
		///     The input's reckon instant (server-clock ms): the per-seq
		///     lag-comp stamp when present (replay-deterministic), else the
		///     live serverNow().
		/// </summary>
		public double ReckonTime;
		public bool LagCompActive;

		internal RollbackController Owner;

		/// <summary>
		///     Memoize a VALUE on the rollback timeline that replay can't
		///     re-derive: computed ONCE on the live step for this seq, frozen,
		///     and returned WITHOUT re-running on every replay. Returns default
		///     when the live step memoized nothing. Keyed form disambiguates
		///     multiple memos per step ("" = the shared key-less slot).
		/// </summary>
		public T Memo<T>(Func<T> compute) => Owner.MemoRun("", IsReplay, Tick, compute);
		public T Memo<T>(string key, Func<T> compute) => Owner.MemoRun(key, IsReplay, Tick, compute);

		/// <summary>
		///     Declare an optimistic discrete EVENT the timeline just produced
		///     into an event channel — fires only on the LIVE step (silently
		///     skipped on every rollback replay).
		/// </summary>
		public void Predict<T>(IPredictSink<T> sink, T payload)
		{
			if (!IsReplay) { sink.PredictFromSim(Tick, payload, Owner.AckWatermark); }
		}
	}

	/// <summary>
	///     Minimal shape <see cref="StepContext.Predict" /> emits into —
	///     implemented by <see cref="PredictedEventChannel{T}" />.
	/// </summary>
	public interface IPredictSink<T>
	{
		void PredictFromSim(int seq, T payload, Func<int> acked);
	}

	/// <summary>Options shared by every rollback controller.</summary>
	public class RollbackOptions
	{
		public InputHandle Input;
		/// <summary>Resolves ReckonTime's unstamped fallback (serverNow).</summary>
		public RoomClock Clock;
		/// <summary>Error-decay time constant (ms); 0 = hard snap; null = the
		///     server's correction cadence (one patch interval) else 50.</summary>
		public double? SmoothMs;
		/// <summary>Teleport threshold — corrections beyond it POP. 0 = off.</summary>
		public double Snap;
		public double? StepMs;
		public double? StepSeconds;
		public int? SubSteps;
		public Action<int> OnReconcile;
		/// <summary>Divergence-warning tolerance; null = off.</summary>
		public double? WarnOnDivergence;
	}

	/// <summary>
	///     Shared rollback engine (port of predict/rollback.ts) — a pure
	///     OBSERVER of an input handle: you mutate + send through the handle;
	///     the controller steps each send eagerly, polls the server ack in
	///     <see cref="Tick" /> and rewinds-to-truth + replays on new acks,
	///     absorbing mispredictions into per-field visual offsets that decay.
	/// </summary>
	/// <summary>
	///     One controller-owned instance and the pose keys its numeric fields map
	///     to. <c>Fields[i]</c> is read back as <c>PoseKeys[i]</c> — the two differ
	///     only on the composite face, where a key is
	///     "&lt;worldField&gt;.&lt;schemaField&gt;".
	/// </summary>
	public class BoundRegistration
	{
		public Schema.Schema Source;
		public IReadOnlyList<string> Fields;
		public IReadOnlyList<string> PoseKeys;
	}

	public abstract class RollbackController : IDisposable
	{
		/// <summary>Per-numeric-field correction injected by the most recent reconcile.</summary>
		public readonly Dictionary<string, double> LastCorrection = new Dictionary<string, double>();
		/// <summary>Max |LastCorrection| across fields.</summary>
		public double LastCorrectionMag;
		/// <summary>Increments once per reconcile.</summary>
		public int ReconcileSeq;
		/// <summary>Rolling reconcile drift.</summary>
		public readonly Drift Drift = new Drift();

		protected readonly Dictionary<string, double> error = new Dictionary<string, double>();
		protected readonly Dictionary<string, double> prev = new Dictionary<string, double>();
		private readonly Dictionary<string, double> renderedBefore = new Dictionary<string, double>();

		private double lastTick = -1;
		protected int lastAcked;
		private int lastEpoch;
		protected int replayFrom;
		protected int predictedSeq;
		private bool catching;
		private Action unsubscribeSend = () => { };
		private double renderAcc;

		protected readonly StepContext stepCtx;
		internal readonly Func<int> AckWatermark;
		// per-seq memo store: seq → (key → value)
		private readonly Dictionary<int, Dictionary<string, object>> memos = new Dictionary<int, Dictionary<string, object>>();

		protected readonly double smoothMs;
		private readonly double snapThreshold;
		/// <summary>The fixed simulation step (ms) this controller predicts at.</summary>
		public readonly double StepMs;
		protected readonly InputHandle input;
		private readonly Action<int> onReconcile;
		internal readonly double? WarnTolerance;
		private readonly RoomClock clock;

		/// <summary>
		///     Is ANY controller re-simulating already-applied inputs right now?
		///     Lets code reached indirectly from a step — an event channel's
		///     <c>Predict()</c> — refuse to fire a second time on the way through a
		///     rollback. <see cref="StepContext.IsReplay" /> is the same fact for
		///     code that holds the ctx; this is for code that doesn't.
		/// </summary>
		public static bool IsReplaying() => replayDepth > 0;
		// depth, not bool: a step that ticks another controller nests a replay
		internal static int replayDepth;

		/// <summary>Set by <see cref="Dispose" />.</summary>
		public bool Dead { get; private set; }
		/// <summary>Number of unacknowledged inputs currently buffered.</summary>
		public int PendingCount => input.PendingCount;

		private readonly List<Action> disposedHooks = new List<Action>();

		protected RollbackController(RollbackOptions opts)
		{
			input = opts.Input ?? throw new Exception("RollbackController: input handle required");
			clock = opts.Clock;
			smoothMs = opts.SmoothMs
				?? (input.PatchRate.HasValue && input.PatchRate.Value > 0 ? input.PatchRate.Value : 50);
			snapThreshold = opts.Snap;
			double? stepMs = opts.StepMs ?? input.StepMs ?? (opts.StepSeconds * 1000);
			StepMs = stepMs ?? throw new Exception(
				"reconciler: fixed simulation step is unknown. The server room must call " +
				"setFixedTimestep() (or setTimestep()), or pass StepMs/StepSeconds explicitly — " +
				"a wrong dt silently diverges rollback-replay.");
			double dt = opts.StepSeconds ?? input.StepSeconds ?? StepMs / 1000;
			int subSteps = opts.SubSteps ?? input.SubSteps;
			if (subSteps < 1) { subSteps = 1; }
			stepCtx = new StepContext
			{
				Dt = dt,
				DtMs = StepMs,
				SubSteps = subSteps,
				SubDt = dt / subSteps,
				SubDtMs = StepMs / subSteps,
				Owner = this,
			};
			onReconcile = opts.OnReconcile;
			WarnTolerance = opts.WarnOnDivergence;
			AckWatermark = () => input.LastProcessed;
			lastAcked = input.LastProcessed;
			lastEpoch = input.Epoch;
			predictedSeq = input.SentCount;
			unsubscribeSend = input.OnSend(_ => CatchUp());
		}

		internal T MemoRun<T>(string key, bool isReplay, int tick, Func<T> compute)
		{
			if (isReplay)
			{
				return memos.TryGetValue(tick, out var slot) && slot.TryGetValue(key, out var stored)
					? (T)stored
					: default;
			}
			T value = compute();
			if (value != null)
			{
				if (!memos.TryGetValue(tick, out var slot))
				{
					slot = new Dictionary<string, object>();
					memos[tick] = slot;
				}
				slot[key] = value;
			}
			return value;
		}

		/// <summary>
		///     Advance one render frame: follow the handle's epoch (self-reset),
		///     poll the server ack (reconcile), decay the correction offsets.
		///     Live inputs are stepped eagerly via the handle's OnSend hook.
		/// </summary>
		public void Tick(double now)
		{
			double dt = lastTick < 0 ? 0 : now - lastTick;
			lastTick = now;
			if (dt > 0 && StepMs > 0) { renderAcc += dt; }

			int epoch = input.Epoch;
			if (epoch != lastEpoch)
			{
				lastEpoch = epoch;
				Reset();
			}

			int acked = input.LastProcessed;
			if (acked > lastAcked)
			{
				lastAcked = acked;
				Reconcile(acked);
			}
			MarkDirty();

			if (dt <= 0) { return; }
			double k = smoothMs <= 0 ? 1 : 1 - Math.Exp(-dt / smoothMs);
			foreach (var f in SmoothedFields()) { error[f] -= error[f] * k; }
		}

		protected double RenderAlpha()
		{
			if (StepMs <= 0) { return 1; }
			double a = renderAcc / StepMs;
			return a < 0 ? 0 : a > 1 ? 1 : a;
		}

		private void CatchUp()
		{
			int sent = input.SentCount;
			if (predictedSeq >= sent || catching) { return; }
			catching = true;
			stepCtx.IsReplay = false;
			for (int seq = predictedSeq + 1; seq <= sent; seq++)
			{
				var inp = input.At(seq);
				if (inp != null)
				{
					SnapshotPrev();
					RunStep(seq, inp);
					RefreshRender();
					renderAcc -= StepMs;
					if (renderAcc < 0) { renderAcc = 0; }
					else if (renderAcc >= StepMs) { renderAcc %= StepMs; }
				}
				predictedSeq = seq;
			}
			catching = false;
		}

		private void RunStep(int seq, object command)
		{
			stepCtx.Tick = seq;
			double raw = input.ReckonTimeAt(seq);
			stepCtx.LagCompActive = raw > 0;
			stepCtx.ReckonTime = raw > 0 ? raw : clock?.ServerNow() ?? 0;
			ApplyStep(command);
		}

		private void Reconcile(int acked)
		{
			if (TruthMatchesAt(acked))
			{
				foreach (var f in SmoothedFields()) { LastCorrection[f] = 0; }
				LastCorrectionMag = 0;
				Drift.Update(0);
				ReconcileSeq++;
				MemoPrune(acked);
				onReconcile?.Invoke(acked);
				return;
			}

			foreach (var f in SmoothedFields()) { renderedBefore[f] = ReadCurrent(f) + GetError(f); }

			AdoptTruth();

			int from = Math.Max(acked, replayFrom);
			stepCtx.IsReplay = true;
			catching = true;
			replayDepth++;
			try
			{
				for (int seq = from + 1; seq <= input.SentCount; seq++)
				{
					var inp = input.At(seq);
					if (inp != null) { RunStep(seq, inp); }
				}
			}
			finally
			{
				replayDepth--;
				catching = false;
			}
			RefreshRender();
			predictedSeq = input.SentCount;

			bool hard = smoothMs <= 0;
			double mag = 0;
			foreach (var f in SmoothedFields())
			{
				double correction = renderedBefore[f] - ReadCurrent(f);
				error[f] = hard ? 0 : correction;
				double a = correction < 0 ? -correction : correction;
				if (a > mag) { mag = a; }
				LastCorrection[f] = correction;
			}

			bool popped = snapThreshold > 0 && mag > snapThreshold;
			if (popped)
			{
				foreach (var f in SmoothedFields())
				{
					error[f] = 0;
					prev[f] = ReadCurrent(f);
				}
			}
			ReconcileSeq++;
			LastCorrectionMag = mag;
			if (!popped) { Drift.Update(mag); }

			MemoPrune(acked);
			onReconcile?.Invoke(acked);
		}

		private void MemoPrune(int acked)
		{
			var stale = new List<int>();
			foreach (var seq in memos.Keys) { if (seq <= acked) { stale.Add(seq); } }
			foreach (var seq in stale) { memos.Remove(seq); }
		}

		protected double GetError(string field) => error.TryGetValue(field, out var e) ? e : 0;

		/// <summary>
		///     Re-seed local state from the authoritative instance(s): clears
		///     the offsets, memos, and the in-flight window. The controller
		///     self-resets on the handle's epoch (reconnect).
		/// </summary>
		public void Reset()
		{
			ReseedState();
			Drift.Reset();
			replayFrom = input.SentCount;
			predictedSeq = input.SentCount;
			lastAcked = input.LastProcessed;
			renderAcc = 0;
			memos.Clear();
			MarkDirty();
		}

		/// <summary>
		///     Rendered pose for one key. Overridden by both faces —
		///     <see cref="Reconciler{S,I}" /> keys by schema field,
		///     <see cref="SimReconciler{W,I}" /> by "&lt;worldField&gt;.&lt;schemaField&gt;".
		///     Declared here so the bound overlay can read a pose without knowing
		///     which face it holds.
		/// </summary>
		public virtual double Value(string key) => double.NaN;

		/// <summary>
		///     What <c>Predict.Value(instance, field)</c> needs to reach this
		///     controller's poses: the ORIGINAL decoded instance per bound entry
		///     (not the mirror), its numeric fields, and the pose key each maps to.
		///     Empty when nothing is bound.
		/// </summary>
		public virtual IReadOnlyList<BoundRegistration> BoundRegistrations =>
			Array.Empty<BoundRegistration>();

		public void OnDisposed(Action hook) => disposedHooks.Add(hook);

		public void Dispose()
		{
			Dead = true;
			unsubscribeSend();
			var hooks = disposedHooks.ToArray();
			disposedHooks.Clear();
			foreach (var hook in hooks) { hook(); }
		}

		// --- Subclass hooks ---------------------------------------------------

		protected abstract IReadOnlyList<string> SmoothedFields();
		protected abstract double ReadCurrent(string field);
		protected abstract void AdoptTruth();
		protected abstract void ApplyStep(object command);
		protected abstract void SnapshotPrev();
		protected abstract void ReseedState();
		protected virtual bool TruthMatchesAt(int acked) => false;
		protected virtual void RefreshRender() { }
		protected virtual void MarkDirty() { }
	}
}
