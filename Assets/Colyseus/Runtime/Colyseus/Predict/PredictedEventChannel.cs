using System;
using System.Collections.Generic;
using System.Linq;

namespace Colyseus.Predict
{
	/// <summary>Constants shared by every <see cref="PredictedEventChannel{T}" />.</summary>
	public static class PredictedEventChannel
	{
		/// <summary>
		///     Default <see cref="PredictedEventChannelOptions{T}.GraceTicks" />:
		///     server progress past a prediction before an unconfirmed sim-born
		///     entry rejects (~333ms of simulation at 30Hz) — generous against
		///     contested-event divergence, and anchored to the ack stream so
		///     latency can't induce a false reject.
		/// </summary>
		public const int DefaultGraceTicks = 10;
	}

	/// <summary>Options for <see cref="PredictedEventChannel{T}" />.</summary>
	public class PredictedEventChannelOptions<T>
	{
		/// <summary>
		///     Diagnostic name. Nothing resolves by it — it only names this channel
		///     in warnings, which starts to matter once a room has several.
		/// </summary>
		public string Label;
		/// <summary>The optimistic feedback — fires the moment the event is predicted.</summary>
		public Action<T> OnPredict;
		/// <summary>The prediction was wrong — undo the optimistic feedback.</summary>
		public Action<T> OnReject;
		/// <summary>The server agreed (fired by Confirm, once per settled entry).</summary>
		public Action<T> OnConfirm;
		/// <summary>A Confirm settled NOTHING — the signal arrived unpredicted.</summary>
		public Action<object> OnUnpredicted;
		/// <summary>Entry identity — payloads mapping to the same value dedupe
		///     while pending. Null: string/number payloads key themselves; other
		///     payloads share one anonymous slot.</summary>
		public Func<T, object> UniqueBy;
		/// <summary>Sim-born settlement deadline in input ticks (default <see cref="PredictedEventChannel.DefaultGraceTicks" />).</summary>
		public int GraceTicks = PredictedEventChannel.DefaultGraceTicks;
		/// <summary>UI-born eviction window (ms); 0 = max(2·rtt, 600).</summary>
		public double TtlMs;
		/// <summary>Min gap (ms) between OnPredict fires; 0 = off.</summary>
		public double CooldownMs;
		/// <summary>
		///     Declarative settlement off a state change (see <see cref="ConfirmOn" />).
		///     Wired by <c>predict.DefineEvent</c>; a standalone channel ignores it.
		/// </summary>
		public ConfirmOn ConfirmOn;
	}

	/// <summary>
	///     Typed optimistic-event channel (port of
	///     predict/predictedEventChannel.ts): birth in the predicted sim
	///     (<see cref="StepContext.Predict" /> — live-only, replay-safe) or
	///     from UI (<see cref="Predict" />); settlement: <see cref="Confirm" />
	///     on the authoritative signal, sim-born grace-tick auto-reject, UI-born
	///     wall-clock TTL.
	/// </summary>
	public class PredictedEventChannel<T> : IPredictSink<T>, IDrivenChild, IDisposable
	{
		private class Entry
		{
			public T Payload;
			public int Seq;
			public Func<int> Acked;
			public double At;
		}

		private static readonly object SingletonKey = new object();

		private readonly PredictedEventChannelOptions<T> opts;
		private readonly RoomClock clock;
		private readonly Dictionary<object, Entry> entries = new Dictionary<object, Entry>();
		private double cooldownUntil = double.NegativeInfinity;
		private bool warnedReplayPredict;

		public bool Dead { get; private set; }
		public int PendingCount => entries.Count;
		internal Action OnDisposedInternal;

		public PredictedEventChannel(PredictedEventChannelOptions<T> options, RoomClock clock)
		{
			opts = options ?? new PredictedEventChannelOptions<T>();
			this.clock = clock;
		}

		private double Now() => clock?.ServerNow() ?? RoomClock.GetNow();

		private object KeyOf(T payload)
		{
			if (opts.UniqueBy != null) { return opts.UniqueBy(payload); }
			if (payload is string || payload is int || payload is long || payload is double) { return payload; }
			return SingletonKey;
		}

