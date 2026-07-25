using System;
using System.Collections.Generic;
using System.Linq;

namespace Colyseus.Predict
{
	/// <summary>Options for <see cref="PredictedSpawns{S,L}" />.</summary>
	public class PredictedSpawnsOptions<S, L>
	{
		/// <summary>Which incoming server entities are this client's to
		///     correlate (null = every entity).</summary>
		public Func<S, bool> Owned;
		/// <summary>Pairing predicate; null = fifo (oldest pending).</summary>
		public Func<L, S, bool> Correlate;
		/// <summary>Server-clock spawn instant of an authoritative entity —
		///     enables the measured input lead.</summary>
		public Func<S, double> SpawnTime;
		/// <summary>Advance a pending local each tick (dt seconds, serverNow axis).</summary>
		public Action<L, double> Step;
		/// <summary>Eviction window (ms) given the current rtt; null = max(2·rtt, 600).</summary>
		public Func<double, double> Ttl;
		/// <summary>Invoked when a prediction is dropped as a mispredict.</summary>
		public Action<L, int> OnReject;
	}

	/// <summary>A merged logical entity — one per logical spawn. Key sprites on Id.</summary>
	public class SpawnEntry<S, L>
	{
		public int Id;
		public S Server;
		public L Local;
		public bool Confirmed;
		/// <summary>Measured input lead (ms) — SpawnTime(server) − at.</summary>
		public double LeadMs;
		internal double At = double.NaN;
		internal bool Accepted;
	}

	/// <summary>
	///     Predicted-spawn store (port of predict/predictedSpawns.ts):
	///     optimistic locals outside the schema collection, correlated to the
	///     authoritative entity on its add (fifo or predicate) and collapsed
	///     onto one logical entry with a STABLE id — the handoff is invisible.
	/// </summary>
	public class PredictedSpawns<S, L> : IDrivenChild where S : class where L : class
	{
		private readonly PredictedSpawnsOptions<S, L> opts;
		private readonly RoomClock clock;
		private readonly Dictionary<int, SpawnEntry<S, L>> byId = new Dictionary<int, SpawnEntry<S, L>>();
		private readonly Dictionary<S, SpawnEntry<S, L>> byServer = new Dictionary<S, SpawnEntry<S, L>>();
		private readonly List<SpawnEntry<S, L>> order = new List<SpawnEntry<S, L>>();
		private int nextId = 1;
		private double lastTickAt = double.NaN;

		public bool Dead { get; private set; }
		public int Size => byId.Count;
		internal Action OnDisposedInternal;

		public PredictedSpawns(PredictedSpawnsOptions<S, L> options, RoomClock clock)
		{
			opts = options ?? new PredictedSpawnsOptions<S, L>();
			this.clock = clock;
		}

		private double Now() => clock?.ServerNow() ?? RoomClock.GetNow();

		/// <summary>Record an optimistic local spawn; returns the entry (stable Id).</summary>
		public SpawnEntry<S, L> Spawn(L local)
		{
			var entry = new SpawnEntry<S, L> { Id = nextId++, Local = local, At = Now() };
			byId[entry.Id] = entry;
			order.Add(entry);
			return entry;
		}

		/// <summary>Drop a still-pending prediction (no-op once confirmed).</summary>
		public void Cancel(int id)
		{
			if (byId.TryGetValue(id, out var entry) && !entry.Confirmed) { Drop(entry); }
		}

		/// <summary>Exempt a still-pending entry from TTL eviction.</summary>
		public void Accept(int id)
		{
			if (byId.TryGetValue(id, out var entry)) { entry.Accepted = true; }
		}

		private void Drop(SpawnEntry<S, L> entry)
		{
			byId.Remove(entry.Id);
			if (entry.Server != null) { byServer.Remove(entry.Server); }
			order.Remove(entry);
		}

		/// <summary>Route the collection's onAdd here.</summary>
		public void HandleAdd(S server)
		{
			if (server == null || byServer.ContainsKey(server)) { return; }

			bool owned = opts.Owned == null || opts.Owned(server);
			SpawnEntry<S, L> matched = null;
			if (owned)
			{
				foreach (var entry in order)
				{
					if (entry.Confirmed || entry.Local == null) { continue; }
					if (opts.Correlate == null || opts.Correlate(entry.Local, server))
					{
						matched = entry;
						break;
					}
				}
			}

			if (matched != null)
			{
				// transition IN PLACE — same Id, the handoff contract
				matched.Server = server;
				matched.Confirmed = true;
				if (opts.SpawnTime != null && !double.IsNaN(matched.At))
				{
					matched.LeadMs = opts.SpawnTime(server) - matched.At;
				}
				byServer[server] = matched;
			}
			else
			{
				var entry = new SpawnEntry<S, L> { Id = nextId++, Server = server, Confirmed = true };
				byId[entry.Id] = entry;
				order.Add(entry);
				byServer[server] = entry;
			}
		}

		/// <summary>Route the collection's onRemove here.</summary>
		public void HandleRemove(S server)
		{
			if (server != null && byServer.TryGetValue(server, out var entry)) { Drop(entry); }
		}

		/// <summary>Advance pending locals on the serverNow axis (the same axis
		///     the lead lives on — the handoff cannot jump).</summary>
		public void Tick(double now)
		{
			double t = clock != null ? clock.ServerNow() : now;
			if (opts.Step != null && !double.IsNaN(lastTickAt))
			{
				double dt = Math.Max(0, (t - lastTickAt) / 1000);
				if (dt > 0)
				{
					foreach (var entry in order)
					{
						if (!entry.Confirmed && entry.Local != null) { opts.Step(entry.Local, dt); }
					}
				}
			}
			lastTickAt = t;
		}

		/// <summary>Drop pending locals older than the TTL — mispredicts.</summary>
		public void Prune()
		{
			if (byId.Count == 0) { return; }
			double now = Now();
			double rtt = clock?.SmoothedRtt() ?? 0;
			double ttl = opts.Ttl?.Invoke(rtt) ?? Math.Max(rtt * 2, 600);
			foreach (var entry in order.ToList())
			{
				if (entry.Confirmed || entry.Accepted || double.IsNaN(entry.At)) { continue; }
				if (now - entry.At > ttl)
				{
					Drop(entry);
					opts.OnReject?.Invoke(entry.Local, entry.Id);
				}
			}
		}

		/// <summary>Iterate the merged view — exactly one entry per logical entity.</summary>
		public IEnumerable<SpawnEntry<S, L>> Entries() => order;

		public SpawnEntry<S, L> EntryFor(S server) =>
			server != null && byServer.TryGetValue(server, out var entry) ? entry : null;

		public bool Alive(int id) => byId.ContainsKey(id);

		/// <summary>Drop all predictions and tracked entries.</summary>
		public void Clear()
		{
			byId.Clear();
			byServer.Clear();
			order.Clear();
		}

		public void Dispose()
		{
			Dead = true;
			OnDisposedInternal?.Invoke();
			Clear();
		}
	}
}
