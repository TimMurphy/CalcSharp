using CalcSharp.Core.Scientific;

namespace CalcSharp.Core.Tests.Scientific;

/// <summary>
/// Compares the engine against results recorded from the real Windows Calculator in Scientific mode
/// by tools/Record-Oracle.ps1 with tools/oracle/scientific.json.
/// </summary>
public class ScientificGoldenTests
{
    public static IEnumerable<object[]> Rows() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "GoldenData"), "scientific*.tsv")
            .SelectMany(File.ReadAllLines)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split('\t'))
            .Where(p => p.Length >= 2)
            .Select(p => new object[] { p[0], p[1], p.Length > 2 ? p[2] : string.Empty });

    [Theory]
    [MemberData(nameof(Rows))]
    public void MatchesRealCalculator(string keys, string display, string expression)
    {
        var c = new ScientificCalculator();
        foreach (var key in keys)
        {
            switch (key)
            {
                case >= '0' and <= '9': c.Digit(key - '0'); break;
                case '.': c.DecimalPoint(); break;
                case '+': c.Operator(ScientificOperator.Add); break;
                case '-': c.Operator(ScientificOperator.Subtract); break;
                case '*': c.Operator(ScientificOperator.Multiply); break;
                case '/': c.Operator(ScientificOperator.Divide); break;
                case '=': c.Evaluate(); break;
                case 'n': c.Negate(); break;
                case 'b': c.Backspace(); break;
                case 'c': c.Clear(); break;
                case 's': c.SquareRoot(); break;
                case 'q': c.Square(); break;
                case 'r': c.Reciprocal(); break;
                case 'a': c.Abs(); break;
                case 'x': c.Exp(); break;
                case 'm': c.Operator(ScientificOperator.Mod); break;
                case '(': c.OpenParenthesis(); break;
                case ')': c.CloseParenthesis(); break;
                case '!': c.Factorial(); break;
                case 'p': c.Pi(); break;
                case 'v': c.E(); break;
                case '^': c.Operator(ScientificOperator.Power); break;
                case 'w': c.TenPower(); break;
                case 'g': c.Log(); break;
                case 'l': c.Ln(); break;
                case 'i': c.Trig(TrigFunction.Sin); break;
                case 'o': c.Trig(TrigFunction.Cos); break;
                case 't': c.Trig(TrigFunction.Tan); break;
                case 'k': c.Trig(TrigFunction.Sec); break;
                case 'j': c.Trig(TrigFunction.Csc); break;
                case 'y': c.Trig(TrigFunction.Cot); break;
                case 'u': c.ToggleInverse(); break;
                case 'z': c.CycleAngleMode(); break;
                case 'f': c.ToggleFixedExponent(); break;
                case '#': c.Cube(); break;
                case '$': c.CubeRoot(); break;
                case '&': c.Operator(ScientificOperator.Root); break;
                case '@': c.TwoPower(); break;
                case ':': c.Operator(ScientificOperator.LogBase); break;
                case ';': c.EPower(); break;
                case 'd': c.Dms(); break;
                case 'e': c.Degrees(); break;
                case '|': c.Floor(); break;
                case '~': c.Ceiling(); break;
                case '[': InverseTrig(c, TrigFunction.Sin); break;
                case ']': InverseTrig(c, TrigFunction.Cos); break;
                case '{': InverseTrig(c, TrigFunction.Tan); break;
                case '}': HyperbolicTrig(c, TrigFunction.Sin); break;
                case '<': HyperbolicTrig(c, TrigFunction.Cos); break;
                case '>': HyperbolicTrig(c, TrigFunction.Tan); break;
                case '%': InverseTrig(c, TrigFunction.Sec); break;
                case '`': InverseTrig(c, TrigFunction.Csc); break;
                case '\'': HyperbolicTrig(c, TrigFunction.Sec); break;
                case '_': HyperbolicTrig(c, TrigFunction.Csc); break;
                default: throw new ArgumentException(keys);
            }
        }

        var expected = expression.Replace("<missing>", string.Empty).Replace("Minus (", "−").Replace("Log base (", "Log base").TrimEnd();
        Assert.Equal((display.Replace(" point", "."), expected), (c.Display, c.Expression));
    }

    private static void InverseTrig(ScientificCalculator c, TrigFunction f)
    {
        var before = c.Inverse;
        c.Inverse = true;
        c.Trig(f);
        c.Inverse = before;
    }

    private static void HyperbolicTrig(ScientificCalculator c, TrigFunction f)
    {
        var before = c.Hyperbolic;
        c.Hyperbolic = true;
        c.Trig(f);
        c.Hyperbolic = before;
    }
}