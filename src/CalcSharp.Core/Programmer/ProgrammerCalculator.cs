using System.Globalization;
using System.Text;

namespace CalcSharp.Core.Programmer;

public enum WordSize
{
    Qword = 64,
    Dword = 32,
    Word = 16,
    Byte = 8,
}

public enum Radix
{
    Hex = 16,
    Dec = 10,
    Oct = 8,
    Bin = 2,
}

public enum ProgrammerOp
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Mod,
    And,
    Or,
    Xor,
    Nand,
    Nor,
    Lsh,
    Rsh,
    RoL,
    RoR,
    RoLC,
    RoRC,
}

/// <summary>Selected in the shift drop-down of the real app; decides which shift buttons are shown.</summary>
public enum ShiftMode
{
    Arithmetic,
    Logical,
    RotateCircular,
    RotateThroughCarry,
}

/// <summary>
/// State machine modelling the "Programmer" mode: signed two's-complement integers of a chosen word size.
/// </summary>
public sealed class ProgrammerCalculator
{
    public const string DivideByZero = "Cannot divide by zero";
    public const string Undefined = "Result is undefined";
    public const string OverflowMessage = "Overflow";

    private sealed record Token(ProgrammerOp? Op, bool IsParen);

    private readonly List<long> _operands = [];
    private readonly List<Token> _stack = [];
    private readonly List<object> _expression = [];   // string (operator, parenthesis), long (number) or ExprNode

    private abstract record ExprNode;

    private sealed record NotNode(object Inner) : ExprNode;

    private sealed record NegateNode(object Inner) : ExprNode;

    private long _current;
    private bool _entering = true;
    private bool _afterOperator;
    private bool _justEvaluated;
    private bool _error;
    private string _errorText = "";
    private bool _carry;
    private ProgrammerOp? _lastOp;
    private long _lastOperand;
    private long _memory;
    private bool _hasMemory;
    private object? _operandNode;        // expression node of the current operand when produced by NOT or negate
    private object? _shownTail;          // what the expression line shows for the current operand
    private List<object>? _finalExpr;    // expression after '=' (rendered with a trailing '=')
    private bool _bareEquals;            // '=' was pressed with nothing to evaluate

    public WordSize WordSize { get; private set; } = WordSize.Qword;

    public Radix Radix { get; private set; } = Radix.Dec;

    public ShiftMode ShiftMode { get; set; } = ShiftMode.Arithmetic;

    public int Bits => (int)WordSize;

    public bool IsError => _error;

    public bool HasMemory => _hasMemory;

    public bool Carry => _carry;

    public long Value => _current;

    public long MemoryValue => _memory;

    public int OpenParentheses => _stack.Count(t => t.IsParen);

    public string Expression => _finalExpr is not null
        ? BuildExpression(_finalExpr, null) + "="
        : BuildExpression(_expression, _shownTail);

    private string BuildExpression(IEnumerable<object> tokens, object? tail)
    {
        var sb = new StringBuilder();
        var previous = string.Empty;
        foreach (var node in tokens.Concat(tail is null ? [] : [tail]))
        {
            var token = Render(node);
            if (sb.Length > 0 && previous != "(" && token != ")")
            {
                sb.Append(' ');
            }

            sb.Append(token);
            previous = token;
        }

        return sb.ToString();
    }

    private string Render(object node) => node switch
    {
        string text => text,
        long value => PlainText(value),
        NotNode not => $"NOT({Render(not.Inner)})",
        NegateNode negate => $"negate({Render(negate.Inner)})",
        _ => string.Empty,
    };

    /// <summary>The current value in the current radix, as used inside the expression line (no grouping).</summary>
    private string PlainText(long v) => Radix switch
    {
        Radix.Dec => Wrap(v).ToString(CultureInfo.InvariantCulture),
        Radix.Bin => Convert.ToString((long)Pattern(v), 2),
        _ => Format(v, Radix),
    };

