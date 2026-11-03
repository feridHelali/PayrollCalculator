using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.4: each rule event checks its own invariants when it is created.</summary>
public class RuleEventTests
{
    private static DomainException Fails(Action action) => Assert.ThrowsAny<DomainException>(action);

    [Fact]
    public void Every_event_has_an_identity_an_effective_date_and_a_citation()
    {
        var enacted = EnactFull(D(2022, 5), 400m);

        Assert.NotEqual(Guid.Empty, enacted.Id);
        Assert.Equal(D(2022, 5), enacted.EffectiveFrom);
        Assert.Equal(1, enacted.Source.LegalSourceId);
        Assert.Throws<ArgumentNullException>(() => new SalaryGridEnacted(D(2022), null!, Workers, Cells(("C1", 1, 1m))));
    }

    [Fact]
    public void A_grid_line_needs_a_name_and_distinct_categories()
    {
        var name = Fails(() => _ = new GridLineDeclared(D(2000), Cite(), Workers, " ", PayUnit.Monthly,
            [new CategoryDefinition(C1, "A")]));
        Assert.Equal((DomainError.Required, DomainField.GridLine), (name.Error, name.Field));

        var empty = Fails(() => _ = new GridLineDeclared(D(2000), Cite(), Workers, "عمال", PayUnit.Monthly, []));
        Assert.Equal((DomainError.Required, DomainField.CategoryCode), (empty.Error, empty.Field));

        var duplicate = Fails(() => Declare(D(2000), PayUnit.Monthly, "C1", "C1"));
        Assert.Equal((DomainError.Duplicate, DomainField.CategoryCode, "C1"), (duplicate.Error, duplicate.Field, duplicate.Detail));
    }

    [Fact]
    public void A_grid_needs_at_least_one_cell_and_no_cell_twice()
    {
        var empty = Fails(() => Enact(D(2000)));
        Assert.Equal((DomainError.Required, DomainField.GridCell), (empty.Error, empty.Field));

        var twice = Fails(() => Enact(D(2000), ("C1", 1, 10m), ("C1", 1, 11m)));
        Assert.Equal((DomainError.Duplicate, DomainField.GridCell, "C1/1"), (twice.Error, twice.Field, twice.Detail));
    }

    [Fact]
    public void A_cell_has_a_positive_degree_and_a_non_negative_amount()
    {
        var degree = Fails(() => _ = new GridCell(C1, 0, M(1m)));
        Assert.Equal((DomainError.MustBePositive, DomainField.Degree), (degree.Error, degree.Field));

        var amount = Fails(() => _ = new GridCell(C1, 1, Money.FromMillimes(-1)));
        Assert.Equal((DomainError.CannotBeNegative, DomainField.Amount), (amount.Error, amount.Field));
    }

    [Fact]
    public void A_career_rule_gives_positive_months_for_positive_degrees()
    {
        var months = Fails(() => CareerRule(D(2000), (1, 0)));
        Assert.Equal((DomainError.MustBePositive, DomainField.MonthsInDegree), (months.Error, months.Field));

        var degree = Fails(() => CareerRule(D(2000), (0, 12)));
        Assert.Equal((DomainError.MustBePositive, DomainField.Degree), (degree.Error, degree.Field));
    }

    [Fact]
    public void An_allowance_cannot_use_the_base_code()
    {
        var error = Fails(() => _ = new AllowanceIntroduced(D(2000), Cite(), ComponentCode.Base, "x", new FixedMonthly(M(1m))));
        Assert.Equal((DomainError.OutOfRange, DomainField.Component, "BASE"), (error.Error, error.Field, error.Detail));
    }

    [Fact]
    public void A_revaluation_of_an_allowance_gives_a_percentage_or_a_published_amount()
    {
        var error = Fails(() => _ = new AllowanceRevalued(D(2000), Cite(), Presence, published: null, percentage: null));
        Assert.Equal((DomainError.Required, DomainField.Amount), (error.Error, error.Field));
    }

    [Fact]
    public void Seniority_brackets_are_distinct_and_not_negative()
    {
        var twice = Fails(() => _ = new SeniorityBrackets([new SeniorityBracket(5, new Percentage(5)), new SeniorityBracket(5, new Percentage(6))]));
        Assert.Equal((DomainError.Duplicate, DomainField.YearsInDegree, "5"), (twice.Error, twice.Field, twice.Detail));

        Fails(() => _ = new SeniorityBracket(-1, new Percentage(1)));
    }
}
