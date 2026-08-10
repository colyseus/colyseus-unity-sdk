using System;
using System.Collections.Generic;
using Colyseus.Schema;

namespace Colyseus.Predict
{
	public enum PredictMode
	{
		Lerp,
		Extrapolate,
		Damped,
		Reckon,
		Raw,
	}

	/// <summary>Per-field smoothing options (defaults mirror the JS reference).</summary>
	public class PredictFieldOptions
	{
		public PredictMode Mode = PredictMode.Lerp;
		/// <summary>Lerp render-time lag (ms).</summary>
		public double Delay = 100;
		/// <summary>Damped/extrapolate spring (1/s). 0 on extrapolate = raw projection.</summary>
		public double Damping = 15;
		/// <summary>Extrapolate overshoot cap (ms).</summary>
		public double MaxExtrapolate = 200;
		/// <summary>Arrival-grid snap (ms); 0 off.</summary>
		public double TickInterval;
		/// <summary>Value-space teleport threshold; 0 off.</summary>
		public double Snap;
		/// <summary>Radian angle — unwrap samples over the shortest arc.</summary>
		public bool Angle;
	}

	/// <summary>
	///     Per-field smoothing config: field name -> a <see cref="PredictMode" />
	///     or a full <see cref="PredictFieldOptions" />. The reference spells this
	///     as a TypeScript union; here the two arms are separate
	///     <c>Attach</c>/<c>AttachAll</c> overloads, which keeps the reckon step
	///     signature type-checked.
	/// </summary>
	public class AttachConfig : Dictionary<string, object> { }

	/// <summary>Options for a reckon attach.</summary>
	public class ReckonOptions<T>
	{
		public IReadOnlyList<string> Fields;
		/// <summary>The pure step function, SHARED with the server: mutate the
		///     scratch in place by dt seconds; elapsedMs is the absolute
		///     server-time at the end of the substep.</summary>
		public Action<T, double, double> Step;
		/// <summary>Offset-decay smoothing (1/s). 0 = raw projection.</summary>
		public double Smoothing = 20;
		/// <summary>Substep length (ms).</summary>
		public double Substep = 16;
		/// <summary>Rebase discontinuities beyond this pop. 0 off.</summary>
		public double Snap;
	}

	/// <summary>
	///     Predict — passive smoothing of the server stream for entities you
	///     DON'T control (port of predict/Predictor.ts, passive engine +
	///     orchestration). One read idiom: <see cref="Value" /> — with raw
	///     instance fallback when untracked. Reconciler/sim factories inject
	///     the room clock and adopt the fixed step for <see cref="Tick" />'s
	///     send budget.
	/// </summary>
	public class Predict : IDisposable
	{
		private const int RingCap = 16;
		private const double GapResumeMult = 3;
		private const double GapResumePatchMult = 1.5;
		private const double GapResumeMaxMs = 250;
		private const int MaxStepsPerFrame = 5;

		private class Slot
		{
			public string Field;
			public Schema.Schema Instance;
			public PredictFieldOptions Opts;
			public double V1;
			public double AuxV;
			public double AuxT;
			public readonly double[] RingT = new double[RingCap];
			public readonly double[] RingV = new double[RingCap];
			public int RingHead;
			public int RingCount;
			public Action Detach;
			/// <summary>Non-null only on a controller-bound slot: its owner and pose key.</summary>
			public RollbackController Ctrl;
			public string PoseKey;
			/// <summary>The passive slot this binding displaced; restored on dispose.</summary>
			public Slot Stash;
		}

		private class SimState
		{
			public Schema.Schema Instance;
			public Schema.Schema Scratch;
			public IReadOnlyList<string> Fields;
			public Action<Schema.Schema, double, double> Step;
			public double Smoothing;
			public double Substep;
			public double Snap;
			public double[] Smoothed;
			public double[] Out;
			public double[] ValueOut;
			public double[] Offset;
			public double[] OutPrev;
			public double[] FrameVel;
			public double LastBaseT = double.NaN;
			public double LastApplyTime = double.NegativeInfinity;
			public IReadOnlyList<string> CopyFields;
			/// <summary>Per-entity forward horizon override (spawns lead). Null = snapshot age.</summary>
			public Func<double> ForwardMs;
		}

		private readonly IPredictCallbacks callbacks;
		private readonly RoomClock clock;
		private double renderTime;
		// refId → field → slot
		private readonly Dictionary<int, Dictionary<string, Slot>> slotsByRef = new Dictionary<int, Dictionary<string, Slot>>();
		private readonly Dictionary<int, SimState> simsByRef = new Dictionary<int, SimState>();
		private readonly List<object> driven = new List<object>();
		// Every AttachAll* off, so Dispose can unhook what the caller never held.
		private readonly List<Action> attachments = new List<Action>();

