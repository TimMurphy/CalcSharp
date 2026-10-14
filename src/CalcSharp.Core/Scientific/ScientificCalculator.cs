using System.Globalization;
using System.Text;

namespace CalcSharp.Core.Scientific;

/// <summary>Binary operators of the Scientific calculator (evaluated with precedence).</summary>
public enum ScientificOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Mod,
    /// <summary>x^y</summary>
    Power,
    /// <summary>y-th root of x (the "ʸ√x" key).</summary>
    Root,
    /// <summary>log base y of x (the 2nd "logᵧx" key).</summary>
    LogBase,
}

public enum AngleMode
{
    Degrees,
    Radians,
    Gradians,
}

public enum TrigFunction
{
    Sin,
    Cos,
    Tan,
    Sec,
    Csc,
    Cot,
}

/// <summary>
/// State machine modelling the "Scientific" mode of a desktop calculator: expression entry with
/// operator precedence and parentheses, scientific functions, angle modes and memory.
/// </summary>
public sealed class ScientificCalculator
{
    public const int MaxDigits = 16;

    private const string DivideByZero = "Cannot divide by zero";
    private const string Undefined = "Result is undefined";
    private const string Invalid = "Invalid input";
    private const string OverflowMessage = "Overflow";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly StringBuilder _entry = new("0");
    private readonly StringBuilder _expr = new();
    private readonly Stack<double> _operands = new();
    private readonly Stack<ScientificOperator?> _ops = new();   // null = open parenthesis
    private readonly Stack<int> _parenStarts = new();

    private double _value;               // the value currently shown (when not entering)
    private string? _operandText;        // expression text of the current operand if it came from a function
    private bool _operandInExpr;         // current operand is a closed "( ... )" group already in the expression
    private int _groupStart;
    private int _opTextLength;           // length of the last operator text appended to the expression
    private bool _entering = true;
    private bool _afterOperator;
    private bool _justEvaluated;
    private bool _error;
    private ScientificOperator? _lastOp;
    private double _lastRight;
    private double _memory;
    private bool _hasMemory;
    private bool _fixedExponent;
    private string? _failure;

    public string Display { get; private set; } = "0";

    public string Expression => _expr.ToString().TrimEnd();

    public bool HasMemory => _hasMemory;

    public double MemoryValue => _memory;

    public bool IsError => _error;

    public int OpenParenCount => _parenStarts.Count;

    /// <summary>The "2nd" toggle.</summary>
    public bool Inverse { get; set; }

    /// <summary>The "hyp" toggle.</summary>
    public bool Hyperbolic { get; set; }

    public AngleMode AngleMode { get; set; } = AngleMode.Degrees;

    /// <summary>The "F-E" toggle: always show the value in exponential notation.</summary>
    public bool FixedExponent
    {
        get => _fixedExponent;
        set
        {
            _fixedExponent = value;
            if (!_entering && !_error)
            {
                Display = Format(_value, true, _fixedExponent);
            }
        }
    }

    public void ToggleInverse() => Inverse = !Inverse;

    public void ToggleHyperbolic() => Hyperbolic = !Hyperbolic;

    public void ToggleFixedExponent() => FixedExponent = !FixedExponent;

    public void CycleAngleMode() =>
        AngleMode = AngleMode switch
        {
            AngleMode.Degrees => AngleMode.Radians,
            AngleMode.Radians => AngleMode.Gradians,
            _ => AngleMode.Degrees,
        };

    // ---- entry -----------------------------------------------------------

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

        var text = _entry.ToString();
        var e = text.IndexOf('e');
        if (e >= 0)
        {
            var expDigits = text[(e + 2)..];
            if (expDigits == "0")
            {
                _entry.Length = e + 2;
                _entry.Append(digit);
            }
            else if (expDigits.Length < 4)
            {
                _entry.Append(digit);
            }

            UpdateEntryDisplay();
            return;
        }

        if (text.Count(char.IsDigit) >= MaxDigits)
        {
            return;
        }

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
        if (!BeginEntry() || _entry.ToString().Contains('.') || _entry.ToString().Contains('e'))
        {
            return;
        }

