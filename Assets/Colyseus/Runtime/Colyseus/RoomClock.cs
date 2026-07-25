using System;
using System.Collections.Generic;

namespace Colyseus
{
	/// <summary>
	///     Per-room clock-sync + RTT estimator, fed by the TIMED prefix the
	///     server prepends to state messages when the room declared input via
	///     <c>defineInput()</c>. Port of the JS SDK's <c>RoomClock.ts</c>
	///     (RoomClockImpl) — pure math.
	///
	///     <para>
	///     Two independent estimates: the clock offset (<see cref="ServerNow" />,
	///     seeded by the first sample then EMA-smoothed, updates gated to
	///     low-jitter packets), and the RTT (<see cref="Rtt" /> /
	///     <see cref="SmoothedRtt" />, fed by the input ack round-trip, outliers
	///     rejected).
	///     </para>
	///
	///     <para>
	///     A room that never declares input never receives TIMED samples: the
	///     clock then reports <c>ServerNow() == Now()</c> and zeros.
	///     </para>
	/// </summary>
	public class RoomClock
	{
		/// <summary>Injectable monotonic ms clock (tests replace this).</summary>
		public static Func<double> GetNow = DefaultNow;

		private static readonly System.Diagnostics.Stopwatch Stopwatch = System.Diagnostics.Stopwatch.StartNew();
		private static double DefaultNow() => Stopwatch.Elapsed.TotalMilliseconds;

		private const double EmaAlpha = 0.1;
		private const double RenderTauDefault = 250;
		private const double RenderSnap = 250;
		private const double RttOutlierX = 4;
		private const double JitterGain = 1.0 / 16.0;
		private const int JitterStallX = 4;
		private const double RttGateFactor = 1.2;
		private const double RttGateWindow = 10000;
		private const int RttGateWarmup = 30;

		internal double _clockOffset;
		private bool clockHasSample;
		private int offsetCount;

		// sliding-window-minimum deque (parallel lists; front = windowed min)
		private readonly List<double> rttFloorT = new List<double>();
		private readonly List<double> rttFloorV = new List<double>();

		private double _rtt;
		private double smoothedRtt;
		private bool rttHasSample;

		private double _jitter;
		private double lastRecvTime = -1;

		private double lastServerTime;
		private double patchIntervalMs;

		private double renderTau = RenderTauDefault;
		private double renderSn;
		private double renderSnAt;

		/// <summary>Local monotonic clock (ms) — the un-offset base <see cref="ServerNow" /> builds on.</summary>
		public virtual double Now() => GetNow();

		/// <summary>
		///     Estimated server clock: ms since room start (reconstructed via the
		///     wire sNow + local offset). Returns the local clock until the first
		///     sample lands.
		/// </summary>
		public virtual double ServerNow() => GetNow() + _clockOffset;

		/// <summary>
		///     Server clock on a SLEW-LIMITED render timeline: free-runs at
		///     1 ms/ms and servos toward <see cref="ServerNow" /> (τ ≈ 250 ms),
		///     snapping past gaps &gt; 250 ms. Use for DRAWING remote entities;
		///     never for server-stamped deadlines.
		/// </summary>
		public virtual double RenderNow()
		{
			double target = ServerNow();
			if (renderTau <= 0) { return target; }
			double t = GetNow();
			if (renderSn == 0) { renderSn = target; renderSnAt = t; return renderSn; }
			double dt = Math.Min(t - renderSnAt, 100); // clamp tab-resume stalls
			if (dt < 0.5) { return renderSn; }         // same frame — advance once
			renderSnAt = t;
			renderSn += dt;                            // free-run at 1 ms/ms
			if (Math.Abs(target - renderSn) > RenderSnap) { renderSn = target; return renderSn; }
			renderSn += (target - renderSn) * (1 - Math.Exp(-dt / renderTau));
			return renderSn;
		}

		/// <summary>Set the <see cref="RenderNow" /> slew time-constant (ms); ≤ 0 disables slewing.</summary>
		public void SetRenderTau(double milliseconds) => renderTau = milliseconds > 0 ? milliseconds : 0;

		/// <summary>Most recent RTT sample (ms); 0 until the first valid sample.</summary>
		public virtual double Rtt() => _rtt;

		/// <summary>EMA-smoothed RTT (ms); prefer for forward-prediction.</summary>
		public virtual double SmoothedRtt() => smoothedRtt;

		/// <summary>RFC 3550-style interarrival jitter (ms) vs the advertised patch cadence.</summary>
		public virtual double Jitter() => _jitter;

