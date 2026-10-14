namespace CalcSharp.Core.Converters;

/// <summary>
/// Static fallback currency table (units per 1 USD). The real app downloads rates; this replica never
/// touches the network, so it is always in the "offline" state using these built-in values.
/// </summary>
public static class CurrencyRates
{
    public const string UpdatedFormat = "Updated %1 %2";
    public const string UpdateFailedText = "Could not update rates";
    public const string RatesUpdatedText = "Currency rates updated";
    public const string UpdatingText = "Updating currency rates";
    public const string UpdateButtonText = "Update rates";

    public static readonly DateTime FallbackDate = new(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);

    // Code, name, units per USD.
    private static readonly (string Code, string Name, double PerUsd)[] Table =
    [
        ("AED", "United Arab Emirates Dirham", 3.6725),
        ("ARS", "Argentine Peso", 1000.0),
        ("AUD", "Australian Dollar", 1.50),
        ("BRL", "Brazilian Real", 5.50),
        ("CAD", "Canadian Dollar", 1.38),
        ("CHF", "Swiss Franc", 0.80),
        ("CLP", "Chilean Peso", 940.0),
        ("CNY", "Chinese Yuan", 7.15),
        ("CZK", "Czech Koruna", 22.5),
        ("DKK", "Danish Krone", 6.9),
        ("EGP", "Egyptian Pound", 48.0),
        ("EUR", "Euro", 0.92),
        ("GBP", "British Pound", 0.78),
        ("HKD", "Hong Kong Dollar", 7.8),
        ("HUF", "Hungarian Forint", 365.0),
        ("IDR", "Indonesian Rupiah", 16000.0),
        ("ILS", "Israeli New Shekel", 3.7),
        ("INR", "Indian Rupee", 84.0),
        ("JPY", "Japanese Yen", 150.0),
        ("KRW", "South Korean Won", 1380.0),
        ("MXN", "Mexican Peso", 19.5),
        ("MYR", "Malaysian Ringgit", 4.4),
        ("NOK", "Norwegian Krone", 10.9),
        ("NZD", "New Zealand Dollar", 1.65),
        ("PHP", "Philippine Peso", 57.0),
        ("PLN", "Polish Zloty", 3.95),
        ("RUB", "Russian Ruble", 95.0),
        ("SAR", "Saudi Riyal", 3.75),
        ("SEK", "Swedish Krona", 10.7),
        ("SGD", "Singapore Dollar", 1.33),
        ("THB", "Thai Baht", 34.0),
        ("TRY", "Turkish Lira", 34.0),
        ("TWD", "New Taiwan Dollar", 32.0),
        ("USD", "United States Dollar", 1.0),
        ("ZAR", "South African Rand", 18.0),
    ];

    /// <summary>True: rates are the embedded fallback, not freshly downloaded.</summary>
    public static bool IsOffline => true;

    public static IReadOnlyList<ConverterUnit> Units { get; } =
        Table.Select(t => new ConverterUnit($"{t.Code} - {t.Name}", t.Code, 1.0 / t.PerUsd)).ToList();

    public static int IndexOf(string code) => Table.Select(t => t.Code).ToList().IndexOf(code);

    /// <summary>Status line shown under the converter, using the real app's strings.</summary>
    public static string StatusText =>
        IsOffline
            ? UpdateFailedText
            : UpdatedFormat.Replace("%1", FallbackDate.ToString("d")).Replace("%2", FallbackDate.ToString("t"));
}