		// room-wide fixed-step accumulator (send budget)
		private double? fixedStepMs;
		private double stepAcc;
		private double lastFrameNow = -1;

		/// <summary>
		///     Create over a room's callbacks + clock. Mirrors
		///     <c>Predict.get(room)</c> — pass <c>CallbacksRef</c> from
		///     <c>Callbacks.Get(room)</c> and the room's <see cref="RoomClock" />.
		/// </summary>
		public Predict(IPredictCallbacks callbacks, RoomClock clock)
		{
			this.callbacks = callbacks;
			this.clock = clock;
		}

		/// <summary>
		///     The one-liner every caller wants: a <see cref="Predict" /> over a
		///     room's callbacks and clock. Equivalent to
		///     <c>new Predict(new PredictCallbacks&lt;T&gt;(Callbacks.Get(room)), room.Clock)</c>,
		///     which is the same two collaborators every time and no decision the
		///     caller is better placed to make.
		/// </summary>
		public static Predict For<TState>(Room<TState> room) where TState : Schema.Schema
			=> new Predict(new PredictCallbacks<TState>(Schema.Callbacks.Get(room)), room.Clock);

		private static int? RefIdOf(Schema.Schema instance) => instance?.__refId;

		// --- Attach -----------------------------------------------------------

		/// <summary>
		///     Track one numeric field for smoothing. Internal primitive under
		///     <see cref="Attach(Schema.Schema, AttachConfig)" /> — PORTING.md
		///     strips track/untrack/trackStepped from the published surface.
		/// </summary>
		private Action Track(Schema.Schema instance, string field, PredictFieldOptions options = null)
		{
			var opts = options ?? new PredictFieldOptions();
			int refId = instance.__refId;
			if (!slotsByRef.TryGetValue(refId, out var perRef))
			{
				perRef = new Dictionary<string, Slot>();
				slotsByRef[refId] = perRef;
			}
			// idempotent per field
			if (perRef.TryGetValue(field, out var existing))
			{
				existing.Detach?.Invoke();
				perRef.Remove(field);
			}

			var slot = new Slot { Field = field, Instance = instance, Opts = opts };
			double initial = ToNumber(instance[field]);
			slot.V1 = initial;
			slot.AuxV = initial;
			slot.AuxT = 0;
			perRef[field] = slot;

			slot.Detach = callbacks.Listen(instance, field, (object current) => OnSample(slot, ToNumber(current)), true);
			return () => Untrack(instance, field);
		}

		/// <summary>
		///     Dead-reckon fields of an instance with a step SHARED with the
		///     server. Internal primitive under
		///     <see cref="Attach{T}(T, ReckonOptions{T})" />; named for the
		///     reference's <c>trackStepped</c>.
		/// </summary>
		private Action TrackStepped<T>(T instance, ReckonOptions<T> options) where T : Schema.Schema
		{
			int refId = instance.__refId;
			var scratch = (Schema.Schema)Activator.CreateInstance(instance.GetType());
			var copyFields = new List<string>();
			foreach (var pair in instance.fieldTypes)
			{
				switch (pair.Value)
				{
					// Every primitive, strings included: the scratch is documented as a
					// FULL copy of the entity so the shared step can read descriptors it
					// never attached (a bot's `kind`). Dropping strings makes step
					// functions that branch on one silently take the default branch — and
					// an attach-all reckon gives the caller no live instance to fall back on.
					case "ref":
					case "array":
					case "map":
						break;
					default:
						copyFields.Add(pair.Key);
						break;
				}
			}
			int n = options.Fields.Count;
			var sim = new SimState
			{
				Instance = instance,
				Scratch = scratch,
				Fields = options.Fields,
				Step = (s, dt, elapsed) => options.Step((T)s, dt, elapsed),
				Smoothing = options.Smoothing,
				Substep = options.Substep > 0 ? options.Substep : 16,
				Snap = options.Snap,
				Smoothed = new double[n],
				Out = new double[n],
				ValueOut = new double[n],
				Offset = new double[n],
				OutPrev = new double[n],
				FrameVel = new double[n],
				CopyFields = copyFields,
			};
			for (int k = 0; k < n; k++) { sim.Smoothed[k] = ToNumber(instance[options.Fields[k]]); }
			simsByRef[refId] = sim;

			// each reckoned field gets a RECKON slot (sample mirror + fallback)
			var offs = new List<Action>();
			foreach (var f in options.Fields)
			{
				offs.Add(Track(instance, f, new PredictFieldOptions { Mode = PredictMode.Reckon }));
			}
			return () =>
			{
				foreach (var off in offs) { off(); }
				simsByRef.Remove(refId);
			};
		}

