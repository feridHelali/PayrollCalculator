using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>07.12: what the review of Gemini's insights added to the domain (G2 and G5).</summary>
public class ReviewAdditionsTests
{
    private static readonly ComponentCode Meal = new("MEAL");

    private static Rulebook Monthly(params RuleEvent[] extra) =>
        Rulebook.From([Declare(D(1990), PayUnit.Monthly, "C1"), Enact(D(1990), ("C1", 1, 300m)), Regime(D(1990), 208m), .. extra]);

    private static PayLedger Ledger(Rulebook rules, DateOnly hired, DateOnly separated) =>
        new PayLedgerBuilder(new CalendarDaysProration()).Build(Career.From([new Hired(hired, Workers, C1, 1, H48), new Separated(separated)]), rules);

    [Fact]
    public void A_per_day_allowance_uses_the_working_days_of_a_full_month_in_proportion()
    {
        var rules = Monthly(new AllowanceIntroduced(D(1990), Cite(), Meal, "منحة القفة", new PerDayWorked(M(2.5m), 22)));
        var ledger = Ledger(rules, D(2000, 4, 16), D(2000, 5, 31));

        var april = ledger.Entries.Single(e => e.Component == Meal && e.Month == new YearMonth(2000, 4));
        var may = ledger.Entries.Single(e => e.Component == Meal && e.Month == new YearMonth(2000, 5));
        Assert.Equal((PayUnit.Daily, M(2.5m), 22m, M(55m)), (may.Unit, may.Rate, may.Quantity, may.Amount));
        Assert.Equal((11m, M(27.5m)), (april.Quantity, april.Amount)); // 15 of 30 days paid: half the working days
    }

    [Fact]
    public void A_per_day_rate_is_raised_by_a_percentage()
    {
        var rules = Monthly(
            new AllowanceIntroduced(D(1990), Cite(), Meal, "منحة القفة", new PerDayWorked(M(2.5m), 26)),
            new AllowanceRevalued(D(1995), Cite(), Meal, null, new Percentage(10)));

        Assert.Equal(new PerDayWorked(M(2.75m), 26), rules.At(D(1995)).Allowance(Meal)!.Basis);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Working_days_must_be_those_of_a_month(int days) =>
        Assert.Equal(DomainError.OutOfRange, Assert.Throws<DomainException>(() => new PerDayWorked(M(1m), days)).Error);

    [Fact]
    public void A_grid_line_is_monthly_or_hourly_only() =>
        Assert.Throws<DomainException>(() => Declare(D(1990), PayUnit.Daily));

    [Fact]
    public void An_amount_lower_than_the_previous_degree_is_a_warning_and_the_written_value_is_kept()
    {
        var rules = Rulebook.From([Declare(D(1990), PayUnit.Hourly, "C1"), Enact(D(1990), ("C1", 1, 2.1m), ("C1", 2, 1.9m), ("C1", 3, 2.3m))]);

        var warning = Assert.Single(rules.Warnings);
        Assert.Equal((RuleWarningKind.NonMonotonicGrid, "C1/2", M(2.1m), M(1.9m)), (warning.Kind, warning.Subject, warning.Computed, warning.Published));
        Assert.Equal(M(1.9m), rules.At(D(1990)).Line(Workers)!.Grid!.AmountFor(C1, 2));
    }

    [Fact]
    public void Equal_amounts_are_not_a_decrease_and_a_raise_does_not_repeat_an_old_warning()
    {
        var flat = Rulebook.From([Declare(D(1990), PayUnit.Monthly, "C1"), Enact(D(1990), ("C1", 1, 300m), ("C1", 2, 300m))]);
        var raised = Rulebook.From([Declare(D(1990), PayUnit.Monthly, "C1"), Enact(D(1990), ("C1", 1, 310m), ("C1", 2, 300m)),
            new SalaryGridRevalued(D(1995), Cite(), Workers, new PercentForAll(new Percentage(5)), RoundingRule.HalfUp)]);
        var published = Rulebook.From([Declare(D(1990), PayUnit.Monthly, "C1"), Enact(D(1990), ("C1", 1, 300m), ("C1", 2, 310m)),
            new SalaryGridRevalued(D(1995), Cite(), Workers, new PercentForAll(new Percentage(5)), RoundingRule.HalfUp, Cells(("C1", 2, 31.5m)))]);

        Assert.Empty(flat.Warnings);
        Assert.Single(raised.Warnings); // from the enactment only
        Assert.Contains(published.Warnings, w => w.Kind == RuleWarningKind.NonMonotonicGrid && w.Subject == "C1/2");
    }
}
