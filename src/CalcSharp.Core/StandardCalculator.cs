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
/// State machine modelling the behaviour of a "Standard" mode desktop calculator.
/// </summary>
public sealed class StandardCalculator
{
    public const int MaxDigits = 16;

    private const string DivideByZero = "Cannot divide by zero";
    private const string Undefined = "Result is undefined";
    private const string Invalid = "Invalid input";

    private readonly StringBuilder _entry = new("0");
    private readonly StringBuilder _expression = new();

    private decimal _accumulator;
    private decimal _value;
    private Operation? _pending;
    private decimal _lastOperand;
    private Operation? _lastOperation;
    private bool _entering = true;   // user is typing a number
    private bool _justEvaluated;
    private bool _error;
    private decimal _memory;
    private bool _hasMemory;

    public string Display { get; private set; } = "0";

    public string Expression => _expression.ToString().TrimEnd();

    public bool HasMemory => _hasMemory;

    public bool IsError => _error;

    public void Digit(int digit)
    {
        if (digit is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        if (!BeginEntry())
        {
            return;
        }

        if (DigitCount() >= MaxDigits)
        {
            return;
        }

        if (_entry.ToString() is "0")
        {
            _entry.Clear();
        }
        else if (_entry.ToString() is "-0")
        {
            _entry.Clear().Append('-');
        }

        _entry.Append(digit);
        UpdateEntryDisplay();
    }

    public void DecimalPoint()
    {
        if (!BeginEntry())
        {
            return;
        }

        if (_entry.ToString().Contains('.'))
        {
            return;
        }

        _entry.Append('.');
        UpdateEntryDisplay();
    }

    public void Backspace()
    {
        if (_error || !_entering || _justEvaluated)
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

        _entry.Clear().Append(negative ? "-" : "").Append(body);
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
            _entry.Clear().Append(text.StartsWith('-') ? text[1..] : "-" + text);
            UpdateEntryDisplay();
            return;
        }

        SetValue(-_value);
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
        if (_justEvaluated)
        {
            _expression.Clear();
            _pending = null;
            _justEvaluated = false;
        }

        Display = "0";
    }

    public void Clear()
    {
        _entry.Clear().Append('0');
        _expression.Clear();
        _accumulator = _value = _lastOperand = 0;
        _pending = _lastOperation = null;
        _entering = true;
        _justEvaluated = _error = false;
        Display = "0";
    }

    public void Operator(Operation op)
    {
        if (_error)
        {
            return;
        }

        if (_pending is not null && _operatorJustPressed)
        {
            // Operator replaced before a second operand was typed.
            _expression.Length -= 2;
            _expression.Append(Symbol(op)).Append(' ');
            _pending = op;
            return;
        }

        var current = CurrentValue();

        if (_justEvaluated)
        {
            _expression.Clear();
        }

        if (_pending is { } pending && !_justEvaluated)
        {
            if (!Compute(_accumulator, current, pending, out var result))
            {
                return;
            }

            _accumulator = result;
            SetDisplayValue(result);
            _expression.Append(Fmt(current)).Append(' ');
        }
        else
        {
            _accumulator = current;
            _expression.Append(Fmt(current)).Append(' ');
        }

        _expression.Append(Symbol(op)).Append(' ');
        _pending = op;
        _value = _accumulator;
        _entering = false;
        _justEvaluated = false;
        _operatorJustPressed = true;
    }

    public void Evaluate()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        decimal left;
        Operation op;
        decimal right;

        if (_pending is { } pending && !_justEvaluated)
        {
            left = _accumulator;
            op = pending;
            right = _operatorJustPressed ? _accumulator : current;
            _expression.Append(Fmt(right)).Append(" =");
        }
        else if (_lastOperation is { } last)
        {
            left = current;
            op = last;
            right = _lastOperand;
            _expression.Clear().Append(Fmt(left)).Append(' ').Append(Symbol(op)).Append(' ').Append(Fmt(right)).Append(" =");
        }
        else
        {
            _expression.Clear().Append(Fmt(current)).Append(" =");
            _justEvaluated = true;
            _entering = false;
            _value = current;
            return;
        }

        if (!Compute(left, right, op, out var result))
        {
            return;
        }

