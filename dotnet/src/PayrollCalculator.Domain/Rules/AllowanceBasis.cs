using System.Collections.Immutable;
using System.Globalization;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;

namespace PayrollCalculator.Domain.Rules;

/// <summary>How an allowance is valued each month (strategy). Valuation is done by the pay ledger.</summary>
public abstract record AllowanceBasis
{
    /// <summary>The amount or rate a text can raise, when the basis has one.</summary>
    public virtual Money? Amount => null;

    /// <summary>The basis after a raise, or null when a percentage raise does not apply to it.</summary>
    public virtual AllowanceBasis? Increase(Percentage percentage, RoundingRule rounding) => null;

    /// <summary>Values the allowance for a month.</summary>
    public abstract ComponentValue ValueFor(ValuationContext context);
}

/// <summary>A fixed amount per month (prorated for a partial month).</summary>
public sealed record FixedMonthly : AllowanceBasis
{
    public FixedMonthly(Money value) =>
        Value = value >= Money.Zero ? value : throw new DomainException(DomainError.CannotBeNegative, DomainField.Amount);

    public Money Value { get; }

    public override Money? Amount => Value;

    public override AllowanceBasis Increase(Percentage percentage, RoundingRule rounding) => new FixedMonthly(Value.Increase(percentage, rounding));

    public override ComponentValue ValueFor(ValuationContext context) =>
        new(PayUnit.Monthly, Value, null, 1m, Value.Times(context.Proration, context.Rounding), LedgerIssue.None);
}

/// <summary>A rate per hour worked, valued with the hours of the employee's regime.</summary>
public sealed record PerHour : AllowanceBasis
{
    public PerHour(Money rate) =>
        Rate = rate >= Money.Zero ? rate : throw new DomainException(DomainError.CannotBeNegative, DomainField.Amount);

    public Money Rate { get; }

    public override Money? Amount => Rate;

    public override AllowanceBasis Increase(Percentage percentage, RoundingRule rounding) => new PerHour(Rate.Increase(percentage, rounding));

    public override ComponentValue ValueFor(ValuationContext context) => context.HoursPerMonth is { } hours
        ? new(PayUnit.Hourly, Rate, null, hours, Rate.Times(hours * context.Proration, context.Rounding), LedgerIssue.None)
        : ComponentValue.Missing(PayUnit.Hourly, Rate, LedgerIssue.RegimeUnknown);
}

/// <summary>A percentage of the month's base salary; it follows the base, so a raise of the base raises it.</summary>
public sealed record PercentOfBase(Percentage Percentage) : AllowanceBasis
{
    public override ComponentValue ValueFor(ValuationContext context) => OfBase(Percentage, context);

    internal static ComponentValue OfBase(Percentage percentage, ValuationContext context) => context.BaseAmount is { } baseAmount
        ? new(PayUnit.Monthly, null, percentage, 1m, Money.Round(baseAmount.Dinars * percentage.Rate, context.Rounding), LedgerIssue.None)
        : ComponentValue.Missing(PayUnit.Monthly, null, LedgerIssue.BaseMissing);
}

/// <summary>A bracket of the seniority prime: from <see cref="FromYears"/> years of service, this percentage of the base.</summary>
public sealed record SeniorityBracket
{
    public SeniorityBracket(int fromYears, Percentage percentage)
    {
        FromYears = Guard.NotNegative(fromYears, DomainField.YearsInDegree);
        Percentage = percentage;
    }

    public int FromYears { get; }

    public Percentage Percentage { get; }
}

/// <summary>A percentage of the base that depends on the years of service (the seniority prime).</summary>
public sealed record SeniorityBrackets : AllowanceBasis
{
    public SeniorityBrackets(IEnumerable<SeniorityBracket> brackets)
    {
        ArgumentNullException.ThrowIfNull(brackets);
        var list = brackets.OrderBy(b => b.FromYears).ToImmutableArray();
        var duplicate = list.GroupBy(b => b.FromYears).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException(DomainError.Duplicate, DomainField.YearsInDegree, duplicate.Key.ToString(CultureInfo.InvariantCulture));
        }

        Brackets = list;
    }

    public IReadOnlyList<SeniorityBracket> Brackets { get; }

    /// <returns>The percentage for <paramref name="yearsOfService"/>, or null below the first bracket.</returns>
    public Percentage? PercentageFor(int yearsOfService) =>
        Brackets.LastOrDefault(b => b.FromYears <= yearsOfService)?.Percentage;

    public override ComponentValue ValueFor(ValuationContext context) => PercentageFor(context.YearsOfService) is { } percentage
        ? PercentOfBase.OfBase(percentage, context)
        : new(PayUnit.Monthly, null, null, 1m, Money.Zero, LedgerIssue.None);
}
