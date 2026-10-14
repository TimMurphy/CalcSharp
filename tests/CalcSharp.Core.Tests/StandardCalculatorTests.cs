using CalcSharp.Core;

namespace CalcSharp.Core.Tests;

public class StandardCalculatorTests
{
    private static StandardCalculator Press(params object[] keys)
    {
        var c = new StandardCalculator();
        foreach (var key in keys)
        {
            switch (key)
            {
                case int d: c.Digit(d); break;
                case Operation op: c.Operator(op); break;
                case "=": c.Evaluate(); break;
                case ".": c.DecimalPoint(); break;
                case "±": c.Negate(); break;
                case "%": c.Percent(); break;
                case "√": c.SquareRoot(); break;
                case "x²": c.Square(); break;
                case "1/x": c.Reciprocal(); break;
                case "⌫": c.Backspace(); break;
                case "CE": c.ClearEntry(); break;
                case "C": c.Clear(); break;
                case "MC": c.MemoryClear(); break;
                case "MR": c.MemoryRecall(); break;
                case "MS": c.MemoryStore(); break;
                case "M+": c.MemoryAdd(); break;
                case "M-": c.MemorySubtract(); break;
                default: throw new ArgumentException(key.ToString());
            }
        }

        return c;
    }

    [Fact]
    public void StartsAtZero() => Assert.Equal("0", new StandardCalculator().Display);

    [Fact]
    public void EnteringDigitsReplacesLeadingZero() => Assert.Equal("12", Press(0, 1, 2).Display);

    [Fact]
    public void LargeNumbersAreGrouped() => Assert.Equal("1,234,567", Press(1, 2, 3, 4, 5, 6, 7).Display);

    [Fact]
    public void DigitEntryIsLimitedToSixteen()
    {
        var c = new StandardCalculator();
        for (var i = 0; i < 20; i++) c.Digit(1);
        Assert.Equal("1,111,111,111,111,111", c.Display);
    }

    [Fact]
    public void DecimalPointIsOnlyAddedOnce() => Assert.Equal("1.5", Press(1, ".", 5, ".").Display);

    [Fact]
    public void LeadingDecimalPointShowsZero() => Assert.Equal("0.5", Press(".", 5).Display);

    [Theory]
    [InlineData(Operation.Add, "8")]
    [InlineData(Operation.Subtract, "2")]
    [InlineData(Operation.Multiply, "15")]
    [InlineData(Operation.Divide, "1.666666666666667")]
    public void BasicArithmetic(Operation op, string expected) =>
        Assert.Equal(expected, Press(5, op, 3, "=").Display);

    [Fact]
    public void ChainedOperationsEvaluateLeftToRight() =>
        Assert.Equal("20", Press(2, Operation.Add, 3, Operation.Multiply, 4, "=").Display);

    [Fact]
    public void ChainedOperatorShowsIntermediateResult() =>
        Assert.Equal("5", Press(2, Operation.Add, 3, Operation.Multiply).Display);

    [Fact]
    public void ExpressionLineTracksOperations() =>
        Assert.Equal("2 + 3 ×", Press(2, Operation.Add, 3, Operation.Multiply).Expression);

    [Fact]
    public void ExpressionLineAfterEquals() =>
        Assert.Equal("2 + 3 =", Press(2, Operation.Add, 3, "=").Expression);

    [Fact]
    public void RepeatedEqualsRepeatsLastOperation() =>
        Assert.Equal("14", Press(5, Operation.Add, 3, "=", "=", "=").Display);

    [Fact]
    public void PressingOperatorTwiceReplacesIt() =>
        Assert.Equal("2", Press(5, Operation.Add, Operation.Subtract, 3, "=").Display);

    [Fact]
    public void OperatorThenEqualsUsesDisplayAsRightOperand() =>
        Assert.Equal("10", Press(5, Operation.Add, "=").Display);

    [Fact]
    public void ContinuingFromResult() =>
        Assert.Equal("10", Press(5, Operation.Add, 3, "=", Operation.Add, 2, "=").Display);