		/// <summary>Per-instance forward-horizon override (the spawns store's lead reckon).</summary>
		internal void BindForward(Schema.Schema instance, Func<double> forwardMs)
		{
			if (simsByRef.TryGetValue(instance.__refId, out var sim)) { sim.ForwardMs = forwardMs; }
		}

		public void Untrack(Schema.Schema instance, string field)
		{
			int refId = instance.__refId;
			if (!slotsByRef.TryGetValue(refId, out var perRef)) { return; }
			if (perRef.TryGetValue(field, out var slot))
			{
				slot.Detach?.Invoke();
				perRef.Remove(field);
				if (perRef.Count == 0) { slotsByRef.Remove(refId); }
			}
		}

		/// <summary>Stop tracking every field (and any reckon sim) of an instance.</summary>
		public void Detach(Schema.Schema instance)
		{
			int refId = instance.__refId;
			if (slotsByRef.TryGetValue(refId, out var perRef))
			{
				foreach (var slot in perRef.Values) { slot.Detach?.Invoke(); }
				slotsByRef.Remove(refId);
			}
			simsByRef.Remove(refId);
		}

		/// <summary>
		///     Release everything this Predict registered: tracked fields, reckon
		///     sims, AttachAll wiring, and every driven child.
		///
		///     This matters more than it looks. Callbacks live on the ROOM, so a
		///     Predict that outlives its owner keeps firing handlers into freed
		///     state — attach/detach are not symmetric unless someone closes the
		///     loop, and the AttachAll offs are held here, not by the caller.
		///     Dispose is that loop; call it when the screen using this goes away.
		/// </summary>
		public void Dispose()
		{
			foreach (var off in attachments) { off?.Invoke(); }
			attachments.Clear();

			foreach (var perRef in slotsByRef.Values)
			{
				foreach (var slot in perRef.Values) { slot.Detach?.Invoke(); }
			}
			slotsByRef.Clear();
			simsByRef.Clear();

			foreach (var child in driven) { (child as IDisposable)?.Dispose(); }
			driven.Clear();
			fixedStepMs = null;
		}

		/// <summary>
		///     Attach prediction to every child of a root-level collection:
		///     wires onAdd → track(fields) and onRemove → detach.
		/// </summary>
		public Action AttachAll(string collection, AttachConfig config)
			=> AttachEach(collection, child => Attach(child, config));

		/// <summary>
		///     Attach prediction to every child of a collection with dead
		///     reckoning — the same call, not a separate flavour, matching the
		///     reference's single <c>attachAll(key, config)</c> over a config union.
		/// </summary>
		public Action AttachAll<T>(string collection, ReckonOptions<T> options) where T : Schema.Schema
			=> AttachEach(collection, child => Attach((T)child, options));

		/// <summary>
		///     Attach prediction to ONE instance from a declarative config — the
		///     per-field smoothing map:
		///     <code>
		///     predict.Attach(boss, new AttachConfig {
		///         ["x"] = PredictMode.Lerp,
		///         ["yaw"] = new PredictFieldOptions { Mode = PredictMode.Damped, Angle = true },
		///     });
		///     </code>
		///     Fields the instance's schema doesn't declare are DROPPED, not an
		///     error: one config can cover a heterogeneous collection, and a field
		///     that isn't there would subscribe to nothing.
		/// </summary>
		public Action Attach(Schema.Schema instance, AttachConfig config)
		{
			var offs = new List<Action>();
			foreach (var pair in config)
			{
				if (!instance.fieldTypes.ContainsKey(pair.Key)) { continue; }
				var opts = pair.Value as PredictFieldOptions
					?? new PredictFieldOptions { Mode = (PredictMode)pair.Value };
				offs.Add(Track(instance, pair.Key, opts));
			}
			return () => { foreach (var off in offs) { off(); } };
		}

		/// <summary>
		///     Attach dead reckoning to ONE instance — the reckon arm of the
		///     reference's config union, expressed here as an overload so the step
		///     signature stays type-checked.
		/// </summary>
		public Action Attach<T>(T instance, ReckonOptions<T> options) where T : Schema.Schema
			=> TrackStepped(instance, options);

