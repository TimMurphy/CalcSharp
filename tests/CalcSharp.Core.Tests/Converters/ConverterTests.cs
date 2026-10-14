using CalcSharp.Core.Converters;

namespace CalcSharp.Core.Tests.Converters;

public class ConverterTests
{
    private static double Conv(string cat, double v, string from, string to)
    {
        var c = ConverterCatalog.Get(cat);
        return c.Convert(v, c.Find(from), c.Find(to));
    }

    [Theory]
    [InlineData("Length", 1, "Miles", "Kilometers", 1.609344)]
    [InlineData("Length", 1, "Feet", "Inches", 12)]
    [InlineData("Length", 1, "Yards", "Feet", 3)]
    [InlineData("Length", 1, "Nautical miles", "Meters", 1852)]
    [InlineData("Weight and mass", 1, "Pounds", "Ounces", 16)]
    [InlineData("Weight and mass", 1, "Kilograms", "Grams", 1000)]
    [InlineData("Weight and mass", 1, "Short tons (US)", "Pounds", 2000)]
    [InlineData("Volume", 1, "Gallons (US)", "Quarts (US)", 4)]
    [InlineData("Volume", 1, "Liters", "Milliliters", 1000)]
    [InlineData("Volume", 1, "Cups (US)", "Fluid ounces (US)", 8)]
    [InlineData("Volume", 1, "Gallons (UK)", "Pints (UK)", 8)]
    [InlineData("Time", 1, "Hours", "Minutes", 60)]
    [InlineData("Time", 1, "Weeks", "Days", 7)]
    [InlineData("Time", 1, "Years", "Days", 365)]
    [InlineData("Area", 1, "Hectares", "Square meters", 10000)]
    [InlineData("Area", 1, "Acres", "Square feet", 43560)]
    [InlineData("Speed", 36, "Kilometers per hour", "Meters per second", 10)]
    [InlineData("Energy", 1, "Kilowatt-hours", "Kilojoules", 3600)]
    [InlineData("Energy", 1, "Food calories", "Thermal calories", 1000)]
    [InlineData("Power", 1, "Kilowatts", "Watts", 1000)]
    [InlineData("Pressure", 1, "Atmospheres", "Pascals", 101325)]
    [InlineData("Pressure", 1, "Bars", "Kilopascals", 100)]
    [InlineData("Angle", 180, "Degrees", "Gradians", 200)]
    public void ExactRatios(string cat, double v, string from, string to, double expected)
    {
        Assert.Equal(expected, Conv(cat, v, from, to), 6);
    }

    [Fact]
    public void RadiansToDegrees() => Assert.Equal(180, Conv("Angle", Math.PI, "Radians", "Degrees"), 9);

    [Theory]
    [InlineData(0, "Degrees Celsius", "Degrees Fahrenheit", 32)]
    [InlineData(100, "Degrees Celsius", "Degrees Fahrenheit", 212)]
    [InlineData(32, "Degrees Fahrenheit", "Degrees Celsius", 0)]
    [InlineData(0, "Degrees Celsius", "Kelvin", 273.15)]
    [InlineData(-40, "Degrees Celsius", "Degrees Fahrenheit", -40)]
    [InlineData(0, "Kelvin", "Degrees Celsius", -273.15)]
    public void Temperature(double v, string from, string to, double expected)
    {
        Assert.Equal(expected, Conv("Temperature", v, from, to), 9);
    }

    [Fact]
    public void DataBinaryVsDecimal()
    {
        Assert.Equal(1024, Conv("Data", 1, "Kibibytes", "Bytes"), 9);
        Assert.Equal(1000, Conv("Data", 1, "Kilobytes", "Bytes"), 9);
        Assert.Equal(8, Conv("Data", 1, "Bytes", "Bits"), 9);
        Assert.Equal(1024, Conv("Data", 1, "Gibibytes", "Mebibytes"), 9);
    }

    [Theory]
    [InlineData("Currency")]
    [InlineData("Volume")]
    [InlineData("Length")]
    [InlineData("Weight and mass")]
    [InlineData("Temperature")]
    [InlineData("Energy")]
    [InlineData("Area")]
    [InlineData("Speed")]
    [InlineData("Time")]
    [InlineData("Power")]
    [InlineData("Data")]
    [InlineData("Pressure")]
    [InlineData("Angle")]
    public void EveryCategoryIsValid(string cat)
    {
        var c = ConverterCatalog.Get(cat);
        Assert.NotEmpty(c.Units);
        Assert.InRange(c.DefaultFrom, 0, c.Units.Count - 1);
        Assert.InRange(c.DefaultTo, 0, c.Units.Count - 1);
        Assert.Equal(c.Units.Count, c.Units.Select(u => u.Name).Distinct().Count());
        Assert.All(c.Units, u => Assert.NotEqual(0, u.Factor));
    }

    [Fact]
    public void CatalogMatchesModeCatalogNames()
    {
        Assert.Equal(13, ConverterCatalog.CategoryNames.Count);
        Assert.All(ConverterCatalog.CategoryNames, n => Assert.Equal(n, ConverterCatalog.Get(n).Name));
    }

    [Fact]
    public void UnknownCategoryThrows() =>
        Assert.Throws<ArgumentException>(() => ConverterCatalog.Get("Nope"));

    [Fact]
    public void RoundTripIsStable()
    {
        foreach (var name in ConverterCatalog.CategoryNames)
        {
            var c = ConverterCatalog.Get(name);
            var a = c.Units[0];
            var b = c.Units[^1];
            var there = c.Convert(123.456, a, b);
            Assert.Equal(123.456, c.Convert(there, b, a), 6);
        }
    }

