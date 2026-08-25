using System;

namespace Colyseus.Schema.Utils
{
    /// <summary>
    ///     Resolved <c>t.quantized()</c> descriptor — precomputed so decode
    ///     (and a future encode) derive the scale from the same fields.
    /// </summary>
    public class QuantizeDescriptor
    {
        public double Min;
        public double Max;
        /// <summary>Max − Min, the domain width.</summary>
        public double Range;
        /// <summary>2^bits for wrap (top step folds onto 0); 2^bits − 1 for clamp.</summary>
        public double Span;
        /// <summary>Wire width: 8 | 16 | 32.</summary>
        public byte Bits;
        /// <summary>false = clamp.</summary>
        public bool Wrap;
    }

    /// <summary>
    ///     <c>t.quantized()</c> codec — a bounded float encoded as a
    ///     fixed-width unsigned integer. Port of @colyseus/schema 5.0
    ///     <c>src/types/quantize.ts</c>; the math must stay bit-identical to
    ///     the reference:
    ///     rounding is explicit <c>Floor(x + 0.5)</c> (NOT <c>Math.Round</c>,
    ///     whose banker's rounding disagrees on the .5 case), wrapping ranges
    ///     are reduced in the FLOAT domain before the integer step, the wrap
    ///     top step folds via <c>%</c> on doubles (bits=32 safe), NaN → q=0,
    ///     ±Inf → q=0 for wrap / natural clamp for clamp. All math in double.
    /// </summary>
    public static class Quantize
    {
        public static QuantizeDescriptor Resolve(double min, double max, byte bits, bool wrap)
        {
            double steps = Math.Pow(2, bits);
            return new QuantizeDescriptor
            {
                Min = min,
                Max = max,
                Range = max - min,
                Span = wrap ? steps : steps - 1,
                Bits = bits,
                Wrap = wrap,
            };
        }

        /// <summary>Float → unsigned wire integer.</summary>
        public static uint QuantizeValue(QuantizeDescriptor desc, double value)
        {
            if (desc.Wrap)
            {
                // non-finite can't be range-reduced; pin to q=0 so both peers agree
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return 0;
                }

                double range = desc.Range;
                // float-domain range reduction → [0, range)
                double a = (value - desc.Min) % range;
                if (a < 0)
                {
                    a += range;
                }

                return (uint)(Math.Floor((a / range) * desc.Span + 0.5) % desc.Span);
            }

            if (double.IsNaN(value))
            {
                return 0; // NaN → min (±Inf clamps naturally below)
            }

            double v = value < desc.Min ? desc.Min : (value > desc.Max ? desc.Max : value);
            return (uint)Math.Floor(((v - desc.Min) / desc.Range) * desc.Span + 0.5);
        }

        /// <summary>Unsigned wire integer → float.</summary>
        public static double Dequantize(QuantizeDescriptor desc, uint q)
        {
            return desc.Min + (q / desc.Span) * desc.Range;
        }

        /// <summary>Wire-exact round-trip — what a quantized field yields after assignment.</summary>
        public static double Snap(QuantizeDescriptor desc, double value)
        {
            return Dequantize(desc, QuantizeValue(desc, value));
        }
    }
}
