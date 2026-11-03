using System.Globalization;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.SalaryTables;

/// <summary>One category row of an imported grid: its label and an amount per degree (null when the cell is empty).</summary>
public sealed record GridCsvRow(string Category, IReadOnlyList<decimal?> Amounts);

/// <summary>A salary grid read from a CSV file: the degrees (columns), the years to spend in each, and the category rows.</summary>
public sealed record GridCsvContent(IReadOnlyList<int> Degrees, IReadOnlyList<int> YearsInDegree, IReadOnlyList<GridCsvRow> Rows);

/// <summary>
/// Reads a salary grid typed or transcribed in a spreadsheet (for example the drafts in <c>data/seed/drafts</c>):
/// <code>
/// category,1,2,3,...          (the degrees)
/// years_in_degree,1,1,1,2,... (years to spend in each degree)
/// I,2.850,2.896,...           (one row per category)
/// </code>
/// The separator is <c>,</c> or <c>;</c>; with <c>;</c> (Excel in French or Arabic), a decimal comma is accepted.
/// </summary>
public static class GridCsv
{
    public static GridCsvContent Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')
            .Select((line, index) => (Line: line.Trim().TrimStart('﻿'), Number: index + 1))
            .Where(l => l.Line.Length > 0)
            .ToList();
        if (lines.Count < 3)
        {
            throw Invalid(lines.Count + 1);
        }

        var separator = lines[0].Line.Contains(';', StringComparison.Ordinal) ? ';' : ',';
        string[] Cells(int i) => lines[i].Line.Split(separator).Select(c => c.Trim()).ToArray();

        var header = Cells(0);
        var degrees = header.Skip(1).Select(c => Integer(c, lines[0].Number)).ToList();
        if (degrees.Count == 0 || degrees.Distinct().Count() != degrees.Count)
        {
            throw Invalid(lines[0].Number);
        }

        var years = Cells(1);
        if (years.Length != header.Length)
        {
            throw Invalid(lines[1].Number);
        }

        var yearsInDegree = years.Skip(1).Select(c => Integer(c, lines[1].Number)).ToList();
        var rows = new List<GridCsvRow>();
        for (var i = 2; i < lines.Count; i++)
        {
            var cells = Cells(i);
            if (cells.Length != header.Length || cells[0].Length == 0)
            {
                throw Invalid(lines[i].Number);
            }

            rows.Add(new GridCsvRow(cells[0], cells.Skip(1).Select(c => Amount(c, separator, lines[i].Number)).ToList()));
        }

        return new GridCsvContent(degrees, yearsInDegree, rows);
    }

    private static int Integer(string cell, int line) =>
        int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : throw Invalid(line);

    private static decimal? Amount(string cell, char separator, int line)
    {
        if (cell.Length == 0)
        {
            return null;
        }

        var normalized = (separator == ';' ? cell.Replace(',', '.') : cell).Replace(" ", string.Empty, StringComparison.Ordinal);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : throw Invalid(line);
    }

    private static DomainException Invalid(int line) =>
        new(DomainError.InvalidImportFile, DomainField.ImportFile, line.ToString(CultureInfo.InvariantCulture));
}
