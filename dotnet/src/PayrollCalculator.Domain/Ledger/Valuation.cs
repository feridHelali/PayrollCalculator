using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Ledger;

/// <summary>Why a component of a month has no amount. Gaps are reported, never guessed.</summary>
public enum LedgerIssue
{
    None = 0,

    /// <summary>No grid of the employee's line is in force that month.</summary>
    NoGridInForce,

    /// <summary>The employee's category is not a category of the line in force.</summary>
    CategoryNotInGrid,

    /// <summary>The grid has no amount for the employee's category and degree.</summary>
    SalaryMissing,

    /// <summary>An hourly rate needs the hours of the employee's regime, and no such regime is in force.</summary>
    RegimeUnknown,

    /// <summary>A percentage of the base cannot be computed because the base has no amount.</summary>
    BaseMissing,
}

/// <summary>How a month's proration factor is computed from the days paid (strategy; sprint 05 question 3).</summary>
public interface IProrationPolicy
{
    decimal Factor(int paidDays, int daysInMonth);
}

/// <summary>Days paid over the calendar days of the month.</summary>
public sealed class CalendarDaysProration : IProrationPolicy
{
    public decimal Factor(int paidDays, int daysInMonth) => (decimal)paidDays / daysInMonth;
}

/// <summary>A full month is 1; otherwise days paid over 30.</summary>
public sealed class ThirtyDayProration : IProrationPolicy
{
    public decimal Factor(int paidDays, int daysInMonth) => paidDays >= daysInMonth ? 1m : Math.Min(paidDays, 30) / 30m;
}

/// <summary>What a component's valuation needs to know about the month.</summary>
/// <param name="BaseAmount">The month's (prorated) base salary, when known.</param>
/// <param name="HoursPerMonth">The hours of the employee's regime, when known.</param>
public sealed record ValuationContext(Money? BaseAmount, decimal? HoursPerMonth, decimal Proration, int YearsOfService, RoundingRule Rounding);

/// <summary>The valuation of one component for one month.</summary>
public sealed record ComponentValue(PayUnit Unit, Money? Rate, Percentage? Percentage, decimal Quantity, Money? Amount, LedgerIssue Issue)
{
    public static ComponentValue Missing(PayUnit unit, Money? rate, LedgerIssue issue) => new(unit, rate, null, 0m, null, issue);
}