        _entry.Append('.');
        UpdateEntryDisplay();
    }

    /// <summary>The "exp" key: begin the exponent of the number being entered.</summary>
    public void Exp()
    {
        if (!BeginEntry() || _entry.ToString().Contains('e'))
        {
            return;
        }

        _entry.Append("e+0");
        UpdateEntryDisplay();
    }

    public void Backspace()
    {
        if (_error || !_entering || _justEvaluated)
        {
            return;
        }

        var text = _entry.ToString();
        var e = text.IndexOf('e');
        if (e >= 0)
        {
            var expDigits = text[(e + 2)..];
            if (expDigits.Length > 1)
            {
                _entry.Length--;
            }
            else if (expDigits != "0")
            {
                _entry.Length = e + 2;
                _entry.Append('0');
            }
            else
            {
                _entry.Length = e;
            }

            UpdateEntryDisplay();
            return;
        }

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
            var e = text.IndexOf('e');
            if (e >= 0)
            {
                _entry[e + 1] = _entry[e + 1] == '+' ? '-' : '+';
            }
            else
            {
                _entry.Clear().Append(text.StartsWith('-') ? text[1..] : "-" + text);
            }

            UpdateEntryDisplay();
            return;
        }

        Unary("negate", v => -v);
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
        _operandText = null;
        if (_justEvaluated)
        {
            ResetExpression();
        }

        _afterOperator = false;
        Display = "0";
    }

    public void Clear()
    {
        _entry.Clear().Append('0');
        ResetExpression();
        _value = 0;
        _lastOp = null;
        _lastRight = 0;
        _entering = true;
        _error = false;
        Display = "0";
    }

    // ---- operators and parentheses ----------------------------------------

    public void Operator(ScientificOperator op)
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            var v = _value;
            ResetExpression();
            _value = v;
            _entering = false;
        }

        if (_afterOperator)
        {
            // Replace the operator that was just typed.
            if (_ops.Count > 0 && _ops.Peek() is not null)
            {
                _ops.Pop();
                _ops.Push(op);
                _expr.Length -= _opTextLength;
                AppendOperator(op);
            }

            return;
        }

        CommitOperand();
        while (_ops.Count > 0 && _ops.Peek() is { } top && Precedence(top) >= Precedence(op))
        {
            if (!Reduce())
            {
                return;
            }
        }

        _ops.Push(op);
        AppendOperator(op);
        _value = _operands.Peek();
        Display = Format(_value, true, _fixedExponent);
        _afterOperator = true;
        _entering = false;
    }

    private void AppendOperator(ScientificOperator op)
    {
        var text = Symbol(op) + " ";
        _expr.Append(text);
        _opTextLength = text.Length;
    }

    public void OpenParenthesis()
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated || _operandInExpr || (!_afterOperator && _ops.Count == 0 && _operands.Count == 0 && _expr.Length > 0))
        {
            ResetExpression();
        }

        _parenStarts.Push(_expr.Length);
        _expr.Append("( ");
        _ops.Push(null);
        _value = 0;
        _entry.Clear().Append('0');
        _entering = true;
        _afterOperator = true;
        _operandText = null;
        Display = "0";
    }

    public void CloseParenthesis()
    {
        if (_error || _parenStarts.Count == 0)
        {
            return;
        }

        CommitOperand();
        while (_ops.Count > 0 && _ops.Peek() is not null)
        {
            if (!Reduce())
            {
                return;
            }
        }

        _ops.Pop();
        _groupStart = _parenStarts.Pop();
        _expr.Append(") ");
        _value = _operands.Pop();
        Display = Format(_value, true, _fixedExponent);
        _entering = false;
        _afterOperator = false;
        _operandInExpr = true;
        _operandText = null;
    }

    public void Evaluate()
    {
        if (_error)
        {
            return;
        }

        double result;
        if (_justEvaluated || (_ops.Count == 0 && _operands.Count == 0 && !_operandInExpr))
        {
            var left = CurrentValue();
            if (_lastOp is not { } last)
            {
                _expr.Clear().Append(_operandText ?? Format(left, false, false)).Append(" =");
                _value = left;
                Display = Format(left, true, _fixedExponent);
                _entering = false;
                _justEvaluated = true;
                _operandText = null;
                return;
            }

            if (!Binary(last, left, _lastRight, out result))
            {
                return;
            }

            _expr.Clear().Append(Format(left, false, false)).Append(' ').Append(Symbol(last)).Append(' ')
                .Append(Format(_lastRight, false, false)).Append(" =");
            Finish(result);
            return;
        }

        if (_afterOperator && _ops.Count > 0 && _ops.Peek() is not null)
        {
            // "5 +" then "=": the displayed value is the right operand.
            _operands.Push(_value);
            _expr.Append(Format(_value, false, false)).Append(' ');
        }
        else
        {
            CommitOperand();
        }

        while (_ops.Count > 0)
        {
            if (_ops.Peek() is null)
            {
                _ops.Pop();
                _parenStarts.Pop();
                _expr.Append(") ");
                continue;
            }

            if (!Reduce())
            {
                return;
            }
        }

        result = _operands.Pop();
        _expr.Append('=');
        Finish(result);
    }

    private void Finish(double result)
    {
        _value = result;
        Display = Format(result, true, _fixedExponent);
        _entering = false;
        _justEvaluated = true;
        _afterOperator = false;
        _operandInExpr = false;
        _operandText = null;
        _parenStarts.Clear();
        _ops.Clear();
        _operands.Clear();
    }

    // ---- unary functions --------------------------------------------------

    public void Square() => Unary("sqr", v => v * v);

    public void Cube() => Unary("cube", v => v * v * v);

    public void SquareRoot() => Unary("sqrt", v => v < 0 ? Fail(Invalid) : Math.Sqrt(v));

    public void CubeRoot() => Unary("cuberoot", Math.Cbrt);

    public void TenPower() => Unary("10^", v => Math.Pow(10, v));

    public void TwoPower() => Unary("2^", v => Math.Pow(2, v));

    public void EPower() => Unary("e^", Math.Exp);

    public void Log() => Unary("log", v => v <= 0 ? Fail(Invalid) : Math.Log10(v));

    public void Ln() => Unary("ln", v => v <= 0 ? Fail(Invalid) : Math.Log(v));

    public void Abs() => Unary("abs", Math.Abs);

    public void Reciprocal() => Unary("1/", v => v == 0 ? Fail(DivideByZero) : 1 / v);

    public void Factorial() => Unary("fact", FactorialOf);

    public void Pi() => Constant("π", Math.PI);

    public void E() => Constant("e", Math.E);

    /// <summary>Converts decimal degrees to degrees.minutes-seconds (d.mmss).</summary>
    public void Dms() => Unary("dms", ToDms);

    /// <summary>Converts d.mmss to decimal degrees.</summary>
    public void Degrees() => Unary("deg", FromDms);

    /// <summary>A trigonometric key honouring the Inverse (2nd) and Hyperbolic toggles.</summary>
    public void Trig(TrigFunction function)
    {
        var name = function.ToString().ToLowerInvariant();
        if (Inverse)
        {
            name = "a" + name;
        }

        if (Hyperbolic)
        {
            name += "h";
        }

        var inverse = Inverse;
        var hyperbolic = Hyperbolic;
        Unary(name, v => hyperbolic
            ? (inverse ? InverseHyperbolic(function, v) : HyperbolicOf(function, v))
            : (inverse ? InverseTrig(function, v) : TrigOf(function, v)));
    }

    private double TrigOf(TrigFunction f, double x)
    {
        var full = AngleMode switch { AngleMode.Degrees => 360.0, AngleMode.Gradians => 400.0, _ => 2 * Math.PI };
        var half = full / 2;
        var quarter = full / 4;
        var a = x % full;
        if (a < 0)
        {
            a += full;
        }

        var exact = AngleMode != AngleMode.Radians;
        var rad = AngleMode switch
        {
            AngleMode.Degrees => x * Math.PI / 180,
            AngleMode.Gradians => x * Math.PI / 200,
            _ => x,
        };

        var sin = RoundSig(Math.Sin(rad), 15);
        var cos = RoundSig(Math.Cos(rad), 15);
        if (exact)
        {
            if (a % half == 0)
            {
                sin = 0;
                cos = a == 0 ? 1 : -1;
            }
            else if (a % half == quarter)
            {
                cos = 0;
                sin = a == quarter ? 1 : -1;
            }
        }
        else
        {
            if (Math.Abs(sin) < 1e-15) sin = 0;
            if (Math.Abs(cos) < 1e-15) cos = 0;
        }

        return f switch
        {
            TrigFunction.Sin => sin,
            TrigFunction.Cos => cos,
            TrigFunction.Tan => cos == 0 ? Fail(DivideByZero) : sin / cos,
            TrigFunction.Sec => cos == 0 ? Fail(DivideByZero) : 1 / cos,
            TrigFunction.Csc => sin == 0 ? Fail(DivideByZero) : 1 / sin,
            _ => sin == 0 ? Fail(DivideByZero) : cos / sin,
        };
    }

    private double InverseTrig(TrigFunction f, double x)
    {
        double rad;
        switch (f)
        {
            case TrigFunction.Sin:
                if (Math.Abs(x) > 1) return Fail(Invalid);
                rad = Math.Asin(x);
                break;
            case TrigFunction.Cos:
                if (Math.Abs(x) > 1) return Fail(Invalid);
                rad = Math.Acos(x);
                break;
            case TrigFunction.Tan:
                rad = Math.Atan(x);
                break;
            case TrigFunction.Sec:
                if (Math.Abs(x) < 1) return Fail(Invalid);
                rad = Math.Acos(1 / x);
                break;
            case TrigFunction.Csc:
                if (Math.Abs(x) < 1) return Fail(Invalid);
                rad = Math.Asin(1 / x);
                break;
            default:
                rad = x == 0 ? Math.PI / 2 : Math.Atan(1 / x);
                break;
        }

        return AngleMode switch
        {
            AngleMode.Degrees => Math.Round(rad * 180 / Math.PI, 13, MidpointRounding.AwayFromZero),
            AngleMode.Gradians => Math.Round(rad * 200 / Math.PI, 13, MidpointRounding.AwayFromZero),
            _ => rad,
        };
    }

    private double HyperbolicOf(TrigFunction f, double x) => f switch
    {
        TrigFunction.Sin => Math.Sinh(x),
        TrigFunction.Cos => Math.Cosh(x),
        TrigFunction.Tan => Math.Tanh(x),
        TrigFunction.Sec => 1 / Math.Cosh(x),
        TrigFunction.Csc => x == 0 ? Fail(DivideByZero) : 1 / Math.Sinh(x),
        _ => x == 0 ? Fail(DivideByZero) : 1 / Math.Tanh(x),
    };

    private double InverseHyperbolic(TrigFunction f, double x) => f switch
    {
        TrigFunction.Sin => Math.Asinh(x),
        TrigFunction.Cos => x < 1 ? Fail(Invalid) : Math.Acosh(x),
        TrigFunction.Tan => Math.Abs(x) >= 1 ? Fail(Invalid) : Math.Atanh(x),
        TrigFunction.Sec => x <= 0 || x > 1 ? Fail(Invalid) : Math.Acosh(1 / x),
        TrigFunction.Csc => x == 0 ? Fail(Invalid) : Math.Asinh(1 / x),
        _ => Math.Abs(x) <= 1 ? Fail(Invalid) : Math.Atanh(1 / x),
    };

    private double FactorialOf(double v)
    {
        if (v < 0)
        {
            return Fail(Invalid);
        }

        if (v == Math.Floor(v))
        {
            if (v > 170)
            {
                return Fail(OverflowMessage);
            }

            double r = 1;
            for (var i = 2; i <= (int)v; i++)
            {
                r *= i;
            }

            return r;
        }

        return v > 170.5 ? Fail(OverflowMessage) : RoundSig(Gamma(v + 1), 15);
    }

    // Lanczos approximation (g = 7, n = 9).
    private static double Gamma(double z)
    {
        double[] c =
        [
            0.99999999999980993, 676.5203681218851, -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905, -0.13857109526572012,
            9.9843695780195716e-6, 1.5056327351493116e-7,
        ];
        z -= 1;
        var x = c[0];
        for (var i = 1; i < 9; i++)
        {
            x += c[i] / (z + i);
        }

        var t = z + 7.5;
        return Math.Sqrt(2 * Math.PI) * Math.Pow(t, z + 0.5) * Math.Exp(-t) * x;
    }

    private static double ToDms(double v)
    {
        var d = (decimal)Math.Round(v, 12);
        var sign = d < 0 ? -1m : 1m;
        d = Math.Abs(d);
        var deg = Math.Floor(d);
        var rem = Math.Round((d - deg) * 60m, 10);
        var min = Math.Floor(rem);
        var sec = Math.Round((rem - min) * 60m, 8);
        return (double)(sign * (deg + min / 100m + sec / 10000m));
    }

    private static double FromDms(double v)
    {
        var d = (decimal)Math.Round(v, 12);
        var sign = d < 0 ? -1m : 1m;
        d = Math.Abs(d);
        var deg = Math.Floor(d);
        var frac = Math.Round((d - deg) * 100m, 10);
        var min = Math.Floor(frac);
        var sec = Math.Round((frac - min) * 100m, 8);
        return (double)(sign * (deg + min / 60m + sec / 3600m));
    }

    // ---- memory -----------------------------------------------------------

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

        Constant(Format(_memory, false, false), _memory);
    }

    public void MemoryStore()
    {
        if (_error)
        {
            return;
        }

        var v = CurrentValue();
        _memory = v;
        _value = v;
        _hasMemory = true;
        _entering = false;
    }

    public void MemoryAdd()
    {
        if (_error)
        {
            return;
        }

        var v = CurrentValue();
        _memory = Round16(_memory + v);
        _value = v;
        _hasMemory = true;
        _entering = false;
    }

    public void MemorySubtract()
    {
        if (_error)
        {
            return;
        }

        var v = CurrentValue();
        _memory = Round16(_memory - v);
        _value = v;
        _hasMemory = true;
        _entering = false;
    }

    // ---- internals --------------------------------------------------------

    // Marks the calculation as failed; the surrounding operation aborts in Check().
    private double Fail(string message)
    {
        _failure ??= message;
        return 0;
    }

    private bool Check(double result, out double clean)
    {
        clean = 0;
        if (_failure is null)
        {
            if (double.IsNaN(result))
            {
                _failure = Invalid;
            }
            else if (double.IsInfinity(result))
            {
                _failure = OverflowMessage;
            }
        }

        if (_failure is not null)
        {
            Display = _failure;
            _failure = null;
            _error = true;
            _entering = false;
            ResetExpression();
            return false;
        }

        clean = Round16(result);
        return true;
    }

    private void ResetExpression()
    {
        _expr.Clear();
        _ops.Clear();
        _operands.Clear();
        _parenStarts.Clear();
        _afterOperator = _justEvaluated = _operandInExpr = false;
        _operandText = null;
    }

    private bool BeginEntry()
    {
        if (_error)
        {
            Clear();
        }

        if (_justEvaluated)
        {
            ResetExpression();
            _entering = false;
        }

        if (_operandInExpr)
        {
            return false;
        }

        if (!_entering)
        {
            _entry.Clear().Append('0');
            _entering = true;
        }

        _afterOperator = false;
        _operandText = null;
        return true;
    }

    private double CurrentValue()
    {
        if (!_entering)
        {
            return _value;
        }

        var text = _entry.ToString().Replace(".e", "e").TrimEnd('.');
        return double.Parse(text, NumberStyles.Float, Inv);
    }

    private void UpdateEntryDisplay()
    {
        var text = _entry.ToString();
        var e = text.IndexOf('e');
        var mant = e < 0 ? text : text[..e];
        var exp = e < 0 ? "" : text[e..];
        var negative = mant.StartsWith('-');
        var body = negative ? mant[1..] : mant;
        var dot = body.IndexOf('.');
        var intPart = dot < 0 ? body : body[..dot];
        var grouped = decimal.Parse(intPart, Inv).ToString("#,##0", Inv);
        var shown = (negative ? "-" : "") + grouped + (dot < 0 ? (exp.Length > 0 ? "." : "") : body[dot..]);
        Display = shown + exp;
        _value = CurrentValue();
    }

    // Pushes the current operand (typed number, function result, constant, group) onto the operand stack.
    private void CommitOperand()
    {
        if (_operandInExpr)
        {
            _operands.Push(_value);
            _operandInExpr = false;
            return;
        }

        var v = CurrentValue();
        _operands.Push(v);
        _expr.Append(_operandText ?? Format(v, false, false)).Append(' ');
        _operandText = null;
        _entering = false;
        _value = v;
    }

    private void Unary(string name, Func<double, double> f)
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            var v0 = _value;
            ResetExpression();
            _value = v0;
            _entering = false;
        }

        var v = CurrentValue();
        if (!Check(f(v), out var clean))
        {
            return;
        }

        string inner;
        if (_operandInExpr)
        {
            inner = _expr.ToString(_groupStart, _expr.Length - _groupStart).Trim();
            _expr.Length = _groupStart;
            _operandInExpr = false;
        }
        else
        {
            inner = _operandText ?? Format(v, false, false);
        }

        _operandText = $"{name}({inner})";
        _value = clean;
        _entering = false;
        _afterOperator = false;
        Display = Format(clean, true, _fixedExponent);
    }

    private void Constant(string text, double value)
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            ResetExpression();
        }

        if (_operandInExpr)
        {
            return;
        }

        _value = Round16(value);
        _operandText = text;
        _entering = false;
        _afterOperator = false;
        Display = Format(_value, true, _fixedExponent);
    }

    private bool Reduce()
    {
        var op = _ops.Pop()!.Value;
        var right = _operands.Pop();
        var left = _operands.Pop();
        if (!Binary(op, left, right, out var result))
        {
            return false;
        }

        _lastOp = op;
        _lastRight = right;
        _operands.Push(result);
        return true;
    }

    private bool Binary(ScientificOperator op, double l, double r, out double result)
    {
        double raw;
        switch (op)
        {
            case ScientificOperator.Add: raw = l + r; break;
            case ScientificOperator.Subtract: raw = l - r; break;
            case ScientificOperator.Multiply: raw = l * r; break;
            case ScientificOperator.Divide:
                raw = r == 0 ? Fail(l == 0 ? Undefined : DivideByZero) : l / r;
                break;
            case ScientificOperator.Mod:
                raw = r == 0 ? Fail(DivideByZero) : l % r;
                break;
            case ScientificOperator.Power:
                if (l == 0 && r == 0)
                {
                    raw = Fail(Undefined);
                }
                else if (l == 0 && r < 0)
                {
                    raw = Fail(DivideByZero);
                }
                else if (l < 0 && r != Math.Floor(r))
                {
                    raw = Fail(Invalid);
                }
                else
                {
                    raw = Math.Pow(l, r);
                }

                break;
            case ScientificOperator.Root:
                if (r == 0)
                {
                    raw = Fail(l == 0 ? Undefined : DivideByZero);
                }
                else if (l < 0)
                {
                    raw = r == Math.Floor(r) && Math.Abs(r % 2) == 1 ? -Math.Pow(-l, 1 / r) : Fail(Invalid);
                }
                else
                {
                    raw = Math.Pow(l, 1 / r);
                }

                break;
            default: // LogBase
                raw = l <= 0 || r <= 0 || r == 1 ? Fail(Invalid) : Math.Log(l) / Math.Log(r);
                break;
        }

        return Check(raw, out result);
    }

    private static int Precedence(ScientificOperator op) => op switch
    {
        ScientificOperator.Add or ScientificOperator.Subtract => 1,
        ScientificOperator.Multiply or ScientificOperator.Divide or ScientificOperator.Mod => 2,
        _ => 3,
    };

    private static string Symbol(ScientificOperator op) => op switch
    {
        ScientificOperator.Add => "+",
        ScientificOperator.Subtract => "−",
        ScientificOperator.Multiply => "×",
        ScientificOperator.Divide => "÷",
        ScientificOperator.Mod => "Mod",
        ScientificOperator.Power => "^",
        ScientificOperator.Root => "yroot",
        _ => "logbase",
    };

    private static double RoundSig(double v, int digits)
    {
        if (v == 0 || double.IsNaN(v) || double.IsInfinity(v))
        {
            return v;
        }

        return double.Parse(v.ToString("E" + (digits - 1), Inv), NumberStyles.Float, Inv);
    }

    private static double Round16(double v)
    {
        if (v == 0 || double.IsNaN(v) || double.IsInfinity(v))
        {
            return v;
        }

        return double.Parse(v.ToString("E15", Inv), NumberStyles.Float, Inv);
    }

    /// <summary>Formats a value as the real calculator would: 16 significant digits, grouping, e-notation.</summary>
    public static string Format(double value, bool group, bool scientific)
    {
        if (value == 0)
        {
            return scientific ? "0.e+0" : "0";
        }

        var s = Math.Abs(value).ToString("E15", Inv);
        var ei = s.IndexOf('E');
        var exp = int.Parse(s[(ei + 1)..], Inv);
        var digits = (s[0] + s[2..ei]).TrimEnd('0');
        if (digits.Length == 0)
        {
            digits = "0";
        }

        var sign = value < 0 ? "-" : "";
        if (scientific || exp >= 16 || exp < -6)
        {
            var mant = digits.Length > 1 ? digits[0] + "." + digits[1..] : digits + (scientific ? "." : "");
            return sign + mant + "e" + (exp < 0 ? "-" : "+") + Math.Abs(exp).ToString(Inv);
        }

        string intDigits, frac;
        if (exp >= 0)
        {
            intDigits = digits.Length > exp + 1 ? digits[..(exp + 1)] : digits.PadRight(exp + 1, '0');
            frac = digits.Length > exp + 1 ? digits[(exp + 1)..] : "";
        }
        else
        {
            intDigits = "0";
            frac = new string('0', -exp - 1) + digits;
        }

        if (group)
        {
            intDigits = decimal.Parse(intDigits, Inv).ToString("#,##0", Inv);
        }

        return sign + intDigits + (frac.Length == 0 ? "" : "." + frac);
    }
}
