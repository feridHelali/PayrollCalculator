using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

public class YearMonthTests
{
    [Fact]
    public void Invalid_month_is_rejected()
    {
        Assert.Throws<DomainException>(() => new YearMonth(2020, 13));
    }

    [Fact]
    public void Next_rolls_over_the_year()
    {
        Assert.Equal(new YearMonth(2021, 1), new YearMonth(2020, 12).Next());
    }

    [Fact]
    public void Months_between_counts_full_calendar_months()
    {
        Assert.Equal(14, new YearMonth(2020, 1).MonthsSince(new YearMonth(2018, 11)));
    }

    [Fact]
    public void Formats_as_iso_year_month()
    {
        Assert.Equal("2020-03", new YearMonth(2020, 3).ToString());
    }
}
