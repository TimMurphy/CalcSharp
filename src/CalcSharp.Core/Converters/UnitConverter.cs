using System.Globalization;

namespace CalcSharp.Core.Converters;

/// <summary>
/// Two-field converter model. The active field receives keypad input; the other shows the converted result.
/// </summary>
public sealed class UnitConverter
{
    public const int MaxDigits = 16;

    private readonly string[] _text = ["0", "0"];
    private readonly ConverterUnit[] _units = new ConverterUnit[2];

    public UnitConverter(string category)
    {
        Category = ConverterCatalog.Get(category);
        _units[0] = Category.Units[Category.DefaultFrom];
        _units[1] = Category.Units[Category.DefaultTo];
        Recompute();
    }

    public ConverterCategory Category { get; }

    /// <summary>0 or 1: the field that is receiving input.</summary>
    public int ActiveField { get; private set; }

    public ConverterUnit FromUnit => _units[0];

    public ConverterUnit ToUnit => _units[1];

    /// <summary>Raw text of the active field (ungrouped).</summary>
    public string InputText => _text[ActiveField];

    public string FromDisplay => Display(0);

    public string ToDisplay => Display(1);

    public bool IsCurrency => Category.Name == "Currency";

    public string? StatusText => IsCurrency ? CurrencyRates.StatusText : null;

    /// <summary>Parsed numeric value of the active field.</summary>
    public double InputValue => Parse(_text[ActiveField]);

    public string Display(int field) =>
        field == ActiveField ? NumberFormatter.Group(_text[field]) : _text[field];

    public ConverterUnit GetUnit(int field) => _units[field];

    /// <summary>Makes a field the input field; its displayed value becomes the typed value.</summary>
    public void SetActiveField(int field)
    {
        if (field == ActiveField)
        {
            return;
        }

        ActiveField = field;
        _text[field] = _text[field].Replace(",", "");
        Recompute();
    }

    public void SetUnit(int field, ConverterUnit unit)
    {
        _units[field] = unit;
        Recompute();
    }

    public void Digit(int d)
    {
        var t = _text[ActiveField];
        if (t == "0")
        {
            t = "";
        }
        else if (t == "-0")
        {
            t = "-";
        }

        if (t.Count(char.IsAsciiDigit) >= MaxDigits)
        {
            return;
        }

        _text[ActiveField] = t + (char)('0' + d);
        Recompute();
    }

    public void DecimalPoint()
    {
        var t = _text[ActiveField];
        if (!t.Contains('.'))
        {
            _text[ActiveField] = t + ".";
            Recompute();
        }
    }

    public void Negate()
    {
        var t = _text[ActiveField];
        if (t == "0")
        {
            return;
        }

        _text[ActiveField] = t.StartsWith('-') ? t[1..] : "-" + t;
        Recompute();
    }

    public void Backspace()
    {
        var t = _text[ActiveField];
        t = t.Length <= 1 ? "0" : t[..^1];
        if (t is "-" or "")
        {
            t = "0";
        }

        _text[ActiveField] = t;
        Recompute();
    }

    public void Clear()
    {
        _text[ActiveField] = "0";
        Recompute();
    }

    private static double Parse(string t) =>
        double.TryParse(t.TrimEnd('.').Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : 0;

    private void Recompute()
    {
        var other = 1 - ActiveField;
        var result = Category.Convert(InputValue, _units[ActiveField], _units[other]);
        _text[other] = NumberFormatter.Format(result);
    }
}
