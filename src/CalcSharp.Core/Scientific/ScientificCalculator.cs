using System.Globalization;
using System.Numerics;
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
/// State machine modelling the "Scientific" mode of the Windows Calculator: expression entry with
/// operator precedence and parentheses, scientific functions, angle modes and memory.
/// Arithmetic is performed with <see cref="BigNum"/> (32 significant digits are displayed).
/// </summary>
public sealed class ScientificCalculator
{
    public const int MaxDigits = BigNum.DisplayDigits;

    private const string DivideByZero = "Cannot divide by zero";
    private const string Undefined = "Result is undefined";
    private const string Invalid = "Invalid input";
    private const string OverflowMessage = "Overflow";
    private const int MaxExponent = 10000;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private sealed class CalcFailure(string message) : Exception(message);

    private readonly StringBuilder _entry = new("0");
    private readonly StringBuilder _expr = new();
    private readonly Stack<BigNum> _operands = new();
    private readonly Stack<ScientificOperator?> _ops = new();   // null = open parenthesis
    private readonly Stack<int> _parenStarts = new();

    private BigNum _value;               // the value currently shown (when not entering)
    private string? _operandText;        // expression text of the current operand if it came from a function
    private string? _shownOperandText;   // what the expression line shows for the operand
    private bool _typed;                 // a number has been typed since the last operator or parenthesis
    private bool _staleExpression;       // the expression is only kept for display after a bare '='
    private bool _operandInExpr;         // current operand is a closed "( ... )" group already in the expression
    private int _groupStart;
    private int _opTextLength;           // length of the last operator text appended to the expression
    private bool _entering = true;
    private bool _afterOperator;
    private bool _justEvaluated;
    private bool _error;
    private ScientificOperator? _lastOp;
    private BigNum _lastRight;
    private BigNum _memory;
    private bool _hasMemory;
    private bool _fixedExponent;

    public string Display { get; private set; } = "0";

    public string Expression => (_expr + (_shownOperandText ?? string.Empty)).TrimEnd();

    public bool HasMemory => _hasMemory;

    public double MemoryValue => _memory.ToDouble();

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
        if (_error)
        {
            Clear();
        }

        if (!_entering || _justEvaluated || _entry.ToString().Contains('e') || !BeginEntry())
        {
            return;
        }

        if (!_entry.ToString().Contains('.'))
        {
            _entry.Append('.');
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
            var e = text.IndexOf('e');
            if (e >= 0)
            {
                _entry[e + 1] = _entry[e + 1] == '+' ? '-' : '+';
            }
            else if (text != "0")
            {
                _entry.Clear().Append(text.StartsWith('-') ? text[1..] : "-" + text);
            }

            UpdateEntryDisplay();
            return;
        }

        Unary("negate(", v => -v);
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
        _value = BigNum.Zero;
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
        _value = BigNum.Zero;
        _lastOp = null;
        _lastRight = BigNum.Zero;
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

