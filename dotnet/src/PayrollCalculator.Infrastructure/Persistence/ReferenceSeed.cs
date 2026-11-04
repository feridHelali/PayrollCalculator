using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Infrastructure.Persistence;

/// <summary>
/// The salary grids and primes shipped with the product, read from <c>data/seed/agreements/{agreement}/</c> (a
/// <c>seed.json</c> plus the grid CSV files, embedded in this assembly) and seeded by the migration (EF <c>HasData</c>).
/// </summary>
/// <remarks>
/// Ids are given in a fixed order: the agreements in the order of <see cref="AgreementSeed.All"/>, then the tables and the
/// primes in the order of each <c>seed.json</c>. Changing a file changes the model: add a migration to ship the change.
/// A <c>0</c> in a CSV is the «-» of the text (no salary at that degree) and is not seeded.
/// </remarks>
internal static class ReferenceSeed
{
    private const string Prefix = "seed/";

    private static readonly Lazy<Rows> Data = new(Load);

    public static Rows All => Data.Value;

    internal sealed record TableRow(int Id, int AgreementId, string TableNumber, SalaryType SalaryType, string ConcernedEmployees,
        DateOnly Start, int WeeklyHours);

    internal sealed record CategoryRow(int Id, int SalaryTableId, string Key, string Label, int Position);

    internal sealed record DegreeRow(int Id, int SalaryTableId, string Key, int Number, int YearsInDegree);

    internal sealed record CellRow(int Id, int SalaryTableId, string CategoryKey, string DegreeKey, decimal Amount);

    internal sealed record AllowanceRow(int Id, int AgreementId, string Name, AllowanceUnit Unit, string? AppliesTo, string? Conditions);

    internal sealed record RateRow(int Id, int AllowanceId, DateOnly EffectiveFrom, decimal Value, string? Source);

    internal sealed record Rows(
        IReadOnlyList<TableRow> Tables,
        IReadOnlyList<CategoryRow> Categories,
        IReadOnlyList<DegreeRow> Degrees,
        IReadOnlyList<CellRow> Cells,
        IReadOnlyList<AllowanceRow> Allowances,
        IReadOnlyList<RateRow> Rates);

    private sealed record SeedFile(string Agreement, IReadOnlyList<SeedTable> Tables, IReadOnlyList<SeedPrime> Primes);

    private sealed record SeedTable(string File, string TableNumber, SalaryType SalaryType, int WeeklyHours, DateOnly From,
        string ConcernedEmployees, string? Source);

    private sealed record SeedPrime(string Name, AllowanceUnit Unit, string? AppliesTo, string? Conditions, IReadOnlyList<SeedRate> Rates);

    private sealed record SeedRate(DateOnly From, decimal Value, string? Source);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static Rows Load()
    {
        var assembly = typeof(ReferenceSeed).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(n => n.Replace('\\', '/'), n => n, StringComparer.Ordinal);

        var tables = new List<TableRow>();
        var categories = new List<CategoryRow>();
        var degrees = new List<DegreeRow>();
        var cells = new List<CellRow>();
        var allowances = new List<AllowanceRow>();
        var rates = new List<RateRow>();

        foreach (var (agreementId, _, code) in AgreementSeed.All)
        {
            if (!resources.TryGetValue(Prefix + code + "/seed.json", out var seedName))
            {
                continue;
            }

            var seed = JsonSerializer.Deserialize<SeedFile>(Read(assembly, seedName), Json)
                ?? throw new InvalidDataException(seedName);
            if (seed.Agreement != code)
            {
                throw new InvalidDataException($"{seedName}: agreement '{seed.Agreement}', expected '{code}'");
            }

            foreach (var t in seed.Tables)
            {
                var csvName = resources.GetValueOrDefault(Prefix + code + "/" + t.File)
                    ?? throw new InvalidDataException($"{seedName}: missing {t.File}");
                var grid = GridCsv.Parse(Read(assembly, csvName));
                var tableId = tables.Count + 1;
                tables.Add(new TableRow(tableId, agreementId, t.TableNumber, t.SalaryType, t.ConcernedEmployees, t.From, t.WeeklyHours));

                for (var d = 0; d < grid.Degrees.Count; d++)
                {
                    degrees.Add(new DegreeRow(degrees.Count + 1, tableId, DegreeKey(grid.Degrees[d]), grid.Degrees[d], grid.YearsInDegree[d]));
                }

                for (var c = 0; c < grid.Rows.Count; c++)
                {
                    var categoryKey = "cat-" + (c + 1).ToString(CultureInfo.InvariantCulture);
                    categories.Add(new CategoryRow(categories.Count + 1, tableId, categoryKey, grid.Rows[c].Category, c));
                    for (var d = 0; d < grid.Degrees.Count; d++)
                    {
                        if (grid.Rows[c].Amounts[d] is { } amount and > 0m)
                        {
                            cells.Add(new CellRow(cells.Count + 1, tableId, categoryKey, DegreeKey(grid.Degrees[d]), amount));
                        }
                    }
                }
            }

            foreach (var p in seed.Primes)
            {
                var allowanceId = allowances.Count + 1;
                allowances.Add(new AllowanceRow(allowanceId, agreementId, p.Name, p.Unit, p.AppliesTo, p.Conditions));
                foreach (var r in p.Rates)
                {
                    rates.Add(new RateRow(rates.Count + 1, allowanceId, r.From, r.Value, r.Source));
                }
            }
        }

        return new Rows(tables, categories, degrees, cells, allowances, rates);
    }

    private static string DegreeKey(int number) => "deg-" + number.ToString(CultureInfo.InvariantCulture);

    private static string Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidDataException(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
