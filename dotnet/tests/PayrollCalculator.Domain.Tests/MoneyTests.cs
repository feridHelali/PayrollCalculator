using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.2: amounts are whole millimes (TND has 3 decimals); rounding happens only where a rule says so.</summary>
public class MoneyTests
{
    [Fact]
    public void Dinars_are_stored_as_whole_millimes()
    {
        var amount = Money.FromDinars(76.491m);

        Assert.Equal(76_491, amount.Millimes);
        Assert.Equal(76.491m, amount.Dinars);
        Assert.Equal("76.491", amount.ToString());
    }

    [Fact]
    public void An_amount_finer_than_a_millime_is_rejected_unless_a_rounding_rule_is_given()
    {
        var error = Assert.Throws<DomainException>(() => Money.FromDinars(1.2345m));
        Assert.Equal((DomainError.OutOfRange, DomainField.Amount), (error.Error, error.Field));

        Assert.Equal(Money.FromDinars(1.235m), Money.Round(1.2345m, RoundingRule.HalfUp));
        Assert.Equal(Money.FromDinars(1.234m), Money.Round(1.2345m, RoundingRule.Truncate));
    }

    [Theory]
    [InlineData(76.491, 6.5, RoundingRule.HalfUp, 81.463)]
    [InlineData(76.491, 6.5, RoundingRule.Truncate, 81.462)]
    [InlineData(106.500, 6.5, RoundingRule.HalfUp, 113.423)]
    [InlineData(106.500, 6.5, RoundingRule.Truncate, 113.422)]
    [InlineData(8.445, 7, RoundingRule.HalfUp, 9.036)]
    public void A_percentage_increase_is_rounded_to_the_millime(double amount, double percent, RoundingRule rule, double expected)
    {
        var increased = Money.FromDinars((decimal)amount).Increase(new Percentage((decimal)percent), rule);

        Assert.Equal(Money.FromDinars((decimal)expected), increased);
    }

    [Fact]
    public void Sums_of_millimes_are_exact()
    {
        var total = Enumerable.Repeat(Money.FromDinars(0.001m), 1000).Aggregate(Money.Zero, (a, b) => a + b);

        Assert.Equal(Money.FromDinars(1m), total);
        Assert.Equal(Money.FromDinars(0.5m), Money.FromDinars(1.5m) - Money.FromDinars(1m));
        Assert.True(Money.FromDinars(2m) > Money.FromDinars(1.999m));
    }

    [Fact]
    public void Multiplying_by_a_quantity_rounds_with_the_given_rule()
    {
        // Hourly rate × 173.33 hours.
        var monthly = Money.FromDinars(2.345m).Times(173.33m, RoundingRule.HalfUp);

        Assert.Equal(Money.Round(2.345m * 173.33m, RoundingRule.HalfUp), monthly);
        Assert.Equal(406.459m, monthly.Dinars);
    }

    [Fact]
    public void A_percentage_gives_its_factor()
    {
        Assert.Equal(1.065m, new Percentage(6.5m).Factor);
        Assert.Equal(0.065m, new Percentage(6.5m).Rate);
    }

    [Fact]
    public void A_work_regime_needs_a_code_and_positive_hours()
    {
        var regime = new WorkRegime(new RegimeCode("H48"), 208m);
        Assert.Equal(208m, regime.HoursPerMonth);

        var error = Assert.Throws<DomainException>(() => new WorkRegime(new RegimeCode("H40"), 0m));
        Assert.Equal((DomainError.MustBePositive, DomainField.HoursPerMonth), (error.Error, error.Field));
        Assert.Throws<DomainException>(() => new RegimeCode(" "));
    }

    [Fact]
    public void Codes_are_trimmed_and_compared_by_value()
    {
        Assert.Equal(new CategoryCode("C1"), new CategoryCode(" C1 "));
        Assert.NotEqual(new CategoryCode("C1"), new CategoryCode("c1"));
        Assert.Equal("WORKERS", new GridLineCode("WORKERS").ToString());
        Assert.Equal(ComponentCode.Base, new ComponentCode("BASE"));
    }
}
