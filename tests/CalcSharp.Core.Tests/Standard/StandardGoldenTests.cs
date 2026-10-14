using CalcSharp.Core;

namespace CalcSharp.Core.Tests.Standard;

/// <summary>
/// Compares the engine against results recorded from the real Windows Calculator by
/// tools/Record-StandardOracle.ps1 (see GoldenData/standard.tsv).
/// </summary>
public class StandardGoldenTests
{
    public static IEnumerable<object[]> Rows() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "GoldenData"), "standard*.tsv").SelectMany(File.ReadAllLines)
            .Select(l => l.Split('\t'))
            .Where(p => p.Length >= 2)
            .Select(p => new object[] { p[0], p[1], p.Length > 2 ? p[2] : string.Empty });

    [Theory]
    [MemberData(nameof(Rows))]
    public void MatchesRealCalculator(string keys, string display, string expression)
    {
        var c = new StandardCalculator();
        foreach (var key in keys)
        {
            switch (key)
            {
                case >= '0' and <= '9': c.Digit(key - '0'); break;
                case '.': c.DecimalPoint(); break;
                case '+': c.Operator(Operation.Add); break;
                case '-': c.Operator(Operation.Subtract); break;
                case '*': c.Operator(Operation.Multiply); break;
                case '/': c.Operator(Operation.Divide); break;
                case '=': c.Evaluate(); break;
                case 'n': c.Negate(); break;
                case 'p': c.Percent(); break;
                case 'r': c.Reciprocal(); break;
                case 's': c.SquareRoot(); break;
                case 'q': c.Square(); break;
                case 'b': c.Backspace(); break;
                case 'e': c.ClearEntry(); break;
                case 'c': c.Clear(); break;
                case 'm': c.MemoryStore(); break;
                case 'k': c.MemoryRecall(); break;
                case 'a': c.MemoryAdd(); break;
                case 'u': c.MemorySubtract(); break;
                case 'l': c.MemoryClear(); break;
                default: throw new ArgumentException(keys);
            }
        }

        // The accessible name of the real expression line spells the minus operator out.
        var expected = expression.Replace("Minus (", "−").TrimEnd();
        Assert.Equal((display.Replace(" point", "."), expected), (c.Display, c.Expression));
    }
}