    /// <summary>The value shown on the main display, in the current radix (or an error message).</summary>
    public string Display => _error ? _errorText : Format(_current, Radix);

    public string Hex => _error ? string.Empty : Format(_current, Radix.Hex);
    public string Dec => _error ? string.Empty : Format(_current, Radix.Dec);
    public string Oct => _error ? string.Empty : Format(_current, Radix.Oct);
    public string Bin => _error ? string.Empty : Format(_current, Radix.Bin);

    public long MaxValue => Bits == 64 ? long.MaxValue : (1L << (Bits - 1)) - 1;
    public long MinValue => Bits == 64 ? long.MinValue : -(1L << (Bits - 1));

    private ulong Mask => Bits == 64 ? ulong.MaxValue : (1UL << Bits) - 1;

    // ---- value helpers -------------------------------------------------------------------

    public long Wrap(long v)
    {
        var u = (ulong)v & Mask;
        if (Bits < 64 && ((u >> (Bits - 1)) & 1) != 0)
        {
            u |= ~Mask;
        }

        return (long)u;
    }

    private ulong Pattern(long v) => (ulong)v & Mask;

    public string Format(long v, Radix radix)
    {
        v = Wrap(v);
        switch (radix)
        {
            case Radix.Dec:
                return v.ToString("N0", CultureInfo.InvariantCulture);
            case Radix.Hex:
                return Pattern(v).ToString("X", CultureInfo.InvariantCulture);
            case Radix.Oct:
                return Convert.ToString((long)Pattern(v), 8);
            default:
                var bin = Convert.ToString((long)Pattern(v), 2);
                if (bin != "0" && bin.Length % 4 != 0)
                {
                    bin = bin.PadLeft(bin.Length + (4 - (bin.Length % 4)), '0');
                }

                var sb = new StringBuilder();
                for (var i = 0; i < bin.Length; i++)
                {
                    if (i > 0 && (bin.Length - i) % 4 == 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(bin[i]);
                }

                return sb.ToString();
        }
    }

    // ---- mode settings -------------------------------------------------------------------

    public void SetWordSize(WordSize size)
    {
        WordSize = size;
        _current = Wrap(_current);
        _memory = Wrap(_memory);
        _lastOperand = Wrap(_lastOperand);
        for (var i = 0; i < _operands.Count; i++)
        {
            _operands[i] = Wrap(_operands[i]);
        }
    }

    /// <summary>Cycles QWORD, DWORD, WORD, BYTE and back, as the word-size button does.</summary>
    public void CycleWordSize() => SetWordSize(WordSize switch
    {
        WordSize.Qword => WordSize.Dword,
        WordSize.Dword => WordSize.Word,
        WordSize.Word => WordSize.Byte,
        _ => WordSize.Qword,
    });

    public void SetRadix(Radix radix)
    {
        if (radix != Radix && !_error)
        {
            _entering = false;
        }

        Radix = radix;
    }

    public bool IsDigitEnabled(int digit) => digit >= 0 && digit < (int)Radix;

    // ---- entry ---------------------------------------------------------------------------

    public void Digit(int digit)
    {
        if (digit < 0 || digit > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        if (digit >= (int)Radix)
        {
            return;
        }

        if (_error)
        {
            ResetAll();
        }

        if (!_entering)
        {
            if (_justEvaluated)
            {
                _operands.Clear();
                _stack.Clear();
                if (!_bareEquals)
                {
                    _expression.Clear();
                    _finalExpr = null;
                    _shownTail = null;
                }

                _justEvaluated = false;
            }

            _current = 0;
            _entering = true;
            _afterOperator = false;
        }

        _operandNode = null;
        if (_stack.Count > 0)
        {
            _shownTail = null;
        }

        if (Radix == Radix.Dec)
        {
            var wide = (Int128)_current * 10 + (_current < 0 ? -digit : digit);
            if (wide > MaxValue || wide < MinValue)
            {
                return;
            }

            _current = (long)wide;
        }
        else
        {
            var wide = (UInt128)Pattern(_current) * (UInt128)(int)Radix + (UInt128)digit;
            if (wide > Mask)
            {
                return;
            }

            _current = Wrap((long)(ulong)wide);
        }
    }

    public void Backspace()
    {
        if (_error || !_entering)
        {
            return;
        }

        if (Radix == Radix.Dec)
        {
            _current /= 10;
        }
        else
        {
            _current = Wrap((long)(Pattern(_current) / (ulong)(int)Radix));
        }
    }

    public void Negate()
    {
        if (_error)
        {
            return;
        }

        if (!_entering)
        {
            var node = new NegateNode(_operandNode ?? _current);
            _operandNode = _shownTail = node;
        }

        _current = Wrap(unchecked(-_current));
    }

    public void Not()
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            _operands.Clear();
            _stack.Clear();
            _expression.Clear();
            _finalExpr = null;
            _justEvaluated = false;
        }

        _operandNode = _shownTail = new NotNode(_operandNode ?? _current);
        _current = Wrap(~_current);
        _entering = false;
        _afterOperator = false;
    }