		/// <summary>
		///     Raw sNow of the last TIMED sample — the snapshot's server-encode
		///     time. <c>ServerNow() − LastServerTime()</c> = the snapshot's age.
		/// </summary>
		public virtual double LastServerTime() => lastServerTime;

		/// <summary>Server snapshot cadence (patchRate, ms); 0 until advertised.</summary>
		public virtual double PatchInterval() => patchIntervalMs;

		/// <summary>Feed the advertised patch cadence (ms) from the input handshake.</summary>
		public virtual void SetPatchInterval(double milliseconds) => patchIntervalMs = milliseconds > 0 ? milliseconds : 0;

		/// <summary>
		///     Feed a decoded TIMED sample: <paramref name="sNow" /> (ms since
		///     room start) updates the clock offset; <paramref name="rttSample" />
		///     (ms round-trip from the input ack, &lt; 0 if none this packet)
		///     updates the RTT estimate.
		/// </summary>
		public virtual void Sample(double sNow, double rttSample)
		{
			double tNow = GetNow();
			const double a = EmaAlpha;

			// jitter vs the cadence — idle patches round to a whole multiple;
			// bursts (mult 0) and stalls (mult > 4) are skipped
			double pi = patchIntervalMs;
			if (lastRecvTime >= 0 && pi > 0)
			{
				double gap = tNow - lastRecvTime;
				double mult = Math.Floor(gap / pi + 0.5);
				if (mult >= 1 && mult <= JitterStallX)
				{
					_jitter += (Math.Abs(gap - mult * pi) - _jitter) * JitterGain;
				}
			}
			lastRecvTime = tNow;

			lastServerTime = sNow;

			// reject impossible / outlier RTT (tab-resume spikes once converged)
			if (rttSample < 0)
			{
				rttSample = -1;
			}
			else if (smoothedRtt > 0 && rttSample > smoothedRtt * RttOutlierX)
			{
				rttSample = -1;
			}

			// offset: prefer the RTT-corrected estimate when a fresh sample exists
			double offsetSample = rttSample >= 0 ? sNow + rttSample / 2 - tNow : sNow - tNow;
			if (!clockHasSample)
			{
				_clockOffset = offsetSample;
				clockHasSample = true;
				if (rttSample >= 0) { PushRttFloor(rttSample, tNow); offsetCount = 1; }
			}
			else if (rttSample >= 0)
			{
				// gate on the windowed-min RTT floor — except during post-reset warmup
				double floor = PushRttFloor(rttSample, tNow);
				bool warming = offsetCount < RttGateWarmup;
				offsetCount++;
				if (warming || rttSample <= floor * RttGateFactor)
				{
					_clockOffset = _clockOffset * (1 - a) + offsetSample * a;
				}
			}

			// RTT: seeded on the first valid sample (separate flag from the offset seed)
			if (rttSample >= 0)
			{
				_rtt = rttSample;
				if (!rttHasSample)
				{
					smoothedRtt = rttSample;
					rttHasSample = true;
				}
				else
				{
					smoothedRtt = smoothedRtt * (1 - a) + rttSample * a;
				}
			}
		}

		/// <summary>Push an RTT sample into the sliding-window-min deque; returns the floor.</summary>
		private double PushRttFloor(double rttSample, double tNow)
		{
			while (rttFloorV.Count > 0 && rttFloorV[rttFloorV.Count - 1] >= rttSample)
			{
				rttFloorV.RemoveAt(rttFloorV.Count - 1);
				rttFloorT.RemoveAt(rttFloorT.Count - 1);
			}
			rttFloorV.Add(rttSample);
			rttFloorT.Add(tNow);
			double cutoff = tNow - RttGateWindow;
			while (rttFloorT.Count > 0 && rttFloorT[0] < cutoff)
			{
				rttFloorT.RemoveAt(0);
				rttFloorV.RemoveAt(0);
			}
			return rttFloorV[0];
		}

		/// <summary>Reset all state (reconnect). τ and patch interval are config — they survive.</summary>
		public void Reset()
		{
			_clockOffset = 0;
			clockHasSample = false;
			offsetCount = 0;
			_rtt = 0;
			smoothedRtt = 0;
			rttHasSample = false;
			_jitter = 0;
			lastRecvTime = -1;
			lastServerTime = 0;
			rttFloorT.Clear();
			rttFloorV.Clear();
			renderSn = 0;
			renderSnAt = 0;
		}
	}
}
