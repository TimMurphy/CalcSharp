using System.Text.RegularExpressions;
using CalcSharp.Core.Programmer;

namespace CalcSharp.Core.Tests.Programmer;

/// <summary>
/// Compares the engine against results recorded from the real Windows Calculator in Programmer mode
/// by tools/Record-Oracle.ps1 with tools/oracle/programmer.json.
/// Columns: keys, display, expression, hex, dec, oct, bin.
/// </summary>
public class ProgrammerGoldenTests
{
    public static IEnumerable<object[]> Rows() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "GoldenData"), "programmer*.tsv")
            .SelectMany(File.ReadAllLines)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split('\t'))
            .Where(p => p.Length >= 7)
            .Select(p => new object[] { p[0], p[1], p[2], p[3], p[4], p[5], p[6] });

    private static string Squash(string s) => Regex.Replace(s, @"\s", string.Empty);

    [Theory]
    [MemberData(nameof(Rows))]
    public void MatchesRealCalculator(
        string keys, string display, string expression, string hex, string dec, string oct, string bin)
    {
        var c = new ProgrammerCalculator();
        foreach (var key in keys)
        {
            switch (key)
            {
                case >= '0' and <= '9': c.Digit(key - '0'); break;
                case 'A': c.Digit(10); break;
                case 'B': c.Digit(11); break;
                case 'C': c.Digit(12); break;
                case 'D': c.Digit(13); break;
                case 'E': c.Digit(14); break;
                case 'F': c.Digit(15); break;
                case '+': c.Operator(ProgrammerOp.Add); break;
                case '-': c.Operator(ProgrammerOp.Subtract); break;
                case '*': c.Operator(ProgrammerOp.Multiply); break;
                case '/': c.Operator(ProgrammerOp.Divide); break;
                case '=': c.Evaluate(); break;
                case 'n': c.Negate(); break;
                case '<': c.Backspace(); break;
                case 'x': c.Clear(); break;
                case 'm': c.Operator(ProgrammerOp.Mod); break;
                case '(': c.OpenParenthesis(); break;
                case '&': c.Operator(ProgrammerOp.And); break;
                case '|': c.Operator(ProgrammerOp.Or); break;
                case '~': c.Not(); break;
                case '@': c.Operator(ProgrammerOp.Nand); break;
                case '#': c.Operator(ProgrammerOp.Nor); break;
                case '^': c.Operator(ProgrammerOp.Xor); break;
                case 'l': c.Operator(LeftShift(c)); break;
                case 'r': c.Operator(RightShift(c)); break;
                case 'h': c.SetRadix(Radix.Hex); break;
                case 'j': c.SetRadix(Radix.Dec); break;
                case 'o': c.SetRadix(Radix.Oct); break;
                case 'i': c.SetRadix(Radix.Bin); break;
                case 'w': c.CycleWordSize(); break;
                case 'u': c.MemoryStore(); break;
                case 'y': c.ShiftMode = ShiftMode.Arithmetic; break;
                case 'z': c.ShiftMode = ShiftMode.Logical; break;
                case 'k': c.ShiftMode = ShiftMode.RotateCircular; break;
                case 'p': c.ShiftMode = ShiftMode.RotateThroughCarry; break;
                default: throw new ArgumentException(keys);
            }
        }

        var expectedExpression = expression
            .Replace("<missing>", string.Empty)
            .Replace("Minus (", "−")
            .Replace("left shift (", "<<")
            .Replace("right shift (", ">>")
            .TrimEnd();

        Assert.Equal(
            (Squash(display), expectedExpression, Squash(hex), Squash(dec), Squash(oct), Squash(bin)),
            (Squash(c.Display), c.Expression, Squash(c.Hex), Squash(c.Dec), Squash(c.Oct), Squash(c.Bin)));
    }

    private static ProgrammerOp LeftShift(ProgrammerCalculator c) => c.ShiftMode switch
    {
        ShiftMode.RotateCircular => ProgrammerOp.RoL,
        ShiftMode.RotateThroughCarry => ProgrammerOp.RoLC,
        _ => ProgrammerOp.Lsh,
    };

    private static ProgrammerOp RightShift(ProgrammerCalculator c) => c.ShiftMode switch
    {
        ShiftMode.RotateCircular => ProgrammerOp.RoR,
        ShiftMode.RotateThroughCarry => ProgrammerOp.RoRC,
        _ => ProgrammerOp.Rsh,
    };
}