		/// <summary>
		///     Shared add/remove wiring behind the AttachAll overloads: a
		///     collection whose members come and go needs no bookkeeping from the
		///     caller.
		/// </summary>
		private Action AttachEach(string collection, Action<Schema.Schema> attach)
		{
			var tracked = new List<Schema.Schema>();
			var addOff = callbacks.OnAdd(collection, (Schema.Schema child, object key) =>
			{
				attach(child);
				tracked.Add(child);
			});
			var removeOff = callbacks.OnRemove(collection, (Schema.Schema child, object key) =>
			{
				tracked.Remove(child);
				Detach(child);
			});
			Action off = () =>
			{
				addOff?.Invoke();
				removeOff?.Invoke();
				foreach (var child in tracked) { Detach(child); }
				tracked.Clear();
			};
			attachments.Add(off);
			return off;
		}

		// --- Factories --------------------------------------------------------

		/// <summary>Spawn a driven <see cref="Reconciler{S,I}" /> (clock injected, fixed step adopted).</summary>
		public Reconciler<S, I> Reconciler<S, I>(S instance, ReconcilerOptions<S, I> opts) where S : Schema.Schema
		{
			opts.Clock = opts.Clock ?? clock;
			BindRenderDelay(opts.Input);
			var recon = new Reconciler<S, I>(instance, opts);
			AdoptFixedStep(recon.StepMs);
			InstallBoundOverlay(recon);
			driven.Add(recon);
			return recon;
		}

		/// <summary>
		///     Route <see cref="Value" /> at every field a controller predicts, so
		///     ONE read idiom covers the whole render layer: passively-smoothed
		///     remotes and controller-owned entities alike, with the caller never
		///     naming a pose key.
		///
		///     Any passive slot already on that field is STASHED, not dropped — its
		///     listener keeps sampling, and Dispose() puts it back.
		/// </summary>
		private void InstallBoundOverlay(RollbackController ctrl)
		{
			var regs = ctrl.BoundRegistrations;
			if (regs.Count == 0) { return; }
			var touched = new List<(Dictionary<string, Slot> perRef, string field)>();
			foreach (var reg in regs)
			{
				if (reg.Source == null) { continue; }
				if (!slotsByRef.TryGetValue(reg.Source.__refId, out var perRef))
				{
					perRef = new Dictionary<string, Slot>();
					slotsByRef[reg.Source.__refId] = perRef;
				}
				for (int k = 0; k < reg.Fields.Count; k++)
				{
					string field = reg.Fields[k];
					perRef.TryGetValue(field, out var stash);
					if (stash != null && stash.Ctrl != null)
					{
						ColyseusContext.Logger.LogWarning(
							$"Colyseus Predict: \"{field}\" is already bound to a controller — " +
							"the newer registration wins.");
						stash = stash.Stash;
					}
					perRef[field] = new Slot
					{
						Field = field,
						Instance = reg.Source,
						Ctrl = ctrl,
						PoseKey = reg.PoseKeys[k],
						Stash = stash,
					};
					touched.Add((perRef, field));
				}
			}
			ctrl.OnDisposed(() =>
			{
				foreach (var (perRef, field) in touched)
				{
					if (perRef.TryGetValue(field, out var slot) && slot.Ctrl != null)
					{
						if (slot.Stash != null) { perRef[field] = slot.Stash; }
						else { perRef.Remove(field); }
					}
				}
			});
		}

		/// <summary>
		///     Tell the input handle how far in the past this client draws, taken
		///     from the lerp delay already attached here.
		///
		///     Worth doing automatically because the failure is silent and
		///     expensive: a lag-compensating server rewinds to
		///     <c>serverNow − (renderDelay + rtt/2)</c>, so leaving renderDelay at
		///     zero makes every rewound read land one full render-delay early, and
		///     shots miss by exactly that much with nothing in the logs to say so.
		///     An explicit <see cref="InputOptions.RenderDelay" /> still wins.
		/// </summary>
		private void BindRenderDelay(InputHandle input)
		{
			if (input == null || input.RenderDelay > 0) { return; }
			foreach (var perRef in slotsByRef.Values)
			{
				foreach (var slot in perRef.Values)
				{
					if (slot.Opts != null && slot.Opts.Mode == PredictMode.Lerp && slot.Opts.Delay > 0)
					{
						input.RenderDelay = slot.Opts.Delay;
						return;
					}
				}
			}
		}

		/// <summary>
		///     Spawn a driven <see cref="SimReconciler{I}" /> — the composite face,
		///     for a world of parts rather than one entity's fields.
		/// </summary>
		public SimReconciler<W, I> Sim<W, I>(SimReconcilerOptions<W, I> opts)
			where W : class
		{
			opts.Clock = opts.Clock ?? clock;
			BindRenderDelay(opts.Input);
			var recon = new SimReconciler<W, I>(opts);
			AdoptFixedStep(recon.StepMs);
			InstallBoundOverlay(recon);
			driven.Add(recon);
			return recon;
		}

