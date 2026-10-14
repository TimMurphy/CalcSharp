using System.Globalization;
using System.Text;

namespace CalcSharp.Core;

public enum Operation
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

/// <summary>
/// State machine modelling the behaviour of the Windows Calculator "Standard" mode.
/// Behaviour was matched against results recorded from the real app (see tools/Record-StandardOracle.ps1).
/// </summary>
public sealed class StandardCalculator
{
    public const int MaxDigits = 16;

    private const string DivideByZero = "Cannot divide by zero";
    private const string Undefined = "Result is undefined";
    private const string Invalid = "Invalid input";

    private readonly StringBuilder _entry = new("0");

    private decimal _value;              // value of the current operand when not being typed
    private decimal _acc;                // left operand of the pending operation
    private Operation? _pending;
    private bool _entering = true;       // the user is typing a number
    private bool _opJustPressed;         // an operator was pressed and no operand has been given since
    private bool _justEvaluated;         // '=' was the last binary-affecting key
    private bool _bareEquals;            // '=' was pressed with nothing to evaluate
    private Operation? _lastOp;          // for repeated '='
    private decimal _lastRight;
    private string _leftText = string.Empty;
    private string _prefix = string.Empty;        // e.g. "5 + "
    private string? _operandText;        // text of the current operand when produced by a function
    private string? _shownOperandText;   // what the expression line shows for the operand
    private string? _finalExpr;          // expression text after '=' (including the '=')
    private bool _error;
    private decimal _memory;
    private bool _hasMemory;

    public string Display { get; private set; } = "0";

    public string Expression => _finalExpr ?? (_prefix + _shownOperandText).TrimEnd();

    public bool HasMemory => _hasMemory;

    public bool IsError => _error;

    // ---- number entry ----------------------------------------------------

    public void Digit(int digit)
    {
        if (digit is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        BeginEntry();
        if (_entry.ToString().Count(char.IsDigit) >= MaxDigits)
        {
            return;
        }

        var text = _entry.ToString();
        if (text == "0")
        {
            _entry.Clear();
        }
        else if (text == "-0")
        {
            _entry.Clear().Append('-');
        }

        _entry.Append(digit);
        UpdateEntryDisplay();
    }

    public void DecimalPoint()
    {
        BeginEntry();
        if (_entry.ToString().Contains('.'))
        {
            return;
        }

        _entry.Append('.');
        UpdateEntryDisplay();
    }

    public void Backspace()
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            // Backspace on a result only discards the expression.
            if (!_bareEquals)
            {
                _finalExpr = null;
                _prefix = string.Empty;
                _shownOperandText = _operandText = null;
            }

            return;
        }

        if (!_entering)
        {
            return;
        }

        var text = _entry.ToString();
        var negative = text.StartsWith('-');
        var body = negative ? text[1..] : text;
        body = body.Length <= 1 ? "0" : body[..^1];
        if (body == "0")
        {
            negative = false;
        }

