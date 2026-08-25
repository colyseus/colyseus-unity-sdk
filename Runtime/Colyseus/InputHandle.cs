using System;
using System.Collections.Generic;
using Colyseus.Schema;

namespace Colyseus
{
	/// <summary>Options accepted by <c>Room.Input()</c>.</summary>
	public class InputOptions
	{
		/// <summary>
		///     "reliable" (default). "unreliable" is rejected for now — it needs a
		///     WebTransport datagram channel, which this SDK does not have.
		/// </summary>
		public string Mode = "reliable";

		/// <summary>Unreliable-mode redundancy ring size (default 3).</summary>
		public int HistorySize = 3;

		/// <summary>Interpolation buffer (ms) — how far in the past remote entities render.</summary>
		public double RenderDelay;

		/// <summary>Per-send gate: stamp only inputs the server may rewind to.</summary>
		public Func<object, bool> AllowRewind;
	}

	/// <summary>
	///     Per-room input handle returned by <c>Room.Input()</c>. Mutate
	///     <see cref="Data" />, then call <see cref="Send" /> to delta-encode and
	///     transmit. Port of the JS SDK's <c>input/InputHandle.ts</c> — wire
	///     contract: PORTING/sdk-ports-input-layer.md
	///     ([19|TIMED?][stamp?][body] framing, delta-coded lag-comp stamps,
	///     implicit 1-based count seq, ack → RTT sample, replay ring,
	///     epoch-counted reset).
	/// </summary>
	public class InputHandle
	{
		/// <summary>Mutable schema instance — mutate, then call <see cref="Send" />.</summary>
		public readonly Schema.Schema Data;

		public string Mode => encoder.Mode;

		/// <summary>Server-advertised fixed step rate (Hz); null when not advertised.</summary>
		public readonly int? TickRate;
		/// <summary>Server-advertised state-patch interval (ms) = the reconcile cadence.</summary>
		public readonly int? PatchRate;
		/// <summary>Physics sub-steps per input tick (1 when the server doesn't sub-step).</summary>
		public readonly int SubSteps = 1;

		/// <summary>The fixed step in seconds (1/TickRate); null when no rate advertised.</summary>
		public double? StepSeconds => TickRate.HasValue ? 1.0 / TickRate.Value : (double?)null;
		/// <summary>The fixed step in milliseconds.</summary>
		public double? StepMs => TickRate.HasValue ? 1000.0 / TickRate.Value : (double?)null;
		/// <summary>The physics sub-step in seconds (StepSeconds / SubSteps).</summary>
		public double? SubStepSeconds => TickRate.HasValue ? (1.0 / TickRate.Value) / SubSteps : (double?)null;
		/// <summary>The physics sub-step in milliseconds.</summary>
		public double? SubStepMs => TickRate.HasValue ? (1000.0 / TickRate.Value) / SubSteps : (double?)null;

		/// <summary>Last input the server acked PROCESSING into its state.</summary>
		public int LastProcessed { get; private set; }
		/// <summary>Reliable inputs transmitted (the seq the server will ack).</summary>
		public int SentCount { get; private set; }
		/// <summary>Sent but not yet acked.</summary>
		public int PendingCount => SentCount - LastProcessed;
		/// <summary>Capacity (in seqs) of the replay ring backing <see cref="At" />.</summary>
		public readonly int ReplayBufferSize;
		/// <summary>Monotonic reset counter — rollback controllers poll it and self-reset.</summary>
		public int Epoch { get; private set; }

		/// <summary>
		///     How far in the PAST this client draws remote entities, in ms. A
		///     lag-compensating server rewinds its targets by this much plus half
		///     the RTT, so it reads the world at the instant you actually saw —
		///     get it wrong and every shot misses by exactly the difference.
		///     <see cref="Predict.Reconciler{S,I}" /> binds it from the lerp
		///     delay you already attached with; set it yourself only to override.
		/// </summary>
		public double RenderDelay
		{
			get => renderDelay;
			set => renderDelay = Math.Max(0, value);
		}

