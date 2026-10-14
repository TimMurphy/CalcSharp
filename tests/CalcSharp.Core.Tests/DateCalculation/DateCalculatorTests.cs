using System.Globalization;
using CalcSharp.Core.DateCalculation;
using Xunit;

namespace CalcSharp.Core.Tests.DateCalculation;

public class DateCalculatorTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact] public void SameDates() => Assert.Equal("Same dates", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 5)).Text);
    [Fact] public void SameDatesHasNoDaysLine() => Assert.Null(DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 5)).DaysText);
    [Fact] public void OneDaySingular() => Assert.Equal("1 day", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 6)).Text);
    [Fact] public void SixDaysNoWeeks() => Assert.Equal("6 days", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 11)).Text);
    [Fact] public void OneWeek() => Assert.Equal("1 week", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 12)).Text);
    [Fact] public void WeeksAndDays() => Assert.Equal("2 weeks, 3 days", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 22)).Text);
    [Fact] public void DaysOnlyHasNoAlternateLine() => Assert.Null(DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 8)).DaysText);
    [Fact] public void WeeksHaveAlternateLine() => Assert.Equal("14 days", DateCalculator.Difference(D(2024, 5, 5), D(2024, 5, 19)).DaysText);
    [Fact] public void OneMonth() => Assert.Equal("1 month", DateCalculator.Difference(D(2024, 1, 15), D(2024, 2, 15)).Text);
    [Fact] public void OneYear() => Assert.Equal("1 year", DateCalculator.Difference(D(2023, 3, 1), D(2024, 3, 1)).Text);
    [Fact] public void FullBreakdown() =>
        Assert.Equal("1 year, 2 months, 3 weeks, 4 days", DateCalculator.Difference(D(2020, 1, 1), D(2021, 3, 26)).Text);
    [Fact] public void DaysLineForFullBreakdown() => Assert.Equal("450 days", DateCalculator.Difference(D(2020, 1, 1), D(2021, 3, 26)).DaysText);
    [Fact] public void TotalDays() => Assert.Equal(365, DateCalculator.Difference(D(2023, 1, 1), D(2024, 1, 1)).TotalDays);
    [Fact] public void LeapYearTotalDays() => Assert.Equal(366, DateCalculator.Difference(D(2024, 1, 1), D(2025, 1, 1)).TotalDays);
    [Fact] public void LeapDayToMarchFirstNextYear() =>
        Assert.Equal("1 year, 1 day", DateCalculator.Difference(D(2024, 2, 29), D(2025, 3, 1)).Text);
    [Fact] public void ReversedOrderSameAsForward() =>
        Assert.Equal(DateCalculator.Difference(D(2020, 1, 1), D(2021, 3, 26)), DateCalculator.Difference(D(2021, 3, 26), D(2020, 1, 1)));
    [Fact] public void MonthEndDifference() => Assert.Equal("1 month", DateCalculator.Difference(D(2024, 1, 31), D(2024, 2, 29)).Text);
    [Fact] public void PluralYears() => Assert.Equal("10 years", DateCalculator.Difference(D(2000, 6, 1), D(2010, 6, 1)).Text);
    [Fact] public void PluralMonths() => Assert.Equal("5 months", DateCalculator.Difference(D(2024, 1, 10), D(2024, 6, 10)).Text);
    [Fact] public void ExtremeRange() => Assert.Equal(DateCalculator.MaxDate.DayNumber - DateCalculator.MinDate.DayNumber,
        DateCalculator.Difference(DateCalculator.MinDate, DateCalculator.MaxDate).TotalDays);

    [Fact] public void AddDays() => Assert.Equal(D(2024, 3, 1), DateCalculator.Offset(D(2024, 2, 28), 0, 0, 2, false).Date);
    [Fact] public void AddDaysNonLeap() => Assert.Equal(D(2023, 3, 1), DateCalculator.Offset(D(2023, 2, 28), 0, 0, 1, false).Date);
    [Fact] public void SubtractDays() => Assert.Equal(D(2023, 12, 31), DateCalculator.Offset(D(2024, 1, 1), 0, 0, 1, true).Date);
    [Fact] public void AddMonthsClampsToMonthEnd() => Assert.Equal(D(2024, 2, 29), DateCalculator.Offset(D(2024, 1, 31), 0, 1, 0, false).Date);
    [Fact] public void AddMonthsClampsNonLeap() => Assert.Equal(D(2023, 2, 28), DateCalculator.Offset(D(2023, 1, 31), 0, 1, 0, false).Date);
    [Fact] public void AddYearsFromLeapDay() => Assert.Equal(D(2025, 2, 28), DateCalculator.Offset(D(2024, 2, 29), 1, 0, 0, false).Date);
    [Fact] public void AddFourYearsFromLeapDay() => Assert.Equal(D(2028, 2, 29), DateCalculator.Offset(D(2024, 2, 29), 4, 0, 0, false).Date);
    [Fact] public void SubtractMonthsAcrossYear() => Assert.Equal(D(2023, 11, 15), DateCalculator.Offset(D(2024, 2, 15), 0, 3, 0, true).Date);
    [Fact] public void SubtractMonthsLandingInDecember() => Assert.Equal(D(2023, 12, 15), DateCalculator.Offset(D(2024, 2, 15), 0, 2, 0, true).Date);
    [Fact] public void AddMonthsAcrossYear() => Assert.Equal(D(2025, 1, 15), DateCalculator.Offset(D(2024, 11, 15), 0, 2, 0, false).Date);
    [Fact] public void CombinedAdd() => Assert.Equal(D(2026, 5, 10), DateCalculator.Offset(D(2024, 2, 5), 2, 3, 5, false).Date);
    [Fact] public void CombinedSubtract() => Assert.Equal(D(2022, 1, 1), DateCalculator.Offset(D(2024, 4, 6), 2, 3, 5, true).Date);
    [Fact] public void ZeroOffsetIsSameDate() => Assert.Equal(D(2024, 4, 6), DateCalculator.Offset(D(2024, 4, 6), 0, 0, 0, false).Date);
    [Fact] public void AddPastMaxIsOutOfBounds() => Assert.True(DateCalculator.Offset(D(9999, 12, 31), 0, 0, 1, false).IsOutOfBounds);
    [Fact] public void SubtractBeforeMinIsOutOfBounds() => Assert.True(DateCalculator.Offset(D(1601, 1, 1), 0, 0, 1, true).IsOutOfBounds);
    [Fact] public void YearsPastMaxIsOutOfBounds() => Assert.True(DateCalculator.Offset(D(9000, 1, 1), 1000, 0, 0, false).IsOutOfBounds);
    [Fact] public void OutOfBoundsText() => Assert.Equal("Date out of bounds", DateCalculator.Offset(D(1601, 1, 1), 1, 0, 0, true).Text);
    [Fact] public void MaxDateReachable() => Assert.Equal(D(9999, 12, 31), DateCalculator.Offset(D(9999, 12, 30), 0, 0, 1, false).Date);
    [Fact] public void MinDateReachable() => Assert.Equal(D(1601, 1, 1), DateCalculator.Offset(D(1601, 1, 2), 0, 0, 1, true).Date);
    [Fact] public void FormatDateLong() =>
        Assert.Equal("Monday, May 6, 2024", DateCalculator.FormatDate(D(2024, 5, 6), CultureInfo.GetCultureInfo("en-US")));
}