        _entry.Clear().Append(negative ? "-" : string.Empty).Append(body);
        UpdateEntryDisplay();
    }

    public void Negate()
    {
        if (_error)
        {
            return;
        }

        if (_entering)
        {
            var text = _entry.ToString();
            if (text != "0")
            {
                _entry.Clear().Append(text.StartsWith('-') ? text[1..] : "-" + text);
                UpdateEntryDisplay();
            }

            return;
        }

        Unary(v => -v, inner => $"negate({inner})");
    }

    public void ClearEntry()
    {
        if (_error)
        {
            Clear();
            return;
        }

        _entry.Clear().Append('0');
        _entering = true;
        _value = 0;
        _opJustPressed = false;
        _operandText = null;
        if (_justEvaluated)
        {
            if (!_bareEquals)
            {
                _finalExpr = null;
                _prefix = string.Empty;
            }

            _pending = null;
            _justEvaluated = false;
        }

        Display = "0";
    }

    public void Clear()
    {
        _entry.Clear().Append('0');
        _acc = _value = _lastRight = 0;
        _pending = _lastOp = null;
        _entering = true;
        _opJustPressed = _justEvaluated = _bareEquals = _error = false;
        _leftText = _prefix = string.Empty;
        _operandText = _shownOperandText = _finalExpr = null;
        Display = "0";
    }

    // ---- binary operators --------------------------------------------------

    public void Operator(Operation op)
    {
        if (_error)
        {
            return;
        }

        if (_pending is not null && _opJustPressed)
        {
            // Operator replaced before a second operand was given.
            _pending = op;
            _prefix = _leftText + " " + Symbol(op) + " ";
            return;
        }

        var current = CurrentValue();

        if (_justEvaluated)
        {
            _acc = current;
            _leftText = Format(current, false);
        }
        else if (_pending is { } pending)
        {
            var rightText = _operandText ?? Format(current, false);
            if (!Compute(_acc, current, pending, out var result))
            {
                _prefix += rightText + " " + Symbol(op) + " ";
                _shownOperandText = null;
                return;
            }

            _acc = result;
            _leftText = Format(result, false);
            SetDisplayValue(result);
        }
        else
        {
            _acc = current;
            _leftText = _operandText ?? Format(current, false);
        }

        _pending = op;
        _prefix = _leftText + " " + Symbol(op) + " ";
        _operandText = _shownOperandText = _finalExpr = null;
        _entering = false;
        _justEvaluated = _bareEquals = false;
        _opJustPressed = true;
        _value = _acc;
    }

    public void Evaluate()
    {
        if (_error)
        {
            Clear();
            return;
        }

        var current = CurrentValue();

        if (_pending is { } pending && !_justEvaluated)
        {
            var right = _opJustPressed ? _acc : current;
            var rightText = _operandText ?? Format(right, false);
            if (!Compute(_acc, right, pending, out var result))
            {
                return;
            }

            Finish(result, _prefix + rightText + "=", pending, right);
        }
        else if (_lastOp is { } last)
        {
            if (!Compute(current, _lastRight, last, out var result))
            {
                return;
            }

            Finish(
                result,
                Format(current, false) + " " + Symbol(last) + " " + Format(_lastRight, false) + "=",
                last,
                _lastRight);
        }
        else
        {
            _finalExpr = (_operandText ?? Format(current, false)) + "=";
            _operandText = _shownOperandText = null;
            _acc = _value = current;
            _entering = false;
            _justEvaluated = true;
            _bareEquals = true;
            _opJustPressed = false;
            SetDisplayValue(current);
        }
    }

    // ---- unary functions ---------------------------------------------------

    public void Percent()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        var result = _pending switch
        {
            Operation.Add or Operation.Subtract when !_justEvaluated => _acc * current / 100m,
            Operation.Multiply or Operation.Divide when !_justEvaluated => current / 100m,
            _ => _acc * current / 100m,
        };

        PrepareForFunction();
        _operandText = _shownOperandText = Format(result, false);
        SetResult(result);
    }

    public void SquareRoot()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        if (current < 0)
        {
            PrepareForFunction();
            _operandText = _shownOperandText = $"√({InnerText(current)})";
            Fail(Invalid);
            return;
        }

        Unary(Sqrt, inner => $"√({inner})");
    }

    public void Square() => Unary(v => v * v, inner => $"square ({inner})");

    public void Reciprocal()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        if (current == 0)
        {
            PrepareForFunction();
            _operandText = _shownOperandText = $"1/({InnerText(current)})";
            Fail(DivideByZero);
            return;
        }

        Unary(v => 1m / v, inner => $"1/({inner})");
    }

    // ---- memory ------------------------------------------------------------

    public void MemoryClear()
    {
        if (!_hasMemory)
        {
            // The button is disabled while memory is empty.
            return;
        }

        _memory = 0;
        _hasMemory = false;
        CommitEntry();
    }

    public void MemoryRecall()
    {
        if (_error || !_hasMemory)
        {
            return;
        }

        PrepareForFunction();
        _operandText = _shownOperandText = null;
        SetResult(_memory);
    }

    public void MemoryStore()
    {
        if (_error)
        {
            return;
        }

        _memory = CurrentValue();
        _hasMemory = true;
        CommitEntry();
    }

    public void MemoryAdd()
    {
        if (_error)
        {
            return;
        }

        _memory += CurrentValue();
        _hasMemory = true;
        CommitEntry();
    }

    public void MemorySubtract()
    {
        if (_error)
        {
            return;
        }

        _memory -= CurrentValue();
        _hasMemory = true;
        CommitEntry();
    }

    // ---- internals ---------------------------------------------------------

    /// <summary>Ends number entry: the typed text becomes a value and is redisplayed normalised.</summary>
    private void CommitEntry()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        _value = current;
        _entering = false;
        SetDisplayValue(current);
    }

    private void BeginEntry()
    {
        if (_error)
        {
            Clear();
        }

        if (_justEvaluated)
        {
            if (!_bareEquals)
            {
                _finalExpr = null;
                _prefix = string.Empty;
                _shownOperandText = null;
            }

            _pending = null;
            _justEvaluated = false;
            _entering = false;
        }

        if (!_entering)
        {
            _entry.Clear().Append('0');
            _entering = true;
        }

        _opJustPressed = false;
        _operandText = null;
        if (_pending is not null)
        {
            _shownOperandText = null;
        }
    }

    private void Finish(decimal result, string expression, Operation op, decimal right)
    {
        _lastOp = op;
        _lastRight = right;
        _pending = null;
        _finalExpr = expression;
        _prefix = string.Empty;
        _operandText = _shownOperandText = null;
        _entering = false;
        _justEvaluated = true;
        _bareEquals = false;
        _opJustPressed = false;
        _acc = _value = result;
        SetDisplayValue(result);
    }

    /// <summary>Applies a single-operand function to the current operand.</summary>
    private void Unary(Func<decimal, decimal> function, Func<string, string> text)
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        var inner = InnerText(current);
        decimal result;
        try
        {
            result = function(current);
        }
        catch (OverflowException)
        {
            PrepareForFunction();
            _operandText = _shownOperandText = text(inner);
            Fail("Overflow");
            return;
        }

        PrepareForFunction();
        _operandText = _shownOperandText = text(inner);
        SetResult(result);
    }

    private string InnerText(decimal current) => _operandText ?? Format(current, false);

    /// <summary>Called before a function result replaces the current operand.</summary>
    private void PrepareForFunction()
    {
        _finalExpr = null;
        if (_justEvaluated)
        {
            _prefix = string.Empty;
            _pending = null;
            _justEvaluated = false;
            _bareEquals = false;
        }
    }

    private void SetResult(decimal value)
    {
        _value = value;
        _entering = false;
        _opJustPressed = false;
        SetDisplayValue(value);
    }

    private decimal CurrentValue() =>
        _entering ? decimal.Parse(_entry.ToString().TrimEnd('.'), CultureInfo.InvariantCulture) : _value;

    private void UpdateEntryDisplay()
    {
        var text = _entry.ToString();
        var negative = text.StartsWith('-');
        var body = negative ? text[1..] : text;
        var dot = body.IndexOf('.');
        var intPart = dot < 0 ? body : body[..dot];
        var grouped = decimal.Parse(intPart, CultureInfo.InvariantCulture)
            .ToString("#,##0", CultureInfo.InvariantCulture);
        Display = (negative ? "-" : string.Empty) + grouped + (dot < 0 ? string.Empty : body[dot..]);
        _value = CurrentValue();
    }

    private void SetDisplayValue(decimal value) => Display = Format(value, true);

    private static decimal Sqrt(decimal value)
    {
        if (value == 0)
        {
            return 0;
        }

        var x = (decimal)Math.Sqrt((double)value);
        for (var i = 0; i < 4; i++)
        {
            x = (x + value / x) / 2m;
        }

        return x;
    }

    private static char Symbol(Operation op) => op switch
    {
        Operation.Add => '+',
        Operation.Subtract => '−',
        Operation.Multiply => '×',
        Operation.Divide => '÷',
        _ => '?',
    };

    private bool Compute(decimal left, decimal right, Operation op, out decimal result)
    {
        result = 0;
        try
        {
            switch (op)
            {
                case Operation.Add:
                    result = left + right;
                    return true;
                case Operation.Subtract:
                    result = left - right;
                    return true;
                case Operation.Multiply:
                    result = left * right;
                    return true;
                case Operation.Divide:
                    if (right == 0)
                    {
                        Fail(left == 0 ? Undefined : DivideByZero);
                        return false;
                    }

                    result = left / right;
                    return true;
            }
        }
        catch (OverflowException)
        {
            Fail("Overflow");
            return false;
        }

        return false;
    }

    private void Fail(string message)
    {
        _error = true;
        Display = message;
        _entering = false;
        _pending = null;
        _justEvaluated = false;
        _opJustPressed = false;
        _finalExpr = null;
    }

    // ---- number formatting -------------------------------------------------

    /// <summary>
    /// Formats a value like the real calculator: up to 16 significant digits, plain notation for
    /// values that fit, otherwise exponent notation ("3.333333333333333e-4", "1.e+16").
    /// </summary>
    internal static string Format(decimal value, bool group)
    {
        if (value == 0)
        {
            return "0";
        }

        var negative = value < 0;
        var a = Math.Abs(value);
        var exponent = Exponent10(a);

        var rounded = Math.Round(a, Math.Clamp(15 - exponent, 0, 28), MidpointRounding.AwayFromZero);
        var reexponent = Exponent10(rounded);
        if (reexponent != exponent)
        {
            exponent = reexponent;
            a = rounded;
        }

        string text;
        if (exponent >= MaxDigits)
        {
            text = Scientific(a, exponent);
        }
        else if (exponent >= -3)
        {
            var decimals = Math.Min(16, 15 - exponent);
            text = Math.Round(a, decimals, MidpointRounding.AwayFromZero)
                .ToString("0." + new string('#', Math.Max(decimals, 1)), CultureInfo.InvariantCulture);
        }
        else
        {
            var places = Math.Min(28, 15 - exponent);
            var r = Math.Round(a, places, MidpointRounding.AwayFromZero);
            var plain = r.ToString("0." + new string('#', places), CultureInfo.InvariantCulture);
            var dot = plain.IndexOf('.');
            var decimalsNeeded = dot < 0 ? 0 : plain.Length - dot - 1;
            text = decimalsNeeded <= 16 && places <= 28 && exponent > -29
                ? plain
                : Scientific(a, exponent);
        }

        if (group && !text.Contains('e'))
        {
            var dot = text.IndexOf('.');
            var integer = dot < 0 ? text : text[..dot];
            var grouped = decimal.Parse(integer, CultureInfo.InvariantCulture)
                .ToString("#,##0", CultureInfo.InvariantCulture);
            text = grouped + (dot < 0 ? string.Empty : text[dot..]);
        }

        return (negative ? "-" : string.Empty) + text;
    }

    private static string Scientific(decimal a, int exponent)
    {
        var mantissa = exponent >= 0 ? a / Pow10(exponent) : a * Pow10(-exponent);
        var digits = Math.Round(mantissa, 15, MidpointRounding.AwayFromZero)
            .ToString("0.###############", CultureInfo.InvariantCulture);
        if (!digits.Contains('.'))
        {
            digits += ".";
        }

        return digits + "e" + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }

    private static int Exponent10(decimal a)
    {
        var e = (int)Math.Floor(Math.Log10((double)a));
        if (e >= 0 ? a < Pow10(e) : a < 1m / Pow10(-e))
        {
            e--;
        }
        else if (e >= -28 && e < 28 && a >= (e + 1 >= 0 ? Pow10(e + 1) : 1m / Pow10(-(e + 1))))
        {
            e++;
        }

        return e;
    }

    private static decimal Pow10(int n)
    {
        var r = 1m;
        for (var i = 0; i < n; i++)
        {
            r *= 10m;
        }

        return r;
    }
}
