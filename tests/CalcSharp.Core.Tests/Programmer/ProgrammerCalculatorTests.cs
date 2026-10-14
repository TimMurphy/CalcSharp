using CalcSharp.Core.Programmer;
using Xunit;

namespace CalcSharp.Core.Tests.Programmer;

public class ProgrammerCalculatorTests
{
    private static ProgrammerCalculator Calc(Radix radix = Radix.Dec, WordSize size = WordSize.Qword)
    {
        var c = new ProgrammerCalculator();
        c.SetRadix(radix);
        c.SetWordSize(size);
        return c;
    }

    private static void Type(ProgrammerCalculator c, string digits)
    {
        foreach (var ch in digits)
        {
            c.Digit(Convert.ToInt32(ch.ToString(), 16));
        }
    }

    private static void Run(ProgrammerCalculator c, string a, ProgrammerOp op, string b)
    {
        Type(c, a);
        c.Operator(op);
        Type(c, b);
        c.Evaluate();
    }

    [Fact]
    public void StartsAtZeroInDecimalQword()
    {
        var c = new ProgrammerCalculator();
        Assert.Equal("0", c.Display);
        Assert.Equal(WordSize.Qword, c.WordSize);
        Assert.Equal(Radix.Dec, c.Radix);
    }

    [Fact]
    public void ShowsValueInAllRadixes()
    {
        var c = Calc();
        Type(c, "255");
        Assert.Equal("FF", c.Hex);
        Assert.Equal("255", c.Dec);
        Assert.Equal("377", c.Oct);
        Assert.Equal("1111 1111", c.Bin);
    }

    [Fact]
    public void HexEntryAcceptsLetters()
    {
        var c = Calc(Radix.Hex);
        Type(c, "1AF");
        Assert.Equal(0x1AF, c.Value);
        Assert.Equal("1AF", c.Display);
    }

    [Fact]
    public void DigitsAboveRadixAreIgnored()
    {
        var c = Calc(Radix.Oct);
        Type(c, "789");
        Assert.Equal(7, c.Value);
        Assert.False(c.IsDigitEnabled(8));
        Assert.True(c.IsDigitEnabled(7));
    }

    [Fact]
    public void BinaryEntryOnlyZeroAndOne()
    {
        var c = Calc(Radix.Bin);
        Type(c, "1021");
        Assert.Equal(0b101, c.Value);
    }

    [Fact]
    public void HexLettersEnabledOnlyInHex()
    {
        var c = Calc(Radix.Hex);
        Assert.True(c.IsDigitEnabled(15));
        c.SetRadix(Radix.Dec);
        Assert.False(c.IsDigitEnabled(10));
    }

    [Fact]
    public void SwitchingRadixKeepsValue()
    {
        var c = Calc();
        Type(c, "26");
        c.SetRadix(Radix.Hex);
        Assert.Equal("1A", c.Display);
        c.SetRadix(Radix.Bin);
        Assert.Equal("0001 1010", c.Display);
    }

    [Fact]
    public void Addition()
    {
        var c = Calc();
        Run(c, "12", ProgrammerOp.Add, "30");
        Assert.Equal("42", c.Display);
    }

    [Fact]
    public void SubtractionGoesNegative()
    {
        var c = Calc();
        Run(c, "5", ProgrammerOp.Subtract, "8");
        Assert.Equal("-3", c.Display);
        Assert.Equal("FFFFFFFFFFFFFFFD", c.Hex);
    }

    [Fact]
    public void IntegerDivisionTruncatesTowardZero()
    {
        var c = Calc();
        Run(c, "7", ProgrammerOp.Divide, "2");
        Assert.Equal(3, c.Value);
        var d = Calc();
        d.Digit(7);
        d.Negate();
        d.Operator(ProgrammerOp.Divide);
        d.Digit(2);
        d.Evaluate();
        Assert.Equal(-3, d.Value);
    }

    [Fact]
    public void ModKeepsSignOfDividend()
    {
        var c = Calc();
        c.Digit(7);
        c.Negate();
        c.Operator(ProgrammerOp.Mod);
        c.Digit(3);
        c.Evaluate();
        Assert.Equal(-1, c.Value);
    }

    [Fact]
    public void DivideByZeroMessage()
    {
        var c = Calc();
        Run(c, "5", ProgrammerOp.Divide, "0");
        Assert.True(c.IsError);
        Assert.Equal("Cannot divide by zero", c.Display);
    }