		/// <summary>Spawn a driven <see cref="PredictedEventChannel{T}" />.</summary>
		public PredictedEventChannel<T> DefineEvent<T>(PredictedEventChannelOptions<T> opts)
		{
			var channel = new PredictedEventChannel<T>(opts, clock);
			driven.Add(channel);
			return channel;
		}

		/// <summary>
		///     Spawn a driven <see cref="PredictedSpawns{S,L}" /> wired to a
		///     root-level collection's add/remove stream.
		///     <para>
		///     With <c>Fields</c> + <c>ReckonStep</c>, the store also owns the
		///     collection's MOTION (no separate <c>AttachAll</c> needed):
		///     confirmed entities are dead-reckoned, and
		///     <c>store.Value(entry, field)</c> is one read path across the
		///     whole life of the entity.
		///     </para>
		/// </summary>
		public PredictedSpawns<S, L> Spawns<S, L>(string collection, PredictedSpawnsOptions<S, L> opts)
			where S : Schema.Schema where L : class
		{
			var store = new PredictedSpawns<S, L>(opts, clock);

			// Reckon wiring: every confirmed entity gets a reckon attach whose
			// horizon is snapshot age PLUS the entry's measured input lead — 0
			// for a foreign entity (server-present, same as an AttachAll
			// reckon), the exact per-spawn uplink for an owned one (see
			// SpawnTime). An owned projectile thus keeps flying the shooter's
			// timeline through the handoff.
			bool reckon = opts?.Fields != null && opts.ReckonStep != null;
			// keyed by refId, like simsByRef
			var untrack = reckon ? new Dictionary<int, Action>() : null;
			var localClock = clock;

			var addOff = callbacks.OnAdd(collection, (server, key) =>
			{
				var s = (S)server;
				store.HandleAdd(s);
				if (!reckon || untrack.ContainsKey(s.__refId)) { return; } // decoder re-fire
				// AFTER HandleAdd: the lead is only measured once the entry collapses.
				double lead = store.EntryFor(s)?.LeadMs ?? 0;
				var off = TrackStepped(s, new ReckonOptions<S>
				{
					Fields = opts.Fields,
					Step = opts.ReckonStep,
					Smoothing = opts.Smoothing,
					Substep = opts.Substep,
				});
				BindForward(s, () =>
				{
					if (localClock == null) { return Math.Max(0, lead); }
					double stamp = localClock.LastServerTime();
					double age = stamp > 0 ? Math.Max(0, localClock.ServerNow() - stamp) : 0;
					return Math.Max(0, age + lead);
				});
				untrack[s.__refId] = off;
			});

			var removeOff = callbacks.OnRemove(collection, (server, key) =>
			{
				var s = (S)server;
				if (untrack != null && untrack.TryGetValue(s.__refId, out var off))
				{
					off?.Invoke();
					untrack.Remove(s.__refId);
				}
				store.HandleRemove(s);
			});

			if (reckon)
			{
				// route store.Value() confirmed reads through the reckon slots
				store.BindReader((server, field) => Value(server, field));
			}

			store.OnDisposedInternal = () =>
			{
				addOff?.Invoke();
				removeOff?.Invoke();
				if (untrack != null)
				{
					foreach (var off in untrack.Values) { off?.Invoke(); }
					untrack.Clear();
				}
			};
			driven.Add(store);
			return store;
		}

		private void AdoptFixedStep(double stepMs)
		{
			if (!fixedStepMs.HasValue)
			{
				fixedStepMs = stepMs;
				stepAcc = 0;
				lastFrameNow = -1;
				return;
			}
			if (Math.Abs(fixedStepMs.Value - stepMs) > 1e-6)
			{
				ColyseusContext.Logger.LogWarning(
					$"Predict: a reconciler's fixed step ({stepMs}ms) differs from this Predict's " +
					$"({fixedStepMs.Value}ms). Tick() paces one rate; use a separate Predict per rate.");
			}
		}

		// --- Per-frame driver -------------------------------------------------

		/// <summary>
		///     Call once per render frame. Returns the SEND BUDGET: how many
		///     fixed input steps are due — send exactly that many inputs, then
		///     read render values. 0 until a reconciler advertises the step.
		/// </summary>
		public int Tick(double now)
		{
			renderTime = now;

			int steps = 0;
			if (fixedStepMs.HasValue && fixedStepMs.Value > 0)
			{
				double dt = lastFrameNow < 0 ? 0 : now - lastFrameNow;
				stepAcc += dt;
				steps = (int)Math.Floor(stepAcc / fixedStepMs.Value);
				if (steps > MaxStepsPerFrame)
				{
					stepAcc = 0;
					steps = MaxStepsPerFrame;
				}
				else
				{
					stepAcc -= steps * fixedStepMs.Value;
				}
			}
			lastFrameNow = now;

			// drive children; compact the dead
			for (int i = driven.Count - 1; i >= 0; i--)
			{
				switch (driven[i])
				{
					case RollbackController controller:
						if (controller.Dead) { driven.RemoveAt(i); continue; }
						controller.Tick(now);
						break;
					case IDrivenChild child:
						if (child.Dead) { driven.RemoveAt(i); continue; }
						child.Tick(now);
						child.Prune();
						break;
				}
			}
			return steps;
		}

