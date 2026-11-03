using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Tests;

public class SalaryTimelineCalculatorTests
{
    private readonly SalaryTimelineCalculator _calculator = new(new SeniorityDegreeProgression());

    /// <summary>Grid: C1/C2 x degrees 1..3, degree 1 lasts 1 year, degree 2 lasts 2 years.</summary>
    private static SalaryTable Table(int id, DateRange period, decimal basis, string number = "4")
    {
        var table = SalaryTable.Create(1, number, SalaryType.Monthly, "x", period);
        table.AssignIdentity(id);
        var c1 = table.AddCategory("C1");
        var c2 = table.AddCategory("C2");
        var d1 = table.AddDegree(1, 1);
        var d2 = table.AddDegree(2, 2);
        var d3 = table.AddDegree(3, 2);
        table.SetSalary(c1.Key, d1.Key, basis + 1);
        table.SetSalary(c1.Key, d2.Key, basis + 2);
        table.SetSalary(c1.Key, d3.Key, basis + 3);
        table.SetSalary(c2.Key, d1.Key, basis + 11);
        return table;
    }

    private static Affair AffairOn(DateOnly start, DateOnly end, int salaryTableId = 1, string category = "C1", int degree = 1)
        => Affair.Open("A", "T", "Claimant", new DateRange(start, end), 1, salaryTableId, category, degree);

    [Fact]
    public void Produces_one_line_per_month_of_employment()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 15), new DateOnly(2020, 4, 2));

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.Equal(
            [new YearMonth(2020, 1), new YearMonth(2020, 2), new YearMonth(2020, 3), new YearMonth(2020, 4)],
            timeline.Lines.Select(l => l.Period));
    }

    [Fact]
    public void Degree_advances_after_the_years_spent_in_each_degree()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));

        var timeline = _calculator.Calculate(affair, [table]);
        int DegreeAt(int y, int m) => timeline.Lines.Single(l => l.Period == new YearMonth(y, m)).Degree;

        Assert.Equal(1, DegreeAt(2020, 1));
        Assert.Equal(1, DegreeAt(2020, 12));
        Assert.Equal(2, DegreeAt(2021, 1));  // 1 year in degree 1
        Assert.Equal(2, DegreeAt(2022, 12));
        Assert.Equal(3, DegreeAt(2023, 1));  // +2 years in degree 2
        Assert.Equal(3, DegreeAt(2024, 12)); // last degree is a ceiling
    }

    [Fact]
    public void Progression_starts_from_the_initial_degree()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 1), new DateOnly(2022, 6, 30), degree: 2);

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.Equal(2, timeline.Lines[0].Degree);
        Assert.Equal(3, timeline.Lines.Single(l => l.Period == new YearMonth(2022, 1)).Degree);
    }

    [Fact]
    public void Each_month_uses_the_table_version_applicable_at_that_time()
    {
        var old = Table(1, new DateRange(new DateOnly(2000, 1, 1), new DateOnly(2020, 6, 30)), 100);
        var current = Table(2, DateRange.From(new DateOnly(2020, 7, 1)), 200);
        var affair = AffairOn(new DateOnly(2020, 5, 1), new DateOnly(2020, 8, 31), salaryTableId: 1);

        var timeline = _calculator.Calculate(affair, [old, current]);

        Assert.Equal([101m, 101m, 201m, 201m], timeline.Lines.Select(l => l.Amount));
        Assert.Equal([1, 1, 2, 2], timeline.Lines.Select(l => l.SalaryTableId!.Value));
    }

    [Fact]
    public void Tables_with_another_table_number_are_ignored()
    {
        var mine = Table(1, new DateRange(new DateOnly(2000, 1, 1), new DateOnly(2020, 6, 30)), 100);
        var other = Table(2, DateRange.From(new DateOnly(2020, 7, 1)), 900, number: "9");
        var affair = AffairOn(new DateOnly(2020, 6, 1), new DateOnly(2020, 7, 31));

        var timeline = _calculator.Calculate(affair, [mine, other]);

        Assert.Equal(TimelineIssue.None, timeline.Lines[0].Issue);
        Assert.Equal(TimelineIssue.NoApplicableTable, timeline.Lines[1].Issue);
        Assert.Null(timeline.Lines[1].Amount);
    }

    [Fact]
    public void Missing_cell_is_reported_not_guessed()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 1), new DateOnly(2021, 3, 31), category: "C2");

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.Equal(111m, timeline.Lines[0].Amount);
        var degree2 = timeline.Lines.Single(l => l.Period == new YearMonth(2021, 1));
        Assert.Equal(TimelineIssue.SalaryMissing, degree2.Issue);
        Assert.Null(degree2.Amount);
    }

    [Fact]
    public void Unknown_category_is_reported()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 31), category: "ZZ");

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.Equal(TimelineIssue.CategoryNotFound, timeline.Lines.Single().Issue);
    }

    [Fact]
    public void Affair_without_salary_table_reports_every_month()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = Affair.Open("A", "T", "C", new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 2, 28)), 1, null, "C1", 1);

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.All(timeline.Lines, l => Assert.Equal(TimelineIssue.NoApplicableTable, l.Issue));
    }

    [Fact]
    public void Summary_totals_only_resolved_monthly_amounts()
    {
        var table = Table(1, DateRange.From(new DateOnly(2000, 1, 1)), 100);
        var affair = AffairOn(new DateOnly(2020, 1, 1), new DateOnly(2021, 2, 28), category: "C2");

        var timeline = _calculator.Calculate(affair, [table]);

        Assert.Equal(12 * 111m, timeline.TotalMonthlyAmount);
        Assert.Equal(2, timeline.UnresolvedMonths);
        Assert.False(timeline.IsComplete);
    }
}
