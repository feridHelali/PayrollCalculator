using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.LegacyImport;

/// <summary>
/// Reads the Electron/TypeORM <c>payroll.sqlite</c> (read-only) and recreates its data through the domain model.
/// Known quirks of the legacy data are normalised; anything that cannot be imported safely is skipped and reported.
/// </summary>
public sealed partial class LegacyElectronImporter(PayrollDbContext db) : ILegacyDataImporter
{
    public async Task<ImportReport> ImportAsync(string legacyDatabasePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(legacyDatabasePath))
        {
            throw new FileNotFoundException("Legacy database not found.", legacyDatabasePath);
        }

        // Seeded agreements are not user data: only agreements, tables or affairs entered by a user block the import.
        if (await db.Agreements.AnyAsync(a => a.Origin == DataOrigin.User, cancellationToken).ConfigureAwait(false)
            || await db.SalaryTables.AnyAsync(cancellationToken).ConfigureAwait(false)
            || await db.Affairs.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The target database already contains data; import only into an empty database.");
        }

        var connectionString = new SqliteConnectionStringBuilder { DataSource = legacyDatabasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        await using var legacy = new SqliteConnection(connectionString);
        await legacy.OpenAsync(cancellationToken).ConfigureAwait(false);

        var run = new ImportRun(legacy);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var agreements = await run.ImportAgreementsAsync(db, cancellationToken).ConfigureAwait(false);
        var tables = await run.ImportSalaryTablesAsync(db, agreements, cancellationToken).ConfigureAwait(false);
        var affairs = await run.ImportAffairsAsync(db, agreements, tables, cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new ImportReport(agreements.Count, tables.Count, run.CellCount, affairs, run.Warnings);
    }

    [GeneratedRegex(@"^\s*[Cc]\s*(\d+)\s*$")]
    private static partial Regex CategoryCode();

    private sealed class ImportRun(SqliteConnection legacy)
    {
        public List<string> Warnings { get; } = [];

        public int CellCount { get; private set; }

        public async Task<Dictionary<long, SectorialJointAgreement>> ImportAgreementsAsync(PayrollDbContext db, CancellationToken ct)
        {
            var map = new Dictionary<long, SectorialJointAgreement>();
            await foreach (var row in QueryAsync("SELECT sectorial_joint_AgreementID, agreement_name, description FROM SectorialJointAgreement", ct))
            {
                var id = row.GetInt64(0);
                var agreement = Try($"Agreement #{id}", () => SectorialJointAgreement.Create(row.GetString(1), Text(row, 2)));
                if (agreement is not null)
                {
                    db.Agreements.Add(agreement);
                    map[id] = agreement;
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return map;
        }

        public async Task<Dictionary<long, SalaryTable>> ImportSalaryTablesAsync(
            PayrollDbContext db, Dictionary<long, SectorialJointAgreement> agreements, CancellationToken ct)
        {
            var cells = await ReadCellsAsync(ct).ConfigureAwait(false);
            var map = new Dictionary<long, SalaryTable>();
            const string sql = """
                SELECT salaryTableID, numero_table, type, consernedEmployee, beginning_date_of_application,
                       end_date_of_application, degrees, categories, agreementID
                FROM SalaryTable
                """;
            await foreach (var row in QueryAsync(sql, ct))
            {
                var id = row.GetInt64(0);
                if (!agreements.TryGetValue(row.GetInt64(8), out var agreement))
                {
                    Warnings.Add($"Salary table #{id}: agreement #{row.GetInt64(8)} not found, skipped.");
                    continue;
                }

                var table = Try($"Salary table #{id}", () => BuildTable(id, row, agreement.Id, cells.GetValueOrDefault(id, [])));
                if (table is not null)
                {
                    db.SalaryTables.Add(table);
                    map[id] = table;
                    CellCount += table.Cells.Count;
                }
            }

            foreach (var orphan in cells.Where(c => c.Key == 0).SelectMany(c => c.Value))
            {
                Warnings.Add($"Cell #{orphan.Id}: no salary table, skipped.");
            }

            foreach (var orphan in cells.Where(c => c.Key != 0 && !map.ContainsKey(c.Key)).SelectMany(c => c.Value))
            {
                Warnings.Add($"Cell #{orphan.Id}: salary table #{orphan.TableId} was not imported, skipped.");
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return map;
        }

        public async Task<int> ImportAffairsAsync(PayrollDbContext db, Dictionary<long, SectorialJointAgreement> agreements,
            Dictionary<long, SalaryTable> tables, CancellationToken ct)
        {
            // The legacy column holding the salary table id was renamed during development.
            var tableColumn = await ColumnExistsAsync("Affair", "salaryTableId", ct).ConfigureAwait(false) ? "salaryTableId" : "numero_table";
            var sql = $"""
                SELECT affairID, affairNumber, title, claimant, startDateOfWork, endDateOfWork,
                       professionalCategoryAtBegining, professionalDegreeAtBegining, agreementID, {tableColumn}
                FROM Affair
                """;
            var count = 0;
            await foreach (var row in QueryAsync(sql, ct))
            {
                var id = row.GetInt64(0);
                if (row.IsDBNull(8) || !agreements.TryGetValue(row.GetInt64(8), out var agreement))
                {
                    Warnings.Add($"Affair #{id}: agreement not found, skipped.");
                    continue;
                }

                SalaryTable? table = null;
                if (!row.IsDBNull(9) && !tables.TryGetValue(row.GetInt64(9), out table))
                {
                    Warnings.Add($"Affair #{id}: salary table #{row.GetInt64(9)} not found, imported without table.");
                }

                if (table is not null && table.AgreementId != agreement.Id)
                {
                    Warnings.Add($"Affair #{id}: salary table belongs to another agreement, imported without table.");
                    table = null;
                }

                var degree = (int)row.GetInt64(7);
                if (degree < 1)
                {
                    Warnings.Add($"Affair #{id}: degree {degree} is invalid, set to 1.");
                    degree = 1;
                }

                var category = ResolveCategory(id, row.GetString(6), table);
                var affair = Try($"Affair #{id}", () => Affair.Open(
                    row.GetString(1), row.GetString(2), row.GetString(3),
                    new DateRange(ParseDate(row.GetString(4))!.Value, ParseDate(row.GetString(5))),
                    agreement.Id, table?.Id, category, degree));
                if (affair is not null)
                {
                    db.Affairs.Add(affair);
                    count++;
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return count;
        }

        private SalaryTable BuildTable(long id, SqliteDataReader row, int agreementId, List<LegacyCell> cells)
        {
            var start = ParseDate(row.GetString(4)) ?? throw new DomainException("Missing start date.");
            var table = SalaryTable.Create(agreementId, row.GetString(1), ParseType(row.GetString(2)), row.GetString(3),
                new DateRange(start, ParseDate(Text(row, 5))));

            var labels = new HashSet<string>(StringComparer.Ordinal);
            foreach (var category in JsonSerializer.Deserialize<List<JsonElement>>(row.GetString(7)) ?? [])
            {
                var key = category.GetProperty("key").GetString()!;
                var label = category.GetProperty("label").GetString() ?? key;
                var unique = label;
                for (var n = 2; !labels.Add(unique); n++)
                {
                    unique = $"{label} ({n})";
                }

                if (unique != label)
                {
                    Warnings.Add($"Salary table #{id}: duplicate category '{label}' renamed to '{unique}'.");
                }

                table.AddCategory(unique, key);
            }

            foreach (var degree in JsonSerializer.Deserialize<List<JsonElement>>(row.GetString(6)) ?? [])
            {
                var key = degree.GetProperty("key").GetString()!;
                var number = ReadInt(degree.GetProperty("degree"));
                var years = degree.TryGetProperty("ageOfWork", out var age) ? ReadInt(age) : null;
                if (number is null or < 1 || table.Degrees.Any(d => d.Number == number))
                {
                    Warnings.Add($"Salary table #{id}: invalid or duplicate degree '{degree}', skipped.");
                    continue;
                }

                if (years is null or < 0)
                {
                    Warnings.Add($"Salary table #{id}: degree {number} has no valid years in degree, set to 0.");
                    years = 0;
                }

                table.AddDegree(number.Value, years.Value, key);
            }

            foreach (var cell in cells)
            {
                var categoryKey = table.Categories.FirstOrDefault(c => c.Key == cell.Category || c.Label == cell.Category)?.Key;
                var degreeKey = ResolveDegreeKey(table, cell.Degree);
                if (categoryKey is null || degreeKey is null)
                {
                    Warnings.Add($"Cell #{cell.Id} of salary table #{id}: unknown category/degree ({cell.Category} / {cell.Degree}), skipped.");
                    continue;
                }

                if (table.SalaryFor(categoryKey, degreeKey) is not null)
                {
                    Warnings.Add($"Cell #{cell.Id} of salary table #{id}: duplicate cell, last value kept.");
                }

                table.SetSalary(categoryKey, degreeKey, cell.Salary);
            }

            return table;
        }

        /// <summary>Legacy cells store the degree as a JSON string key, or (older rows) as an object with a degree number.</summary>
        private static string? ResolveDegreeKey(SalaryTable table, string json)
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(json);
                return element.ValueKind switch
                {
                    JsonValueKind.String => table.Degrees.FirstOrDefault(d => d.Key == element.GetString())?.Key,
                    JsonValueKind.Object when element.TryGetProperty("degree", out var n) && ReadInt(n) is { } number =>
                        table.Degrees.FirstOrDefault(d => d.Number == number)?.Key,
                    _ => null,
                };
            }
            catch (JsonException)
            {
                return table.Degrees.FirstOrDefault(d => d.Key == json)?.Key;
            }
        }

        private string ResolveCategory(long affairId, string raw, SalaryTable? table)
        {
            if (table is null || table.FindCategory(raw) is not null)
            {
                return raw;
            }

            var code = CategoryCode().Match(raw);
            if (code.Success && int.TryParse(code.Groups[1].Value, CultureInfo.InvariantCulture, out var n)
                && n >= 1 && n <= table.Categories.Count)
            {
                var label = table.Categories[n - 1].Label;
                Warnings.Add($"Affair #{affairId}: category '{raw}' mapped to '{label}'.");
                return label;
            }

            Warnings.Add($"Affair #{affairId}: category '{raw}' not found in its salary table; kept as is.");
            return raw;
        }

        private async Task<Dictionary<long, List<LegacyCell>>> ReadCellsAsync(CancellationToken ct)
        {
            var cells = new Dictionary<long, List<LegacyCell>>();
            await foreach (var row in QueryAsync("SELECT id, professionalCategory, ProfessionalDegree, salary, salaryTableId FROM SalaryTableCell ORDER BY id", ct))
            {
                var tableId = row.IsDBNull(4) ? 0 : row.GetInt64(4);
                var cell = new LegacyCell(row.GetInt64(0), tableId, row.GetString(1), row.GetString(2), Convert.ToDecimal(row.GetDouble(3)));
                if (!cells.TryGetValue(tableId, out var list))
                {
                    cells[tableId] = list = [];
                }

                list.Add(cell);
            }

            return cells;
        }

        private async Task<bool> ColumnExistsAsync(string table, string column, CancellationToken ct)
        {
            await foreach (var row in QueryAsync($"PRAGMA table_info(\"{table}\")", ct))
            {
                if (string.Equals(row.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async IAsyncEnumerable<SqliteDataReader> QueryAsync(string sql, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await using var command = legacy.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                yield return reader;
            }
        }

        private T? Try<T>(string what, Func<T> build) where T : class
        {
            try
            {
                return build();
            }
            catch (Exception ex) when (ex is DomainException or FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                Warnings.Add($"{what}: {ex.Message} Skipped.");
                return null;
            }
        }

        private static string? Text(SqliteDataReader row, int ordinal) => row.IsDBNull(ordinal) ? null : row.GetString(ordinal);

        private static DateOnly? ParseDate(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : DateOnly.FromDateTime(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));

        private static int? ReadInt(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(element.GetString(), CultureInfo.InvariantCulture, out var n) => n,
            _ => null,
        };

        private static SalaryType ParseType(string legacyType) =>
            legacyType.Contains("ساعة", StringComparison.Ordinal) || legacyType.Contains("horaire", StringComparison.OrdinalIgnoreCase)
                ? SalaryType.Hourly
                : SalaryType.Monthly;
    }

    private sealed record LegacyCell(long Id, long TableId, string Category, string Degree, decimal Salary);
}
