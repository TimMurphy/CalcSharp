namespace CalcSharp.Core.Converters;

/// <summary>The converter categories offered by the app, with the real app's unit lists.</summary>
public static class ConverterCatalog
{
    private static ConverterUnit U(string name, string abbr, double factor, double offset = 0) =>
        new(name, abbr, factor, offset);

    public static IReadOnlyList<string> CategoryNames { get; } =
    [
        "Currency", "Volume", "Length", "Weight and mass", "Temperature", "Energy", "Area",
        "Speed", "Time", "Power", "Data", "Pressure", "Angle",
    ];

    public static ConverterCategory Get(string name) => name switch
    {
        "Currency" => new("Currency", CurrencyRates.Units, CurrencyRates.IndexOf("USD"), CurrencyRates.IndexOf("EUR")),
        "Volume" => Volume(),
        "Length" => Length(),
        "Weight and mass" => Weight(),
        "Temperature" => Temperature(),
        "Energy" => Energy(),
        "Area" => Area(),
        "Speed" => Speed(),
        "Time" => Time(),
        "Power" => Power(),
        "Data" => Data(),
        "Pressure" => Pressure(),
        "Angle" => Angle(),
        _ => throw new ArgumentException($"Unknown converter category '{name}'.", nameof(name)),
    };

    // Base: cubic metres.
    private static ConverterCategory Volume()
    {
        const double usGal = 0.003785411784;
        const double ukGal = 0.00454609;
        return new("Volume",
        [
            U("Milliliters", "mL", 1e-6),
            U("Cubic centimeters", "cm³", 1e-6),
            U("Liters", "L", 1e-3),
            U("Cubic meters", "m³", 1),
            U("Teaspoons (US)", "tsp. (US)", usGal / 768),
            U("Tablespoons (US)", "tbsp. (US)", usGal / 256),
            U("Fluid ounces (US)", "fl oz (US)", usGal / 128),
            U("Cups (US)", "cup (US)", usGal / 16),
            U("Pints (US)", "pt (US)", usGal / 8),
            U("Quarts (US)", "qt (US)", usGal / 4),
            U("Gallons (US)", "gal (US)", usGal),
            U("Cubic inches", "in³", 1.6387064e-5),
            U("Cubic feet", "ft³", 0.028316846592),
            U("Cubic yards", "yd³", 0.764554857984),
            U("Teaspoons (UK)", "tsp. (UK)", ukGal / 768),
            U("Tablespoons (UK)", "tbsp. (UK)", ukGal / 256),
            U("Fluid ounces (UK)", "fl oz (UK)", ukGal / 160),
            U("Pints (UK)", "pt (UK)", ukGal / 8),
            U("Quarts (UK)", "qt (UK)", ukGal / 4),
            U("Gallons (UK)", "gal (UK)", ukGal),
            U("Coffee cups", "cup", 0.000236588),
            U("Bathtubs", "bathtub", 0.378541),
            U("Swimming pools", "pool", 2500),
        ], 0, 7);
    }

    // Base: metres.
    private static ConverterCategory Length() => new("Length",
    [
        U("Angstroms", "Å", 1e-10),
        U("Nanometers", "nm", 1e-9),
        U("Microns", "µm", 1e-6),
        U("Millimeters", "mm", 1e-3),
        U("Centimeters", "cm", 1e-2),
        U("Meters", "m", 1),
        U("Kilometers", "km", 1000),
        U("Inches", "in", 0.0254),
        U("Feet", "ft", 0.3048),
        U("Yards", "yd", 0.9144),
        U("Miles", "mi", 1609.344),
        U("Nautical miles", "nmi", 1852),
        U("Paperclips", "paperclip", 0.035052),
        U("Hands", "hand", 0.1016),
        U("Jumbo jets", "jumbo jet", 76),
    ], 5, 8);

    // Base: kilograms.
    private static ConverterCategory Weight() => new("Weight and mass",
    [
        U("Carats", "ct", 0.0002),
        U("Milligrams", "mg", 1e-6),
        U("Centigrams", "cg", 1e-5),
        U("Decigrams", "dg", 1e-4),
        U("Grams", "g", 1e-3),
        U("Decagrams", "dag", 1e-2),
        U("Hectograms", "hg", 0.1),
        U("Kilograms", "kg", 1),
        U("Metric tonnes", "t", 1000),
        U("Ounces", "oz", 0.028349523125),
        U("Pounds", "lb", 0.45359237),
        U("Stone", "st", 6.35029318),
        U("Short tons (US)", "ton (US)", 907.18474),
        U("Long tons (UK)", "ton (UK)", 1016.0469088),
        U("Snowflakes", "snowflake", 2e-6),
        U("Soccer balls", "soccer ball", 0.4325),
        U("Elephants", "elephant", 4000),
        U("Whales", "whale", 90000),
    ], 7, 10);

    // Base: kelvin. (value + offset) * factor.
    private static ConverterCategory Temperature() => new("Temperature",
    [
        U("Degrees Celsius", "°C", 1, 273.15),
        U("Degrees Fahrenheit", "°F", 5.0 / 9.0, 459.67),
        U("Kelvin", "K", 1),
    ], 0, 1);

