using CalcSharp.Core.Scientific;

namespace CalcSharp.Core.Tests.Scientific;

public class ScientificCalculatorTests
{
    /// <summary>Presses space-separated keys, e.g. "2 + 3 * 4 =".</summary>
    private static ScientificCalculator Keys(string keys, ScientificCalculator? calc = null)
    {
        var c = calc ?? new ScientificCalculator();
        foreach (var key in keys.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (key)
            {
                case "+": c.Operator(ScientificOperator.Add); break;
                case "-": c.Operator(ScientificOperator.Subtract); break;
                case "*": c.Operator(ScientificOperator.Multiply); break;
                case "/": c.Operator(ScientificOperator.Divide); break;
                case "mod": c.Operator(ScientificOperator.Mod); break;
                case "pow": c.Operator(ScientificOperator.Power); break;
                case "yroot": c.Operator(ScientificOperator.Root); break;
                case "logy": c.Operator(ScientificOperator.LogBase); break;
                case "=": c.Evaluate(); break;
                case ".": c.DecimalPoint(); break;
                case "(": c.OpenParenthesis(); break;
                case ")": c.CloseParenthesis(); break;
                case "neg": c.Negate(); break;
                case "sqr": c.Square(); break;
                case "cube": c.Cube(); break;
                case "sqrt": c.SquareRoot(); break;
                case "cbrt": c.CubeRoot(); break;
                case "10x": c.TenPower(); break;
                case "2x": c.TwoPower(); break;
                case "ex": c.EPower(); break;
                case "log": c.Log(); break;
                case "ln": c.Ln(); break;
                case "fact": c.Factorial(); break;
                case "inv": c.Reciprocal(); break;
                case "abs": c.Abs(); break;
                case "exp": c.Exp(); break;
                case "pi": c.Pi(); break;
                case "e": c.E(); break;
                case "sin": c.Trig(TrigFunction.Sin); break;
                case "cos": c.Trig(TrigFunction.Cos); break;
                case "tan": c.Trig(TrigFunction.Tan); break;
                case "sec": c.Trig(TrigFunction.Sec); break;
                case "csc": c.Trig(TrigFunction.Csc); break;
                case "cot": c.Trig(TrigFunction.Cot); break;
                case "2nd": c.ToggleInverse(); break;
                case "hyp": c.ToggleHyperbolic(); break;
                case "angle": c.CycleAngleMode(); break;
                case "fe": c.ToggleFixedExponent(); break;
                case "dms": c.Dms(); break;
                case "deg": c.Degrees(); break;
                case "back": c.Backspace(); break;
                case "CE": c.ClearEntry(); break;
                case "C": c.Clear(); break;
                case "MC": c.MemoryClear(); break;
                case "MR": c.MemoryRecall(); break;
                case "MS": c.MemoryStore(); break;
                case "M+": c.MemoryAdd(); break;
                case "M-": c.MemorySubtract(); break;
                default:
                    foreach (var ch in key)
                    {
                        c.Digit(ch - '0');
                    }

                    break;
            }
        }

        return c;
    }

    private static string Show(string keys) => Keys(keys).Display;

    [Fact]
    public void InitialDisplayIsZero() => Assert.Equal("0", new ScientificCalculator().Display);

    [Fact]
    public void DigitsAreGrouped() => Assert.Equal("1,234,567", Show("1234567"));

    [Fact]
    public void LimitsEntryToSixteenDigits() => Assert.Equal("1,234,567,890,123,456", Show("12345678901234567"));

    [Fact]
    public void DecimalEntry() => Assert.Equal("3.14", Show("3 . 1 4"));

    [Fact]
    public void AdditionWorks() => Assert.Equal("5", Show("2 + 3 ="));

    [Fact]
    public void MultiplicationBindsTighterThanAddition() => Assert.Equal("14", Show("2 + 3 * 4 ="));

