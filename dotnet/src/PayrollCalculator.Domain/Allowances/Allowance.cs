using System.Globalization;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Allowances;

/// <summary>How the amount of a prime or allowance is expressed.</summary>
public enum AllowanceUnit
{
    /// <summary>Dinars per month.</summary>
    PerMonth = 0,

    /// <summary>Dinars per hour worked.</summary>
    PerHour = 1,

    /// <summary>Dinars per day worked.</summary>
    PerDayWorked = 2,

    /// <summary>A percentage of the base salary.</summary>
    PercentOfBase = 3,
}

/// <summary>An amount of a prime in force from a date, until the next one, with the text it comes from.</summary>
public sealed class AllowanceRate
{
    internal AllowanceRate(DateOnly effectiveFrom, decimal value, string? source)
    {
        EffectiveFrom = effectiveFrom;
        Value = value;
        Source = source;
    }

    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Dinars (up to three decimals), or a percentage for <see cref="AllowanceUnit.PercentOfBase"/>.</summary>
    public decimal Value { get; private set; }

    /// <summary>Where the amount is written, for example "JORT 49/2024, p. 3516, art. 49".</summary>
    public string? Source { get; private set; }
}

/// <summary>Input describing a rate when editing a prime.</summary>
public sealed record AllowanceRateSpec(DateOnly EffectiveFrom, decimal Value, string? Source);

/// <summary>
/// Aggregate root: a prime or allowance (منحة) of an agreement, with its amounts over time.
/// Each amount applies from its date until the next one (no end dates, so no gaps).
/// </summary>
public sealed class Allowance : Entity
{
    private readonly List<AllowanceRate> _rates = [];

    private Allowance(int agreementId, string name, AllowanceUnit unit, string? appliesTo, string? conditions)
    {
        AgreementId = agreementId;
        Name = name;
        Unit = unit;
        AppliesTo = appliesTo;
        Conditions = conditions;
    }

    // Required by EF Core.
    private Allowance()
    {
        Name = string.Empty;
    }

    public int AgreementId { get; private set; }

    public string Name { get; private set; }

    public AllowanceUnit Unit { get; private set; }

    /// <summary>Who receives it, when not every employee (for example «الإطارات»).</summary>
    public string? AppliesTo { get; private set; }

    /// <summary>Conditions written in the text (for example: lost after more than one unjustified absence in the month).</summary>
    public string? Conditions { get; private set; }

    /// <summary>The amounts, by effective date.</summary>
    public IReadOnlyList<AllowanceRate> Rates => _rates.OrderBy(r => r.EffectiveFrom).ToList();

    public static Allowance Create(int agreementId, string name, AllowanceUnit unit, string? appliesTo, string? conditions) =>
        new(Guard.Positive(agreementId, DomainField.Agreement), Guard.Required(name, DomainField.AllowanceName), unit,
            Guard.Optional(appliesTo), Guard.Optional(conditions));

    public void Describe(string name, AllowanceUnit unit, string? appliesTo, string? conditions)
    {
        Name = Guard.Required(name, DomainField.AllowanceName);
        Unit = unit;
        AppliesTo = Guard.Optional(appliesTo);
        Conditions = Guard.Optional(conditions);
        Validate(_rates.Select(r => new AllowanceRateSpec(r.EffectiveFrom, r.Value, r.Source)).ToList());
    }

    /// <summary>Replaces all the amounts (as edited in the form). Dates must be distinct.</summary>
    public void ReplaceRates(IEnumerable<AllowanceRateSpec> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        var list = rates.ToList();
        Validate(list);
        _rates.Clear();
        _rates.AddRange(list.Select(r => new AllowanceRate(r.EffectiveFrom, r.Value, Guard.Optional(r.Source))));
    }

    /// <returns>The amount in force on <paramref name="day"/>, or null before the first one.</returns>
    public AllowanceRate? RateOn(DateOnly day) => _rates.Where(r => r.EffectiveFrom <= day).MaxBy(r => r.EffectiveFrom);

    private void Validate(IReadOnlyList<AllowanceRateSpec> rates)
    {
        var duplicate = rates.GroupBy(r => r.EffectiveFrom).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException(DomainError.Duplicate, DomainField.EffectiveDate, duplicate.Key.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        foreach (var rate in rates)
        {
            Guard.NotNegative(rate.Value, DomainField.Amount);
            if (Unit == AllowanceUnit.PercentOfBase && rate.Value > 100)
            {
                throw new DomainException(DomainError.OutOfRange, DomainField.Percentage, rate.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (Unit != AllowanceUnit.PercentOfBase && decimal.Round(rate.Value, 3) != rate.Value)
            {
                throw new DomainException(DomainError.OutOfRange, DomainField.Amount, rate.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}

public interface IAllowanceRepository : IRepository<Allowance>
{
    Task<IReadOnlyList<Allowance>> ListByAgreementAsync(int? agreementId, CancellationToken cancellationToken = default);
}