    public void ClearEntry()
    {
        _error = false;
        _errorText = "";
        _current = 0;
        _entering = true;
        _afterOperator = false;
        _justEvaluated = false;
        _operandNode = null;
    }

    public void Clear() => ResetAll();

    private void ResetAll()
    {
        _operands.Clear();
        _stack.Clear();
        _expression.Clear();
        _current = 0;
        _entering = true;
        _afterOperator = false;
        _justEvaluated = false;
        _error = false;
        _errorText = "";
        _lastOp = null;
        _lastOperand = 0;
        _carry = false;
        _operandNode = _shownTail = _finalExpr = null;
        _bareEquals = false;
    }

    // ---- bit-flip panel ------------------------------------------------------------------

    public bool GetBit(int index) => index >= 0 && index < Bits && ((Pattern(_current) >> index) & 1) != 0;

    public void ToggleBit(int index)
    {
        if (_error || index < 0 || index >= Bits)
        {
            return;
        }

        _current = Wrap((long)(Pattern(_current) ^ (1UL << index)));
        _entering = false;
        _afterOperator = false;
    }

    // ---- operators -----------------------------------------------------------------------

    private static int Precedence(ProgrammerOp op) => op switch
    {
        ProgrammerOp.Or => 1,
        ProgrammerOp.Xor => 2,
        ProgrammerOp.And or ProgrammerOp.Nand or ProgrammerOp.Nor => 3,
        ProgrammerOp.Lsh or ProgrammerOp.Rsh or ProgrammerOp.RoL or ProgrammerOp.RoR
            or ProgrammerOp.RoLC or ProgrammerOp.RoRC => 4,
        ProgrammerOp.Add or ProgrammerOp.Subtract => 5,
        _ => 6,
    };

    public static string Symbol(ProgrammerOp op) => op switch
    {
        ProgrammerOp.Add => "+",
        ProgrammerOp.Subtract => "−",
        ProgrammerOp.Multiply => "×",
        ProgrammerOp.Divide => "÷",
        ProgrammerOp.Mod => "%",
        ProgrammerOp.And => "AND",
        ProgrammerOp.Or => "OR",
        ProgrammerOp.Xor => "XOR",
        ProgrammerOp.Nand => "NAND",
        ProgrammerOp.Nor => "NOR",
        ProgrammerOp.Lsh => "<<",
        ProgrammerOp.Rsh => ">>",
        ProgrammerOp.RoL => "RoL",
        ProgrammerOp.RoR => "RoR",
        ProgrammerOp.RoLC => "RoLC",
        _ => "RoRC",
    };