    [Fact]
    public void DivisionBindsTighterThanSubtraction() => Assert.Equal("4", Show("10 - 12 / 2 ="));

    [Fact]
    public void PowerBindsTighterThanMultiplication() => Assert.Equal("18", Show("2 * 3 pow 2 ="));

    [Fact]
    public void ParenthesesOverridePrecedence() => Assert.Equal("20", Show("( 2 + 3 ) * 4 ="));

    [Fact]
    public void NestedParentheses() => Assert.Equal("26", Show("2 * ( 3 + ( 4 * 2 ) + 2 ) ="));

    [Fact]
    public void OpenParenCountIsTracked()
    {
        var c = Keys("( ( 2");
        Assert.Equal(2, c.OpenParenCount);
        Keys(")", c);
        Assert.Equal(1, c.OpenParenCount);
    }

    [Fact]
    public void UnclosedParenthesesAreClosedOnEquals()
    {
        var c = Keys("2 * ( 3 + 4 =");
        Assert.Equal("14", c.Display);
        Assert.Equal(0, c.OpenParenCount);
    }

    [Fact]
    public void ExtraCloseParenthesisIsIgnored() => Assert.Equal("5", Show("5 ) ="));

    [Fact]
    public void OperatorReplacedBeforeSecondOperand() => Assert.Equal("6", Show("2 + * 3 ="));

    [Fact]
    public void EqualsRepeatsLastOperation() => Assert.Equal("11", Show("5 + 3 = ="));

    [Fact]
    public void EqualsAfterOperatorUsesDisplayedValue() => Assert.Equal("10", Show("5 + ="));

    [Fact]
    public void ExpressionLineShowsFormula()
    {
        var c = Keys("2 + 3 *");
        Assert.Equal("2 + 3 ×", c.Expression);
    }

    [Fact]
    public void Square() => Assert.Equal("49", Show("7 sqr"));

    [Fact]
    public void Cube() => Assert.Equal("27", Show("3 cube"));

    [Fact]
    public void SquareRoot() => Assert.Equal("12", Show("144 sqrt"));

    [Fact]
    public void CubeRoot() => Assert.Equal("3", Show("27 cbrt"));

    [Fact]
    public void CubeRootOfNegative() => Assert.Equal("-3", Show("27 neg cbrt"));

    [Fact]
    public void SqrtTwoSquaredIsTwo() => Assert.Equal("2", Show("2 sqrt sqr"));

    [Fact]
    public void XToTheY() => Assert.Equal("1,024", Show("2 pow 10 ="));

    [Fact]
    public void YthRoot() => Assert.Equal("3", Show("81 yroot 4 ="));

    [Fact]
    public void TenToTheX() => Assert.Equal("1,000", Show("3 10x"));

    [Fact]
    public void TwoToTheX() => Assert.Equal("256", Show("8 2x"));

    [Fact]
    public void ETheX() => Assert.Equal("1", Show("0 ex"));

    [Fact]
    public void Log10() => Assert.Equal("3", Show("1000 log"));

    [Fact]
    public void NaturalLog() => Assert.Equal("1", Show("e ln"));

    [Fact]
    public void LogBaseY() => Assert.Equal("5", Show("32 logy 2 ="));

    [Fact]
    public void FactorialOfFive() => Assert.Equal("120", Show("5 fact"));

    [Fact]
    public void FactorialOfZero() => Assert.Equal("1", Show("0 fact"));

    [Fact]
    public void FactorialOfNonInteger() => Assert.Equal("1.32934038817914", Show("1 . 5 fact"));

    [Fact]
    public void ReciprocalOfFour() => Assert.Equal("0.25", Show("4 inv"));

    [Fact]
    public void AbsoluteValue() => Assert.Equal("7", Show("7 neg abs"));

    [Fact]
    public void ModTakesSignOfDividend() => Assert.Equal("-1", Show("7 neg mod 3 ="));