    [Fact]
    public void TypingAfterEqualsStartsNewCalculation() =>
        Assert.Equal("7", Press(5, Operation.Add, 3, "=", 7).Display);

    [Fact]
    public void DivideByZero()
    {
        var c = Press(5, Operation.Divide, 0, "=");
        Assert.Equal("Cannot divide by zero", c.Display);
        Assert.True(c.IsError);
    }

    [Fact]
    public void ZeroDividedByZeroIsUndefined() =>
        Assert.Equal("Result is undefined", Press(0, Operation.Divide, 0, "=").Display);

    [Fact]
    public void ErrorIsClearedByTypingDigit() =>
        Assert.Equal("4", Press(5, Operation.Divide, 0, "=", 4).Display);

    [Fact]
    public void Negate() => Assert.Equal("-5", Press(5, "±").Display);

    [Fact]
    public void NegateTwiceRestores() => Assert.Equal("5", Press(5, "±", "±").Display);

    [Fact]
    public void NegateZeroEntryKeepsPrefix() => Assert.Equal("-5", Press("±", 5).Display);

    [Fact]
    public void NegateResult() => Assert.Equal("-8", Press(5, Operation.Add, 3, "=", "±").Display);

    [Fact]
    public void SquareRoot() => Assert.Equal("3", Press(9, "√").Display);

    [Fact]
    public void SquareRootOfNegativeIsInvalid() =>
        Assert.Equal("Invalid input", Press(9, "±", "√").Display);

    [Fact]
    public void Square() => Assert.Equal("49", Press(7, "x²").Display);

    [Fact]
    public void Reciprocal() => Assert.Equal("0.25", Press(4, "1/x").Display);

    [Fact]
    public void ReciprocalOfZeroIsError() =>
        Assert.Equal("Cannot divide by zero", Press(0, "1/x").Display);

    [Fact]
    public void PercentIsRelativeToLeftOperand() =>
        Assert.Equal("10", Press(2, 0, 0, Operation.Multiply, 5, "%").Display);

    [Fact]
    public void PercentWithAddition() =>
        Assert.Equal("220", Press(2, 0, 0, Operation.Add, 1, 0, "%", "=").Display);

    [Fact]
    public void PercentWithNoOperatorIsZero() => Assert.Equal("0", Press(5, "%").Display);

    [Fact]
    public void BackspaceRemovesLastDigit() => Assert.Equal("12", Press(1, 2, 3, "⌫").Display);

    [Fact]
    public void BackspaceOnSingleDigitGivesZero() => Assert.Equal("0", Press(5, "⌫").Display);

    [Fact]
    public void BackspaceDoesNothingOnResult() =>
        Assert.Equal("8", Press(5, Operation.Add, 3, "=", "⌫").Display);

    [Fact]
    public void ClearEntryOnlyClearsCurrentOperand() =>
        Assert.Equal("9", Press(5, Operation.Add, 3, "CE", 4, "=").Display);

    [Fact]
    public void ClearResetsEverything()
    {
        var c = Press(5, Operation.Add, 3, "C");
        Assert.Equal("0", c.Display);
        Assert.Equal("", c.Expression);
    }

    [Fact]
    public void MemoryStoreAndRecall()
    {
        var c = Press(5, "MS", "C", "MR");
        Assert.Equal("5", c.Display);
        Assert.True(c.HasMemory);
    }

    [Fact]
    public void MemoryAddAndSubtract() =>
        Assert.Equal("7", Press(5, "MS", 4, "M+", 2, "M-", "C", "MR").Display);

    [Fact]
    public void MemoryClear()
    {
        var c = Press(5, "MS", "MC");
        Assert.False(c.HasMemory);
    }

    [Fact]
    public void PrecisionIsLimitedToSixteenDigits() =>
        Assert.Equal("0.3333333333333333", Press(1, Operation.Divide, 3, "=").Display);

    [Fact]
    public void DecimalAdditionIsExact() =>
        Assert.Equal("0.3", Press(0, ".", 1, Operation.Add, 0, ".", 2, "=").Display);
}
