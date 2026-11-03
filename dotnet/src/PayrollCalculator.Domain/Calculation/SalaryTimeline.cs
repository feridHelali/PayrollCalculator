using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Calculation;

/// <summary>Why a month could not be valued. Gaps are reported, never guessed.</summary>
public enum TimelineIssue
{
    None = 0,
    NoApplicableTable,
    CategoryNotFound,
    DegreeNotFound,
    SalaryMissing,
}

/// <summary>The base salary owed for one month of employment.</summary>
public sealed record MonthlySalaryLine(
    YearMonth Period,
    int MonthsOfSeniority,
    int Degree,
    int? SalaryTableId,
    string? TableNumber,
    SalaryType? SalaryType,
    decimal? Amount,
    TimelineIssue Issue);

/// <summary>Month-by-month reconstruction of an affair's base salary.</summary>
public sealed record SalaryTimeline(IReadOnlyList<MonthlySalaryLine> Lines)
{
    /// <summary>Sum of resolved amounts of monthly grids. Hourly grids hold rates, which are not summed.</summary>
    public decimal TotalMonthlyAmount =>
        Lines.Where(l => l is { Issue: TimelineIssue.None, SalaryType: SalaryTables.SalaryType.Monthly })
             .Sum(l => l.Amount ?? 0m);

    public int UnresolvedMonths => Lines.Count(l => l.Issue != TimelineIssue.None);

    public bool IsComplete => UnresolvedMonths == 0;
}
