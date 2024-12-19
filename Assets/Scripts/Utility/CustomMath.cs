using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

public static class CustomMath
{
    public const double EPSILON = 0.001d;
    
    private readonly struct Interval
    {
        public readonly double value;
        private readonly double min;
        private readonly double max;

        private Interval(double value)
        {
            this.value = value;

            double ulp = ULP(value);
            min = value - ulp;
            max = value + ulp;
        }

        private Interval(double value, double min, double max)
        {
            this.value = value;
            this.min = min;
            this.max = max;
        }

        private static double ULP(double x)
        {
            if (x == 0d)
                return double.Epsilon;

            long bits = BitConverter.DoubleToInt64Bits(x);
            long nextBits = bits + (x > 0 ? 1 : -1);

            double next = BitConverter.Int64BitsToDouble(nextBits);
            return Math.Abs(next - x);
        }

        public static implicit operator double(Interval x)
        {
            return x.value;
        }

        public static implicit operator Interval(double x)
        {
            return new Interval(x);
        }

        public static bool operator ==(Interval a, double b)
        {
            return a.min <= b && b <= a.max;
        }

        public static bool operator !=(Interval a, double b)
        {
            return b < a.min || b > a.max;
        }

        public static bool operator ==(Interval a, Interval b)
        {
            return a.max >= b.min && a.min < b.max || b.max >= a.min && b.min < a.max;
        }

        public static bool operator !=(Interval a, Interval b)
        {
            return !(a == b);
        }

        public static bool operator >(Interval a, double b)
        {
            return a.min > b;
        }

        public static bool operator <(Interval a, double b)
        {
            return a.max < b;
        }

        public static bool operator >=(Interval a, double b)
        {
            return a > b || a == b;
        }

        public static bool operator <=(Interval a, double b)
        {
            return a < b || a == b;
        }

        public static Interval operator +(Interval a, Interval b)
        {
            return new Interval(a.value + b.value, a.min + b.min, a.max + b.max);
        }

        public static Interval operator -(Interval a, Interval b)
        {
            return new Interval(a.value - b.value, a.min - b.max, a.max - b.min);
        }

        public static Interval operator -(Interval x)
        {
            return new Interval(-x.value, -x.max, -x.min);
        }

        public static Interval operator *(Interval a, Interval b)
        {
            double[] bounds = { a.min * b.min, a.min * b.max, a.max * b.min, a.max * b.max };
            return new Interval(a.value * b.value, bounds.Min(), bounds.Max());
        }

        public static Interval operator *(double a, Interval b)
        {
            return (Interval) a * b;
        }

        public static Interval operator *(Interval a, double b)
        {
            return b * a;
        }

        public static Interval operator /(Interval a, Interval b)
        {
            double bMin = b.max == 0d ? double.NegativeInfinity : 1d / b.max;
            double bMax = b.min == 0d ? double.PositiveInfinity : 1d / b.min;

            double[] bounds = { a.min * bMin, a.min * bMax, a.max * bMin, a.max * bMax };
            return new Interval(a.value / b.value, bounds.Min(), bounds.Max());
        }

        public override string ToString()
        {
            return $"{value} [{min}, {max}]";
        }
    }

    
    public static double[] SolveQuartic(double a, double b, double c, double d, double e)
    {
        if (a == 0d) // solve cubic
            return SolveCubic(b, c, d, e);

        double A = -3d * b * b / (8d * a * a) + c / a;
        Interval B = GetB();
        double C = -3d * b * b * b * b / (256d * a * a * a * a) + c * b * b / (16d * a * a * a) - b * d / (4d * a * a) + e / a;

        if (B == 0d) // biquadratic case
        {
            double term1 = -b / (4d * a);
            Complex term2 = Complex.Sqrt(A * A - 4d *  C);

            return GetRealRoots(
                new[]
                {
                    term1 + Complex.Sqrt(-A + term2) / 2d,
                    term1 + Complex.Sqrt(-A - term2) / 2d,
                    term1 - Complex.Sqrt(-A + term2) / 2d,
                    term1 - Complex.Sqrt(-A - term2) / 2d
                }
            );
        }

        double p = -A * A / 12d - C;
        double q = -A * A * A / 108d + A * C / 3d - B.value * B.value / 8d;
        Complex r = -q / 2d + Complex.Sqrt(q * q / 4d + p * p * p / 27d);
        Complex u = Complex.Pow(r, 1d / 3d);
        
        Complex y = -5d / 6d * A + (u == 0d ? -Complex.Pow(q, 1d / 3d) : u - p / (3d * u));
        Complex w = Complex.Sqrt( A + 2d * y);
        
        double term3 = -b / (4d * a);
        Complex term4 = 3d * A + 2d * y;
        Complex term5 = 2d * B.value / w;

        return GetRealRoots(
            new[]
            {
                term3 + (w + Complex.Sqrt(-term4 - term5)) / 2d,
                term3 + (-w + Complex.Sqrt(-term4 + term5)) / 2d, 
                term3 + (w - Complex.Sqrt(-term4 - term5)) / 2d, 
                term3 + (-w - Complex.Sqrt(-term4 + term5)) / 2d
            }
        );

        Interval GetB()
        {
            Interval A = a, B = b, C = c, D = d;
            return B * B * B / (8d * A * A * A) - B * C / (2d * A * A) + D / A;
        }
    }

    public static double[] SolveCubic(double a, double b, double c, double d)
    {
        if (a == 0f)
            return SolveQuadratic(b, c, d);

        Complex term1 = -2d * b;

        double p1 = -2d * b * b * b + 9d * a * b * c - 27d * a * a * d;
        double p2 = b * b - 3d * a * c;
        Complex p3 = Complex.Sqrt(p1 * p1 - 4d * p2 * p2 * p2);
        
        Complex p4 = 0.5d * (-1d + Complex.Sqrt(-3f));
        Complex p5 = 0.5d * (-1d - Complex.Sqrt(-3f));
        Complex term2 = Complex.Pow(4d * (p1 + p3), 1d / 3f);
        Complex term3 = Complex.Pow(4d * (p1 - p3), 1d / 3f);
        Complex denominator = 6d * a;
        
        return GetRealRoots(
            new[]
            {
                (term1 + term2 + term3) / denominator,
                (term1 + p4 * term2 + p5 * term3) / denominator,
                (term1 + p4 * p4 * term2 + p5 * p5 * term3) / denominator
            }
        );
    }

    public static double[] SolveQuadratic(double a, double b, double c)
    {
        if (a == 0f)
            return SolveLinear(b, c);

        Complex delta = Complex.Sqrt(b * b - 4d * a * c);
        return GetRealRoots(
            new[]
            {
                (-b + delta) / (2d * a),
                (-b - delta) / (2d * a)
            }
        );
    }

    public static double[] SolveLinear(double a, double b)
    {
        return a == 0d ? null : new[] { -b / a };
    }

    private static double[] GetRealRoots(Complex[] roots)
    {
        return roots
            .Where(root => Math.Abs(root.Imaginary) < EPSILON)
            .OrderBy(root => Math.Abs(root.Imaginary))
            .Select(realRoot => realRoot.Real)
            .ToArray();
    }
}