    public void Operator(ProgrammerOp op)
    {
        if (_error)
        {
            return;
        }

        if (_afterOperator && _stack.Count > 0 && _stack[^1].Op is not null)
        {
            // replace the pending operator
            _stack[^1] = new Token(op, false);
            _expression[^1] = Symbol(op);
            return;
        }

        if (_justEvaluated)
        {
            _operands.Clear();
            _stack.Clear();
            _expression.Clear();
            _finalExpr = null;
            _justEvaluated = false;
        }

        _expression.Add(_operandNode ?? _current);
        _operandNode = _shownTail = null;
        _operands.Add(_current);

        while (_stack.Count > 0 && _stack[^1].Op is { } top && Precedence(top) >= Precedence(op))
        {
            if (!ReduceOne())
            {
                _expression.Add(Symbol(op));
                return;
            }
        }

        _current = _operands[^1];
        _stack.Add(new Token(op, false));
        _expression.Add(Symbol(op));
        _afterOperator = true;
        _entering = false;
    }

    public void OpenParenthesis()
    {
        if (_error)
        {
            return;
        }

        if (_justEvaluated)
        {
            _operands.Clear();
            _stack.Clear();
            _expression.Clear();
            _finalExpr = null;
            _justEvaluated = false;
        }

        _stack.Add(new Token(null, true));
        _expression.Add("(");
        _current = 0;
        _entering = true;
        _afterOperator = true;
    }

    public void CloseParenthesis()
    {
        if (_error || OpenParentheses == 0)
        {
            return;
        }

        _operands.Add(_current);
        _expression.Add(_operandNode ?? _current);
        _operandNode = _shownTail = null;
        while (_stack[^1].Op is not null)
        {
            if (!ReduceOne())
            {
                return;
            }
        }

        _stack.RemoveAt(_stack.Count - 1);
        _expression.Add(")");
        _current = _operands[^1];
        _operands.RemoveAt(_operands.Count - 1);
        _entering = false;
        _afterOperator = false;
    }

    public void Evaluate()
    {
        if (_error)
        {
            return;
        }

        if (_stack.Count == 0)
        {
            if (_lastOp is { } op)
            {
                var left = _current;
                var result = Apply(left, op, _lastOperand, out var err);
                if (err is not null)
                {
                    _expression.Clear();
                    _expression.AddRange([left, Symbol(op)]);
                    SetError(err);
                    return;
                }

                _finalExpr = [left, Symbol(op), _lastOperand];
                _current = result;
                _bareEquals = false;
            }
            else
            {
                _finalExpr = [_operandNode ?? _current];
                _bareEquals = true;
            }

            _expression.Clear();
            _operandNode = _shownTail = null;
            _entering = false;
            _afterOperator = false;
            _justEvaluated = true;
            return;
        }

        var right = _operandNode ?? _current;
        _operandNode = _shownTail = null;
        var mark = _expression.Count;
        _expression.Add(right);
        _operands.Add(_current);
        while (_stack.Count > 0)
        {
            if (_stack[^1].IsParen)
            {
                _stack.RemoveAt(_stack.Count - 1);
                continue;
            }

            if (!ReduceOne(remember: true))
            {
                if (_expression.Count > mark)
                {
                    _expression.RemoveRange(mark, _expression.Count - mark);
                }

                return;
            }
        }

        _current = _operands[^1];
        _operands.Clear();
        _finalExpr = [.. _expression];
        _expression.Clear();
        _bareEquals = false;
        _entering = false;
        _afterOperator = false;
        _justEvaluated = true;
    }

    private bool ReduceOne(bool remember = false)
    {
        var op = _stack[^1].Op!.Value;
        var right = _operands[^1];
        var left = _operands[^2];
        var result = Apply(left, op, right, out var err);
        if (err is not null)
        {
            SetError(err);
            return false;
        }

        _stack.RemoveAt(_stack.Count - 1);
        _operands.RemoveRange(_operands.Count - 2, 2);
        _operands.Add(result);
        if (remember)
        {
            _lastOp = op;
            _lastOperand = right;
        }

        return true;
    }

    private void SetError(string text)
    {
        _error = true;
        _errorText = text;
        _operands.Clear();
        _stack.Clear();
        _finalExpr = null;
        _lastOp = null;
        _afterOperator = false;
        _entering = false;
        _justEvaluated = false;
    }