    [Fact]
    public void ModBasic() => Assert.Equal("1", Show("10 mod 3 ="));

    [Fact]
    public void PiHasSixteenDigits() => Assert.Equal("3.141592653589793", Show("pi"));

    [Fact]
    public void EHasSixteenDigits() => Assert.Equal("2.718281828459045", Show("e"));

    [Fact]
    public void ExpEntry() => Assert.Equal("1,500", Show("1 . 5 exp 3 ="));

    [Fact]
    public void ExpEntryShowsExponent() => Assert.Equal("1.5e+3", Show("1 . 5 exp 3"));

    [Fact]
    public void ExpEntryNegativeExponent() => Assert.Equal("0.015", Show("1 . 5 exp 2 neg ="));

    [Fact]
    public void SinNinetyDegrees() => Assert.Equal("1", Show("90 sin"));

    [Fact]
    public void SinOfThirtyDegreesIsHalf() => Assert.Equal("0.5", Show("30 sin"));

    [Fact]
    public void CosSixtyDegrees() => Assert.Equal("0.5", Show("60 cos"));

    [Fact]
    public void CosNinetyDegreesIsExactlyZero() => Assert.Equal("0", Show("90 cos"));

    [Fact]
    public void SinOneEightyDegreesIsExactlyZero() => Assert.Equal("0", Show("180 sin"));

    [Fact]
    public void TanFortyFive() => Assert.Equal("1", Show("45 tan"));

    [Fact]
    public void TanNinetyDivideByZero() => Assert.Equal("Cannot divide by zero", Show("90 tan"));

    [Fact]
    public void SecSixtyDegrees() => Assert.Equal("2", Show("60 sec"));

    [Fact]
    public void CscThirtyDegrees() => Assert.Equal("2", Show("30 csc"));

    [Fact]
    public void CotFortyFive() => Assert.Equal("1", Show("45 cot"));

    [Fact]
    public void RadiansMode()
    {
        var c = Keys("angle");
        Assert.Equal(AngleMode.Radians, c.AngleMode);
        Keys("pi cos", c);
        Assert.Equal("-1", c.Display);
    }

    [Fact]
    public void GradiansMode()
    {
        var c = Keys("angle angle 100 sin");
        Assert.Equal(AngleMode.Gradians, c.AngleMode);
        Assert.Equal("1", c.Display);
    }

    [Fact]
    public void AngleModeCyclesBackToDegrees()
    {
        var c = Keys("angle angle angle");
        Assert.Equal(AngleMode.Degrees, c.AngleMode);
    }

    [Fact]
    public void InverseSine() => Assert.Equal("30", Show("2nd 0 . 5 sin"));

    [Fact]
    public void InverseCosine() => Assert.Equal("60", Show("2nd 0 . 5 cos"));

    [Fact]
    public void InverseTangent() => Assert.Equal("45", Show("2nd 1 tan"));

    [Fact]
    public void InverseSineInRadians() => Assert.Equal("1.570796326794897", Show("angle 2nd 1 sin"));

    [Fact]
    public void InverseSineOutOfDomain() => Assert.Equal("Invalid input", Show("2nd 2 sin"));

    [Fact]
    public void HyperbolicSineOfZero() => Assert.Equal("0", Show("hyp 0 sin"));

    [Fact]
    public void HyperbolicCosineOfZero() => Assert.Equal("1", Show("hyp 0 cos"));

    [Fact]
    public void InverseHyperbolicSine() => Assert.Equal("0", Show("hyp 2nd 0 sin"));

    [Fact]
    public void InverseHyperbolicCosineOutOfDomain() => Assert.Equal("Invalid input", Show("hyp 2nd 0 cos"));

    [Fact]
    public void DegreesToDms() => Assert.Equal("12.3", Show("1 2 . 5 dms"));

    [Fact]
    public void DmsToDegrees() => Assert.Equal("12.5", Show("1 2 . 3 deg"));

