using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Migration;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.12: the first model's tables and affairs become events.</summary>
public class LegacyTranslatorTests
{
    private static readonly Citation Source = new(1, 1);

    private static SalaryTable Table(string number, DateOnly start, DateOnly? end, decimal amount, int yearsInDegree1 = 1,
        SalaryType type = SalaryType.Monthly)
    {
        var table = SalaryTable.Create(1, number, type, "عمال", new DateRange(start, end));
        var c1 = table.AddCategory(" C1 ");
        var d1 = table.AddDegree(1, yearsInDegree1);
        var d2 = table.AddDegree(2, 0);
        table.SetSalary(c1.Key, d1.Key, amount);
        table.SetSalary(c1.Key, d2.Key, amount + 10);
        return table;
    }

    [Fact]
    public void Each_version_declares_enacts_and_sets_the_career_rule_of_its_line()
    {
        var warnings = new List<TranslationWarning>();
        var events = LegacyTranslator.RuleEvents(
            [Table("4", new DateOnly(2010, 1, 1), null, 300m), Table("4", new DateOnly(2000, 1, 1), new DateOnly(2009, 12, 31), 200m)], Source, warnings);

        Assert.Equal(
            [RuleEventKind.GridLineDeclared, RuleEventKind.SalaryGridEnacted, RuleEventKind.CareerRuleChanged,
             RuleEventKind.GridLineDeclared, RuleEventKind.SalaryGridEnacted, RuleEventKind.CareerRuleChanged],
            events.Select(e => e.Kind));
        var rules = Rulebook.From(events);
        var line = LegacyTranslator.LineOf("4");
        Assert.Equal(Money.FromDinars(200m), rules.At(new DateOnly(2005, 1, 1)).Line(line)!.Grid!.AmountFor(new CategoryCode("C1"), 1));
        Assert.Equal(Money.FromDinars(310m), rules.At(new DateOnly(2010, 1, 1)).Line(line)!.Grid!.AmountFor(new CategoryCode("C1"), 2));
        Assert.Equal(12, rules.At(new DateOnly(2010, 1, 1)).Line(line)!.CareerRule!.MonthsIn(1));
        Assert.Empty(warnings);
    }

    [Fact]
    public void End_dates_are_dropped_and_gaps_filled_with_a_warning()
    {
        var warnings = new List<TranslationWarning>();
        LegacyTranslator.RuleEvents(
            [Table("4", new DateOnly(2000, 1, 1), new DateOnly(2004, 12, 31), 200m), Table("4", new DateOnly(2006, 1, 1), new DateOnly(2010, 12, 31), 300m)],
            Source, warnings);

        Assert.Equal([TranslationNote.GapFilled, TranslationNote.EndDateDropped], warnings.Select(w => w.Note));
    }

    [Fact]
    public void A_zero_year_degree_below_the_last_is_reported_as_a_ceiling()
    {
        var warnings = new List<TranslationWarning>();
        var events = LegacyTranslator.RuleEvents([Table("2", new DateOnly(2000, 1, 1), null, 1.5m, yearsInDegree1: 0, type: SalaryType.Hourly)], Source, warnings);

        Assert.Equal(TranslationNote.ZeroYearsIsCeiling, Assert.Single(warnings).Note);
        Assert.Empty(((CareerRuleChanged)events[2]).MonthsInDegree);
        Assert.Equal(PayUnit.Hourly, ((GridLineDeclared)events[0]).Unit);
    }

    [Fact]
    public void An_affair_becomes_a_hiring_and_a_separation_on_the_line_of_its_table()
    {
        var table = Table("5", new DateOnly(2000, 1, 1), null, 300m);
        var affair = Affair.Open("1", "", "Salem", new DateRange(new DateOnly(2001, 3, 15), new DateOnly(2004, 6, 30)), 1, 7, "C1", 2);
        var warnings = new List<TranslationWarning>();

        var career = LegacyTranslator.CareerEvents(affair, table, warnings)!;

        var hired = Assert.IsType<Hired>(career[0]);
        Assert.Equal((new DateOnly(2001, 3, 15), LegacyTranslator.LineOf("5"), new CategoryCode("C1"), 2), (hired.Date, hired.Line, hired.Category, hired.Degree));
        Assert.Equal(new DateOnly(2004, 6, 30), Assert.IsType<Separated>(career[1]).Date);
        Assert.Null(LegacyTranslator.CareerEvents(affair, null, warnings));
        Assert.Equal(TranslationNote.AffairWithoutTable, Assert.Single(warnings).Note);
    }
}
