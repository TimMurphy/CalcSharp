using System.Globalization;
using System.Text;

namespace CalcSharp.Core.Converters;

/// <summary>Formats converter values: rounded significant digits, grouping, trailing zeros trimmed.</summary>
public static class NumberFormatter
{
    /// <summary>Significant digits kept in a converted value.</summary>
    public const int SignificantDigits = 10;

    public static string Format(double value, bool group = true)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "Overflow";
        }

        if (value == 0)
        {
            return "0";
        }

        var sci = value.ToString("E" + (SignificantDigits - 1), CultureInfo.InvariantCulture);
        var rounded = double.Parse(sci, CultureInfo.InvariantCulture);
        var exponent = (int)Math.Floor(Math.Log10(Math.Abs(rounded)) + 1e-12);

        if (exponent < -7 || exponent >= 16)
        {
            var mantissa = rounded / Math.Pow(10, exponent);
            var m = mantissa.ToString("0.#########", CultureInfo.InvariantCulture);
            return $"{m}e{(exponent < 0 ? "-" : "+")}{Math.Abs(exponent)}";
        }

        var text = rounded.ToString("0.####################", CultureInfo.InvariantCulture);
        if (text == "-0")
        {
            return "0";
        }

        return group ? Group(text) : text;
    }

    /// <summary>Inserts thousands separators into the integer part of a plain numeric string.</summary>
    public static string Group(string text)
    {
        if (text.Contains(',') || text.Contains('e'))
        {
            return text;
        }

        var negative = text.StartsWith('-');
        var body = negative ? text[1..] : text;
        var dot = body.IndexOf('.');
        var integer = dot < 0 ? body : body[..dot];
        var fraction = dot < 0 ? "" : body[dot..];

        var sb = new StringBuilder();
        for (var i = 0; i < integer.Length; i++)
        {
            if (i > 0 && (integer.Length - i) % 3 == 0)
            {
                sb.Append(',');
            }

            sb.Append(integer[i]);
        }

        return (negative ? "-" : "") + sb + fraction;
    }
}
