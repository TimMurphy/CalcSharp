namespace CalcSharp.Core.Graphing;

public enum EquationKind
{
    /// <summary>y = f(x)</summary>
    YOfX,
    /// <summary>x = g(y)</summary>
    XOfY,
    /// <summary>f(x, y) = g(x, y), plotted as the zero set of f - g.</summary>
    Implicit,
}

/// <summary>A parsed graphable equation.</summary>
public sealed class GraphEquation
{
    private readonly Expr _expr;

    private GraphEquation(string source, EquationKind kind, Expr expr)
    {
        Source = source;
        Kind = kind;
        _expr = expr;
    }

    public string Source { get; }
    public EquationKind Kind { get; }

    /// <summary>For YOfX: y at x. For XOfY: x at y (pass y as the argument).</summary>
    public double Evaluate(double t) => Kind == EquationKind.XOfY ? _expr.Eval(0, t) : _expr.Eval(t, 0);

    /// <summary>Residual f(x,y) (left minus right) for implicit equations.</summary>
    public double Residual(double x, double y) => _expr.Eval(x, y);

    /// <summary>Parses text such as "x^2", "y=sin(x)", "x=3", "x^2+y^2=25". Throws <see cref="GraphException"/>.</summary>
    public static GraphEquation Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new GraphException(GraphException.EmptyExpression);
        string[] parts = text.Split('=');
        if (parts.Length > 2 || (parts.Length == 2 && parts.Any(string.IsNullOrWhiteSpace)))
            throw new GraphException(GraphException.EqualWithoutEquation);

        if (parts.Length == 1)
        {
            var e = ExpressionParser.Parse(parts[0]);
            if (!e.Uses('x') && !e.Uses('y')) throw new GraphException(GraphException.NoVariable);
            return new GraphEquation(text, e.Uses('y') ? EquationKind.Implicit : EquationKind.YOfX, e);
        }

        var l = ExpressionParser.Parse(parts[0]);
        var r = ExpressionParser.Parse(parts[1]);
        if (!(l.Uses('x') || l.Uses('y') || r.Uses('x') || r.Uses('y')))
        {
            double a = l.Eval(0, 0), b = r.Eval(0, 0);
            throw new GraphException(Math.Abs(a - b) < 1e-12 ? GraphException.NoVariable : GraphException.NoSolution);
        }

        if (IsVar(parts[0], 'y') && !r.Uses('y')) return new GraphEquation(text, EquationKind.YOfX, r);
        if (IsVar(parts[1], 'y') && !l.Uses('y')) return new GraphEquation(text, EquationKind.YOfX, l);
        if (IsVar(parts[0], 'x') && !r.Uses('x')) return new GraphEquation(text, EquationKind.XOfY, r);
        if (IsVar(parts[1], 'x') && !l.Uses('x')) return new GraphEquation(text, EquationKind.XOfY, l);
        return new GraphEquation(text, EquationKind.Implicit, new Binary('-', l, r));
    }

    private static bool IsVar(string s, char v) => s.Trim().Equals(v.ToString(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Non-throwing parse; error is the real-app message on failure.</summary>
    public static bool TryParse(string text, out GraphEquation? equation, out string? error)
    {
        try
        {
            equation = Parse(text);
            error = null;
            return true;
        }
        catch (GraphException ex)
        {
            equation = null;
            error = ex.Message;
            return false;
        }
    }
}
