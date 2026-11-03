using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.7 and 05.9: the ledger values every component of every month, and says why.</summary>
public class PayLedgerTests
{
    private static readonly PayLedgerBuilder Builder = new(new CalendarDaysProration());

    private static Career Career(DateOnly hired, DateOnly separated, int degree = 1) =>
        Careers.Career.From([new Hired(hired, Workers, C1, degree, H48), new Separated(separated)]);

    private static Rulebook Monthly(params RuleEvent[] extra) =>
        Rulebook.From([Declare(D(1990), PayUnit.Monthly, "C1"), Enact(D(1990), ("C1", 1, 300m), ("C1", 2, 310m), ("C1", 3, 320m)), Regime(D(1990), 208m), .. extra]);

    private static LedgerEntry Base(PayLedger ledger, int year, int month) =>
        ledger.Entries.Single(e => e.Component == ComponentCode.Base && e.Month == new YearMonth(year, month));

    private static LedgerEntry Entry(PayLedger ledger, ComponentCode component, int year, int month) =>
        ledger.Entries.Single(e => e.Component == component && e.Month == new YearMonth(year, month));

    [Fact]
    public void A_monthly_grid_is_prorated_by_calendar_days_in_the_first_and_last_months()
    {
        var ledger = Builder.Build(Career(D(2000, 1, 15), D(2000, 3, 10)), Monthly());

        Assert.Equal([M(164.516m), M(300m), M(96.774m)], ledger.Entries.Select(e => e.Amount!.Value)); // 300 × 17/31, 300, 300 × 10/31
        Assert.Equal(M(300m), Base(ledger, 2000, 1).Rate);
        Assert.Equal(17m / 31m, Base(ledger, 2000, 1).Proration);
        Assert.Equal(M(561.29m), ledger.Total);
        Assert.True(ledger.IsComplete);
    }

    [Fact]
    public void A_thirty_day_base_is_available_as_another_policy()
    {
        var ledger = new PayLedgerBuilder(new ThirtyDayProration()).Build(Career(D(2000, 1, 15), D(2000, 3, 10)), Monthly());

        Assert.Equal([M(170m), M(300m), M(100m)], ledger.Entries.Select(e => e.Amount!.Value));
    }

    [Fact]
    public void An_hourly_grid_is_valued_with_the_hours_of_the_regime()
    {
        var rules = Rulebook.From([Declare(D(1990), PayUnit.Hourly), Enact(D(1990), ("C1", 1, 1.5m)), Regime(D(1990), 208m)]);
        var entry = Base(Builder.Build(Career(D(2000), D(2000, 1, 31)), rules), 2000, 1);

        Assert.Equal((PayUnit.Hourly, M(1.5m), 208m, M(312m)), (entry.Unit, entry.Rate, entry.Quantity, entry.Amount));
    }

    [Fact]
    public void An_hourly_rate_without_a_known_regime_is_an_issue_not_a_guess()
    {
        var rules = Rulebook.From([Declare(D(1990), PayUnit.Hourly), Enact(D(1990), ("C1", 1, 1.5m))]);
        var ledger = Builder.Build(Career(D(2000), D(2000, 1, 31)), rules);

        Assert.Equal((LedgerIssue.RegimeUnknown, (Money?)null), (Base(ledger, 2000, 1).Issue, Base(ledger, 2000, 1).Amount));
        Assert.False(ledger.IsComplete);
        Assert.Equal(1, ledger.UnresolvedEntries);
    }

    [Fact]
    public void Months_before_any_grid_or_with_a_missing_cell_are_reported()
    {
        var rules = Rulebook.From([Declare(D(1990)), Enact(D(2000, 3), ("C1", 1, 300m))]);
        var ledger = Builder.Build(Career(D(2000, 1, 1), D(2000, 4, 30), degree: 2), rules);

        Assert.Equal(LedgerIssue.NoGridInForce, Base(ledger, 2000, 1).Issue);
        Assert.Equal(LedgerIssue.SalaryMissing, Base(ledger, 2000, 3).Issue);
        Assert.Null(Base(ledger, 2000, 3).Amount);
    }

    [Fact]
    public void A_raise_changes_the_amount_from_its_effective_month_and_is_cited()
    {
        var raise = new SalaryGridRevalued(D(2000, 7), Cite(5), Workers, new PercentForAll(new Percentage(10)), RoundingRule.HalfUp);
        var rules = Monthly(raise);
        var ledger = Builder.Build(Career(D(2000), D(2000, 12, 31)), rules);

        Assert.Equal(M(300m), Base(ledger, 2000, 6).Amount);
        Assert.Equal(M(330m), Base(ledger, 2000, 7).Amount);
        Assert.Contains(raise.Id, Base(ledger, 2000, 7).Causes);
        Assert.Equal(5, ledger.SourceOf(raise.Id)!.Source.Page);
    }

    [Fact]
    public void The_degree_of_the_career_selects_the_cell()
    {
        var rules = Monthly(CareerRule(D(1990), (1, 12), (2, 12)));
        var ledger = Builder.Build(Career(D(2000), D(2002, 12, 31)), rules);

        Assert.Equal((1, M(300m)), (Base(ledger, 2000, 12).Degree, Base(ledger, 2000, 12).Amount));
        Assert.Equal((2, M(310m)), (Base(ledger, 2001, 1).Degree, Base(ledger, 2001, 1).Amount));
        Assert.Equal((3, M(320m)), (Base(ledger, 2002, 1).Degree, Base(ledger, 2002, 1).Amount));
    }

