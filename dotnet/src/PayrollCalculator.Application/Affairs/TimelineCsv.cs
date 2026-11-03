using System.Globalization;
using System.Text;
using PayrollCalculator.Domain.Calculation;

namespace PayrollCalculator.Application.Affairs;

/// <summary>Serialises a salary timeline as a semicolon-separated CSV (opens directly in Excel with fr/ar locales).</summary>
public static class TimelineCsv
{
    private const char Separator = ';';

    public static string Write(SalaryTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var sb = new StringBuilder();
        sb.AppendLine("Period;MonthsOfSeniority;Degree;TableNumber;SalaryType;Amount;Issue");
        foreach (var l in timeline.Lines)
        {
            string?[] fields =
            [
                l.Period.ToString(),
                l.MonthsOfSeniority.ToString(CultureInfo.InvariantCulture),
                l.Degree.ToString(CultureInfo.InvariantCulture),
                l.TableNumber,
                l.SalaryType?.ToString(),
                l.Amount?.ToString(CultureInfo.InvariantCulture),
                l.Issue.ToString(),
            ];
            sb.AppendLine(string.Join(Separator, fields.Select(Escape)));
        }

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.IndexOfAny([Separator, '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}