    [Fact]
    public void FixedExponentToggle()
    {
        var c = Keys("5 + 3 =");
        Keys("fe", c);
        Assert.Equal("8.e+0", c.Display);
        Keys("fe", c);
        Assert.Equal("8", c.Display);
    }

    [Fact]
    public void LargeNumbersUseExponentNotation() => Assert.Equal("1e+16", Show("1 0 pow 1 6 ="));

    [Fact]
    public void SmallNumbersUseExponentNotation() => Assert.Equal("1e-10", Show("1 0 pow 1 0 neg ="));

    [Fact]
    public void DivideByZero() => Assert.Equal("Cannot divide by zero", Show("5 / 0 ="));

    [Fact]
    public void ZeroDividedByZeroIsUndefined() => Assert.Equal("Result is undefined", Show("0 / 0 ="));

    [Fact]
    public void ReciprocalOfZero() => Assert.Equal("Cannot divide by zero", Show("0 inv"));

    [Fact]
    public void SquareRootOfNegativeIsInvalid() => Assert.Equal("Invalid input", Show("4 neg sqrt"));

    [Fact]
    public void LogOfZeroIsInvalid() => Assert.Equal("Invalid input", Show("0 log"));

    [Fact]
    public void FactorialOfNegativeIsInvalid() => Assert.Equal("Invalid input", Show("3 neg fact"));

    [Fact]
    public void ZeroToTheZeroIsUndefined() => Assert.Equal("Result is undefined", Show("0 pow 0 ="));

    [Fact]
    public void OverflowReported() => Assert.Equal("Overflow", Show("1 0 0 0 10x"));

    [Fact]
    public void ErrorIsClearedByDigit()
    {
        var c = Keys("5 / 0 =");
        Assert.True(c.IsError);
        Keys("7", c);
        Assert.False(c.IsError);
        Assert.Equal("7", c.Display);
    }

    [Fact]
    public void MemoryStoreAndRecall()
    {
        var c = Keys("1 2 MS C");
        Assert.True(c.HasMemory);
        Keys("MR", c);
        Assert.Equal("12", c.Display);
    }

    [Fact]
    public void MemoryAddAndSubtract()
    {
        var c = Keys("1 0 MS 5 M+ 2 M- MR");
        Assert.Equal("13", c.Display);
    }

    [Fact]
    public void MemoryClearResetsIndicator()
    {
        var c = Keys("5 MS MC");
        Assert.False(c.HasMemory);
    }

    [Fact]
    public void BackspaceRemovesLastDigit() => Assert.Equal("12", Show("1 2 3 back"));

    [Fact]
    public void BackspaceOnSingleDigitGivesZero() => Assert.Equal("0", Show("7 back"));

    [Fact]
    public void NegateWhileEntering() => Assert.Equal("-5", Show("5 neg"));

    [Fact]
    public void ClearEntryKeepsPendingExpression()
    {
        var c = Keys("2 + 9 CE 3 =");
        Assert.Equal("5", c.Display);
    }

    [Fact]
    public void ClearResetsEverything()
    {
        var c = Keys("2 + ( 3 C");
        Assert.Equal("0", c.Display);
        Assert.Equal(0, c.OpenParenCount);
        Assert.Equal("", c.Expression);
    }

    [Fact]
    public void UnaryResultFeedsOperator() => Assert.Equal("13", Show("4 + 3 sqr ="));

    [Fact]
    public void FunctionOfParenthesisedGroup() => Assert.Equal("25", Show("( 2 + 3 ) sqr"));

    [Fact]
    public void ExpressionShowsFunctionCallOnceCommitted()
    {
        var c = Keys("9 sqrt +");
        Assert.Equal("sqrt(9) +", c.Expression);
    }

    [Fact]
    public void DigitAfterEqualsStartsNewCalculation() => Assert.Equal("7", Show("2 + 3 = 7"));
}