		private readonly InputEncoder encoder;
		private readonly Func<Connection> getConnection;
		private readonly Func<RoomClock> getClock;

		private readonly bool stampRender;
		private readonly bool stampReckon;
		private double renderDelay;
		private readonly Func<object, bool> allowRewind;
		private double lastStamp;      // delta-coded stamp baseline
		private double pendingReckon;

		private readonly double[] sendTimes;           // seq % size → send time
		private Schema.Schema[] inputBuffer;           // replay ring (lazily allocated)
		private double[] reckonTimes;                  // per-seq reckon stamps
		private List<Action<int>> sendListeners;
		private static bool warnedBufferOverflow;

		private const int BufferFloor = 64;
		private const double BufferRttBudgetMs = 1000;
		private const double BufferHeadroom = 1.5;

		internal InputHandle(
			Schema.Schema data,
			InputEncoder encoder,
			bool stampRender, bool stampReckon,
			double renderDelay, Func<object, bool> allowRewind,
			int? tickRate, int? patchRate, int? subSteps,
			Func<Connection> getConnection, Func<RoomClock> getClock)
		{
			Data = data;
			this.encoder = encoder;
			this.getConnection = getConnection;
			this.getClock = getClock;
			this.stampRender = stampRender;
			this.stampReckon = stampReckon;
			this.renderDelay = renderDelay;
			this.allowRewind = allowRewind;
			TickRate = tickRate;
			PatchRate = patchRate;
			SubSteps = subSteps.GetValueOrDefault(1) > 1 ? subSteps.Value : 1;

			// replay ring sized to worst-case in-flight = (RTT budget + patch interval) × input rate
			double stepMs = TickRate.HasValue ? 1000.0 / TickRate.Value : 1000.0 / 60;
			double window = BufferRttBudgetMs + PatchRate.GetValueOrDefault(0);
			ReplayBufferSize = Math.Max(BufferFloor, (int)Math.Ceiling(window / stepMs * BufferHeadroom));

			sendTimes = new double[ReplayBufferSize];
		}

		/// <summary>
		///     Encode the staged input and send it. Always transmits one input
		///     when the connection is open — a body-less frame on a no-change
		///     tick. Returns the seq assigned to this input, or 0 when nothing
		///     was sent (seqs are 1-based).
		/// </summary>
		public int Send()
		{
			var conn = getConnection();
			if (conn == null || !conn.IsOpen) { return 0; }

			byte[] body = encoder.Encode();
			bool reliable = encoder.Mode == "reliable";

			bool wantStamp = (stampReckon || stampRender) && reliable
				&& (allowRewind == null || allowRewind(Data));
			bool both = stampReckon && stampRender;

			var output = new List<byte>();
			byte opcode = reliable ? Protocol.ROOM_INPUT_RELIABLE : Protocol.ROOM_INPUT_UNRELIABLE;
			if (wantStamp) { opcode |= (byte)ProtocolModifier.TIMED; }
			output.Add(opcode);

			if (wantStamp)
			{
				var clock = getClock();
				bool synced = clock != null && clock.LastServerTime() > 0;
				double rk = synced ? Math.Max(0, Math.Floor(clock.ServerNow() + 0.5)) : 0;
				pendingReckon = rk;
				double renderDelta = synced
					? Math.Min(0xffff, Math.Max(0, Math.Floor(renderDelay + clock.SmoothedRtt() / 2 + 0.5)))
					: 0;
				// the mode's single u32 timeline, delta-coded vs the baseline;
				// BOTH mode trails the absolute u16 renderDelta
				double stamp = stampReckon ? rk : (rk > renderDelta ? rk - renderDelta : 0);
				Schema.Utils.Encode.Number(output, stamp - lastStamp);
				lastStamp = stamp;
				if (both) { Schema.Utils.Encode.Uint16(output, (ushort)renderDelta); }
			}
			else
			{
				pendingReckon = 0;
			}

			output.AddRange(body);
			// WS-only transport: unreliable falls back to the reliable channel
			_ = conn.Send(output.ToArray());

			int seq;
			if (reliable)
			{
				seq = ++SentCount; // implicit count seq — mirrors the server's consumed counter
			}
			else
			{
				seq = SentCount = encoder.Seq; // adopt the framework seq (stamped on the wire)
			}
			RecordSent(seq);
			return seq;
		}