    [Fact]
    public void TemperatureUnitOrder() =>
        Assert.Equal(["Degrees Celsius", "Degrees Fahrenheit", "Kelvin"],
            ConverterCatalog.Get("Temperature").Units.Select(u => u.Name));

    [Fact]
    public void AngleUnitOrder() =>
        Assert.Equal(["Degrees", "Radians", "Gradians"],
            ConverterCatalog.Get("Angle").Units.Select(u => u.Name));
}

public class FormatterTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(1234567, "1,234,567")]
    [InlineData(-1234.5, "-1,234.5")]
    [InlineData(0.5, "0.5")]
    [InlineData(1.0 / 3.0, "0.3333333333")]
    [InlineData(3.28083989501312, "3.280839895")]
    [InlineData(1e-12, "1e-12")]
    [InlineData(1e20, "1e+20")]
    public void Format(double v, string expected) => Assert.Equal(expected, NumberFormatter.Format(v));

    [Fact]
    public void FormatTrimsTrailingZeros() => Assert.Equal("2.5", NumberFormatter.Format(2.50000000001));

    [Fact]
    public void FormatNegativeZero() => Assert.Equal("0", NumberFormatter.Format(-0.0));

    [Fact]
    public void FormatNaNIsOverflow() => Assert.Equal("Overflow", NumberFormatter.Format(double.NaN));

    [Fact]
    public void GroupKeepsFraction() => Assert.Equal("1,000.50", NumberFormatter.Group("1000.50"));
}

public class UnitConverterTests
{
    [Fact]
    public void StartsAtZero()
    {
        var c = new UnitConverter("Length");
        Assert.Equal("0", c.FromDisplay);
        Assert.Equal("0", c.ToDisplay);
    }

    [Fact]
    public void TypingConvertsLive()
    {
        var c = new UnitConverter("Length"); // Meters -> Feet
        c.Digit(1);
        Assert.Equal("1", c.FromDisplay);
        Assert.Equal("3.280839895", c.ToDisplay);
    }

    [Fact]
    public void LeadingZeroReplaced()
    {
        var c = new UnitConverter("Length");
        c.Digit(0);
        c.Digit(5);
        Assert.Equal("5", c.FromDisplay);
    }

    [Fact]
    public void DecimalAndGrouping()
    {
        var c = new UnitConverter("Length");
        foreach (var d in new[] { 1, 2, 3, 4 })
        {
            c.Digit(d);
        }

        c.DecimalPoint();
        c.Digit(5);
        Assert.Equal("1,234.5", c.FromDisplay);
    }

    [Fact]
    public void DecimalPointOnlyOnce()
    {
        var c = new UnitConverter("Length");
        c.DecimalPoint();
        c.DecimalPoint();
        Assert.Equal("0.", c.FromDisplay);
    }

    [Fact]
    public void BackspaceAndClear()
    {
        var c = new UnitConverter("Length");
        c.Digit(1);
        c.Digit(2);
        c.Backspace();
        Assert.Equal("1", c.FromDisplay);
        c.Backspace();
        Assert.Equal("0", c.FromDisplay);
        c.Digit(9);
        c.Clear();
        Assert.Equal("0", c.FromDisplay);
        Assert.Equal("0", c.ToDisplay);
    }

    [Fact]
    public void NegateWorks()
    {
        var c = new UnitConverter("Temperature");
        c.Digit(4);
        c.Digit(0);
        c.Negate();
        Assert.Equal("-40", c.FromDisplay);
        Assert.Equal("-40", c.ToDisplay);
        c.Negate();
        Assert.Equal("40", c.FromDisplay);
    }

    [Fact]
    public void TemperatureLive()
    {
        var c = new UnitConverter("Temperature");
        c.Digit(1);
        c.Digit(0);
        c.Digit(0);
        Assert.Equal("212", c.ToDisplay);
    }

    [Fact]
    public void ChangingUnitRecomputes()
    {
        var c = new UnitConverter("Length");
        c.Digit(1);
        c.SetUnit(1, c.Category.Find("Centimeters"));
        Assert.Equal("100", c.ToDisplay);
    }

    [Fact]
    public void SwitchingActiveFieldEditsOutput()
    {
        var c = new UnitConverter("Time"); // Minutes -> Hours
        c.Digit(6);
        c.Digit(0);
        Assert.Equal("1", c.ToDisplay);
        c.SetActiveField(1);
        Assert.Equal(1, c.ActiveField);
        c.Digit(2);
        Assert.Equal("12", c.ToDisplay);
        Assert.Equal("720", c.FromDisplay);
    }

    [Fact]
    public void DigitLimit()
    {
        var c = new UnitConverter("Length");
        for (var i = 0; i < 20; i++)
        {
            c.Digit(1);
        }

        Assert.Equal(UnitConverter.MaxDigits, c.InputText.Length);
    }

    [Fact]
    public void CurrencyOfflineState()
    {
        var c = new UnitConverter("Currency");
        Assert.True(c.IsCurrency);
        Assert.True(CurrencyRates.IsOffline);
        Assert.Equal("Could not update rates", c.StatusText);
        Assert.Equal("USD", c.FromUnit.Abbreviation);
    }

    [Fact]
    public void CurrencyConvertsUsingFallbackTable()
    {
        var c = new UnitConverter("Currency");
        c.Digit(1);
        c.Digit(0);
        c.Digit(0);
        Assert.Equal("92", c.ToDisplay);
    }

    [Fact]
    public void NonCurrencyHasNoStatus() => Assert.Null(new UnitConverter("Power").StatusText);
}
