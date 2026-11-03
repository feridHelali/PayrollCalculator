using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Tests;

public class SalaryTableTests
{
    private static SalaryTable NewTable() => SalaryTable.Create(
        agreementId: 1,
        tableNumber: "4",
        salaryType: SalaryType.Monthly,
        concernedEmployees: "Administratifs",
        applicationPeriod: DateRange.From(new DateOnly(2023, 1, 1)));

    [Fact]
    public void Create_requires_a_table_number()
    {
        Assert.Throws<DomainException>(() => SalaryTable.Create(1, " ", SalaryType.Monthly, "x", DateRange.From(new DateOnly(2023, 1, 1))));
    }

    [Fact]
    public void Categories_keep_insertion_order()
    {
        var table = NewTable();
        table.AddCategory("C1");
        table.AddCategory("C2");
        Assert.Equal(["C1", "C2"], table.Categories.Select(c => c.Label));
    }

    [Fact]
    public void Duplicate_category_label_is_rejected()
    {
        var table = NewTable();
        table.AddCategory("C1");
        Assert.Throws<DomainException>(() => table.AddCategory(" C1 "));
    }

    [Fact]
    public void Degrees_are_ordered_by_number_and_unique()
    {
        var table = NewTable();
        table.AddDegree(2, yearsInDegree: 2);
        table.AddDegree(1, yearsInDegree: 1);
        Assert.Equal([1, 2], table.Degrees.Select(d => d.Number));
        Assert.Throws<DomainException>(() => table.AddDegree(2, 3));
    }

    [Fact]
    public void Negative_years_in_degree_is_rejected()
    {
        Assert.Throws<DomainException>(() => NewTable().AddDegree(1, -1));
    }

    [Fact]
    public void Set_and_read_salary_for_a_cell()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);

        table.SetSalary(c1.Key, d1.Key, 450.5m);

        Assert.Equal(450.5m, table.SalaryFor(c1.Key, d1.Key));
        Assert.Equal(450.5m, table.SalaryFor("C1", 1));
    }

    [Fact]
    public void Setting_salary_overwrites_the_existing_cell()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        table.SetSalary(c1.Key, d1.Key, 1m);
        table.SetSalary(c1.Key, d1.Key, 2m);
        Assert.Single(table.Cells);
        Assert.Equal(2m, table.SalaryFor(c1.Key, d1.Key));
    }

    [Fact]
    public void Salary_for_unknown_axis_is_rejected()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        Assert.Throws<DomainException>(() => table.SetSalary(c1.Key, "nope", 1m));
        Assert.Throws<DomainException>(() => table.SetSalary("nope", "nope", 1m));
    }

    [Fact]
    public void Negative_salary_is_rejected()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        Assert.Throws<DomainException>(() => table.SetSalary(c1.Key, d1.Key, -1m));
    }

    [Fact]
    public void Clearing_a_cell_removes_it()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        table.SetSalary(c1.Key, d1.Key, 5m);
        table.ClearSalary(c1.Key, d1.Key);
        Assert.Null(table.SalaryFor(c1.Key, d1.Key));
    }

    [Fact]
    public void Removing_a_category_removes_its_cells()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var c2 = table.AddCategory("C2");
        var d1 = table.AddDegree(1, 1);
        table.SetSalary(c1.Key, d1.Key, 1m);
        table.SetSalary(c2.Key, d1.Key, 2m);

        table.RemoveCategory(c1.Key);

        Assert.Equal(["C2"], table.Categories.Select(c => c.Label));
        Assert.All(table.Cells, cell => Assert.Equal(c2.Key, cell.CategoryKey));
    }

    [Fact]
    public void Removing_a_degree_removes_its_cells()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        var d2 = table.AddDegree(2, 2);
        table.SetSalary(c1.Key, d1.Key, 1m);
        table.SetSalary(c1.Key, d2.Key, 2m);

        table.RemoveDegree(d1.Key);

        Assert.Equal([2], table.Degrees.Select(d => d.Number));
        Assert.Single(table.Cells);
    }

    [Fact]
    public void Renaming_a_category_keeps_its_cells()
    {
        var table = NewTable();
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        table.SetSalary(c1.Key, d1.Key, 7m);

        table.RenameCategory(c1.Key, "Ouvrier");

        Assert.Equal(7m, table.SalaryFor("Ouvrier", 1));
    }

    [Fact]
    public void Rebuild_grid_replaces_axes_and_cells_keeping_given_keys_and_order()
    {
        var table = NewTable();
        var old = table.AddCategory("Old");
        var d = table.AddDegree(1, 1);
        table.SetSalary(old.Key, d.Key, 1m);

        table.RebuildGrid(
            [new("k2", "B"), new("k1", "A")],
            [new("d2", 2, 2), new("d1", 1, 1)],
            [new("k1", "d2", 9m)]);

        Assert.Equal(["B", "A"], table.Categories.Select(c => c.Label));
        Assert.Equal(["d1", "d2"], table.Degrees.Select(x => x.Key));
        Assert.Equal(9m, table.SalaryFor("A", 2));
        Assert.Single(table.Cells);
    }

    [Fact]
    public void Rebuild_grid_rejects_cells_outside_the_axes()
    {
        var table = NewTable();
        Assert.Throws<DomainException>(() => table.RebuildGrid(
            [new("k1", "A")], [new("d1", 1, 1)], [new("k1", "zz", 1m)]));
    }

    [Fact]
    public void Applicability_follows_the_application_period()
    {
        var table = SalaryTable.Create(1, "4", SalaryType.Hourly, "x",
            new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)));
        Assert.True(table.IsApplicableOn(new DateOnly(2020, 5, 1)));
        Assert.False(table.IsApplicableOn(new DateOnly(2021, 1, 1)));
    }
}
