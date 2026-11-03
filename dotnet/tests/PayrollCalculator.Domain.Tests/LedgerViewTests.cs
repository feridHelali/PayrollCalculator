using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.10: the same ledger seen as every month, as changes only, as collapsed periods, or filtered.</summary>
public class LedgerViewTests
{
    // 2000: a base of 300, raised by 10 % in July; a presence allowance of 20 from April. Hired on 15 January.
    private static PayLedger Year2000()
    {
        var rules = Rulebook.From([
            Declare(D(1990)), Enact(D(1990), ("C1", 1, 300m)), Regime(D(1990), 208m),
            new SalaryGridRevalued(D(2000, 7), Cite(), Workers, new PercentForAll(new Percentage(10)), RoundingRule.HalfUp),
            new AllowanceIntroduced(D(2000, 4), Cite(), Presence, "منحة الحضور", new FixedMonthly(M(20m))),
        ]);
        var career = Career.From([new Hired(D(2000, 1, 15), Workers, C1, 1, H48), new Separated(D(2000, 12, 31))]);
        return new PayLedgerBuilder(new CalendarDaysProration()).Build(career, rules);
    }

    [Fact]
    public void Changes_only_keeps_the_first_month_and_each_month_a_value_changes()
    {
        var changes = Year2000().ChangesOnly();

        Assert.Equal(
            ["BASE 2000-01", "PRESENCE 2000-04", "BASE 2000-07"],
            changes.Entries.Select(e => $"{e.Component} {e.Month}"));
    }

    [Fact]
    public void Runs_of_identical_months_collapse_into_periods_even_when_prorated()
    {
        var runs = Year2000().Collapse();

        Assert.Equal(3, runs.Count);
        var first = runs[0];
        Assert.Equal((ComponentCode.Base, new YearMonth(2000, 1), new YearMonth(2000, 6), 6), (first.Component, first.From, first.To, first.Months));
        Assert.Equal(M(300m), first.Rate);
        Assert.Equal(M(164.516m) + M(1500m), first.Total);
        Assert.Equal((new YearMonth(2000, 7), new YearMonth(2000, 12), M(1980m)), (runs[1].From, runs[1].To, runs[1].Total));
        Assert.Equal((Presence, 9, M(180m)), (runs[2].Component, runs[2].Months, runs[2].Total));
    }

    [Fact]
    public void Filters_combine_component_period_and_issues()
    {
        var ledger = Year2000();

        var presence = ledger.Filter(new LedgerFilter(Components: [Presence]));
        Assert.All(presence.Entries, e => Assert.Equal(Presence, e.Component));
        Assert.Equal(9, presence.Entries.Count);

        var summer = ledger.Filter(new LedgerFilter(From: new YearMonth(2000, 6), To: new YearMonth(2000, 8)));
        Assert.Equal(6, summer.Entries.Count);

        Assert.Empty(ledger.Filter(new LedgerFilter(IssuesOnly: true)).Entries);
        Assert.Equal(M(700m), ledger.Filter(new LedgerFilter(From: new YearMonth(2000, 8), To: new YearMonth(2000, 9))).Total);
    }

    [Fact]
    public void Totals_are_given_by_component_and_by_year()
    {
        var ledger = Year2000();

        Assert.Equal(M(164.516m) + M(1500m) + M(1980m), ledger.TotalFor(ComponentCode.Base));
        Assert.Equal(M(180m), ledger.TotalFor(Presence));
        Assert.Equal(ledger.Total, Assert.Single(ledger.TotalsByYear()).Value);
        Assert.Equal(2000, ledger.TotalsByYear().Single().Key);
    }
}