    [Fact]
    public void ZeroDividedByZeroIsUndefined()
    {
        var c = Calc();
        Run(c, "0", ProgrammerOp.Divide, "0");
        Assert.Equal("Result is undefined", c.Display);
    }

    [Fact]
    public void MinValueDividedByMinusOneOverflows()
    {
        var c = Calc(size: WordSize.Byte);
        Type(c, "128"); // rejected digit keeps 12
        c.ClearEntry();
        c.Digit(1);
        c.Digit(2);
        c.Digit(7);
        c.Negate();
        c.Operator(ProgrammerOp.Subtract);
        c.Digit(1);
        c.Evaluate();
        Assert.Equal(-128, c.Value);
        c.Operator(ProgrammerOp.Divide);
        c.Digit(1);
        c.Negate();
        c.Evaluate();
        Assert.Equal("Overflow", c.Display);
    }

    [Fact]
    public void ErrorClearedByClearEntry()
    {
        var c = Calc();
        Run(c, "1", ProgrammerOp.Divide, "0");
        c.ClearEntry();
        Assert.False(c.IsError);
        Assert.Equal("0", c.Display);
    }

    [Fact]
    public void ErrorClearedByTypingDigit()
    {
        var c = Calc();
        Run(c, "1", ProgrammerOp.Divide, "0");
        c.Digit(4);
        Assert.Equal("4", c.Display);
    }

    [Fact]
    public void AdditionWrapsInByte()
    {
        var c = Calc(size: WordSize.Byte);
        Run(c, "127", ProgrammerOp.Add, "1");
        Assert.Equal(-128, c.Value);
        Assert.Equal("80", c.Hex);
    }

    [Fact]
    public void MultiplicationWrapsInWord()
    {
        var c = Calc(size: WordSize.Word);
        Run(c, "300", ProgrammerOp.Multiply, "300");
        Assert.Equal(unchecked((short)90000), c.Value);
    }

    [Fact]
    public void QwordWrapsAtMaxValue()
    {
        var c = Calc();
        Type(c, "9223372036854775807");
        c.Operator(ProgrammerOp.Add);
        c.Digit(1);
        c.Evaluate();
        Assert.Equal(long.MinValue, c.Value);
    }

    [Fact]
    public void DecimalEntryStopsAtMaximum()
    {
        var c = Calc(size: WordSize.Byte);
        Type(c, "1279");
        Assert.Equal(127, c.Value);
    }

