using System.Globalization;

namespace PayrollCalculator.Domain.Common;

/// <summary>How a computed amount is brought back to a whole millime.</summary>
public enum RoundingRule
{
    /// <summary>To the nearest millime, halves away from zero (0.0005 → 0.001).</summary>
    HalfUp = 0,

    /// <summary>The extra digits are cut (toward zero).</summary>
    Truncate = 1,
}

/// <summary>Unit of a rate in a grid or an allowance.</summary>
public enum PayUnit
{
    Monthly = 0,
    Hourly = 1,

    /// <summary>Per day worked (for example a meal allowance). Allowances only: a grid is monthly or hourly.</summary>
    Daily = 2,
}

/// <summary>
/// An amount in Tunisian dinars, held as whole millimes (3 decimals) so sums are exact.
/// A value finer than a millime only arises from a computation, which must say how it is rounded.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    private Money(long millimes) => Millimes = millimes;

    public static Money Zero { get; } = new(0);

    public long Millimes { get; }

    public decimal Dinars => Millimes / 1000m;

    public static Money FromMillimes(long millimes) => new(millimes);

    /// <summary>An amount as written in a text; more than three decimals is a reading error.</summary>
    public static Money FromDinars(decimal dinars)
    {
        var millimes = dinars * 1000m;
        if (millimes != decimal.Truncate(millimes))
        {
            throw new DomainException(DomainError.OutOfRange, DomainField.Amount, dinars.ToString(CultureInfo.InvariantCulture));
        }

        return new Money((long)millimes);
    }

    /// <summary>A computed amount, brought back to the millime with <paramref name="rule"/>.</summary>
    public static Money Round(decimal dinars, RoundingRule rule)
    {
        var millimes = dinars * 1000m;
        var whole = rule switch
        {
            RoundingRule.Truncate => decimal.Truncate(millimes),
            _ => decimal.Round(millimes, 0, MidpointRounding.AwayFromZero),
        };
        return new Money((long)whole);
    }

    public Money Increase(Percentage percentage, RoundingRule rule) => Round(Dinars * percentage.Factor, rule);

    public Money Times(decimal quantity, RoundingRule rule) => Round(Dinars * quantity, rule);

    public static Money operator +(Money left, Money right) => new(left.Millimes + right.Millimes);

    public static Money operator -(Money left, Money right) => new(left.Millimes - right.Millimes);

    public static bool operator <(Money left, Money right) => left.Millimes < right.Millimes;

    public static bool operator >(Money left, Money right) => left.Millimes > right.Millimes;

    public static bool operator <=(Money left, Money right) => left.Millimes <= right.Millimes;

    public static bool operator >=(Money left, Money right) => left.Millimes >= right.Millimes;

    public int CompareTo(Money other) => Millimes.CompareTo(other.Millimes);

    public Money Add(Money other) => this + other;

    public Money Subtract(Money other) => this - other;

    public override string ToString() => Dinars.ToString("0.000", CultureInfo.InvariantCulture);
}

/// <summary>A percentage as written in a text: 6.5 means 6.5 %.</summary>
public readonly record struct Percentage(decimal Value)
{
    /// <summary>The value as a fraction (6.5 % → 0.065).</summary>
    public decimal Rate => Value / 100m;

    /// <summary>The multiplier of an increase (6.5 % → 1.065).</summary>
    public decimal Factor => 1m + Rate;

    public override string ToString() => Value.ToString("0.##", CultureInfo.InvariantCulture) + " %";
}

/// <summary>A work regime (for example 48 h or 40 h a week) and its hours per month, used to value hourly rates.</summary>
public sealed record WorkRegime
{
    public WorkRegime(RegimeCode code, decimal hoursPerMonth)
    {
        Code = code;
        HoursPerMonth = hoursPerMonth > 0 ? hoursPerMonth : throw new DomainException(DomainError.MustBePositive, DomainField.HoursPerMonth);
    }

    public RegimeCode Code { get; }

    public decimal HoursPerMonth { get; }
}