        _lastOperation = op;
        _lastOperand = right;
        _pending = null;
        _entering = false;
        _justEvaluated = true;
        _operatorJustPressed = false;
        _value = result;
        _accumulator = result;
        SetDisplayValue(result);
    }

    public void Percent()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        var result = _pending is not null ? _accumulator * current / 100m : 0m;
        SetValue(result);
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
            Fail(Invalid);
            return;
        }

        SetValue((decimal)Math.Sqrt((double)current));
    }

    public void Square()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        if (!TryMultiply(current, current, out var result))
        {
            Fail("Overflow");
            return;
        }

        SetValue(result);
    }

    public void Reciprocal()
    {
        if (_error)
        {
            return;
        }

        var current = CurrentValue();
        if (current == 0)
        {
            Fail(DivideByZero);
            return;
        }

        SetValue(1m / current);
    }

    public void MemoryClear()
    {
        _memory = 0;
        _hasMemory = false;
    }

    public void MemoryRecall()
    {
        if (_error || !_hasMemory)
        {
            return;
        }

        SetValue(_memory);
        _entering = false;
    }

    public void MemoryStore()
    {
        if (_error)
        {
            return;
        }

        _memory = CurrentValue();
        _hasMemory = true;
        _entering = false;
    }

    public void MemoryAdd()
    {
        if (_error)
        {
            return;
        }

        _memory += CurrentValue();
        _hasMemory = true;
        _entering = false;
    }

    public void MemorySubtract()
    {
        if (_error)
        {
            return;
        }

        _memory -= CurrentValue();
        _hasMemory = true;
        _entering = false;
    }

    // ---- internals -------------------------------------------------------

    private bool _operatorJustPressed;

    private bool BeginEntry()
    {
        if (_error)
        {
            Clear();
        }

        if (_justEvaluated)
        {
            _expression.Clear();
            _justEvaluated = false;
            _pending = null;
            _entering = false;
        }

        if (!_entering)
        {
            _entry.Clear().Append('0');
            _entering = true;
        }

        _operatorJustPressed = false;
        return true;
    }

    private int DigitCount() => _entry.ToString().Count(char.IsDigit);

    private decimal CurrentValue()
    {
        if (_entering)
        {
            return decimal.Parse(_entry.ToString().TrimEnd('.'), CultureInfo.InvariantCulture);
        }

        return _value;
    }

    private void UpdateEntryDisplay()
    {
        var text = _entry.ToString();
        var negative = text.StartsWith('-');
        var body = negative ? text[1..] : text;
        var dot = body.IndexOf('.');
        var intPart = dot < 0 ? body : body[..dot];
        var grouped = decimal.Parse(intPart, CultureInfo.InvariantCulture)
            .ToString("#,##0", CultureInfo.InvariantCulture);
        Display = (negative ? "-" : "") + grouped + (dot < 0 ? "" : body[dot..]);
        _value = CurrentValue();
    }

    private void SetValue(decimal value)
    {
        _value = value;
        _entering = false;
        SetDisplayValue(value);
        _operatorJustPressed = false;
    }

    private void SetDisplayValue(decimal value) => Display = FormatDisplay(value);

    private static string FormatDisplay(decimal value)
    {
        value = RoundSignificant(value, MaxDigits);
        var text = value.ToString("#,##0.################", CultureInfo.InvariantCulture);
        if (text.Count(char.IsDigit) > MaxDigits + 1 || Math.Abs(value) >= 1e16m)
        {
            return value.ToString("0.###############e+0", CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string Fmt(decimal value) =>
        RoundSignificant(value, MaxDigits).ToString("0.################", CultureInfo.InvariantCulture);

    private static decimal RoundSignificant(decimal value, int digits)
    {
        if (value == 0)
        {
            return 0;
        }

        var magnitude = (int)Math.Floor(Math.Log10((double)Math.Abs(value))) + 1;
        var decimals = digits - magnitude;
        if (decimals < 0)
        {
            return value;
        }

        return Math.Round(value, Math.Min(decimals, 28), MidpointRounding.AwayFromZero);
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

    private static bool TryMultiply(decimal a, decimal b, out decimal result)
    {
        try
        {
            result = a * b;
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private void Fail(string message)
    {
        _error = true;
        Display = message;
        _entering = false;
        _pending = null;
        _justEvaluated = false;
        _expression.Clear();
    }
}