		// --- Sample pipeline --------------------------------------------------

		private static double ToNumber(object value)
		{
			if (value == null) { return 0; }
			if (value is bool b) { return b ? 1 : 0; }
			try { return Convert.ToDouble(value); }
			catch { return 0; }
		}

		private void OnSample(Slot slot, double current)
		{
			double now = clock != null && clock.LastServerTime() > 0
				? clock.LastServerTime()
				: RoomClock.GetNow();

			if (slot.Opts.Angle)
			{
				double previous = slot.V1;
				current = previous + Math.Atan2(Math.Sin(current - previous), Math.Cos(current - previous));
			}

			int head = slot.RingHead;
			int count = slot.RingCount;

			if (slot.Opts.Snap > 0 && count > 0 && Math.Abs(current - slot.V1) > slot.Opts.Snap)
			{
				head = 0;
				count = 0;
				slot.AuxV = current;
			}

			double lastT1 = count == 0
				? double.NegativeInfinity
				: slot.RingT[head == 0 ? RingCap - 1 : head - 1];

			double snapT = now;
			double tickInterval = slot.Opts.TickInterval;
			if (tickInterval > 0 && !double.IsNegativeInfinity(lastT1))
			{
				double elapsed = now - lastT1;
				double ticks = elapsed > 0 ? Math.Max(1, Math.Floor(elapsed / tickInterval + 0.5)) : 1;
				snapT = lastT1 + ticks * tickInterval;
				double cap = now + tickInterval;
				if (snapT > cap) { snapT = cap; }
			}

			slot.V1 = current;

			if (count >= 2 && !double.IsNegativeInfinity(lastT1))
			{
				int h1 = head == 0 ? RingCap - 1 : head - 1;
				int h2 = h1 == 0 ? RingCap - 1 : h1 - 1;
				double lastInterval = lastT1 - slot.RingT[h2];
				double patchMs = clock?.PatchInterval() ?? 0;
				double span = patchMs > 0 ? patchMs : lastInterval;
				double trigger = patchMs > 0 ? GapResumePatchMult * patchMs : GapResumeMult * lastInterval;
				if (span > 0 && snapT - lastT1 > trigger)
				{
					double resumeSpan = span < GapResumeMaxMs ? span : GapResumeMaxMs;
					slot.RingT[head] = snapT - resumeSpan;
					slot.RingV[head] = slot.RingV[h1];
					head = head + 1 >= RingCap ? 0 : head + 1;
					if (count < RingCap) { count++; }
				}
			}

			slot.RingT[head] = snapT;
			slot.RingV[head] = current;
			slot.RingHead = head + 1 >= RingCap ? 0 : head + 1;
			slot.RingCount = count < RingCap ? count + 1 : count;
		}

		// --- Reads ------------------------------------------------------------

		/// <summary>Smoothed/predicted RENDER value with raw instance fallback.</summary>
		public double Value(Schema.Schema instance, string field)
		{
			if (!slotsByRef.TryGetValue(instance.__refId, out var perRef)
				|| !perRef.TryGetValue(field, out var slot))
			{
				return ToNumber(instance[field]);
			}
			// Controller-owned: a reconciler claimed this field, so the pose comes
			// from its rollback rather than a smoothing curve over the server stream.
			if (slot.Ctrl != null) { return slot.Ctrl.Value(slot.PoseKey); }
			switch (slot.Opts.Mode)
			{
				case PredictMode.Lerp: return ComputeLerp(slot);
				case PredictMode.Damped: return ComputeDamped(slot);
				case PredictMode.Extrapolate: return ComputeExtrapolate(slot);
				case PredictMode.Reckon: return ComputeReckon(slot);
				default: return slot.V1;
			}
		}