    [Fact]
    public void Allowances_are_valued_by_their_basis()
    {
        var fixedOne = new ComponentCode("TRANSPORT");
        var hourly = new ComponentCode("HOURLY");
        var percent = new ComponentCode("OUTPUT");
        var rules = Monthly(
            new AllowanceIntroduced(D(1990), Cite(), fixedOne, "منحة النقل", new FixedMonthly(M(20m))),
            new AllowanceIntroduced(D(1990), Cite(), hourly, "منحة بالساعة", new PerHour(M(0.1m))),
            new AllowanceIntroduced(D(1990), Cite(), percent, "منحة الإنتاج", new PercentOfBase(new Percentage(10))));
        var ledger = Builder.Build(Career(D(2000, 1, 15), D(2000, 2, 29)), rules);

        Assert.Equal(M(10.968m), Entry(ledger, fixedOne, 2000, 1).Amount);  // 20 × 17/31
        Assert.Equal(M(11.406m), Entry(ledger, hourly, 2000, 1).Amount);    // 0.1 × 208 × 17/31
        Assert.Equal(M(16.452m), Entry(ledger, percent, 2000, 1).Amount);   // 10 % of 164.516
        Assert.Equal(M(30m), Entry(ledger, percent, 2000, 2).Amount);
        Assert.Equal(new Percentage(10), Entry(ledger, percent, 2000, 2).Percentage);
        Assert.Equal([ComponentCode.Base, hourly, percent, fixedOne], ledger.Components);
    }

    [Fact]
    public void A_percentage_of_base_follows_a_raise_without_a_new_event()
    {
        var percent = new ComponentCode("OUTPUT");
        var rules = Monthly(
            new AllowanceIntroduced(D(1990), Cite(), percent, "منحة الإنتاج", new PercentOfBase(new Percentage(10))),
            new SalaryGridRevalued(D(2000, 2), Cite(), Workers, new PercentForAll(new Percentage(10)), RoundingRule.HalfUp));
        var ledger = Builder.Build(Career(D(2000), D(2000, 2, 29)), rules);

        Assert.Equal(M(30m), Entry(ledger, percent, 2000, 1).Amount);
        Assert.Equal(M(33m), Entry(ledger, percent, 2000, 2).Amount);
    }

    [Fact]
    public void A_percentage_of_a_missing_base_is_an_issue()
    {
        var percent = new ComponentCode("OUTPUT");
        var rules = Rulebook.From([Declare(D(1990)), Enact(D(1990), ("C2", 1, 1m)),
            new AllowanceIntroduced(D(1990), Cite(), percent, "منحة", new PercentOfBase(new Percentage(10)))]);
        var ledger = Builder.Build(Career(D(2000), D(2000, 1, 31)), rules);

        Assert.Equal(LedgerIssue.BaseMissing, Entry(ledger, percent, 2000, 1).Issue);
    }

    [Fact]
    public void The_seniority_prime_uses_the_bracket_of_the_years_of_service()
    {
        var seniority = new ComponentCode("SENIORITY");
        var rules = Monthly(new AllowanceIntroduced(D(1990), Cite(), seniority, "منحة الأقدمية",
            new SeniorityBrackets([new SeniorityBracket(5, new Percentage(5)), new SeniorityBracket(10, new Percentage(10))])));
        var ledger = Builder.Build(Career(D(2000), D(2010, 12, 31)), rules);

        Assert.Equal(Money.Zero, Entry(ledger, seniority, 2004, 12).Amount);
        Assert.Equal(M(15m), Entry(ledger, seniority, 2005, 1).Amount);  // 5 % of 300
        Assert.Equal(M(30m), Entry(ledger, seniority, 2010, 1).Amount);  // 10 %
    }

    [Fact]
    public void An_abolished_allowance_stops_from_its_date()
    {
        var presence = Presence;
        var rules = Monthly(
            new AllowanceIntroduced(D(1990), Cite(), presence, "منحة الحضور", new FixedMonthly(M(20m))),
            new AllowanceAbolished(D(2000, 2), Cite(), presence));
        var ledger = Builder.Build(Career(D(2000), D(2000, 3, 31)), rules);

        Assert.Single(ledger.Entries, e => e.Component == presence);
        Assert.Equal(M(20m), ledger.TotalFor(presence));
    }

    [Fact]
    public void The_ledger_carries_the_rule_warnings()
    {
        var raise = new SalaryGridRevalued(D(2000, 7), Cite(), Workers, new PercentForAll(new Percentage(6.5m)), RoundingRule.HalfUp,
            published: Cells(("C1", 1, 319.4m)));
        var ledger = Builder.Build(Career(D(2000), D(2000, 12, 31)), Monthly(raise));

        Assert.Single(ledger.RuleWarnings);
        Assert.Equal(M(319.4m), Base(ledger, 2000, 7).Amount);
    }
}
