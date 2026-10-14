using CalcSharp.Core.Scientific;

namespace CalcSharp.Core.Tests.Scientific;

public class BigNumTests
{
    private static string Show(BigNum n) => n.Round(BigNum.DisplayDigits).ToPlainString();

    [Fact]
    public void ParseAndPrint() => Assert.Equal("-12.5", Show(BigNum.Parse("-12.50")));

    [Fact]
    public void DivisionKeepsThirtyTwoDigits() =>
        Assert.Equal("0.33333333333333333333333333333333", Show(BigNum.One / BigNum.FromInt(3)));

    [Fact]
    public void OneThirdTimesThreeIsOne() =>
        Assert.Equal("1", Show((BigNum.One / BigNum.FromInt(3)) * BigNum.FromInt(3)));

    [Fact]
    public void ExpOfOneIsE() =>
        Assert.Equal("2.7182818284590452353602874713527", Show(BigNum.Exp(BigNum.One)));

    [Fact]
    public void PiIsPi() =>
        Assert.Equal("3.1415926535897932384626433832795", Show(BigNum.Pi));

    [Fact]
    public void SqrtOfTwo() =>
        Assert.Equal("1.4142135623730950488016887242097", Show(BigNum.Sqrt(BigNum.Two)));

    [Fact]
    public void NaturalLogOfTwo() =>
        Assert.Equal("0.69314718055994530941723212145818", Show(BigNum.Ln(BigNum.Two)));

    [Fact]
    public void LogBaseTenOfHundred() =>
        Assert.Equal("2", Show(BigNum.Log10(BigNum.FromInt(100))));

    [Fact]
    public void TwoToTheThousand() =>
        Assert.StartsWith("1071508607186267320948425049060", Show(BigNum.IntPow(BigNum.Two, 1000)).Replace(".", "")[..31]);

    [Fact]
    public void SineOfThirtyDegrees()
    {
        var radians = BigNum.FromInt(30) * BigNum.Pi / BigNum.FromInt(180);
        Assert.Equal("0.5", Show(BigNum.Sin(radians)));
    }

    [Fact]
    public void CosineOfSixtyDegrees()
    {
        var radians = BigNum.FromInt(60) * BigNum.Pi / BigNum.FromInt(180);
        Assert.Equal("0.5", Show(BigNum.Cos(radians)));
    }

    [Fact]
    public void ArcTangentOfOneIsQuarterPi() =>
        Assert.Equal("0.78539816339744830961566084581988", Show(BigNum.Atan(BigNum.One)));

    [Fact]
    public void TwentyFactorial() =>
        Assert.Equal("2432902008176640000", Show(BigNum.Factorial(20)));

    [Fact]
    public void ComparisonsWork()
    {
        Assert.True(BigNum.Parse("1.5") < BigNum.Parse("2"));
        Assert.True(BigNum.Parse("-3") < BigNum.Parse("2"));
        Assert.True(BigNum.Parse("100") > BigNum.Parse("99.99"));
    }
}