		/// <summary>
		///     RAW reckoned value at an arbitrary server-time instant (no
		///     smoothing offset) — for game logic / lag-comp hit tests. Reckons
		///     forward from the latest snapshot; the past clamps to it.
		/// </summary>
		public double ValueAt(Schema.Schema instance, string field, double time)
		{
			if (!slotsByRef.TryGetValue(instance.__refId, out var perRef)
				|| !perRef.TryGetValue(field, out var slot)
				|| slot.Opts.Mode != PredictMode.Reckon)
			{
				return Value(instance, field);
			}
			if (simsByRef.TryGetValue(instance.__refId, out var sim))
			{
				int pos = IndexOf(sim.Fields, field);
				if (pos >= 0)
				{
					double baseT = clock?.LastServerTime() ?? double.NaN;
					double forward = double.IsNaN(baseT) ? 0 : Math.Max(0, time - baseT);
					Advance(sim, forward, sim.ValueOut, time);
					return sim.ValueOut[pos];
				}
			}
			return slot.V1;
		}

		private static int IndexOf(IReadOnlyList<string> list, string value)
		{
			for (int i = 0; i < list.Count; i++) { if (list[i] == value) { return i; } }
			return -1;
		}

		private double ComputeDamped(Slot slot)
		{
			double now = renderTime;
			double dtFrame = now - slot.AuxT;
			slot.AuxT = now;
			if (dtFrame > 0)
			{
				double k = 1 - Math.Exp(-slot.Opts.Damping * dtFrame / 1000);
				slot.AuxV += (slot.V1 - slot.AuxV) * k;
			}
			return slot.AuxV;
		}

		private double ComputeLerp(Slot slot)
		{
			int count = slot.RingCount;
			if (count == 0) { return slot.V1; }
			int head = slot.RingHead;
			int start = (head - count + RingCap) % RingCap;
			int newest = (start + count - 1) % RingCap;
			if (count == 1) { return slot.RingV[newest]; }

			double target = clock != null && clock.LastServerTime() > 0
				? clock.ServerNow() - slot.Opts.Delay - clock.SmoothedRtt() / 2
				: renderTime - slot.Opts.Delay;

			if (target <= slot.RingT[start]) { return slot.RingV[start]; }
			if (target >= slot.RingT[newest]) { return slot.RingV[newest]; }

			int k = count - 2;
			int phys = (start + k) % RingCap;
			while (k > 0)
			{
				if (slot.RingT[phys] <= target) { break; }
				k--;
				phys = phys == 0 ? RingCap - 1 : phys - 1;
			}
			int b = phys + 1 >= RingCap ? 0 : phys + 1;
			double tA = slot.RingT[phys], tB = slot.RingT[b];
			double span = tB - tA;
			if (span <= 0) { return slot.RingV[b]; }
			double u = (target - tA) / span;
			return slot.RingV[phys] + (slot.RingV[b] - slot.RingV[phys]) * u;
		}

		private double ComputeExtrapolate(Slot slot)
		{
			int count = slot.RingCount;
			if (count == 0) { return slot.V1; }
			int head = slot.RingHead;
			int start = (head - count + RingCap) % RingCap;
			int newest = (start + count - 1) % RingCap;
			double newestT = slot.RingT[newest];
			double newestV = slot.RingV[newest];
			double now = renderTime;

			double raw;
			if (count == 1)
			{
				raw = newestV;
			}
			else
			{
				int steps = count >= 3 ? 2 : 1;
				int lb = (start + count - 1 - steps + RingCap) % RingCap;
				double dt = newestT - slot.RingT[lb];
				if (dt <= 0)
				{
					raw = newestV;
				}
				else
				{
					double slope = (newestV - slot.RingV[lb]) / dt;
					double ahead = now - newestT;
					if (ahead < 0) { ahead = 0; }
					else if (ahead > slot.Opts.MaxExtrapolate) { ahead = slot.Opts.MaxExtrapolate; }
					raw = newestV + slope * ahead;
				}
			}

			double lastT = slot.AuxT;
			slot.AuxT = now;
			if (slot.Opts.Damping <= 0)
			{
				slot.AuxV = raw;
				return raw;
			}
			double dtFrame = now - lastT;
			if (dtFrame > 0)
			{
				double k = 1 - Math.Exp(-slot.Opts.Damping * dtFrame / 1000);
				slot.AuxV += (raw - slot.AuxV) * k;
			}
			return slot.AuxV;
		}

		private double ComputeReckon(Slot slot)
		{
			if (simsByRef.TryGetValue(slot.Instance.__refId, out var sim))
			{
				int pos = IndexOf(sim.Fields, slot.Field);
				if (pos >= 0)
				{
					ApplySimulation(sim);
					return sim.Smoothed[pos];
				}
			}
			return slot.V1;
		}

