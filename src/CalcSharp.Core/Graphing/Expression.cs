using System.Globalization;

namespace CalcSharp.Core.Graphing;

/// <summary>Raised when an equation cannot be parsed. Message matches the real app's wording.</summary>
public sealed class GraphException(string message) : Exception(message)
{
    public const string EmptyExpression = "The expression is empty";
    public const string NoVariable = "The function must contain at least one x or y variable";
    public const string NoSolution = "The equation has no solution";
    public const string EqualWithoutEquation = "Equal was used without an equation";
    public const string MissingClose = "The equation is missing a closing parenthesis";
    public const string MissingOpen = "The equation is missing an opening parenthesis";
    public const string InvalidSyntax = "The equation syntax is invalid";
}

/// <summary>Node of a parsed expression of x and y.</summary>
public abstract class Expr
{
    public abstract double Eval(double x, double y);
    public abstract bool Uses(char variable);
}

internal sealed class Num(double v) : Expr
{
    public override double Eval(double x, double y) => v;
    public override bool Uses(char variable) => false;
}

internal sealed class Var(char name) : Expr
{
    public override double Eval(double x, double y) => name == 'x' ? x : y;
    public override bool Uses(char variable) => variable == name;
}

internal sealed class Neg(Expr a) : Expr
{
    public override double Eval(double x, double y) => -a.Eval(x, y);
    public override bool Uses(char variable) => a.Uses(variable);
}

internal sealed class Binary(char op, Expr a, Expr b) : Expr
{
    public override double Eval(double x, double y)
    {
        double l = a.Eval(x, y), r = b.Eval(x, y);
        return op switch
        {
            '+' => l + r,
            '-' => l - r,
            '*' => l * r,
            '/' => r == 0 ? double.NaN : l / r,
            _ => Pow(l, r),
        };
    }

    // Real-valued power: negative base only with an odd-denominator rational exponent.
    private static double Pow(double l, double r)
    {
        if (l < 0 && r != Math.Floor(r))
        {
            for (int d = 3; d <= 9; d += 2)
            {
                double n = r * d;
                if (Math.Abs(n - Math.Round(n)) < 1e-9)
                {
                    double v = Math.Pow(-l, r);
                    return Math.Abs(Math.Round(n) % 2) == 0 ? v : -v;
                }
            }
            return double.NaN;
        }
        if (l == 0 && r < 0) return double.NaN;
        return Math.Pow(l, r);
    }

    public override bool Uses(char variable) => a.Uses(variable) || b.Uses(variable);
}

internal sealed class Call(string name, Expr[] args) : Expr
{
    public override double Eval(double x, double y)
    {
        double a = args[0].Eval(x, y);
        double b = args.Length > 1 ? args[1].Eval(x, y) : 0;
        return name switch
        {
            "sqrt" => a < 0 ? double.NaN : Math.Sqrt(a),
            "abs" => Math.Abs(a),
            "sin" => Math.Sin(a),
            "cos" => Math.Cos(a),
            "tan" => Math.Abs(Math.Cos(a)) < 1e-15 ? double.NaN : Math.Tan(a),
            "asin" => Math.Asin(a),
            "acos" => Math.Acos(a),
            "atan" => Math.Atan(a),
            "sinh" => Math.Sinh(a),
            "cosh" => Math.Cosh(a),
            "tanh" => Math.Tanh(a),
            "ln" => a <= 0 ? double.NaN : Math.Log(a),
            "log" => a <= 0 ? double.NaN : Math.Log10(a),
            "exp" => Math.Exp(a),
            "floor" => Math.Floor(a),
            "ceil" => Math.Ceiling(a),
            "round" => Math.Round(a, MidpointRounding.AwayFromZero),
            "sign" => double.IsNaN(a) ? double.NaN : Math.Sign(a),
            "min" => Math.Min(a, b),
            "max" => Math.Max(a, b),
            _ => double.NaN,
        };
    }

    public override bool Uses(char variable) => args.Any(a => a.Uses(variable));
}

/// <summary>Recursive-descent parser for expressions of x and y with implicit multiplication.</summary>
public sealed class ExpressionParser
{
    private static readonly Dictionary<string, int> Functions = new()
    {
        ["sqrt"] = 1, ["abs"] = 1, ["sin"] = 1, ["cos"] = 1, ["tan"] = 1, ["asin"] = 1, ["acos"] = 1,
        ["atan"] = 1, ["sinh"] = 1, ["cosh"] = 1, ["tanh"] = 1, ["ln"] = 1, ["log"] = 1, ["exp"] = 1,
        ["floor"] = 1, ["ceil"] = 1, ["round"] = 1, ["sign"] = 1, ["min"] = 2, ["max"] = 2,
    };

    private static readonly string[] IdentNames =
        [.. Functions.Keys.Concat(["pi", "e", "x", "y"]).OrderByDescending(s => s.Length)];

    private enum T { Num, Name, Op, LParen, RParen, Comma, End }

    private readonly List<(T Kind, string Text, double Value)> _tokens = [];
    private int _pos;

    private ExpressionParser(string text) => Tokenize(text);

