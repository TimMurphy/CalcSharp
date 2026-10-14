using System.Globalization;
using System.Numerics;

namespace CalcSharp.Core.Scientific;

/// <summary>
/// Arbitrary-precision decimal number: <c>Mantissa × 10^Exponent</c>. Results of every operation are
/// rounded to <see cref="Precision"/> significant digits, which is enough to display 32 digits exactly
/// like the real calculator.
/// </summary>
public readonly struct BigNum : IComparable<BigNum>, IEquatable<BigNum>
{
    /// <summary>Significant digits kept by arithmetic.</summary>
    public const int Precision = 40;

    /// <summary>Significant digits shown to the user.</summary>
    public const int DisplayDigits = 32;

    private const int Guard = 20;

    public static readonly BigNum Zero = new(BigInteger.Zero, 0);
    public static readonly BigNum One = new(BigInteger.One, 0);
    public static readonly BigNum Two = new(2, 0);
    public static readonly BigNum Ten = new(1, 1);

    private static readonly BigNum PiConstant = Parse("3.14159265358979323846264338327950288419716939937510582097494459230781640628620899");

    private static BigNum _ln2;
    private static BigNum _ln10;

    public BigNum(BigInteger mantissa, int exponent)
    {
        if (mantissa.IsZero)
        {
            Mantissa = BigInteger.Zero;
            Exponent = 0;
            return;
        }

        while ((mantissa % 10).IsZero)
        {
            mantissa /= 10;
            exponent++;
        }

        Mantissa = mantissa;
        Exponent = exponent;
    }

    public BigInteger Mantissa { get; }

    public int Exponent { get; }

    public bool IsZero => Mantissa.IsZero;

    public int Sign => Mantissa.Sign;

    public bool IsInteger => IsZero || Exponent >= 0;

    /// <summary>Number of significant digits in the mantissa.</summary>
    public int Digits => IsZero ? 0 : DigitCount(Mantissa);

    /// <summary>Power of ten of the leading digit, for example 3 for 1234.5.</summary>
    public int Magnitude => IsZero ? 0 : Digits + Exponent - 1;

    public static BigNum FromInt(long value) => new(value, 0);

    public static BigNum FromDecimal(decimal value)
    {
        var bits = decimal.GetBits(value);
        var mantissa = new BigInteger(new decimal(bits[0], bits[1], bits[2], false, 0));
        var scale = (bits[3] >> 16) & 0xFF;
        if (value < 0)
        {
            mantissa = -mantissa;
        }

        return new BigNum(mantissa, -scale);
    }

    public static BigNum FromDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new OverflowException();
        }

        return Parse(value.ToString("G17", CultureInfo.InvariantCulture));
    }

    public static BigNum Parse(string text)
    {
        text = text.Trim();
        var negative = text.StartsWith('-');
        if (negative || text.StartsWith('+'))
        {
            text = text[1..];
        }

        var exponent = 0;
        var e = text.IndexOfAny(['e', 'E']);
        if (e >= 0)
        {
            exponent = int.Parse(text[(e + 1)..], CultureInfo.InvariantCulture);
            text = text[..e];
        }

        var dot = text.IndexOf('.');
        if (dot >= 0)
        {
            exponent -= text.Length - dot - 1;
            text = text.Remove(dot, 1);
        }

        if (text.Length == 0)
        {
            text = "0";
        }

        var m = BigInteger.Parse(text, CultureInfo.InvariantCulture);
        return new BigNum(negative ? -m : m, exponent);
    }

    public double ToDouble() => IsZero ? 0 : double.Parse(ToPlainString(), CultureInfo.InvariantCulture);

    // ---- arithmetic ---------------------------------------------------------

    public static BigNum operator -(BigNum a) => new(-a.Mantissa, a.Exponent);

    public static BigNum operator +(BigNum a, BigNum b)
    {
        if (a.IsZero)
        {
            return b;
        }

        if (b.IsZero)
        {
            return a;
        }

        var gap = a.Magnitude - b.Magnitude;
        if (gap > Precision + Guard + 5)
        {
            return a;
        }

        if (-gap > Precision + Guard + 5)
        {
            return b;
        }

        var e = Math.Min(a.Exponent, b.Exponent);
        var m = (a.Mantissa * BigInteger.Pow(10, a.Exponent - e)) + (b.Mantissa * BigInteger.Pow(10, b.Exponent - e));
        return new BigNum(m, e).Round(Precision + Guard);
    }

    public static BigNum operator -(BigNum a, BigNum b) => a + (-b);

    public static BigNum operator *(BigNum a, BigNum b) =>
        new BigNum(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent).Round(Precision + Guard);

    public static BigNum operator /(BigNum a, BigNum b)
    {
        if (b.IsZero)
        {
            throw new DivideByZeroException();
        }

        if (a.IsZero)
        {
            return Zero;
        }

        var shift = Precision + Guard + 2 + b.Digits - a.Digits;
        if (shift < 0)
        {
            shift = 0;
        }

        var q = BigInteger.Divide(a.Mantissa * BigInteger.Pow(10, shift), b.Mantissa);
        return new BigNum(q, a.Exponent - b.Exponent - shift).Round(Precision + Guard);
    }

    public static bool operator <(BigNum a, BigNum b) => a.CompareTo(b) < 0;

    public static bool operator >(BigNum a, BigNum b) => a.CompareTo(b) > 0;

    public static bool operator <=(BigNum a, BigNum b) => a.CompareTo(b) <= 0;

    public static bool operator >=(BigNum a, BigNum b) => a.CompareTo(b) >= 0;

    public static bool operator ==(BigNum a, BigNum b) => a.CompareTo(b) == 0;

    public static bool operator !=(BigNum a, BigNum b) => a.CompareTo(b) != 0;

    public BigNum Abs() => Sign < 0 ? -this : this;

    public int CompareTo(BigNum other)
    {
        if (Sign != other.Sign)
        {
            return Sign.CompareTo(other.Sign);
        }

        if (IsZero)
        {
            return 0;
        }

        var magnitude = Magnitude.CompareTo(other.Magnitude);
        if (magnitude != 0)
        {
            return Sign > 0 ? magnitude : -magnitude;
        }

        var e = Math.Min(Exponent, other.Exponent);
        var a = Mantissa * BigInteger.Pow(10, Exponent - e);
        var b = other.Mantissa * BigInteger.Pow(10, other.Exponent - e);
        return a.CompareTo(b);
    }

    public bool Equals(BigNum other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is BigNum n && Equals(n);

    public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

    /// <summary>Rounds to <paramref name="significantDigits"/> significant digits (half away from zero).</summary>
    public BigNum Round(int significantDigits)
    {
        var digits = Digits;
        if (digits <= significantDigits)
        {
            return this;
        }

        return RoundAtExponent(Exponent + (digits - significantDigits));
    }

    /// <summary>Rounds so that no digit below 10^<paramref name="exponent"/> remains.</summary>
    public BigNum RoundAtExponent(int exponent)
    {
        if (IsZero || Exponent >= exponent)
        {
            return this;
        }

        var drop = exponent - Exponent;
        var divisor = BigInteger.Pow(10, drop);
        var abs = BigInteger.Abs(Mantissa);
        var q = BigInteger.DivRem(abs, divisor, out var r);
        if (r * 2 >= divisor)
        {
            q += 1;
        }

        return new BigNum(Sign < 0 ? -q : q, exponent);
    }

    /// <summary>Truncates towards zero to an integer.</summary>
    public BigNum Truncate()
    {
        if (Exponent >= 0)
        {
            return this;
        }

        return new BigNum(BigInteger.Divide(Mantissa, BigInteger.Pow(10, -Exponent)), 0);
    }

    public BigNum Floor()
    {
        var t = Truncate();
        return Sign < 0 && t != this ? t - One : t;
    }

    public BigNum Ceiling()
    {
        var t = Truncate();
        return Sign > 0 && t != this ? t + One : t;
    }

    public BigNum Frac() => this - Truncate();

    // ---- functions ----------------------------------------------------------

    public static BigNum Pi => PiConstant;

    public static BigNum Sqrt(BigNum x)
    {
        if (x.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (x.IsZero)
        {
            return Zero;
        }

        // Scale so that the integer square root has Precision + Guard digits and the exponent is even.
        var wanted = (Precision + Guard + 2) * 2;
        var shift = Math.Max(0, wanted - x.Digits);
        if (((x.Exponent - shift) & 1) != 0)
        {
            shift++;
        }

        var scaled = x.Mantissa * BigInteger.Pow(10, shift);
        var root = ISqrt(scaled);
        return new BigNum(root, (x.Exponent - shift) / 2).Round(Precision + Guard);
    }

    public static BigNum Exp(BigNum x)
    {
        if (x.IsZero)
        {
            return One;
        }

        // Reduce: x = r * 2^m with |r| < 0.001, then square m times.
        var m = Math.Max(0, x.Magnitude + 12);
        var r = x / FromInt(1) / Pow2(m);
        var sum = One;
        var term = One;
        for (var n = 1; n < 200; n++)
        {
            term = (term * r) / FromInt(n);
            sum += term;
            if (term.IsZero || term.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        for (var i = 0; i < m; i++)
        {
            sum *= sum;
        }

        return sum;
    }

    public static BigNum Ln(BigNum x)
    {
        if (x.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        // x = m * 10^e with m in [1, 10).
        var e = x.Magnitude;
        var m = new BigNum(x.Mantissa, -(x.Digits - 1));

        // Take square roots until m is near 1, then use the atanh series.
        var k = 0;
        while (Math.Abs((m - One).ToDouble()) > 0.01 && k < 40)
        {
            m = Sqrt(m);
            k++;
        }

        var z = (m - One) / (m + One);
        var z2 = z * z;
        var term = z;
        var sum = z;
        for (var n = 3; n < 400; n += 2)
        {
            term *= z2;
            var t = term / FromInt(n);
            sum += t;
            if (t.IsZero || t.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        var lnm = sum * Two * Pow2(k);
        return lnm + (FromInt(e) * Ln10());
    }

    public static BigNum Ln2()
    {
        if (_ln2.IsZero)
        {
            _ln2 = Two * AtanhSeries(One / FromInt(3));
        }

        return _ln2;
    }

    public static BigNum Ln10()
    {
        if (_ln10.IsZero)
        {
            // ln 10 = 3 ln 2 + ln 1.25 ; ln 1.25 = 2 atanh(1/9)
            _ln10 = (FromInt(3) * Ln2()) + (Two * AtanhSeries(One / FromInt(9)));
        }

        return _ln10;
    }

    public static BigNum Log10(BigNum x) => Ln(x) / Ln10();

    public static BigNum Pow(BigNum x, BigNum y)
    {
        if (y.IsZero)
        {
            return One;
        }

        if (x.IsZero)
        {
            if (y.Sign < 0)
            {
                throw new DivideByZeroException();
            }

            return Zero;
        }

        if (y.IsInteger && y.Magnitude <= 6)
        {
            var n = (long)y.Mantissa * (long)Math.Pow(10, y.Exponent);
            var negative = n < 0;
            var result = IntPow(x, Math.Abs(n));
            return negative ? One / result : result;
        }

        if (x.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        return Exp(y * Ln(x));
    }

    public static BigNum IntPow(BigNum x, long n)
    {
        var result = One;
        var b = x;
        while (n > 0)
        {
            if ((n & 1) == 1)
            {
                result *= b;
            }

            n >>= 1;
            if (n > 0)
            {
                b *= b;
            }
        }

        return result;
    }

    public static BigNum Factorial(long n)
    {
        BigInteger r = 1;
        for (long i = 2; i <= n; i++)
        {
            r *= i;
        }

        return new BigNum(r, 0).Round(Precision + Guard);
    }

    // Bernoulli numbers B2, B4, ..., B30 as numerator/denominator pairs.
    private static readonly (long Num, long Den)[] Bernoulli =
    [
        (1, 6), (-1, 30), (1, 42), (-1, 30), (5, 66), (-691, 2730), (7, 6), (-3617, 510),
        (43867, 798), (-174611, 330), (854513, 138), (-236364091, 2730), (8553103, 6),
        (-23749461029, 870), (8615841276005, 14322),
    ];

    /// <summary>The gamma function for positive real arguments (Stirling series with a shift).</summary>
    public static BigNum Gamma(BigNum x)
    {
        if (x.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        // Shift the argument up so the asymptotic series converges quickly.
        var y = x;
        var product = One;
        var threshold = FromInt(60);
        while (y < threshold)
        {
            product *= y;
            y += One;
        }

        var lnY = Ln(y);
        var sum = ((y - (One / Two)) * lnY) - y + (Ln(Pi * Two) / Two);
        var yPow = y;                       // y^(2k-1)
        var ySquared = y * y;
        for (var k = 1; k <= Bernoulli.Length; k++)
        {
            var (num, den) = Bernoulli[k - 1];
            var coefficient = FromInt(num) / (FromInt(den) * FromInt(2 * k) * FromInt((2 * k) - 1));
            sum += coefficient / yPow;
            yPow *= ySquared;
        }

        return Exp(sum) / product;
    }

    public static BigNum Sin(BigNum x)
    {
        var r = ReduceRadians(x);
        var r2 = r * r;
        var term = r;
        var sum = r;
        for (var n = 1; n < 200; n++)
        {
            term = -(term * r2) / FromInt((2 * n) * ((2 * n) + 1));
            sum += term;
            if (term.IsZero || term.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        return sum;
    }

    public static BigNum Cos(BigNum x)
    {
        var r = ReduceRadians(x);
        var r2 = r * r;
        var term = One;
        var sum = One;
        for (var n = 1; n < 200; n++)
        {
            term = -(term * r2) / FromInt(((2 * n) - 1) * (2 * n));
            sum += term;
            if (term.IsZero || term.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        return sum;
    }

    public static BigNum Atan(BigNum x)
    {
        if (x.IsZero)
        {
            return Zero;
        }

        if (x.Sign < 0)
        {
            return -Atan(-x);
        }

        if (x > One)
        {
            return (Pi / Two) - Atan(One / x);
        }

        // Halve the argument three times: atan x = 2 atan(x / (1 + sqrt(1 + x^2))).
        var halvings = 4;
        var a = x;
        for (var i = 0; i < halvings; i++)
        {
            a = a / (One + Sqrt(One + (a * a)));
        }

        var a2 = a * a;
        var term = a;
        var sum = a;
        for (var n = 1; n < 400; n++)
        {
            term = -(term * a2);
            var t = term / FromInt((2 * n) + 1);
            sum += t;
            if (t.IsZero || t.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        return sum * Pow2(halvings);
    }

    public static BigNum Asin(BigNum x)
    {
        var abs = x.Abs();
        if (abs > One)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (abs == One)
        {
            return x.Sign > 0 ? Pi / Two : -(Pi / Two);
        }

        return Atan(x / Sqrt(One - (x * x)));
    }

    public static BigNum Acos(BigNum x) => (Pi / Two) - Asin(x);

    public static BigNum Sinh(BigNum x) => (Exp(x) - Exp(-x)) / Two;

    public static BigNum Cosh(BigNum x) => (Exp(x) + Exp(-x)) / Two;

    public static BigNum Tanh(BigNum x)
    {
        var a = Exp(x);
        var b = Exp(-x);
        return (a - b) / (a + b);
    }

    public static BigNum Asinh(BigNum x) =>
        x.Sign < 0 ? -Asinh(-x) : Ln(x + Sqrt((x * x) + One));

    public static BigNum Acosh(BigNum x) => Ln(x + Sqrt((x * x) - One));

    public static BigNum Atanh(BigNum x) => Ln((One + x) / (One - x)) / Two;

    // ---- formatting ---------------------------------------------------------

    /// <summary>Plain decimal notation with no grouping and no exponent.</summary>
    public string ToPlainString()
    {
        if (IsZero)
        {
            return "0";
        }

        var digits = BigInteger.Abs(Mantissa).ToString(CultureInfo.InvariantCulture);
        string text;
        if (Exponent >= 0)
        {
            text = digits + new string('0', Exponent);
        }
        else if (-Exponent >= digits.Length)
        {
            text = "0." + new string('0', -Exponent - digits.Length) + digits;
        }
        else
        {
            text = digits[..(digits.Length + Exponent)] + "." + digits[(digits.Length + Exponent)..];
        }

        return (Sign < 0 ? "-" : string.Empty) + text;
    }

    public override string ToString() => ToPlainString();

    // ---- helpers ------------------------------------------------------------

    private static int DigitCount(BigInteger value)
    {
        value = BigInteger.Abs(value);
        if (value < 10)
        {
            return 1;
        }

        var estimate = (int)Math.Floor(BigInteger.Log10(value)) + 1;
        var power = BigInteger.Pow(10, estimate - 1);
        if (value < power)
        {
            estimate--;
        }
        else if (value >= power * 10)
        {
            estimate++;
        }

        return estimate;
    }

    private static BigNum Pow2(int n) => IntPow(Two, n);

    private static BigNum AtanhSeries(BigNum z)
    {
        var z2 = z * z;
        var term = z;
        var sum = z;
        for (var n = 3; n < 600; n += 2)
        {
            term *= z2;
            var t = term / FromInt(n);
            sum += t;
            if (t.IsZero || t.Magnitude < -(Precision + Guard + 5))
            {
                break;
            }
        }

        return sum;
    }

    private static BigNum ReduceRadians(BigNum x)
    {
        var twoPi = Pi * Two;
        var k = (x / twoPi).Round(Precision + Guard);
        var whole = k.Floor();
        var r = x - (whole * twoPi);
        if (r > Pi)
        {
            r -= twoPi;
        }

        return r;
    }

    private static BigInteger ISqrt(BigInteger n)
    {
        if (n.IsZero)
        {
            return BigInteger.Zero;
        }

        var x = BigInteger.One << (int)((n.GetBitLength() + 1) / 2);
        while (true)
        {
            var y = (x + (n / x)) >> 1;
            if (y >= x)
            {
                return x;
            }

            x = y;
        }
    }
}
