using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Tests;

/// <summary>Small builders for rule scenarios: one grid line "WORKERS" with categories C1 and C2.</summary>
internal static class RuleFixtures
{
    public static readonly GridLineCode Workers = new("WORKERS");
    public static readonly CategoryCode C1 = new("C1");
    public static readonly CategoryCode C2 = new("C2");
    public static readonly RegimeCode H48 = new("H48");
    public static readonly ComponentCode Presence = new("PRESENCE");

    public static Citation Cite(int page = 1) => new(1, page);

    public static DateOnly D(int year, int month = 1, int day = 1) => new(year, month, day);

    public static Money M(decimal dinars) => Money.FromDinars(dinars);

    public static GridLineDeclared Declare(DateOnly from, PayUnit unit = PayUnit.Monthly, params string[] categories) =>
        new(from, Cite(), Workers, "عمال", unit,
            (categories.Length == 0 ? ["C1", "C2"] : categories).Select(c => new CategoryDefinition(new CategoryCode(c), "صنف " + c)));

    /// <summary>Cells as (category, degree, amount) triples.</summary>
    public static SalaryGridEnacted Enact(DateOnly from, params (string Category, int Degree, decimal Amount)[] cells) =>
        new(from, Cite(), Workers, Cells(cells));

    public static IEnumerable<GridCell> Cells(params (string Category, int Degree, decimal Amount)[] cells) =>
        cells.Select(c => new GridCell(new CategoryCode(c.Category), c.Degree, M(c.Amount)));

    /// <summary>A complete 2 × 3 grid: C1 = base, base+10, base+20; C2 = base+100, ...</summary>
    public static SalaryGridEnacted EnactFull(DateOnly from, decimal baseAmount) =>
        Enact(from,
            ("C1", 1, baseAmount), ("C1", 2, baseAmount + 10), ("C1", 3, baseAmount + 20),
            ("C2", 1, baseAmount + 100), ("C2", 2, baseAmount + 110), ("C2", 3, baseAmount + 120));

    public static CareerRuleChanged CareerRule(DateOnly from, params (int Degree, int Months)[] months) =>
        new(from, Cite(), Workers, months.ToDictionary(m => m.Degree, m => m.Months));

    public static WorkingTimeChanged Regime(DateOnly from, decimal hours, RegimeCode? code = null) =>
        new(from, Cite(), code ?? H48, hours);
}
