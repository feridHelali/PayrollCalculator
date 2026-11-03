using System.Globalization;
using System.Text;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Affairs;

/// <summary>A column of the ledger: the base salary or one prime.</summary>
public sealed record LedgerColumnDto(string Code, string Name);

/// <summary>One month of the ledger: the position, the amount of each column (null when it cannot be valued) and the total.</summary>
public sealed record LedgerRowDto(
    YearMonth Month,
    string Category,
    int Degree,
    decimal? BaseRate,
    decimal Proration,
    IReadOnlyDictionary<string, decimal?> Amounts,
    decimal Total,
    LedgerIssue Issue);

/// <summary>The month-by-month ledger of an affair.</summary>
public sealed record AffairLedgerDto(
    IReadOnlyList<LedgerColumnDto> Columns,
    IReadOnlyList<LedgerRowDto> Rows,
    IReadOnlyDictionary<string, decimal> Totals,
    decimal GrandTotal,
    int UnresolvedMonths)
{
    public const string BaseCode = "BASE";
}

/// <summary>Builds the ledger of an affair: base salary from its grids, plus the primes chosen for the employee.</summary>
public sealed class LedgerService(IAffairRepository affairs, ISalaryTableRepository tables, IAllowanceRepository allowances)
{
    /// <param name="allowanceIds">The primes of the affair's agreement that apply to this employee.</param>
    public async Task<AffairLedgerDto> BuildAsync(int affairId, IReadOnlyCollection<int> allowanceIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(allowanceIds);
        var affair = await NotFoundException.OrThrow(affairs.GetAsync(affairId, ct), "Affair", affairId).ConfigureAwait(false);
        if (affair.SalaryTableId is not { } tableId)
        {
            throw new DomainException(DomainError.Required, DomainField.SalaryTable);
        }

        var table = await NotFoundException.OrThrow(tables.GetAsync(tableId, ct), "Salary table", tableId).ConfigureAwait(false);
        var family = await tables.ListByAgreementAsync(table.AgreementId, ct).ConfigureAwait(false);
        var chosen = (await allowances.ListByAgreementAsync(affair.AgreementId, ct).ConfigureAwait(false))
            .Where(a => allowanceIds.Contains(a.Id))
            .OrderBy(a => a.Name, StringComparer.CurrentCulture)
            .ToList();

        var ledger = AffairLedgerBuilder.Build(affair, family, chosen)
            ?? throw new DomainException(DomainError.Required, DomainField.SalaryTable);
        return ToDto(ledger, chosen);
    }

    private static AffairLedgerDto ToDto(PayLedger ledger, List<Allowance> chosen)
    {
        var columns = new List<LedgerColumnDto> { new(AffairLedgerDto.BaseCode, string.Empty) };
        columns.AddRange(chosen.Select(a => new LedgerColumnDto(AffairLedgerBuilder.CodeOf(a).Value, a.Name)));

        var rows = ledger.Entries.GroupBy(e => e.Month).OrderBy(g => g.Key).Select(month =>
        {
            var byCode = month.ToDictionary(e => e.Component.Value);
            var baseEntry = byCode[AffairLedgerDto.BaseCode];
            var amounts = columns.ToDictionary(c => c.Code, c => byCode.TryGetValue(c.Code, out var e) ? e.Amount?.Dinars : null);
            var issue = month.Select(e => e.Issue).FirstOrDefault(i => i != LedgerIssue.None);
            return new LedgerRowDto(month.Key, baseEntry.Category.Value, baseEntry.Degree, baseEntry.Rate?.Dinars, baseEntry.Proration, amounts,
                amounts.Values.Sum(v => v ?? 0m), issue);
        }).ToList();

        var totals = columns.ToDictionary(c => c.Code, c => rows.Sum(r => r.Amounts[c.Code] ?? 0m));
        return new AffairLedgerDto(columns, rows, totals, totals.Values.Sum(), rows.Count(r => r.Issue != LedgerIssue.None));
    }
}

/// <summary>Writes a ledger as a semicolon-separated CSV (UTF-8 with BOM, so Excel shows the Arabic names).</summary>
public static class LedgerCsv
{
    private static readonly string[] Leading = ["Month", "Category", "Degree", "Proration"];

    public static string Write(AffairLedgerDto ledger, string baseHeader)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var columns = ledger.Columns.Select(c => c.Code == AffairLedgerDto.BaseCode ? baseHeader : c.Name).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', Leading.Concat(columns).Append("Total").Append("Issue").Select(Escape)));
        foreach (var r in ledger.Rows)
        {
            IEnumerable<string> fields =
            [
                r.Month.ToString(), r.Category, r.Degree.ToString(CultureInfo.InvariantCulture), r.Proration.ToString("0.####", CultureInfo.InvariantCulture),
                .. ledger.Columns.Select(c => r.Amounts[c.Code]?.ToString("0.000", CultureInfo.InvariantCulture) ?? string.Empty),
                r.Total.ToString("0.000", CultureInfo.InvariantCulture),
                r.Issue == LedgerIssue.None ? string.Empty : r.Issue.ToString(),
            ];
            sb.AppendLine(string.Join(';', fields.Select(Escape)));
        }

        sb.AppendLine(string.Join(';', new[] { "Total", string.Empty, string.Empty, string.Empty }
            .Concat(ledger.Columns.Select(c => ledger.Totals[c.Code].ToString("0.000", CultureInfo.InvariantCulture)))
            .Append(ledger.GrandTotal.ToString("0.000", CultureInfo.InvariantCulture)).Append(string.Empty)));
        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
