using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.5: the rules in force on a day are the fold of the events effective by then.</summary>
public class RulebookTests
{
    [Fact]
    public void Nothing_is_in_force_before_the_first_event()
    {
        var book = Rulebook.From([Declare(D(2000)), EnactFull(D(2000), 300m)]);

        Assert.Null(book.At(D(1999, 12, 31)).Line(Workers));
        Assert.Equal(M(300m), book.At(D(2000)).Line(Workers)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void A_grid_stays_in_force_until_a_later_one_replaces_it()
    {
        var book = Rulebook.From([Declare(D(2000)), EnactFull(D(2000), 300m), EnactFull(D(2022, 5), 400m)]);

        Assert.Equal(M(300m), book.At(D(2015, 6, 30)).Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(300m), book.At(D(2022, 4, 30)).Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(400m), book.At(D(2022, 5)).Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(400m), book.At(D(2040)).Line(Workers)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void Events_apply_by_effective_date_not_by_the_order_they_were_recorded()
    {
        // An order published in 2025 with effect from 2024 is recorded after the 2025 grid.
        var grid2025 = EnactFull(D(2025), 500m);
        var retroactive2024 = EnactFull(D(2024), 450m);
        var book = Rulebook.From([Declare(D(2000)), EnactFull(D(2000), 300m), grid2025, retroactive2024]);

        Assert.Equal(M(450m), book.At(D(2024, 6)).Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(500m), book.At(D(2025, 6)).Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal([D(2000), D(2024), D(2025)], book.ChangeDates);
    }

    [Fact]
    public void Events_on_the_same_day_apply_in_a_fixed_order_of_kinds()
    {
        // Recorded backwards: the revaluation first, then the grid, then the declaration.
        var revaluation = new SalaryGridRevalued(D(2000), Cite(), Workers, new PercentForAll(new Percentage(10)), RoundingRule.HalfUp);
        var book = Rulebook.From([revaluation, EnactFull(D(2000), 300m), Declare(D(2000))]);

        Assert.Equal(M(330m), book.At(D(2000)).Line(Workers)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void Two_events_of_the_same_kind_on_the_same_day_apply_in_recorded_order()
    {
        var book = Rulebook.From([Declare(D(2000)), EnactFull(D(2000), 300m), EnactFull(D(2000), 310m)]);

        Assert.Equal(M(310m), book.At(D(2000)).Line(Workers)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void A_grid_for_an_undeclared_line_or_category_is_rejected()
    {
        var line = Assert.Throws<DomainException>(() => Rulebook.From([EnactFull(D(2000), 300m)]));
        Assert.Equal((DomainError.Undeclared, DomainField.GridLine, "WORKERS"), (line.Error, line.Field, line.Detail));

        var category = Assert.Throws<DomainException>(() => Rulebook.From([Declare(D(2000)), Enact(D(2000), ("C9", 1, 1m))]));
        Assert.Equal((DomainError.Unknown, DomainField.CategoryCode, "C9"), (category.Error, category.Field, category.Detail));
    }

    [Fact]
    public void An_incomplete_grid_is_kept_and_reported()
    {
        var grid = Enact(D(2000), ("C1", 1, 300m), ("C1", 2, 310m), ("C2", 1, 400m));
        var book = Rulebook.From([Declare(D(2000)), grid]);

        Assert.Null(book.At(D(2000)).Line(Workers)!.Grid!.AmountFor(C2, 2));
        var warning = Assert.Single(book.Warnings);
        Assert.Equal((RuleWarningKind.GridIncomplete, grid.Id, "C2/2"), (warning.Kind, warning.EventId, warning.Subject));
    }

    [Fact]
    public void Each_value_remembers_the_event_that_set_it()
    {
        var grid = EnactFull(D(2000), 300m);
        var rule = CareerRule(D(2001), (1, 24));
        var regime = Regime(D(2000), 208m);
        var book = Rulebook.From([Declare(D(2000)), grid, rule, regime]);
        var rules = book.At(D(2001));

        Assert.Equal(grid.Id, rules.Line(Workers)!.Grid!.Cell(C1, 1)!.SetBy);
        Assert.Equal(rule.Id, rules.Line(Workers)!.CareerRule!.SetBy);
        Assert.Equal(24, rules.Line(Workers)!.CareerRule!.MonthsIn(1));
        Assert.Null(rules.Line(Workers)!.CareerRule!.MonthsIn(3));
        Assert.Equal(208m, rules.Regime(H48)!.Regime.HoursPerMonth);
        Assert.Equal(regime.Id, rules.Regime(H48)!.SetBy);
        Assert.Same(grid, book.Find(grid.Id));
    }

    [Fact]
    public void Declaring_a_line_again_amends_it_and_keeps_its_grid()
    {
        var book = Rulebook.From([
            Declare(D(2000)), EnactFull(D(2000), 300m),
            Declare(D(2010), PayUnit.Monthly, "C1", "C2", "C3"),
        ]);

        var line = book.At(D(2010)).Line(Workers)!;
        Assert.Equal(3, line.Categories.Count);
        Assert.Equal(M(300m), line.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void Allowances_are_introduced_revalued_and_abolished()
    {
        var introduced = new AllowanceIntroduced(D(2010), Cite(), Presence, "منحة الحضور", new FixedMonthly(M(20m)));
        var revalued = new AllowanceRevalued(D(2015), Cite(), Presence, new FixedMonthly(M(25m)));
        var abolished = new AllowanceAbolished(D(2020), Cite(), Presence);
        var book = Rulebook.From([introduced, revalued, abolished]);

        Assert.Null(book.At(D(2009)).Allowance(Presence));
        Assert.Equal(new FixedMonthly(M(20m)), book.At(D(2014)).Allowance(Presence)!.Basis);
        Assert.Equal(revalued.Id, book.At(D(2016)).Allowance(Presence)!.SetBy);
        Assert.Null(book.At(D(2020)).Allowance(Presence));
        Assert.Empty(book.At(D(2020)).Allowances);
    }

    [Fact]
    public void An_allowance_introduced_twice_or_revalued_before_it_exists_is_rejected()
    {
        var introduced = new AllowanceIntroduced(D(2010), Cite(), Presence, "منحة الحضور", new FixedMonthly(M(20m)));
        var twice = Assert.Throws<DomainException>(() => Rulebook.From([introduced, introduced with { }]));
        Assert.Equal((DomainError.Duplicate, DomainField.Component), (twice.Error, twice.Field));

        var early = Assert.Throws<DomainException>(() =>
            Rulebook.From([new AllowanceRevalued(D(2009), Cite(), Presence, new FixedMonthly(M(1m))), introduced]));
        Assert.Equal((DomainError.Undeclared, DomainField.Component, "PRESENCE"), (early.Error, early.Field, early.Detail));
    }
}