		/// <summary>
		///     Subscribe to sends: <c>listener(seq)</c> fires synchronously at the
		///     end of each <see cref="Send" /> (replay ring + reckon instant
		///     already recorded). Returns an unsubscribe action.
		/// </summary>
		public Action OnSend(Action<int> listener)
		{
			sendListeners ??= new List<Action<int>>();
			sendListeners.Add(listener);
			return () => sendListeners?.Remove(listener);
		}

		/// <summary>
		///     Reset encoder + round-trip state (the SDK calls this on
		///     reconnect): next send emits a full snapshot; the stamp baseline
		///     re-zeroes; <see cref="Epoch" /> increments so observing
		///     controllers self-reset.
		/// </summary>
		public void Reset()
		{
			encoder.Reset();
			SentCount = LastProcessed = encoder.Seq;
			lastStamp = 0;
			Array.Clear(sendTimes, 0, sendTimes.Length);
			Epoch++;
		}

		/// <summary>
		///     The buffered snapshot of reliable input <paramref name="seq" />
		///     for reconciliation replay. Null if already acked, never sent, or
		///     aged out. The instance is REUSED — read synchronously.
		/// </summary>
		public Schema.Schema At(int seq)
		{
			if (inputBuffer == null) { return null; }
			if (seq <= LastProcessed || seq > SentCount) { return null; }
			if (SentCount - seq >= ReplayBufferSize)
			{
				if (!warnedBufferOverflow)
				{
					warnedBufferOverflow = true;
					ColyseusContext.Logger.LogWarning(
						$"colyseus: input replay buffer ({ReplayBufferSize}) overflowed — reconciliation may drift.");
				}
				return null;
			}
			return inputBuffer[seq % ReplayBufferSize];
		}

		/// <summary>
		///     The reckon instant (server-clock ms) stamped onto reliable input
		///     <paramref name="seq" />; 0 when reckon stamping is off or seq is
		///     outside the valid window.
		/// </summary>
		public double ReckonTimeAt(int seq)
		{
			if (reckonTimes == null) { return 0; }
			if (seq <= LastProcessed || seq > SentCount) { return 0; }
			if (SentCount - seq >= ReplayBufferSize) { return 0; }
			return reckonTimes[seq % ReplayBufferSize];
		}

		/// <summary>
		///     Feed the server's last-PROCESSED input seq (decoded from the TIMED
		///     prefix). Advances <see cref="LastProcessed" /> and returns the RTT
		///     sample for that ack, or -1 when unknown.
		/// </summary>
		internal double AckInput(int seq)
		{
			if (seq <= LastProcessed) { return -1; }
			bool aged = SentCount - seq >= ReplayBufferSize;
			LastProcessed = seq;
			if (aged) { return -1; }
			double sentAt = sendTimes[seq % ReplayBufferSize];
			return sentAt > 0 ? RoomClock.GetNow() - sentAt : -1;
		}

		/// <summary>Snapshot the just-sent input + its send time (and reckon stamp) by seq.</summary>
		private void RecordSent(int seq)
		{
			if (inputBuffer == null)
			{
				inputBuffer = new Schema.Schema[ReplayBufferSize];
				for (int i = 0; i < ReplayBufferSize; i++)
				{
					inputBuffer[i] = (Schema.Schema)Activator.CreateInstance(Data.GetType());
				}
			}
			encoder.CopyInto(inputBuffer[seq % ReplayBufferSize]);
			sendTimes[seq % ReplayBufferSize] = RoomClock.GetNow();
			if (stampReckon)
			{
				reckonTimes ??= new double[ReplayBufferSize];
				reckonTimes[seq % ReplayBufferSize] = pendingReckon;
			}
			if (sendListeners != null)
			{
				// iterate a copy — a listener may unsubscribe mid-dispatch
				foreach (var listener in sendListeners.ToArray()) { listener(seq); }
			}
		}
	}
}