        DropStale();
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
                _expr.Append(" " + Symbol(op) + " ");
                return;
            }
        }

        _ops.Push(op);
        AppendOperator(op);
        _value = _operands.Peek();
        Display = Format(_value, true, _fixedExponent);
        _afterOperator = true;
        _entering = false;
        _typed = false;
    }

    private void AppendOperator(ScientificOperator op)
    {
        var text = " " + Symbol(op) + " ";
        _expr.Append(text);
        _opTextLength = text.Length;
    }

    public void OpenParenthesis()
    {
        if (_error)
        {
            return;
        }

        DropStale();
        if (_justEvaluated)
        {
            ResetExpression();
            _entering = true;
            _entry.Clear().Append('0');
            _value = BigNum.Zero;
            Display = "0";
        }

        if (!_afterOperator && (_typed || _operandText is not null || _operandInExpr))
        {
            // A number or group directly followed by "(" is an implicit multiplication.
            Operator(ScientificOperator.Multiply);
            if (_error)
            {
                return;
            }
        }
        else
        {
            _entry.Clear().Append('0');
            _entering = true;
            _value = BigNum.Zero;
            Display = "0";
        }

        _parenStarts.Push(_expr.Length);
        _expr.Append('(');
        _ops.Push(null);
        _afterOperator = false;
        _typed = false;
        _operandText = null;
        _shownOperandText = null;
    }

    public void CloseParenthesis()
    {
        if (_error)
        {
            return;
        }

        if (_parenStarts.Count == 0)
        {
            if (_entering)
            {
                _value = CurrentValue();
                _entering = false;
                Display = Format(_value, true, _fixedExponent);
            }

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
        _expr.Append(')');
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
            Clear();
            return;
        }

        DropStale();
        BigNum result;
        if (_justEvaluated || (_ops.Count == 0 && _operands.Count == 0 && !_operandInExpr))
        {
            var left = CurrentValue();
            if (_lastOp is not { } last)
            {
                _expr.Clear().Append(_operandText ?? Format(left, false, false)).Append('=');
                _value = left;
                Display = Format(left, true, _fixedExponent);
                _entering = false;
                _staleExpression = true;
                _operandText = _shownOperandText = null;
                return;
            }

            if (!Binary(last, left, _lastRight, out result))
            {
                return;
            }

            _expr.Clear().Append(Format(left, false, false)).Append(' ').Append(Symbol(last)).Append(' ')
                .Append(Format(_lastRight, false, false)).Append('=');
            Finish(result);
            return;
        }

        var mark = _expr.Length;
        if (_afterOperator && _ops.Count > 0 && _ops.Peek() is not null)
        {
            // "5 +" then "=": the displayed value is the right operand.
            _operands.Push(_value);
            _expr.Append(Format(_value, false, false));
            _shownOperandText = null;
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
                _expr.Append(')');
                continue;
            }

            if (!Reduce())
            {
                _expr.Length = mark;
                return;
            }
        }

        result = _operands.Pop();
        _expr.Append('=');
        Finish(result);
    }

    private void Finish(BigNum result)
    {
        _value = result;
        Display = Format(result, true, _fixedExponent);
        _entering = false;
        _typed = false;
        _justEvaluated = true;
        _afterOperator = false;
        _operandInExpr = false;
        _operandText = null;
        _parenStarts.Clear();
        _ops.Clear();
        _operands.Clear();
    }

    // ---- unary functions --------------------------------------------------

    public void Square() => Unary("square (", v => v * v);

    public void Cube() => Unary("cube (", v => v * v * v);

    public void SquareRoot() => Unary("√(", v => v.Sign < 0 ? throw new CalcFailure(Invalid) : BigNum.Sqrt(v));

    public void CubeRoot() => Unary("cuberoot (", CubeRootOf);

    public void TenPower() => Unary("10^(", v => BigNum.Pow(BigNum.Ten, v));

    public void TwoPower() => Unary("2^(", v => BigNum.Pow(BigNum.Two, v));

    public void EPower() => Unary("e^(", BigNum.Exp);

    public void Log() => Unary("log(", v => v.Sign <= 0 ? throw new CalcFailure(Invalid) : BigNum.Log10(v));

    public void Ln() => Unary("natural log (", v => v.Sign <= 0 ? throw new CalcFailure(Invalid) : BigNum.Ln(v));

    public void Abs() => Unary("Absolute value (", v => v.Abs());

    public void Reciprocal() => Unary("1/(", v => v.IsZero ? throw new CalcFailure(DivideByZero) : BigNum.One / v);

    public void Factorial() => Unary("factorial (", FactorialOf);

    public void Pi() => Constant(BigNum.Pi);

    public void E() => Constant(BigNum.Exp(BigNum.One));

    /// <summary>Converts decimal degrees to degrees.minutes-seconds (d.mmss).</summary>
    public void Dms() => Unary("dms (", ToDms);

    /// <summary>Converts d.mmss to decimal degrees.</summary>
    public void Degrees() => Unary("degrees (", FromDms);

    /// <summary>A trigonometric key honouring the Inverse (2nd) and Hyperbolic toggles.</summary>
    public void Trig(TrigFunction function)
    {
        var baseName = function switch
        {
            TrigFunction.Sin => "sine",
            TrigFunction.Cos => "cosine",
            TrigFunction.Tan => "tangent",
            TrigFunction.Sec => "secant",
            TrigFunction.Csc => "cosecant",
            _ => "cotangent",
        };

        var inverse = Inverse;
        var hyperbolic = Hyperbolic;
        var name = (inverse ? "inverse " : string.Empty) + (hyperbolic ? "hyperbolic " : string.Empty) + baseName;
        if (!hyperbolic)
        {
            name += AngleMode switch
            {
                AngleMode.Degrees => " degrees",
                AngleMode.Radians => " radians",
                _ => " gradians",
            };
        }

        Unary(name + " (", v => hyperbolic
            ? (inverse ? InverseHyperbolic(function, v) : HyperbolicOf(function, v))
            : (inverse ? InverseTrig(function, v) : TrigOf(function, v)));
    }

    private BigNum ToRadians(BigNum x) => AngleMode switch
    {
        AngleMode.Degrees => x * BigNum.Pi / BigNum.FromInt(180),
        AngleMode.Gradians => x * BigNum.Pi / BigNum.FromInt(200),
        _ => x,
    };

    private BigNum FromRadians(BigNum x) => AngleMode switch
    {
        AngleMode.Degrees => x * BigNum.FromInt(180) / BigNum.Pi,
        AngleMode.Gradians => x * BigNum.FromInt(200) / BigNum.Pi,
        _ => x,
    };

    private BigNum TrigOf(TrigFunction f, BigNum x)
    {
        BigNum sin;
        BigNum cos;

        if (AngleMode != AngleMode.Radians)
        {
            var full = BigNum.FromInt(AngleMode == AngleMode.Degrees ? 360 : 400);
            var quarter = full / BigNum.FromInt(4);
            var turns = (x / full).Floor();
            var a = x - (turns * full);   // 0 <= a < full
            if ((a / quarter).IsInteger)
            {
                var q = (int)(a / quarter).ToDouble();
                (sin, cos) = q switch
                {
                    0 => (BigNum.Zero, BigNum.One),
                    1 => (BigNum.One, BigNum.Zero),
                    2 => (BigNum.Zero, -BigNum.One),
                    _ => (-BigNum.One, BigNum.Zero),
                };

                return Combine(f, sin, cos);
            }
        }

        var rad = ToRadians(x);
        sin = BigNum.Sin(rad).Round(BigNum.Precision);
        cos = BigNum.Cos(rad).Round(BigNum.Precision);
        return Combine(f, sin, cos);
    }

    private static BigNum Combine(TrigFunction f, BigNum sin, BigNum cos) => f switch
    {
        TrigFunction.Sin => sin,
        TrigFunction.Cos => cos,
        TrigFunction.Tan => cos.IsZero ? throw new CalcFailure(DivideByZero) : sin / cos,
        TrigFunction.Sec => cos.IsZero ? throw new CalcFailure(DivideByZero) : BigNum.One / cos,
        TrigFunction.Csc => sin.IsZero ? throw new CalcFailure(DivideByZero) : BigNum.One / sin,
        _ => sin.IsZero ? throw new CalcFailure(DivideByZero) : cos / sin,
    };

    private BigNum InverseTrig(TrigFunction f, BigNum x)
    {
        var half = BigNum.Pi / BigNum.Two;
        BigNum rad;
        switch (f)
        {
            case TrigFunction.Sin:
                rad = x.Abs() > BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Asin(x);
                break;
            case TrigFunction.Cos:
                rad = x.Abs() > BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Acos(x);
                break;
            case TrigFunction.Tan:
                rad = BigNum.Atan(x);
                break;
            case TrigFunction.Sec:
                rad = x.Abs() < BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Acos(BigNum.One / x);
                break;
            case TrigFunction.Csc:
                rad = x.Abs() < BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Asin(BigNum.One / x);
                break;
            default:
                rad = x.IsZero ? half : BigNum.Atan(BigNum.One / x);
                break;
        }

        return FromRadians(rad).Round(BigNum.Precision);
    }

    private static BigNum HyperbolicOf(TrigFunction f, BigNum x) => f switch
    {
        TrigFunction.Sin => BigNum.Sinh(x),
        TrigFunction.Cos => BigNum.Cosh(x),
        TrigFunction.Tan => BigNum.Tanh(x),
        TrigFunction.Sec => BigNum.One / BigNum.Cosh(x),
        TrigFunction.Csc => x.IsZero ? throw new CalcFailure(DivideByZero) : BigNum.One / BigNum.Sinh(x),
        _ => x.IsZero ? throw new CalcFailure(DivideByZero) : BigNum.One / BigNum.Tanh(x),
    };

    private static BigNum InverseHyperbolic(TrigFunction f, BigNum x) => f switch
    {
        TrigFunction.Sin => BigNum.Asinh(x),
        TrigFunction.Cos => x < BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Acosh(x),
        TrigFunction.Tan => x.Abs() >= BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Atanh(x),
        TrigFunction.Sec => x.Sign <= 0 || x > BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Acosh(BigNum.One / x),
        TrigFunction.Csc => x.IsZero ? throw new CalcFailure(Invalid) : BigNum.Asinh(BigNum.One / x),
        _ => x.Abs() <= BigNum.One ? throw new CalcFailure(Invalid) : BigNum.Atanh(BigNum.One / x),
    };

    private static BigNum CubeRootOf(BigNum v)
    {
        if (v.IsZero)
        {
            return BigNum.Zero;
        }

        var root = BigNum.Exp(BigNum.Ln(v.Abs()) / BigNum.FromInt(3));
        var rounded = root.Round(BigNum.DisplayDigits + 4);
        return v.Sign < 0 ? -rounded : rounded;
    }

    private static BigNum FactorialOf(BigNum v)
    {
        if (v.Sign < 0)
        {
            throw new CalcFailure(Invalid);
        }

        if (v.IsInteger)
        {
            if (v > BigNum.FromInt(3248))
            {
                throw new CalcFailure(OverflowMessage);
            }

            return BigNum.Factorial((long)v.ToDouble());
        }

        if (v > BigNum.FromInt(3248))
        {
            throw new CalcFailure(OverflowMessage);
        }

        return BigNum.FromDouble(Gamma(v.ToDouble() + 1));
    }

    // Lanczos approximation (g = 7, n = 9). Only used for non-integer factorials.
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

    private static BigNum ToDms(BigNum v)
    {
        var sign = v.Sign < 0 ? -1 : 1;
        var d = v.Abs();
        var deg = d.Truncate();
        var rem = (d - deg) * BigNum.FromInt(60);
        var min = rem.Truncate();
        var sec = (rem - min) * BigNum.FromInt(60);
        var r = deg + (min / BigNum.FromInt(100)) + (sec / BigNum.FromInt(10000));
        return sign < 0 ? -r : r;
    }

    private static BigNum FromDms(BigNum v)
    {
        var sign = v.Sign < 0 ? -1 : 1;
        var d = v.Abs();
        var deg = d.Truncate();
        var frac = (d - deg) * BigNum.FromInt(100);
        var min = frac.Truncate();
        var sec = (frac - min) * BigNum.FromInt(100);
        var r = deg + (min / BigNum.FromInt(60)) + (sec / BigNum.FromInt(3600));
        return sign < 0 ? -r : r;
    }

    // ---- memory -----------------------------------------------------------

    public void MemoryClear()
    {
        _memory = BigNum.Zero;
        _hasMemory = false;
    }

    public void MemoryRecall()
    {
        if (_error || !_hasMemory)
        {
            return;
        }

        Constant(_memory);
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
        _memory += v;
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
        _memory -= v;
        _value = v;
        _hasMemory = true;
        _entering = false;
    }

    // ---- internals --------------------------------------------------------

    private void ShowError(string message)
    {
        Display = message;
        _error = true;
        _entering = false;
        _ops.Clear();
        _operands.Clear();
        _parenStarts.Clear();
        _afterOperator = _justEvaluated = _operandInExpr = false;
    }

    private bool Guarded(Func<BigNum> compute, out BigNum result)
    {
        result = BigNum.Zero;
        try
        {
            var raw = compute();
            if (!raw.IsZero && raw.Magnitude >= MaxExponent)
            {
                ShowError(OverflowMessage);
                return false;
            }

            result = raw.Round(BigNum.Precision);
            return true;
        }
        catch (CalcFailure f)
        {
            ShowError(f.Message);
        }
        catch (DivideByZeroException)
        {
            ShowError(DivideByZero);
        }
        catch (ArgumentOutOfRangeException)
        {
            ShowError(Invalid);
        }
        catch (OverflowException)
        {
            ShowError(OverflowMessage);
        }

        return false;
    }

    private void ResetExpression()
    {
        _expr.Clear();
        _ops.Clear();
        _operands.Clear();
        _parenStarts.Clear();
        _afterOperator = _justEvaluated = _operandInExpr = false;
        _operandText = null;
        _shownOperandText = null;
        _staleExpression = false;
        _typed = false;
    }

    private void DropStale()
    {
        if (_staleExpression)
        {
            _expr.Clear();
            _staleExpression = false;
            _shownOperandText = null;
        }
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
        _typed = true;
        if (_ops.Count > 0)
        {
            _shownOperandText = null;
        }

        return true;
    }

    private BigNum CurrentValue()
    {
        if (!_entering)
        {
            return _value;
        }

        var text = _entry.ToString().Replace(".e", "e").TrimEnd('.');
        return BigNum.Parse(text);
    }

    private void UpdateEntryDisplay()
    {
        var text = _entry.ToString();
        var e = text.IndexOf('e');
        var mant = e < 0 ? text : text[..e];
        var exp = e < 0 ? string.Empty : text[e..];
        var negative = mant.StartsWith('-');
        var body = negative ? mant[1..] : mant;
        var dot = body.IndexOf('.');
        var intPart = dot < 0 ? body : body[..dot];
        var grouped = BigInteger.Parse(intPart, Inv).ToString("#,##0", Inv);
        var shown = (negative ? "-" : string.Empty) + grouped + (dot < 0 ? string.Empty : body[dot..]);
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
        _expr.Append(_operandText ?? Format(v, false, false));
        _operandText = null;
        _shownOperandText = null;
        _entering = false;
        _value = v;
    }

    private void Unary(string name, Func<BigNum, BigNum> f)
    {
        if (_error)
        {
            return;
        }

        DropStale();
        if (_justEvaluated)
        {
            var v0 = _value;
            ResetExpression();
            _value = v0;
            _entering = false;
        }

        var v = CurrentValue();
        string inner;
        if (_operandInExpr)
        {
            inner = _expr.ToString(_groupStart, _expr.Length - _groupStart).Trim();
        }
        else
        {
            inner = _operandText ?? Format(v, false, false);
        }

        if (!Guarded(() => f(v), out var clean))
        {
            _shownOperandText = name + inner + ")";
            return;
        }

        if (_operandInExpr)
        {
            _expr.Length = _groupStart;
            _operandInExpr = false;
        }

        _operandText = _shownOperandText = name + inner + ")";
        _value = clean;
        _entering = false;
        _afterOperator = false;
        _typed = false;
        Display = Format(clean, true, _fixedExponent);
    }

    private void Constant(BigNum value)
    {
        if (_error)
        {
            return;
        }

        DropStale();
        if (_justEvaluated)
        {
            ResetExpression();
        }

        if (_operandInExpr)
        {
            return;
        }

        _value = value.Round(BigNum.DisplayDigits);
        _operandText = null;
        _shownOperandText = null;
        _typed = false;
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

    private bool Binary(ScientificOperator op, BigNum l, BigNum r, out BigNum result) =>
        Guarded(
            () => op switch
            {
                ScientificOperator.Add => l + r,
                ScientificOperator.Subtract => l - r,
                ScientificOperator.Multiply => l * r,
                ScientificOperator.Divide => r.IsZero
                    ? throw new CalcFailure(l.IsZero ? Undefined : DivideByZero)
                    : l / r,
                ScientificOperator.Mod => r.IsZero
                    ? throw new CalcFailure(DivideByZero)
                    : l - (r * (l / r).Truncate()),
                ScientificOperator.Power => PowerOf(l, r),
                ScientificOperator.Root => RootOf(l, r),
                _ => l.Sign <= 0 || r.Sign <= 0 || r == BigNum.One
                    ? throw new CalcFailure(Invalid)
                    : BigNum.Ln(l) / BigNum.Ln(r),
            },
            out result);

    private static BigNum PowerOf(BigNum l, BigNum r)
    {
        if (l.IsZero && r.IsZero)
        {
            throw new CalcFailure(Undefined);
        }

        if (l.IsZero && r.Sign < 0)
        {
            throw new CalcFailure(DivideByZero);
        }

        if (l.Sign < 0 && !r.IsInteger)
        {
            throw new CalcFailure(Invalid);
        }

        if (r.IsInteger && r.Magnitude <= 6)
        {
            return BigNum.Pow(l, r);
        }

        if (l.Sign < 0)
        {
            var even = ((BigInteger)r.Truncate().Mantissa * BigInteger.Pow(10, Math.Max(0, r.Exponent))).IsEven;
            var mag = BigNum.Pow(l.Abs(), r);
            return even ? mag : -mag;
        }

        return BigNum.Pow(l, r);
    }

    private static BigNum RootOf(BigNum l, BigNum r)
    {
        if (r.IsZero)
        {
            throw new CalcFailure(l.IsZero ? Undefined : DivideByZero);
        }

        if (l.Sign < 0)
        {
            var oddInteger = r.IsInteger && !((BigInteger)r.Truncate().Mantissa * BigInteger.Pow(10, Math.Max(0, r.Exponent))).IsEven;
            if (!oddInteger)
            {
                throw new CalcFailure(Invalid);
            }

            return -BigNum.Pow(-l, BigNum.One / r);
        }

        return BigNum.Pow(l, BigNum.One / r);
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

    /// <summary>
    /// Formats a value like the real calculator: up to 32 significant digits, grouping, and
    /// exponent notation (for example "1.e+5533") when the value does not fit.
    /// </summary>
    public static string Format(BigNum value, bool group, bool scientific)
    {
        if (value.IsZero)
        {
            return scientific ? "0.e+0" : "0";
        }

        var r = value.Round(MaxDigits);
        if (r.Magnitude < 0)
        {
            r = r.RoundAtExponent(-MaxDigits);
        }

        if (r.IsZero)
        {
            return scientific ? "0.e+0" : "0";
        }

        var negative = r.Sign < 0;
        var a = r.Abs();
        var exp = a.Magnitude;
        var digits = BigInteger.Abs(a.Mantissa).ToString(Inv);   // trailing zeros already trimmed

        bool useScientific;
        if (scientific)
        {
            useScientific = true;
        }
        else if (exp >= MaxDigits)
        {
            useScientific = true;
        }
        else if (exp >= -3)
        {
            useScientific = false;
        }
        else
        {
            var decimals = digits.Length - exp - 1;
            useScientific = decimals > MaxDigits;
        }

        string text;
        if (useScientific)
        {
            var mantissa = digits.Length > 1 ? digits[0] + "." + digits[1..] : digits + ".";
            text = mantissa + "e" + (exp < 0 ? "-" : "+") + Math.Abs(exp).ToString(Inv);
        }
        else
        {
            string intDigits;
            string frac;
            if (exp >= 0)
            {
                intDigits = digits.Length > exp + 1 ? digits[..(exp + 1)] : digits.PadRight(exp + 1, '0');
                frac = digits.Length > exp + 1 ? digits[(exp + 1)..] : string.Empty;
            }
            else
            {
                intDigits = "0";
                frac = new string('0', -exp - 1) + digits;
            }

            if (frac.Length > MaxDigits)
            {
                frac = frac[..MaxDigits].TrimEnd('0');
            }

            if (group)
            {
                intDigits = BigInteger.Parse(intDigits, Inv).ToString("#,##0", Inv);
            }

            text = intDigits + (frac.Length == 0 ? string.Empty : "." + frac);
        }

        return (negative ? "-" : string.Empty) + text;
    }
}
