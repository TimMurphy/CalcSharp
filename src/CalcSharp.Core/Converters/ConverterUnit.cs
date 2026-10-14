namespace CalcSharp.Core.Converters;

/// <summary>
/// A unit within a category. A value in this unit converts to the category's base unit as
/// <c>(value + Offset) * Factor</c>; this lets Temperature use offsets while everything else is a ratio.
/// </summary>
public sealed record ConverterUnit(string Name, string Abbreviation, double Factor, double Offset = 0)
{
    public double ToBase(double value) => (value + Offset) * Factor;

    public double FromBase(double baseValue) => baseValue / Factor - Offset;

    public override string ToString() => Name;
}
