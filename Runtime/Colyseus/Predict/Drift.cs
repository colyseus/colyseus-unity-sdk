namespace Colyseus.Predict
{
	/// <summary>
	///     Rolling reconcile-drift telemetry (port of predict/drift.ts).
	///     <c>Ema</c> is the persistent component (steady nonzero = genuine
	///     divergence); <c>Peak</c> a decaying max (spike over a low ema =
	///     network jitter).
	/// </summary>
	public class Drift
	{
		public double Ema;
		public double Peak;

		/// <summary>Fold one reconcile's max |correction| into the telemetry.</summary>
		public void Update(double mag)
		{
			Ema += (mag - Ema) * 0.1;
			Peak = mag > Peak * 0.9 ? mag : Peak * 0.9;
		}

		public void Reset()
		{
			Ema = 0;
			Peak = 0;
		}
	}

	public enum DriftStatus
	{
		Matched,
		Jitter,
		Diverging,
	}

	public static class DriftClassifier
	{
		private const double FloatNoise = 1e-3;

		/// <summary>
		///     Actionable read of the rolling drift: matched / jitter /
		///     diverging. <paramref name="tolerance" /> ≤ the float-noise floor
		///     uses the floor (deterministic replay reproduces the server
		///     exactly, so anything at that level is rounding).
		/// </summary>
		public static DriftStatus Classify(Drift drift, double tolerance = 0)
		{
			double floor = tolerance > FloatNoise ? tolerance : FloatNoise;
			if (drift.Ema >= floor) { return DriftStatus.Diverging; }
			if (drift.Peak >= floor) { return DriftStatus.Jitter; }
			return DriftStatus.Matched;
		}
	}
}
