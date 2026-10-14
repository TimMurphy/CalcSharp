namespace CalcSharp.Core.Converters;

/// <summary>A converter category (Length, Volume, ...) with its units in the real app's order.</summary>
public sealed class ConverterCategory
{
    public ConverterCategory(string name, IReadOnlyList<ConverterUnit> units, int defaultFrom, int defaultTo)
    {
        Name = name;
        Units = units;
        DefaultFrom = defaultFrom;
        DefaultTo = defaultTo;
    }

    public string Name { get; }

    public IReadOnlyList<ConverterUnit> Units { get; }

    public int DefaultFrom { get; }

    public int DefaultTo { get; }

    public ConverterUnit Find(string name) => Units.First(u => u.Name == name);

    public double Convert(double value, ConverterUnit from, ConverterUnit to) =>
        to.FromBase(from.ToBase(value));
}
