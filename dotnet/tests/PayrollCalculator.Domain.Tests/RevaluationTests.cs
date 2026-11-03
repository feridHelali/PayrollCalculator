using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>
/// 05.6: a raise derives a new grid from the one in force. The amounts published in the text win over the computed ones,
/// and any difference is reported (the 2024 order: 76.491 × 1.065 = 81.4629, published 81.462).
/// </summary>
public class RevaluationTests
{
    private static PayRules After(params RuleEvent[] events) => Rulebook.From([Declare(D(2000)), .. events]).At(D(2030));

    [Fact]
    public void A_percentage_for_all_raises_every_cell()
    {
        var rules = After(EnactFull(D(2000), 300m),
            new SalaryGridRevalued(D(2024, 5), Cite(), Workers, new PercentForAll(new Percentage(6.5m)), RoundingRule.HalfUp));

        Assert.Equal(M(319.5m), rules.Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(436.65m), rules.Line(Workers)!.Grid!.AmountFor(C2, 2));
    }

    [Fact]
    public void Raises_compound_on_the_amount_in_force()
    {
        var rules = After(Enact(D(2000), ("C1", 1, 76.491m)),
            new SalaryGridRevalued(D(2024, 5), Cite(), Workers, new PercentForAll(new Percentage(6.5m)), RoundingRule.HalfUp),
            new SalaryGridRevalued(D(2025, 5), Cite(), Workers, new PercentForAll(new Percentage(7m)), RoundingRule.HalfUp));

        // 76.491 → 81.463 → 87.165 (81.463 × 1.07 = 87.16541)
        Assert.Equal(M(87.165m), rules.Line(Workers)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public void A_published_amount_wins_and_a_difference_is_reported()
    {
        var raise = new SalaryGridRevalued(D(2024, 5), Cite(2), Workers, new PercentForAll(new Percentage(6.5m)), RoundingRule.HalfUp,
            published: Cells(("C1", 1, 81.462m)));
        var book = Rulebook.From([Declare(D(2000), PayUnit.Monthly, "C1"), Enact(D(2000), ("C1", 1, 76.491m), ("C1", 2, 100m)), raise]);
        var cell = book.At(D(2024, 5)).Line(Workers)!.Grid!.Cell(C1, 1)!;

        Assert.Equal(M(81.462m), cell.Amount);
        Assert.Equal(raise.Id, cell.SetBy);
        Assert.Equal(M(106.5m), book.At(D(2024, 5)).Line(Workers)!.Grid!.AmountFor(C1, 2));

        var warning = Assert.Single(book.Warnings);
        Assert.Equal(RuleWarningKind.PublishedDiffersFromComputed, warning.Kind);
        Assert.Equal((raise.Id, "C1/1", M(81.463m), M(81.462m)), (warning.EventId, warning.Subject, warning.Computed, warning.Published));
        Assert.Equal(-1, warning.Difference!.Value.Millimes);
    }

    [Fact]
    public void A_published_amount_equal_to_the_computed_one_raises_no_warning()
    {
        var raise = new SalaryGridRevalued(D(2024, 5), Cite(), Workers, new PercentForAll(new Percentage(6.5m)), RoundingRule.Truncate,
            published: Cells(("C1", 1, 81.462m)));
        var book = Rulebook.From([Declare(D(2000), PayUnit.Monthly, "C1"), Enact(D(2000), ("C1", 1, 76.491m)), raise]);

        Assert.Empty(book.Warnings);
    }

    [Fact]
    public void A_percentage_by_category_raises_only_the_categories_it_names()
    {
        var rule = new PercentByCategory(new Dictionary<CategoryCode, Percentage> { [C1] = new(11.6m) });
        var rules = After(EnactFull(D(2000), 100m), new SalaryGridRevalued(D(2014), Cite(), Workers, rule, RoundingRule.HalfUp));

        Assert.Equal(M(111.6m), rules.Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(200m), rules.Line(Workers)!.Grid!.AmountFor(C2, 1));
    }

    [Fact]
    public void A_fixed_increase_adds_the_same_amount_to_every_cell()
    {
        var rules = After(EnactFull(D(2000), 100m),
            new SalaryGridRevalued(D(2014), Cite(), Workers, new FixedIncrease(M(12.33m)), RoundingRule.HalfUp));

        Assert.Equal(M(112.33m), rules.Line(Workers)!.Grid!.AmountFor(C1, 1));
        Assert.Equal(M(232.33m), rules.Line(Workers)!.Grid!.AmountFor(C2, 3));
    }

    [Fact]
    public void A_revaluation_without_a_grid_or_for_an_unknown_category_is_rejected()
    {
        var noGrid = Assert.Throws<DomainException>(() =>
            After(new SalaryGridRevalued(D(2014), Cite(), Workers, new PercentForAll(new Percentage(1)), RoundingRule.HalfUp)));
        Assert.Equal((DomainError.NotApplicable, DomainField.GridLine), (noGrid.Error, noGrid.Field));

        var unknown = new PercentByCategory(new Dictionary<CategoryCode, Percentage> { [new CategoryCode("C9")] = new(1) });
        var category = Assert.Throws<DomainException>(() =>
            After(EnactFull(D(2000), 100m), new SalaryGridRevalued(D(2014), Cite(), Workers, unknown, RoundingRule.HalfUp)));
        Assert.Equal((DomainError.Unknown, DomainField.CategoryCode, "C9"), (category.Error, category.Field, category.Detail));

        var published = Assert.Throws<DomainException>(() => After(EnactFull(D(2000), 100m),
            new SalaryGridRevalued(D(2014), Cite(), Workers, new PercentForAll(new Percentage(1)), RoundingRule.HalfUp, Cells(("C1", 9, 1m)))));
        Assert.Equal((DomainError.Unknown, DomainField.GridCell, "C1/9"), (published.Error, published.Field, published.Detail));
    }

    [Fact]
    public void An_allowance_is_revalued_by_percentage_with_its_published_amount_checked()
    {
        var introduced = new AllowanceIntroduced(D(2022), Cite(), Presence, "منحة", new FixedMonthly(M(7.930m)));
        var raise = new AllowanceRevalued(D(2025), Cite(), Presence, new FixedMonthly(M(8.445m)), new Percentage(6.5m), RoundingRule.HalfUp);
        var book = Rulebook.From([introduced, raise]);

        Assert.Equal(new FixedMonthly(M(8.445m)), book.At(D(2025)).Allowance(Presence)!.Basis);
        Assert.Empty(book.Warnings); // 7.930 × 1.065 = 8.44545 → 8.445

        var byPercentOnly = Rulebook.From([introduced, new AllowanceRevalued(D(2025), Cite(), Presence, null, new Percentage(10), RoundingRule.HalfUp)]);
        Assert.Equal(new FixedMonthly(M(8.723m)), byPercentOnly.At(D(2025)).Allowance(Presence)!.Basis);
    }

    [Fact]
    public void A_percentage_of_base_allowance_cannot_be_raised_by_a_percentage()
    {
        var introduced = new AllowanceIntroduced(D(2022), Cite(), Presence, "منحة", new PercentOfBase(new Percentage(5)));
        var error = Assert.Throws<DomainException>(() =>
            Rulebook.From([introduced, new AllowanceRevalued(D(2025), Cite(), Presence, null, new Percentage(10), RoundingRule.HalfUp)]));

        Assert.Equal((DomainError.NotApplicable, DomainField.Component, "PRESENCE"), (error.Error, error.Field, error.Detail));
    }
}