    /// <summary>Parses an expression (no equals sign). Throws <see cref="GraphException"/>.</summary>
    public static Expr Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new GraphException(GraphException.EmptyExpression);
        var p = new ExpressionParser(text);
        var e = p.Additive();
        if (p.Peek.Kind == T.RParen) throw new GraphException(GraphException.MissingOpen);
        if (p.Peek.Kind != T.End) throw new GraphException(GraphException.InvalidSyntax);
        return e;
    }

    private (T Kind, string Text, double Value) Peek => _tokens[_pos];

    private bool PeekOp(params string[] ops) => Peek.Kind == T.Op && ops.Contains(Peek.Text);

    private void Tokenize(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsAsciiDigit(c) || c == '.')
            {
                int st = i;
                while (i < s.Length && (char.IsAsciiDigit(s[i]) || s[i] == '.')) i++;
                if (!double.TryParse(s.AsSpan(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    throw new GraphException(GraphException.InvalidSyntax);
                _tokens.Add((T.Num, "", v));
            }
            else if (char.IsLetter(c))
            {
                int st = i;
                while (i < s.Length && char.IsLetter(s[i])) i++;
                SplitName(s[st..i].ToLowerInvariant().Replace("π", "pi"));
            }
            else
            {
                i++;
                switch (c)
                {
                    case '(' or '[' or '{': _tokens.Add((T.LParen, "(", 0)); break;
                    case ')' or ']' or '}': _tokens.Add((T.RParen, ")", 0)); break;
                    case ',': _tokens.Add((T.Comma, ",", 0)); break;
                    case '+' or '^': _tokens.Add((T.Op, c.ToString(), 0)); break;
                    case '-' or '−' or '–': _tokens.Add((T.Op, "-", 0)); break;
                    case '*' or '×' or '·': _tokens.Add((T.Op, "*", 0)); break;
                    case '/' or '÷': _tokens.Add((T.Op, "/", 0)); break;
                    case '²': _tokens.Add((T.Op, "^", 0)); _tokens.Add((T.Num, "", 2)); break;
                    case '³': _tokens.Add((T.Op, "^", 0)); _tokens.Add((T.Num, "", 3)); break;
                    case '√': _tokens.Add((T.Name, "sqrt", 0)); break;
                    default: throw new GraphException(GraphException.InvalidSyntax);
                }
            }
        }
        _tokens.Add((T.End, "", 0));
    }

    // Splits e.g. "xsin" -> x, sin ; "pix" -> pi, x.
    private void SplitName(string word)
    {
        int i = 0;
        while (i < word.Length)
        {
            int at = i;
            string? hit = IdentNames.FirstOrDefault(n => string.CompareOrdinal(word, at, n, 0, n.Length) == 0);
            if (hit is null) throw new GraphException(GraphException.InvalidSyntax);
            _tokens.Add((T.Name, hit, 0));
            i += hit.Length;
        }
    }

    private Expr Additive()
    {
        var l = Multiplicative();
        while (PeekOp("+", "-"))
        {
            char op = _tokens[_pos++].Text[0];
            l = new Binary(op, l, Multiplicative());
        }
        return l;
    }

    private Expr Multiplicative()
    {
        var l = Unary();
        while (true)
        {
            if (PeekOp("*", "/"))
            {
                char op = _tokens[_pos++].Text[0];
                l = new Binary(op, l, Unary());
            }
            else if (Peek.Kind is T.Num or T.Name or T.LParen)
            {
                l = new Binary('*', l, Unary()); // implicit multiplication
            }
            else return l;
        }
    }

    private Expr Unary()
    {
        if (PeekOp("-", "+"))
        {
            bool neg = _tokens[_pos++].Text == "-";
            var inner = Unary();
            return neg ? new Neg(inner) : inner;
        }
        return Power();
    }

    private Expr Power()
    {
        var b = Primary();
        if (PeekOp("^"))
        {
            _pos++;
            return new Binary('^', b, Unary()); // right-associative, allows 2^-x
        }
        return b;
    }

    private Expr Primary()
    {
        var t = _tokens[_pos];
        switch (t.Kind)
        {
            case T.Num: _pos++; return new Num(t.Value);
            case T.LParen:
            {
                _pos++;
                var e = Additive();
                if (Peek.Kind != T.RParen) throw new GraphException(GraphException.MissingClose);
                _pos++;
                return e;
            }
            case T.Name:
                _pos++;
                switch (t.Text)
                {
                    case "x" or "y": return new Var(t.Text[0]);
                    case "pi": return new Num(Math.PI);
                    case "e": return new Num(Math.E);
                }
                return FunctionCall(t.Text);
            case T.RParen: throw new GraphException(GraphException.MissingOpen);
            default: throw new GraphException(GraphException.InvalidSyntax);
        }
    }

    private Expr FunctionCall(string name)
    {
        int arity = Functions[name];
        if (Peek.Kind != T.LParen)
        {
            if (arity != 1) throw new GraphException(GraphException.InvalidSyntax);
            return new Call(name, [Power()]); // "sin x", "sinx"
        }
        _pos++;
        var args = new List<Expr> { Additive() };
        while (Peek.Kind == T.Comma)
        {
            _pos++;
            args.Add(Additive());
        }
        if (Peek.Kind != T.RParen) throw new GraphException(GraphException.MissingClose);
        _pos++;
        if (args.Count != arity) throw new GraphException(GraphException.InvalidSyntax);
        return new Call(name, [.. args]);
    }
}
