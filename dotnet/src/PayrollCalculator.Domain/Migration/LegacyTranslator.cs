using System.Globalization;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Migration;

/// <summary>Why a part of the old data was translated differently, or not at all.</summary>
public enum TranslationNote
{
    /// <summary>A version had an end date; grids now stay in force until the next one (design review C1).</summary>
    EndDateDropped,

    /// <summary>There was a gap between two versions; the earlier grid now covers it (C1).</summary>
    GapFilled,

    /// <summary>A version had no amount at all, so it does not replace the grid in force.</summary>
    EmptyVersionSkipped,

    /// <summary>A degree with 0 years is now a ceiling; the old calculator skipped through it at once.</summary>
    ZeroYearsIsCeiling,

    /// <summary>The affair has no salary table, so no career could be built.</summary>
    AffairWithoutTable,
}

public sealed record TranslationWarning(TranslationNote Note, string Subject);

/// <summary>
/// Translates the grids and affairs of the first model (05.12) into events: each version of a salary table becomes a
/// <see cref="GridLineDeclared"/>, a <see cref="SalaryGridEnacted"/> and a <see cref="CareerRuleChanged"/> on the line
/// of its table number; an affair's start, initial position and end become <see cref="Hired"/> and <see cref="Separated"/>.
/// Category labels become codes (the trimmed label, which is how the old calculator matched them).
/// </summary>
public static class LegacyTranslator
{
    /// <summary>The regime given to migrated careers. The old model did not know the hours of a regime.</summary>
    public static RegimeCode Regime { get; } = new("H48");

    public static GridLineCode LineOf(string tableNumber) => new("T" + Guard.Required(tableNumber, DomainField.TableNumber));

    public static CategoryCode CodeOf(string label) => new(Guard.Required(label, DomainField.CategoryLabel));

    /// <param name="tables">The tables of one agreement.</param>
    /// <param name="source">The citation given to every event (the old data has no source).</param>
    public static IReadOnlyList<RuleEvent> RuleEvents(IEnumerable<SalaryTable> tables, Citation source, ICollection<TranslationWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(warnings);
        var events = new List<RuleEvent>();
        foreach (var family in tables.GroupBy(t => t.TableNumber.Trim()).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var line = LineOf(family.Key);
            var versions = family.OrderBy(t => t.ApplicationPeriod.Start).ToList();
            for (var i = 0; i < versions.Count; i++)
            {
                var table = versions[i];
                var start = table.ApplicationPeriod.Start;
                var subject = string.Create(CultureInfo.InvariantCulture, $"{line}@{start:yyyy-MM-dd}");
                NoteEndDate(table, i + 1 < versions.Count ? versions[i + 1] : null, subject, warnings);

                events.Add(new GridLineDeclared(start, source, line, Name(table), table.SalaryType == SalaryType.Hourly ? PayUnit.Hourly : PayUnit.Monthly,
                    table.Categories.Select(c => new CategoryDefinition(CodeOf(c.Label), c.Label))));

                var cells = Cells(table);
                if (cells.Count == 0)
                {
                    warnings.Add(new TranslationWarning(TranslationNote.EmptyVersionSkipped, subject));
                }
                else
                {
                    events.Add(new SalaryGridEnacted(start, source, line, cells));
                }

                events.Add(new CareerRuleChanged(start, source, line, MonthsInDegree(table, subject, warnings)));
            }
        }

        return events;
    }

    /// <returns>The career of the affair, or null when it has no salary table.</returns>
    public static IReadOnlyList<CareerEvent>? CareerEvents(Affair affair, SalaryTable? table, ICollection<TranslationWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(affair);
        ArgumentNullException.ThrowIfNull(warnings);
        if (table is null)
        {
            warnings.Add(new TranslationWarning(TranslationNote.AffairWithoutTable, affair.AffairNumber));
            return null;
        }

        return
        [
            new Hired(affair.EmploymentPeriod.Start, LineOf(table.TableNumber), CodeOf(affair.InitialPosition.Category), affair.InitialPosition.Degree, Regime),
            new Separated(affair.EmploymentPeriod.End!.Value),
        ];
    }

    private static string Name(SalaryTable table) =>
        string.IsNullOrWhiteSpace(table.ConcernedEmployees) ? "جدول " + table.TableNumber : table.ConcernedEmployees;

    private static List<GridCell> Cells(SalaryTable table)
    {
        var labels = table.Categories.ToDictionary(c => c.Key, c => c.Label);
        var degrees = table.Degrees.ToDictionary(d => d.Key, d => d.Number);
        return table.Cells
            .Select(c => new GridCell(CodeOf(labels[c.CategoryKey]), degrees[c.DegreeKey], Money.Round(c.Amount, RoundingRule.HalfUp)))
            .ToList();
    }

    private static Dictionary<int, int> MonthsInDegree(SalaryTable table, string subject, ICollection<TranslationWarning> warnings)
    {
        var degrees = table.Degrees;
        var zero = degrees.Take(degrees.Count - 1).Where(d => d.YearsInDegree == 0).Select(d => d.Number).ToList();
        if (zero.Count > 0)
        {
            warnings.Add(new TranslationWarning(TranslationNote.ZeroYearsIsCeiling,
                subject + " degrees " + string.Join(",", zero.Select(n => n.ToString(CultureInfo.InvariantCulture)))));
        }

        return degrees.Where(d => d.YearsInDegree > 0).ToDictionary(d => d.Number, d => d.YearsInDegree * 12);
    }

    private static void NoteEndDate(SalaryTable table, SalaryTable? next, string subject, ICollection<TranslationWarning> warnings)
    {
        if (table.ApplicationPeriod.End is not { } end)
        {
            return;
        }

        if (next is null)
        {
            warnings.Add(new TranslationWarning(TranslationNote.EndDateDropped, subject));
        }
        else if (next.ApplicationPeriod.Start > end.AddDays(1))
        {
            warnings.Add(new TranslationWarning(TranslationNote.GapFilled, subject));
        }
    }
}
