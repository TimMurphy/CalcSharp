using System.Globalization;

namespace CalcSharp.Core.DateCalculation;

/// <summary>The result of the "Difference between dates" calculation.</summary>
public readonly record struct DateDifference(int Years, int Months, int Weeks, int Days, int TotalDays)
{
    /// <summary>Main result line, e.g. "Same dates" or "1 year, 2 months, 3 weeks, 4 days".</summary>
    public string Text => DateCalculator.FormatDifference(this);

    /// <summary>The alternate "N days" line; null when the main line is already just a day count.</summary>
    public string? DaysText => DateCalculator.FormatDaysLine(this);
}

/// <summary>Result of "Add or subtract days"; <see cref="Date"/> is null when out of bounds.</summary>
public readonly record struct DateOffsetResult(DateOnly? Date)
{
    public bool IsOutOfBounds => Date is null;

    /// <summary>"Date out of bounds" or the long date (e.g. "Wednesday, October 14, 2026").</summary>
    public string Text => Date is { } d ? DateCalculator.FormatDate(d) : DateCalculator.OutOfBoundsText;
}

public static class DateCalculator
{
    public static readonly DateOnly MinDate = new(1601, 1, 1);
    public static readonly DateOnly MaxDate = new(9999, 12, 31);
    public const string SameDatesText = "Same dates";
    public const string OutOfBoundsText = "Date out of bounds";

    /// <summary>Difference between two dates; order does not matter (the earlier date is the start).</summary>
    public static DateDifference Difference(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var total = to.DayNumber - from.DayNumber;
        var years = to.Year - from.Year;
        if (from.AddYears(years) > to)
        {
            years--;
        }

        var afterYears = from.AddYears(years);
        var months = (to.Year - afterYears.Year) * 12 + to.Month - afterYears.Month;
        if (afterYears.AddMonths(months) > to)
        {
            months--;
        }

        var afterMonths = afterYears.AddMonths(months);
        var days = to.DayNumber - afterMonths.DayNumber;
        return new DateDifference(years, months, days / 7, days % 7, total);
    }

    public static string FormatDifference(DateDifference d)
    {
        if (d.TotalDays == 0)
        {
            return SameDatesText;
        }

        var parts = new List<string>();
        Add(parts, d.Years, "year");
        Add(parts, d.Months, "month");
        Add(parts, d.Weeks, "week");
        Add(parts, d.Days, "day");
        return string.Join(", ", parts);
    }

    public static string? FormatDaysLine(DateDifference d)
    {
        if (d.TotalDays == 0 || (d.Years == 0 && d.Months == 0 && d.Weeks == 0))
        {
            return null;
        }

        return Plural(d.TotalDays, "day");
    }

    private static void Add(List<string> parts, int n, string unit)
    {
        if (n > 0)
        {
            parts.Add(Plural(n, unit));
        }
    }

    private static string Plural(int n, string unit) => n == 1 ? $"1 {unit}" : $"{n} {unit}s";

    /// <summary>Adds (or subtracts) years, then months (day clamped to month end), then days.</summary>
    public static DateOffsetResult Offset(DateOnly from, int years, int months, int days, bool subtract)
    {
        var sign = subtract ? -1 : 1;
        var monthIndex = (long)from.Year * 12 + (from.Month - 1) + ((long)years * 12 + months) * sign;
        var year = monthIndex / 12;
        var month = (int)(monthIndex % 12) + 1;
        if (year < MinDate.Year || year > MaxDate.Year)
        {
            return new DateOffsetResult(null);
        }

        var day = Math.Min(from.Day, DateTime.DaysInMonth((int)year, month));
        var dayNumber = new DateOnly((int)year, month, day).DayNumber + (long)days * sign;
        if (dayNumber < MinDate.DayNumber || dayNumber > MaxDate.DayNumber)
        {
            return new DateOffsetResult(null);
        }

        return new DateOffsetResult(DateOnly.FromDayNumber((int)dayNumber));
    }

    public static string FormatDate(DateOnly d, IFormatProvider? provider = null) =>
        d.ToString("dddd, MMMM d, yyyy", provider ?? CultureInfo.CurrentCulture);
}
