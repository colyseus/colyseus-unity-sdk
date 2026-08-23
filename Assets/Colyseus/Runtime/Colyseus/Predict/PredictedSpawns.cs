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

		/// <summary>
		///     Dead-reckon confirmed entities on these fields. Each confirmed
		///     entity gets a reckon slot (readable via <c>predict.Value()</c>
		///     or, uniformly across the handoff, the store's
		///     <see cref="PredictedSpawns{S,L}.Value" />): foreign entities
		///     forward to server-present (snapshot age); owned ones
		///     additionally forward by the entry's measured input lead when
		///     <see cref="SpawnTime" /> is set. Requires
		///     <see cref="ReckonStep" />.
		/// </summary>
		public IReadOnlyList<string> Fields;

		/// <summary>
		///     The reckon's step, operating on a schema scratch of type
		///     <typeparamref name="S" />. This is the S-typed twin of
		///     <see cref="Step" />, which advances the pending local of type
		///     <typeparamref name="L" /> — the reference passes ONE `step` to
		///     both because JS types it structurally, whereas here the local
		///     and the schema are distinct types that no interface can unify
		///     (schemas are generated). Both must integrate the same motion, or
		///     the handoff jumps.
		/// </summary>
		public Action<S, double, double> ReckonStep;

		/// <summary>Reckon smoothing time constant (ms) for confirmed entities.
		///     Default 0 — a deterministic constant-step projectile rebases
		///     exactly, so smoothing only adds lag.</summary>
		public double SmoothMs;

		/// <summary>Reckon substep in ms. Smaller = more accurate bounces. Default 16.</summary>
		public double Substep = 16;

		/// <summary>
		///     Per-entry render scratch factory (a sprite handle, a tween). The
		///     returned object is exposed as <see cref="SpawnEntry{S,L}.Data" /> and
		///     dropped with the entry. Null = no scratch (<c>Data</c> stays null).
		/// </summary>
		public Func<object> Data;

		/// <summary>
		///     How to read a named field off a pending local — what lets
		///     <see cref="PredictedSpawns{S,L}.Value" /> span the handoff. The
		///     reference gets this for free (<c>local[field]</c>);
		///     <typeparamref name="L" /> is an arbitrary type here, so it has
		///     to be written down. Optional: without it <c>Value()</c> still
		///     serves confirmed entries.
		/// </summary>
		public Func<L, string, double> ReadLocal;
	}

	/// <summary>
	///     A merged logical entity — one per logical spawn. Key sprites on Id.
	///     Also the handle <see cref="PredictedSpawns{S,L}.Spawn" /> returns
	///     (the reference's <c>SpawnHandle</c>): <see cref="Cancel" /> /
	///     <see cref="Accept" /> act on this entry without naming its id.
	/// </summary>
	public class SpawnEntry<S, L> where S : class where L : class
	{
		public int Id;
		public S Server;
		public L Local;
		public bool Confirmed;
		/// <summary>Measured input lead (ms) — SpawnTime(server) − at.</summary>
		public double LeadMs;
		/// <summary>Render scratch from <see cref="PredictedSpawnsOptions{S,L}.Data" />; null when no factory was given.</summary>
		public object Data;
		internal double At = double.NaN;
		internal bool Accepted;
		internal PredictedSpawns<S, L> Store;

		/// <summary>Drop this prediction (a local cancel or rollback). No-op once confirmed by its authoritative add.</summary>
		public void Cancel() => Store?.Cancel(Id);
		/// <summary>Mark the prediction accepted: exempt the still-pending entry from TTL eviction.</summary>
		public void Accept() => Store?.Accept(Id);
	}

	/// <summary>
	///     Predicted-spawn store (port of predict/predictedSpawns.ts):
	///     optimistic locals outside the schema collection, correlated to the
	///     authoritative entity on its add (fifo or predicate) and collapsed
	///     onto one logical entry with a STABLE id — the handoff is invisible.
	/// </summary>
	public class PredictedSpawns<S, L> : IDrivenChild, IDisposable where S : class where L : class
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
			var entry = NewEntry();
			entry.Local = local;
			entry.At = Now();
			return entry;
		}

		/// <summary>Allocate, id and register an entry; the caller fills in which side it came from.</summary>
		private SpawnEntry<S, L> NewEntry()
		{
			var entry = new SpawnEntry<S, L> { Id = nextId++, Data = opts.Data?.Invoke(), Store = this };
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
				var entry = NewEntry();
				entry.Server = server;
				entry.Confirmed = true;
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

		/// <summary>
		///     Unified field read across the predicted → authoritative handoff:
		///     pending entries read the stepped local through
		///     <see cref="PredictedSpawnsOptions{S,L}.ReadLocal" />, confirmed
		///     entries read the authoritative instance through the bound reader
		///     — <c>predict.Value()</c> (reckoned, lead-aware) when created via
		///     <c>predict.Spawns(...)</c> with <c>Fields</c>, a raw field read
		///     otherwise. Render from this and the handoff is invisible: same
		///     id, same timeline, one code path.
		/// </summary>
		/// <returns>NaN when the entry is pending and no <c>ReadLocal</c> was given.</returns>
		public double Value(SpawnEntry<S, L> entry, string field)
		{
			if (entry.Server != null) { return readServer(entry.Server, field); }
			if (entry.Local == null || opts.ReadLocal == null) { return double.NaN; }
			return opts.ReadLocal(entry.Local, field);
		}

		/// <summary>Route confirmed-entry <see cref="Value" /> reads (wired by
		///     <c>predict.Spawns</c> to its reckon slots; standalone stores keep
		///     the raw default).</summary>
		public void BindReader(Func<S, string, double> read) => readServer = read;

		private Func<S, string, double> readServer = (server, field) =>
			server is Schema.Schema s ? ToNumber(s[field]) : double.NaN;

		private static double ToNumber(object value) =>
			value == null ? double.NaN : Convert.ToDouble(value);

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