		/// <summary>Sim-born prediction (reached via ctx.Predict — live steps only).</summary>
		public void PredictFromSim(int seq, T payload, Func<int> acked) => Add(seq, payload, acked);

		/// <summary>
		///     Predict from OUTSIDE the sim (UI-optimistic; wall-clock TTL). Inside
		///     a reconciler step use <c>ctx.Predict(channel, payload)</c> instead —
		///     this form is backstopped (a call that lands inside a rollback replay
		///     no-ops and warns once) but the ctx form is live-only by construction.
		/// </summary>
		public void Predict(T payload)
		{
			if (RollbackController.IsReplaying())
			{
				if (!warnedReplayPredict)
				{
					warnedReplayPredict = true;
					string name = opts.Label != null ? $" \"{opts.Label}\"" : "";
					ColyseusContext.Logger.LogWarning(
						$"colyseus: PredictedEventChannel{name}.Predict() was called during a rollback " +
						"replay and ignored. Inside a reconciler step, use ctx.Predict(channel, payload).");
				}
				return;
			}
			Add(-1, payload, null);
		}

		private void Add(int seq, T payload, Func<int> acked)
		{
			object key = KeyOf(payload);
			if (entries.ContainsKey(key)) { return; }   // pending dedupe
			double t = Now();
			if (opts.CooldownMs > 0)
			{
				if (t < cooldownUntil) { return; }
				cooldownUntil = t + opts.CooldownMs;
			}
			entries[key] = new Entry { Payload = payload, Seq = seq, Acked = acked, At = t };
			opts.OnPredict?.Invoke(payload);
		}

		/// <summary>Is a prediction pending? Null key = any.</summary>
		public bool Has(object key = null) => key == null ? entries.Count > 0 : entries.ContainsKey(key);

		/// <summary>
		///     The server agreed: settle the entry for <paramref name="key" />
		///     (null = EVERY pending entry). The entry is removed BEFORE
		///     OnConfirm fires. Returns the count; 0 fires OnUnpredicted.
		/// </summary>
		public int Confirm(object key = null)
		{
			int settled = 0;
			foreach (var k in SettleKeys(key))
			{
				if (!entries.TryGetValue(k, out var entry)) { continue; }
				entries.Remove(k);
				opts.OnConfirm?.Invoke(entry.Payload);
				settled++;
			}
			if (settled == 0) { opts.OnUnpredicted?.Invoke(key); }
			return settled;
		}

		/// <summary>The server overruled: reject (null key = every pending). Fires OnReject.</summary>
		public int Reject(object key = null)
		{
			int rejected = 0;
			foreach (var k in SettleKeys(key))
			{
				if (!entries.TryGetValue(k, out var entry)) { continue; }
				entries.Remove(k);
				opts.OnReject?.Invoke(entry.Payload);
				rejected++;
			}
			return rejected;
		}

		private List<object> SettleKeys(object key) =>
			key != null ? new List<object> { key } : entries.Keys.ToList();

		/// <summary>Drop every pending entry SILENTLY (no callbacks).</summary>
		public void Clear() => entries.Clear();

		public void Tick(double now) { }

		/// <summary>Sim-born grace auto-rejects first, then wall-clock TTL.</summary>
		public void Prune()
		{
			if (entries.Count > 0)
			{
				foreach (var pair in entries.ToList())
				{
					var entry = pair.Value;
					if (entry.Acked != null && entry.Acked() >= entry.Seq + opts.GraceTicks)
					{
						entries.Remove(pair.Key);
						opts.OnReject?.Invoke(entry.Payload);
					}
				}
			}
			double rtt = clock?.SmoothedRtt() ?? 0;
			double ttl = opts.TtlMs > 0 ? opts.TtlMs : Math.Max(rtt * 2, 600);
			double now = Now();
			foreach (var pair in entries.ToList())
			{
				var entry = pair.Value;
				if (entry.Acked != null) { continue; }   // sim-born: progress-settled
				if (now - entry.At > ttl)
				{
					entries.Remove(pair.Key);
					opts.OnReject?.Invoke(entry.Payload);
				}
			}
		}

		/// <summary>Stop being driven and drop all entries (silently).</summary>
		public void Dispose()
		{
			if (Dead) { return; }
			Dead = true;
			OnDisposedInternal?.Invoke();
			Clear();
		}
	}
}
