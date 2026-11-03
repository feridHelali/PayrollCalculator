using System.Globalization;
using System.Runtime.CompilerServices;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Tests;

/// <summary>
/// 05.1, golden case 01 (SIMULATED): Ste Yosra, IT lead, September 1998 to August 2008.
/// The rules are synthetic (see golden_case_01.md); the expected ledger comes from an independent Python oracle,
/// <c>tests/fixtures/golden_01/oracle.py</c>, so two separate implementations must agree to the millime.
/// </summary>
public class GoldenCase01Tests
{
    private static readonly GridLineCode Line = new("SIM-ELEC-RETAIL");
    private static readonly CategoryCode Cadre = new("C");
    private static readonly Citation Synthetic = new(1, 1, "simulated");

    private static readonly (string Code, string Name, decimal Amount)[] Allowances =
    [
        ("TRANSPORT", "منحة النقل", 8.000m),
        ("PRESENCE", "منحة الحضور", 2.080m),
        ("ASSIDUITY", "منحة المواظبة", 12.000m),
    ];

    private static DateOnly D(int year, int month, int day = 1) => new(year, month, day);

    private static Rulebook Rules()
    {
        decimal[] cadre = [480, 495, 510, 525, 540, 555];
        List<RuleEvent> events =
        [
            new GridLineDeclared(D(1998, 1), Synthetic, Line, "تجارة التجهيزات الكهربائية المنزلية (محاكاة)", PayUnit.Monthly,
                [new CategoryDefinition(Cadre, "إطارات")]),
            new SalaryGridEnacted(D(1998, 1), Synthetic, Line, cadre.Select((amount, i) => new GridCell(Cadre, i + 1, Money.FromDinars(amount)))),
            new CareerRuleChanged(D(1998, 1), Synthetic, Line, Enumerable.Range(1, 5).ToDictionary(d => d, _ => 24)),
            .. new (DateOnly Day, decimal Percent)[] { (D(1999, 5), 4.5m), (D(2002, 5), 5m), (D(2005, 5), 5m), (D(2008, 5), 4.5m) }
                .Select(r => new SalaryGridRevalued(r.Day, Synthetic, Line, new PercentForAll(new Percentage(r.Percent)), RoundingRule.HalfUp)),
            .. Allowances.Select(a => new AllowanceIntroduced(D(1998, 1), Synthetic, new ComponentCode(a.Code), a.Name, new FixedMonthly(Money.FromDinars(a.Amount)))),
        ];
        return Rulebook.From(events);
    }

    private static Career Career() => Careers.Career.From(
        [new Hired(D(1998, 9), Line, Cadre, 1, new RegimeCode("H48")), new Separated(D(2008, 8, 31))]);

    private static List<Dictionary<string, string>> Expected([CallerFilePath] string here = "")
    {
        var file = Path.Combine(Path.GetDirectoryName(here)!, "..", "fixtures", "golden_01", "expected.csv");
        var lines = File.ReadAllLines(file);
        var header = lines[0].Split(',');
        return lines.Skip(1).Select(l => header.Zip(l.Split(',')).ToDictionary(p => p.First, p => p.Second)).ToList();
    }

    private static Money M(string dinars) => Money.FromDinars(decimal.Parse(dinars, CultureInfo.InvariantCulture));

    [Fact]
    public void The_ledger_matches_the_independent_oracle_for_every_month_and_component()
    {
        var ledger = LedgerSettings.Default.Build(Career(), Rules());
        var expected = Expected();

        Assert.Equal(120, expected.Count);
        Assert.True(ledger.IsComplete);
        Assert.Empty(ledger.RuleWarnings);
        foreach (var row in expected)
        {
            var month = row["month"];
            var entries = ledger.Entries.Where(e => e.Month.ToString() == month).ToDictionary(e => e.Component.Value);
            Assert.Equal(int.Parse(row["degree"], CultureInfo.InvariantCulture), entries["BASE"].Degree);
            foreach (var component in Allowances.Select(a => a.Code).Prepend("BASE"))
            {
                Assert.True(M(row[component]) == entries[component].Amount, $"{month} {component}: expected {row[component]}, got {entries[component].Amount}");
            }

            Assert.Equal(M(row["owed"]), entries.Values.Aggregate(Money.Zero, (sum, e) => sum + e.Amount!.Value));
        }

        Assert.Equal(M("69793.168"), ledger.Total);
    }

    [Fact]
    public void The_concise_view_shows_one_period_per_degree_or_raise()
    {
        var runs = LedgerSettings.Default.Build(Career(), Rules()).Collapse().Where(r => r.Component.IsBase).ToList();

        // Changes: raises in 1999-05, 2002-05, 2005-05, 2008-05; degrees in 2000-09, 2002-09, 2004-09, 2006-09.
        Assert.Equal(9, runs.Count);
        Assert.Equal(120, runs.Sum(r => r.Months));
        Assert.Equal((new YearMonth(2008, 5), new YearMonth(2008, 8), 5), (runs[^1].From, runs[^1].To, runs[^1].Degree));
    }
}
