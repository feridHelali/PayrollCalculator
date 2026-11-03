using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

public class AllowanceTests
{
    private static Allowance Presence()
    {
        var presence = Allowance.Create(1, "منحة الحضور", AllowanceUnit.PerMonth, null, null);
        presence.ReplaceRates(
        [
            new AllowanceRateSpec(new DateOnly(2026, 1, 1), 9.036m, "JORT 49/2024 p. 3516"),
            new AllowanceRateSpec(new DateOnly(2024, 1, 1), 7.930m, "JORT 49/2024 p. 3516"),
            new AllowanceRateSpec(new DateOnly(2025, 1, 1), 8.445m, "JORT 49/2024 p. 3516"),
        ]);
        return presence;
    }

    [Fact]
    public void Each_amount_applies_from_its_date_until_the_next_one()
    {
        var presence = Presence();

        Assert.Null(presence.RateOn(new DateOnly(2023, 12, 31)));
        Assert.Equal(7.930m, presence.RateOn(new DateOnly(2024, 6, 1))!.Value);
        Assert.Equal(8.445m, presence.RateOn(new DateOnly(2025, 1, 1))!.Value);
        Assert.Equal(9.036m, presence.RateOn(new DateOnly(2030, 1, 1))!.Value);
        Assert.Equal([2024, 2025, 2026], presence.Rates.Select(r => r.EffectiveFrom.Year));
    }

    [Fact]
    public void Two_amounts_cannot_share_a_date()
    {
        var error = Assert.Throws<DomainException>(() => Presence().ReplaceRates(
            [new AllowanceRateSpec(new DateOnly(2024, 1, 1), 1m, null), new AllowanceRateSpec(new DateOnly(2024, 1, 1), 2m, null)]));

        Assert.Equal((DomainError.Duplicate, DomainField.EffectiveDate), (error.Error, error.Field));
    }

    [Fact]
    public void An_amount_has_at_most_three_decimals_and_a_percentage_at_most_100()
    {
        var monthly = Allowance.Create(1, "x", AllowanceUnit.PerMonth, null, null);
        var percent = Allowance.Create(1, "y", AllowanceUnit.PercentOfBase, null, null);

        Assert.Throws<DomainException>(() => monthly.ReplaceRates([new AllowanceRateSpec(new DateOnly(2024, 1, 1), 1.0005m, null)]));
        Assert.Throws<DomainException>(() => percent.ReplaceRates([new AllowanceRateSpec(new DateOnly(2024, 1, 1), 101m, null)]));
        Assert.Throws<DomainException>(() => monthly.ReplaceRates([new AllowanceRateSpec(new DateOnly(2024, 1, 1), -1m, null)]));
        percent.ReplaceRates([new AllowanceRateSpec(new DateOnly(2024, 1, 1), 2.5m, null)]);
    }

    [Fact]
    public void A_prime_needs_a_name_and_an_agreement()
    {
        Assert.Equal(DomainField.AllowanceName, Assert.Throws<DomainException>(() => Allowance.Create(1, " ", AllowanceUnit.PerMonth, null, null)).Field);
        Assert.Throws<DomainException>(() => Allowance.Create(0, "x", AllowanceUnit.PerMonth, null, null));
    }
}
