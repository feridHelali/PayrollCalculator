using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Tests;

public class TimelineCsvTests
{
    [Fact]
    public void Writes_a_header_and_one_invariant_culture_row_per_month()
    {
        var timeline = new SalaryTimeline([
            new MonthlySalaryLine(new YearMonth(2020, 1), 0, 1, 7, "4", SalaryType.Monthly, 1234.5m, TimelineIssue.None),
            new MonthlySalaryLine(new YearMonth(2020, 2), 1, 1, null, null, null, null, TimelineIssue.NoApplicableTable),
        ]);

        var lines = TimelineCsv.Write(timeline).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Period;MonthsOfSeniority;Degree;TableNumber;SalaryType;Amount;Issue", lines[0]);
        Assert.Equal("2020-01;0;1;4;Monthly;1234.5;None", lines[1]);
        Assert.Equal("2020-02;1;1;;;;NoApplicableTable", lines[2]);
    }

    [Fact]
    public void Values_containing_the_separator_are_quoted()
    {
        var timeline = new SalaryTimeline([
            new MonthlySalaryLine(new YearMonth(2020, 1), 0, 1, 7, "4;b", SalaryType.Hourly, 1m, TimelineIssue.None),
        ]);

        Assert.Contains("\"4;b\"", TimelineCsv.Write(timeline), StringComparison.Ordinal);
    }
}