    // Base: joules.
    private static ConverterCategory Energy() => new("Energy",
    [
        U("Electron volts", "eV", 1.602176634e-19),
        U("Joules", "J", 1),
        U("Kilojoules", "kJ", 1000),
        U("Thermal calories", "cal", 4.184),
        U("Food calories", "kcal", 4184),
        U("Foot-pounds", "ft⋅lb", 1.3558179483314004),
        U("British thermal units", "BTU", 1055.05585262),
        U("Kilowatt-hours", "kWh", 3600000),
        U("Batteries", "battery", 9000),
        U("Bananas", "banana", 439320),
        U("Slices of cake", "slice of cake", 1046000),
    ], 1, 4);

    // Base: square metres.
    private static ConverterCategory Area() => new("Area",
    [
        U("Square millimeters", "mm²", 1e-6),
        U("Square centimeters", "cm²", 1e-4),
        U("Square meters", "m²", 1),
        U("Hectares", "ha", 10000),
        U("Square kilometers", "km²", 1e6),
        U("Square inches", "in²", 0.00064516),
        U("Square feet", "ft²", 0.09290304),
        U("Square yards", "yd²", 0.83612736),
        U("Acres", "ac", 4046.8564224),
        U("Square miles", "mi²", 2589988.110336),
        U("Paper", "paper", 0.06032246),
        U("Soccer fields", "soccer field", 10869.66),
    ], 2, 6);

    // Base: metres per second.
    private static ConverterCategory Speed() => new("Speed",
    [
        U("Centimeters per second", "cm/s", 0.01),
        U("Meters per second", "m/s", 1),
        U("Kilometers per hour", "km/h", 1000.0 / 3600.0),
        U("Feet per second", "ft/s", 0.3048),
        U("Miles per hour", "mph", 0.44704),
        U("Knots", "kn", 1852.0 / 3600.0),
        U("Mach", "Mach", 340.3),
        U("Turtles", "turtle", 0.114),
        U("Horses", "horse", 20.1168),
        U("Jets", "jet", 250),
    ], 1, 4);

    // Base: seconds.
    private static ConverterCategory Time() => new("Time",
    [
        U("Microseconds", "µs", 1e-6),
        U("Milliseconds", "ms", 1e-3),
        U("Seconds", "s", 1),
        U("Minutes", "min", 60),
        U("Hours", "h", 3600),
        U("Days", "d", 86400),
        U("Weeks", "wk", 604800),
        U("Years", "yr", 31536000),
    ], 3, 4);

    // Base: watts.
    private static ConverterCategory Power() => new("Power",
    [
        U("Watts", "W", 1),
        U("Kilowatts", "kW", 1000),
        U("Horsepower (US)", "hp", 745.69987158227022),
        U("Foot-pounds/minute", "ft⋅lb/min", 0.022596966),
        U("BTUs/minute", "BTU/min", 17.58426666),
        U("Light bulbs", "light bulb", 60),
        U("Horses", "horse", 745.7),
        U("Train engines", "train engine", 2982800),
    ], 0, 1);

    // Base: bits.
    private static ConverterCategory Data()
    {
        var units = new List<ConverterUnit>
        {
            U("Bits", "b", 1),
            U("Nibbles", "nibble", 4),
            U("Bytes", "B", 8),
        };
        (string Dec, string Bin, string DecAbbr, string BinAbbr)[] prefixes =
        [
            ("Kilo", "Kibi", "k", "Ki"),
            ("Mega", "Mebi", "M", "Mi"),
            ("Giga", "Gibi", "G", "Gi"),
            ("Tera", "Tebi", "T", "Ti"),
            ("Peta", "Pebi", "P", "Pi"),
            ("Exa", "Exbi", "E", "Ei"),
            ("Zeta", "Zebi", "Z", "Zi"),
            ("Yotta", "Yobi", "Y", "Yi"),
        ];
        for (var i = 0; i < prefixes.Length; i++)
        {
            var p = prefixes[i];
            var dec = Math.Pow(1000, i + 1);
            var bin = Math.Pow(1024, i + 1);
            units.Add(U($"{p.Dec}bits", $"{p.DecAbbr}b", dec));
            units.Add(U($"{p.Bin}bits", $"{p.BinAbbr}b", bin));
            units.Add(U($"{p.Dec}bytes", $"{p.DecAbbr}B", dec * 8));
            units.Add(U($"{p.Bin}bytes", $"{p.BinAbbr}B", bin * 8));
        }

        units.Add(U("Floppy disks", "floppy disk", 1474560.0 * 8));
        units.Add(U("CDs", "CD", 737280000.0 * 8));
        units.Add(U("DVDs", "DVD", 5046586573.0 * 8));
        return new("Data", units, 2, 11);
    }

    // Base: pascals.
    private static ConverterCategory Pressure() => new("Pressure",
    [
        U("Atmospheres", "atm", 101325),
        U("Bars", "bar", 100000),
        U("Kilopascals", "kPa", 1000),
        U("Millimeters of mercury", "mmHg", 133.322387415),
        U("Pascals", "Pa", 1),
        U("Pounds per square inch", "psi", 6894.757293168),
    ], 0, 1);

    // Base: degrees.
    private static ConverterCategory Angle() => new("Angle",
    [
        U("Degrees", "°", 1),
        U("Radians", "rad", 180.0 / Math.PI),
        U("Gradians", "grad", 0.9),
    ], 0, 1);
}
