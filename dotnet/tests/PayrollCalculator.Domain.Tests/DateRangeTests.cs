using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

public class DateRangeTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void End_before_start_is_rejected()
    {
        Assert.Throws<DomainException>(() => new DateRange(D(2020, 1, 2), D(2020, 1, 1)));
    }

    [Theory]
    [InlineData(2020, 1, 1, true)]
    [InlineData(2020, 6, 15, true)]
    [InlineData(2020, 12, 31, true)]
    [InlineData(2019, 12, 31, false)]
    [InlineData(2021, 1, 1, false)]
    public void Contains_is_inclusive_on_both_bounds(int y, int m, int d, bool expected)
    {
        var range = new DateRange(D(2020, 1, 1), D(2020, 12, 31));
        Assert.Equal(expected, range.Contains(D(y, m, d)));
    }

    [Fact]
    public void Open_ended_range_contains_any_later_date()
    {
        var range = DateRange.From(D(2020, 1, 1));
        Assert.True(range.IsOpenEnded);
        Assert.True(range.Contains(D(2099, 1, 1)));
    }

    [Fact]
    public void Months_enumerates_every_calendar_month_touched()
    {
        var range = new DateRange(D(2020, 11, 15), D(2021, 2, 3));
        Assert.Equal(
            [new YearMonth(2020, 11), new YearMonth(2020, 12), new YearMonth(2021, 1), new YearMonth(2021, 2)],
            range.Months().ToList());
    }

    [Fact]
    public void Months_of_open_ended_range_is_rejected()
    {
        Assert.Throws<DomainException>(() => DateRange.From(D(2020, 1, 1)).Months().ToList());
    }
}
