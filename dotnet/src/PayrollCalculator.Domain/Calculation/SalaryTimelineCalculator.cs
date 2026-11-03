using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Calculation;

/// <summary>
/// Domain service: for each month of an affair's employment period, picks the version of the
/// affair's grid (same agreement and table number) applicable that month, derives the degree from
/// seniority, and reads the salary cell.
/// </summary>
public sealed class SalaryTimelineCalculator(IDegreeProgressionPolicy progression)
{
    public SalaryTimeline Calculate(Affair affair, IEnumerable<SalaryTable> tables)
    {
        ArgumentNullException.ThrowIfNull(affair);
        var all = tables.ToList();
        var reference = all.FirstOrDefault(t => t.Id == affair.SalaryTableId);
        List<SalaryTable> family = reference is null
            ? []
            : all.Where(t => t.AgreementId == reference.AgreementId && t.TableNumber == reference.TableNumber).ToList();

        var start = affair.EmploymentPeriod.Start;
        var firstMonth = YearMonth.Of(start);
        var lines = affair.EmploymentPeriod.Months()
            .Select(month => LineFor(affair, family, reference, month, month.MonthsSince(firstMonth), start))
            .ToList();
        return new SalaryTimeline(lines);
    }

    private MonthlySalaryLine LineFor(Affair affair, List<SalaryTable> family, SalaryTable? reference,
        YearMonth month, int seniority, DateOnly employmentStart)
    {
        var position = affair.InitialPosition;
        var day = month.FirstDay < employmentStart ? employmentStart : month.FirstDay;
        var table = family.Where(t => t.IsApplicableOn(day))
                          .OrderByDescending(t => t.ApplicationPeriod.Start)
                          .FirstOrDefault();

        if (table is null)
        {
            var fallbackDegree = reference is null ? null : progression.DegreeAt(reference.Degrees, position.Degree, seniority);
            return new MonthlySalaryLine(month, seniority, fallbackDegree?.Number ?? position.Degree,
                null, null, null, null, TimelineIssue.NoApplicableTable);
        }

        MonthlySalaryLine Line(int degree, decimal? amount, TimelineIssue issue) =>
            new(month, seniority, degree, table.Id, table.TableNumber, table.SalaryType, amount, issue);

        var degreeStep = progression.DegreeAt(table.Degrees, position.Degree, seniority);
        if (degreeStep is null)
        {
            return Line(position.Degree, null, TimelineIssue.DegreeNotFound);
        }

        var category = table.FindCategory(position.Category);
        if (category is null)
        {
            return Line(degreeStep.Number, null, TimelineIssue.CategoryNotFound);
        }

        var amount = table.SalaryFor(category.Key, degreeStep.Key);
        return amount is null
            ? Line(degreeStep.Number, null, TimelineIssue.SalaryMissing)
            : Line(degreeStep.Number, amount, TimelineIssue.None);
    }
}