    [Fact]
    public void HexEntryInByteLimitedToTwoDigits()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Type(c, "FFF");
        Assert.Equal(-1, c.Value);
        Assert.Equal("FF", c.Display);
    }

    [Fact]
    public void ChangingWordSizeTruncatesValue()
    {
        var c = Calc();
        Type(c, "511");
        c.SetWordSize(WordSize.Byte);
        Assert.Equal(-1, c.Value);
        Assert.Equal("FF", c.Hex);
        c.SetWordSize(WordSize.Qword);
        Assert.Equal(-1, c.Value);
    }

    [Fact]
    public void CycleWordSizeVisitsAllSizes()
    {
        var c = new ProgrammerCalculator();
        c.CycleWordSize();
        Assert.Equal(WordSize.Dword, c.WordSize);
        c.CycleWordSize();
        Assert.Equal(WordSize.Word, c.WordSize);
        c.CycleWordSize();
        Assert.Equal(WordSize.Byte, c.WordSize);
        c.CycleWordSize();
        Assert.Equal(WordSize.Qword, c.WordSize);
    }

    [Fact]
    public void AndOrXor()
    {
        var c = Calc(Radix.Hex);
        Run(c, "F0", ProgrammerOp.And, "3C");
        Assert.Equal("30", c.Display);
        c.Clear();
        Run(c, "F0", ProgrammerOp.Or, "0F");
        Assert.Equal("FF", c.Display);
        c.Clear();
        Run(c, "FF", ProgrammerOp.Xor, "0F");
        Assert.Equal("F0", c.Display);
    }

    [Fact]
    public void NandNor()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "F0", ProgrammerOp.Nand, "3C");
        Assert.Equal("CF", c.Display);
        c.Clear();
        Run(c, "F0", ProgrammerOp.Nor, "0C");
        Assert.Equal("3", c.Display);
    }

    [Fact]
    public void NotInvertsAllBitsOfWordSize()
    {
        var c = Calc(Radix.Hex, WordSize.Word);
        Type(c, "FF");
        c.Not();
        Assert.Equal("FF00", c.Display);
        Assert.Equal(-256, c.Value);
    }

    [Fact]
    public void NotOfZeroIsMinusOne()
    {
        var c = Calc();
        c.Not();
        Assert.Equal("-1", c.Display);
    }

    [Fact]
    public void LeftShift()
    {
        var c = Calc();
        Run(c, "3", ProgrammerOp.Lsh, "4");
        Assert.Equal(48, c.Value);
    }

    [Fact]
    public void LeftShiftDropsBitsOutOfWord()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "81", ProgrammerOp.Lsh, "1");
        Assert.Equal("2", c.Display);
    }

    [Fact]
    public void ArithmeticRightShiftKeepsSign()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "80", ProgrammerOp.Rsh, "2");
        Assert.Equal("E0", c.Display);
    }

    [Fact]
    public void LogicalRightShiftFillsZero()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        c.ShiftMode = ShiftMode.Logical;
        Run(c, "80", ProgrammerOp.Rsh, "2");
        Assert.Equal("20", c.Display);
    }

    [Fact]
    public void ShiftByWordSizeOrMoreGivesZero()
    {
        var c = Calc(size: WordSize.Dword);
        Run(c, "1", ProgrammerOp.Lsh, "32");
        Assert.Equal("Result not defined", c.Display);
    }

    [Fact]
    public void RotateLeftCircular()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "81", ProgrammerOp.RoL, "1");
        Assert.Equal("3", c.Display);
    }

    [Fact]
    public void RotateRightCircular()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "81", ProgrammerOp.RoR, "1");
        Assert.Equal("C0", c.Display);
    }

    [Fact]
    public void RotateFullTurnIsIdentity()
    {
        var c = Calc(Radix.Hex, WordSize.Word);
        Run(c, "1234", ProgrammerOp.RoL, "10");
        Assert.Equal("1234", c.Display);
    }

    [Fact]
    public void RotateLeftThroughCarry()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "81", ProgrammerOp.RoLC, "1");
        Assert.Equal("2", c.Display);
        Assert.True(c.Carry);
        c.Operator(ProgrammerOp.RoLC);
        c.Digit(1);
        c.Evaluate();
        Assert.Equal("5", c.Display);
        Assert.False(c.Carry);
    }

    [Fact]
    public void RotateRightThroughCarry()
    {
        var c = Calc(Radix.Hex, WordSize.Byte);
        Run(c, "1", ProgrammerOp.RoRC, "1");
        Assert.Equal("0", c.Display);
        Assert.True(c.Carry);
        c.Operator(ProgrammerOp.RoRC);
        c.Digit(1);
        c.Evaluate();
        Assert.Equal("80", c.Display);
    }

    [Fact]
    public void NegateTwiceRestores()
    {
        var c = Calc();
        Type(c, "42");
        c.Negate();
        Assert.Equal("-42", c.Display);
        c.Negate();
        Assert.Equal("42", c.Display);
    }

    [Fact]
    public void NegateMinValueWrapsToItself()
    {
        var c = Calc(size: WordSize.Byte);
        Type(c, "127");
        c.Negate();
        c.Operator(ProgrammerOp.Subtract);
        c.Digit(1);
        c.Evaluate();
        c.Negate();
        Assert.Equal(-128, c.Value);
    }

    [Fact]
    public void DecimalDigitsAfterNegateContinueNegative()
    {
        var c = Calc();
        c.Digit(5);
        c.Negate();
        c.Digit(3);
        Assert.Equal(-53, c.Value);
    }

    [Fact]
    public void BackspaceRemovesLastDigit()
    {
        var c = Calc();
        Type(c, "123");
        c.Backspace();
        Assert.Equal(12, c.Value);
        c.Backspace();
        c.Backspace();
        c.Backspace();
        Assert.Equal(0, c.Value);
    }

    [Fact]
    public void BackspaceInHex()
    {
        var c = Calc(Radix.Hex);
        Type(c, "1FA");
        c.Backspace();
        Assert.Equal(0x1F, c.Value);
    }

    [Fact]
    public void ClearEntryKeepsPendingOperation()
    {
        var c = Calc();
        Type(c, "5");
        c.Operator(ProgrammerOp.Add);
        Type(c, "9");
        c.ClearEntry();
        Type(c, "2");
        c.Evaluate();
        Assert.Equal(7, c.Value);
    }

    [Fact]
    public void ClearResetsEverything()
    {
        var c = Calc();
        Type(c, "5");
        c.Operator(ProgrammerOp.Add);
        c.Clear();
        c.Digit(3);
        c.Evaluate();
        Assert.Equal(3, c.Value);
        Assert.Equal("3=", c.Expression);
    }

    [Fact]
    public void RepeatedEqualsRepeatsLastOperation()
    {
        var c = Calc();
        Run(c, "5", ProgrammerOp.Add, "3");
        c.Evaluate();
        c.Evaluate();
        Assert.Equal(14, c.Value);
    }

    [Fact]
    public void ChainedOperationsRespectPrecedence()
    {
        var c = Calc();
        Type(c, "2");
        c.Operator(ProgrammerOp.Add);
        Type(c, "3");
        c.Operator(ProgrammerOp.Multiply);
        Type(c, "4");
        c.Evaluate();
        Assert.Equal(14, c.Value);
    }

    [Fact]
    public void ParenthesesOverridePrecedence()
    {
        var c = Calc();
        c.OpenParenthesis();
        Type(c, "2");
        c.Operator(ProgrammerOp.Add);
        Type(c, "3");
        c.CloseParenthesis();
        c.Operator(ProgrammerOp.Multiply);
        Type(c, "4");
        c.Evaluate();
        Assert.Equal(20, c.Value);
    }

    [Fact]
    public void UnclosedParenthesesCloseOnEquals()
    {
        var c = Calc();
        Type(c, "2");
        c.Operator(ProgrammerOp.Multiply);
        c.OpenParenthesis();
        Type(c, "3");
        c.Operator(ProgrammerOp.Add);
        Type(c, "4");
        c.Evaluate();
        Assert.Equal(14, c.Value);
    }

    [Fact]
    public void CloseWithoutOpenIsIgnored()
    {
        var c = Calc();
        Type(c, "9");
        c.CloseParenthesis();
        Assert.Equal(9, c.Value);
        Assert.Equal(0, c.OpenParentheses);
    }

    [Fact]
    public void ChangingOperatorReplacesPrevious()
    {
        var c = Calc();
        Type(c, "6");
        c.Operator(ProgrammerOp.Add);
        c.Operator(ProgrammerOp.Multiply);
        Type(c, "7");
        c.Evaluate();
        Assert.Equal(42, c.Value);
    }

    [Fact]
    public void ToggleBitSetsAndClears()
    {
        var c = Calc();
        c.ToggleBit(3);
        Assert.Equal(8, c.Value);
        Assert.True(c.GetBit(3));
        c.ToggleBit(3);
        Assert.Equal(0, c.Value);
    }

    [Fact]
    public void ToggleTopBitMakesNegative()
    {
        var c = Calc(size: WordSize.Byte);
        c.ToggleBit(7);
        Assert.Equal(-128, c.Value);
        c.ToggleBit(8); // outside the word: ignored
        Assert.Equal(-128, c.Value);
    }

    [Fact]
    public void ToggleBit63OnQword()
    {
        var c = Calc();
        c.ToggleBit(63);
        Assert.Equal(long.MinValue, c.Value);
        Assert.Equal("8000000000000000", c.Hex);
    }

    [Fact]
    public void MemoryStoreRecallAddSubtractClear()
    {
        var c = Calc();
        Type(c, "10");
        c.MemoryStore();
        Assert.True(c.HasMemory);
        c.ClearEntry();
        Assert.Equal("0", c.Display);
        c.MemoryRecall();
        Assert.Equal(10, c.Value);
        c.MemoryAdd();
        c.MemoryAdd();
        c.MemorySubtract();
        c.MemoryRecall();
        Assert.Equal(20, c.Value);
        c.MemoryClear();
        Assert.False(c.HasMemory);
    }

    [Fact]
    public void MemoryIsRewrappedOnWordSizeChange()
    {
        var c = Calc();
        Type(c, "256");
        c.MemoryStore();
        c.SetWordSize(WordSize.Byte);
        c.MemoryRecall();
        Assert.Equal(0, c.Value);
    }

    [Fact]
    public void ExpressionShowsPendingOperation()
    {
        var c = Calc(Radix.Hex);
        Type(c, "A");
        c.Operator(ProgrammerOp.Xor);
        Assert.Equal("A XOR", c.Expression);
    }

    [Fact]
    public void TypingAfterEqualsStartsNewNumber()
    {
        var c = Calc();
        Run(c, "2", ProgrammerOp.Add, "2");
        c.Digit(9);
        Assert.Equal(9, c.Value);
    }

    [Fact]
    public void OperatorAfterEqualsContinuesWithResult()
    {
        var c = Calc();
        Run(c, "2", ProgrammerOp.Add, "2");
        c.Operator(ProgrammerOp.Multiply);
        c.Digit(5);
        c.Evaluate();
        Assert.Equal(20, c.Value);
    }
}