		private void Advance(SimState sim, double forwardMs, double[] output, double endElapsed)
		{
			foreach (var f in sim.CopyFields) { sim.Scratch[f] = sim.Instance[f]; }
			double remaining = forwardMs;
			double elapsed = endElapsed - forwardMs;
			while (remaining > 0)
			{
				double stepMs = remaining < sim.Substep ? remaining : sim.Substep;
				elapsed += stepMs;
				sim.Step(sim.Scratch, stepMs / 1000, elapsed);
				remaining -= stepMs;
			}
			for (int k = 0; k < sim.Fields.Count; k++) { output[k] = ToNumber(sim.Scratch[sim.Fields[k]]); }
		}

		private void ApplySimulation(SimState sim)
		{
			double now = renderTime;
			if (sim.LastApplyTime == now) { return; }

			int n = sim.Fields.Count;
			double baseT = clock?.LastServerTime() ?? double.NaN;
			double present = clock?.ServerNow() ?? 0;
			double forward = sim.ForwardMs?.Invoke()
				?? (!double.IsNaN(baseT) && baseT > 0 ? Math.Max(0, present - baseT) : 0);
			Advance(sim, forward, sim.Out, present);

			bool first = double.IsNegativeInfinity(sim.LastApplyTime);
			if (first || sim.Smoothing <= 0)
			{
				for (int k = 0; k < n; k++) { sim.Offset[k] = 0; sim.Smoothed[k] = sim.Out[k]; }
			}
			else if (!double.IsNaN(baseT))
			{
				double dtMs = Math.Max(0, Math.Min(now - sim.LastApplyTime, 100));
				if (baseT != sim.LastBaseT && !double.IsNaN(sim.LastBaseT))
				{
					for (int k = 0; k < n; k++)
					{
						double d = sim.Smoothed[k] + sim.FrameVel[k] * dtMs - sim.Out[k];
						sim.Offset[k] = sim.Snap > 0 && Math.Abs(d) > sim.Snap ? 0 : d;
					}
				}
				else if (dtMs > 0)
				{
					for (int k = 0; k < n; k++) { sim.FrameVel[k] = (sim.Out[k] - sim.OutPrev[k]) / dtMs; }
				}
				double decay = Math.Exp(-sim.Smoothing * dtMs / 1000);
				for (int k = 0; k < n; k++)
				{
					sim.Offset[k] *= decay;
					sim.Smoothed[k] = sim.Out[k] + sim.Offset[k];
				}
			}
			else
			{
				double dtMs = Math.Max(0, Math.Min(now - sim.LastApplyTime, 100));
				double k2 = 1 - Math.Exp(-sim.Smoothing * dtMs / 1000);
				for (int k = 0; k < n; k++) { sim.Smoothed[k] += (sim.Out[k] - sim.Smoothed[k]) * k2; }
			}
			for (int k = 0; k < n; k++) { sim.OutPrev[k] = sim.Out[k]; }
			sim.LastBaseT = baseT;
			sim.LastApplyTime = now;
		}
	}

	/// <summary>Driven-child face for event channels / spawn stores.</summary>
	public interface IDrivenChild
	{
		bool Dead { get; }
		void Tick(double now);
		void Prune();
	}

	/// <summary>
	///     The callbacks face <see cref="Predict" /> consumes — engine-level,
	///     string-keyed. <see cref="PredictCallbacks{TState}" /> adapts the
	///     SDK's <see cref="StateCallbackStrategy{TState}" />.
	/// </summary>
	public interface IPredictCallbacks
	{
		Action Listen(Schema.Schema instance, string field, Action<object> handler, bool immediate);
		Action OnAdd(string collection, Action<Schema.Schema, object> handler);
		Action OnRemove(string collection, Action<Schema.Schema, object> handler);
	}

	/// <summary>Adapter over the SDK's typed callbacks strategy.</summary>
	public class PredictCallbacks<TState> : IPredictCallbacks where TState : Schema.Schema
	{
		private readonly StateCallbackStrategy<TState> strategy;

		public PredictCallbacks(StateCallbackStrategy<TState> strategy)
		{
			this.strategy = strategy;
		}

		public Action Listen(Schema.Schema instance, string field, Action<object> handler, bool immediate)
			=> strategy.Listen<object>(instance, field, (current, previous) => handler(current), immediate);

		public Action OnAdd(string collection, Action<Schema.Schema, object> handler)
			=> strategy.OnAdd(collection, new KeyValueEventHandler<string, object>(
				(key, value) => handler((Schema.Schema)value, key)), true);

		public Action OnRemove(string collection, Action<Schema.Schema, object> handler)
			=> strategy.OnRemove(collection, new KeyValueEventHandler<string, object>(
				(key, value) => handler((Schema.Schema)value, key)));
	}
}