    /// <summary>Applies a binary operator on already word-size-wrapped operands.</summary>
    private long Apply(long a, ProgrammerOp op, long b, out string? error)
    {
        error = null;
        switch (op)
        {
            case ProgrammerOp.Add: return Wrap(unchecked(a + b));
            case ProgrammerOp.Subtract: return Wrap(unchecked(a - b));
            case ProgrammerOp.Multiply: return Wrap(unchecked(a * b));
            case ProgrammerOp.Divide:
            case ProgrammerOp.Mod:
                if (b == 0)
                {
                    error = op == ProgrammerOp.Mod || a == 0 ? Undefined : DivideByZero;
                    return 0;
                }

                if (a == MinValue && b == -1)
                {
                    if (op == ProgrammerOp.Mod)
                    {
                        return 0;
                    }

                    error = OverflowMessage;
                    return 0;
                }

                return op == ProgrammerOp.Divide ? a / b : a % b;
            case ProgrammerOp.And: return Wrap(a & b);
            case ProgrammerOp.Or: return Wrap(a | b);
            case ProgrammerOp.Xor: return Wrap(a ^ b);
            case ProgrammerOp.Nand: return Wrap(~(a & b));
            case ProgrammerOp.Nor: return Wrap(~(a | b));
            case ProgrammerOp.Lsh:
            case ProgrammerOp.Rsh:
                if (b < 0)
                {
                    error = "Result not defined";
                    return 0;
                }

                if (b >= Bits)
                {
                    error = "Result not defined";
                    return 0;
                }

                if (op == ProgrammerOp.Lsh)
                {
                    return Wrap((long)(Pattern(a) << (int)b));
                }

                return ShiftMode == ShiftMode.Logical
                    ? Wrap((long)(Pattern(a) >> (int)b))
                    : Wrap(a >> (int)b);
            case ProgrammerOp.RoL:
            case ProgrammerOp.RoR:
            {
                var n = (int)(((b % Bits) + Bits) % Bits);
                if (op == ProgrammerOp.RoR)
                {
                    n = (Bits - n) % Bits;
                }

                if (n == 0)
                {
                    return a;
                }

                var p = Pattern(a);
                return Wrap((long)(((p << n) | (p >> (Bits - n))) & Mask));
            }

            default:
            {
                var steps = (int)(((b % (Bits + 1)) + Bits + 1) % (Bits + 1));
                var p = Pattern(a);
                for (var i = 0; i < steps; i++)
                {
                    if (op == ProgrammerOp.RoLC)
                    {
                        var msb = ((p >> (Bits - 1)) & 1) != 0;
                        p = ((p << 1) | (_carry ? 1UL : 0)) & Mask;
                        _carry = msb;
                    }
                    else
                    {
                        var lsb = (p & 1) != 0;
                        p = (p >> 1) | (_carry ? 1UL << (Bits - 1) : 0);
                        _carry = lsb;
                    }
                }

                return Wrap((long)p);
            }
        }
    }

    // ---- memory --------------------------------------------------------------------------

    public void MemoryClear()
    {
        _memory = 0;
        _hasMemory = false;
    }

    public void MemoryStore()
    {
        if (_error)
        {
            return;
        }

        _memory = _current;
        _hasMemory = true;
        _entering = false;
    }

    public void MemoryRecall()
    {
        if (!_hasMemory)
        {
            return;
        }

        if (_error)
        {
            ResetAll();
        }

        _current = _memory;
        _entering = false;
        _afterOperator = false;
    }

    public void MemoryAdd()
    {
        if (_error)
        {
            return;
        }

        _memory = Wrap(unchecked(_memory + _current));
        _hasMemory = true;
        _entering = false;
    }

    public void MemorySubtract()
    {
        if (_error)
        {
            return;
        }

        _memory = Wrap(unchecked(_memory - _current));
        _hasMemory = true;
        _entering = false;
    }
}
