using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Tests;

/// <summary>Task 3: the grids and primes edited in the screens, valued month by month by the event model.</summary>
public class AffairLedgerBuilderTests
{
    private static SalaryTable Table(int id, DateOnly start, decimal degree1, decimal degree2, SalaryType type = SalaryType.Monthly)
    {
        var table = SalaryTable.Create(1, "1", type, "كل الأصناف", new DateRange(start, null));
        table.AssignIdentity(id);
        var c1 = table.AddCategory("1");
        var d1 = table.AddDegree(1, 1);
        var d2 = table.AddDegree(2, 2);
        table.SetSalary(c1.Key, d1.Key, degree1);
        table.SetSalary(c1.Key, d2.Key, degree2);
        return table;
    }

    private static Allowance Prime(int id, string name, AllowanceUnit unit, params (int Year, decimal Value)[] rates)
    {
        var prime = Allowance.Create(1, name, unit, null, null);
        prime.AssignIdentity(id);
        prime.ReplaceRates(rates.Select(r => new AllowanceRateSpec(new DateOnly(r.Year, 5, 1), r.Value, "JORT 120/2022")));
        return prime;
    }

    private static Affair Affair(int tableId, DateOnly start, DateOnly end) =>
        Affairs.Affair.Open("1", "", "Salem", new DateRange(start, end), 1, tableId, "1", 1);

    private static decimal? Amount(PayLedger ledger, string component, int year, int month) =>
        ledger.Entries.Single(e => e.Component.Value == component && e.Month == new YearMonth(year, month)).Amount?.Dinars;

    [Fact]
    public void The_base_follows_the_grid_versions_and_each_prime_its_dated_rates()
    {
        SalaryTable[] tables = [Table(10, new DateOnly(2022, 5, 1), 504.153m, 516.756m), Table(11, new DateOnly(2023, 5, 1), 536.922m, 550.345m)];
        Allowance[] primes =
        [
            Prime(1, "منحة النقل", AllowanceUnit.PerMonth, (2022, 75.089m), (2023, 79.970m)),
            Prime(2, "منحة الحضور", AllowanceUnit.PerDayWorked, (2022, 0.452m), (2023, 0.481m)),
            Prime(3, "منحة الإنتاج", AllowanceUnit.PercentOfBase, (2022, 10m)),
        ];

        var ledger = AffairLedgerBuilder.Build(Affair(10, new DateOnly(2022, 5, 1), new DateOnly(2023, 6, 30)), tables, primes)!;

        Assert.Equal(504.153m, Amount(ledger, "BASE", 2022, 5));
        Assert.Equal(504.153m, Amount(ledger, "BASE", 2023, 4));           // old grid, degree 1 (one year in degree 1)
        Assert.Equal(550.345m, Amount(ledger, "BASE", 2023, 5));           // new grid and degree 2, both from 1 May 2023
        Assert.Equal(75.089m, Amount(ledger, "PRIME_1", 2022, 5));
        Assert.Equal(79.970m, Amount(ledger, "PRIME_1", 2023, 5));
        Assert.Equal(11.752m, Amount(ledger, "PRIME_2", 2022, 5));         // 0.452 × 26 working days
        Assert.Equal(12.506m, Amount(ledger, "PRIME_2", 2023, 5));         // 0.481 × 26
        Assert.Equal(50.415m, Amount(ledger, "PRIME_3", 2022, 5));         // 10 % of 504.153
        Assert.True(ledger.IsComplete);
        Assert.Equal(14, ledger.Entries.Select(e => e.Month).Distinct().Count());
    }

    [Fact]
    public void An_hourly_grid_is_turned_into_a_monthly_base_with_208_hours()
    {
        var ledger = AffairLedgerBuilder.Build(Affair(20, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31)),
            [Table(20, new DateOnly(2024, 1, 1), 2.850m, 2.896m, SalaryType.Hourly)], [])!;

        var entry = Assert.Single(ledger.Entries);
        Assert.Equal((PayUnit.Hourly, 208m, 592.800m), (entry.Unit, entry.Quantity, entry.Amount!.Value.Dinars)); // 2.850 × 208
    }

    [Fact]
    public void A_partial_month_is_prorated_for_the_base_and_the_monthly_primes()
    {
        var ledger = AffairLedgerBuilder.Build(Affair(10, new DateOnly(2022, 5, 17), new DateOnly(2022, 5, 31)),
            [Table(10, new DateOnly(2022, 5, 1), 504.153m, 516.756m)], [Prime(1, "منحة النقل", AllowanceUnit.PerMonth, (2022, 75.089m))])!;

        Assert.Equal(243.945m, Amount(ledger, "BASE", 2022, 5));           // 504.153 × 15/31
        Assert.Equal(36.333m, Amount(ledger, "PRIME_1", 2022, 5));         // 75.089 × 15/31
    }

    [Fact]
    public void An_affair_without_a_salary_table_has_no_ledger() =>
        Assert.Null(AffairLedgerBuilder.Build(Affair(99, new DateOnly(2022, 5, 1), new DateOnly(2022, 6, 30)),
            [Table(10, new DateOnly(2022, 5, 1), 1m, 2m)], []));
}
