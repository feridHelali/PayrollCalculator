using Xunit.Abstractions;
using Microsoft.Data.Sqlite;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Infrastructure.LegacyImport;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Tests;

public sealed class LegacyImporterTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly string _legacyPath = Path.Combine(Path.GetTempPath(), $"legacy-{Guid.NewGuid():N}.sqlite");

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_legacyPath);
    }

    /// <summary>Schema copied from the Electron app's payroll.sqlite (TypeORM-generated).</summary>
    private const string LegacySchema = """
        CREATE TABLE "SectorialJointAgreement" ("sectorial_joint_AgreementID" integer PRIMARY KEY AUTOINCREMENT NOT NULL, "agreement_name" text NOT NULL, "description" text);
        CREATE TABLE "SalaryTable" ("salaryTableID" integer PRIMARY KEY AUTOINCREMENT NOT NULL, "numero_table" text NOT NULL, "type" text NOT NULL, "consernedEmployee" text NOT NULL, "beginning_date_of_application" date NOT NULL, "end_date_of_application" date, "degrees" json NOT NULL, "categories" json NOT NULL, "agreementID" integer NOT NULL);
        CREATE TABLE "SalaryTableCell" ("id" integer PRIMARY KEY AUTOINCREMENT NOT NULL, "professionalCategory" text NOT NULL, "prefessionalSubCategory" text, "ProfessionalDegree" json NOT NULL, "salary" real NOT NULL, "salaryTableId" integer);
        CREATE TABLE "Affair" ("affairID" integer PRIMARY KEY AUTOINCREMENT NOT NULL, "affairNumber" text NOT NULL, "title" text NOT NULL, "claimant" text NOT NULL, "startDateOfWork" date NOT NULL, "endDateOfWork" date NOT NULL, "professionalCategoryAtBegining" text NOT NULL, "professionalDegreeAtBegining" integer NOT NULL, "agreementID" integer, "numero_table" integer);
        """;

    private void CreateLegacy(string data)
    {
        using var conn = new SqliteConnection($"Data Source={_legacyPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = LegacySchema + data;
        cmd.ExecuteNonQuery();
    }

    private const string SampleData = """
        INSERT INTO SectorialJointAgreement VALUES (2, 'المخابز', '');
        INSERT INTO SectorialJointAgreement VALUES (5, 'وكالات الأسفار', NULL);
        INSERT INTO SalaryTable VALUES (7, '5', 'جدول الاجور بالساعة', 'الاعوان الفنيون', '2023-01-01', '',
          '[{"key":"d1","degree":1,"ageOfWork":1},{"key":"d2","degree":2,"ageOfWork":"2"}]',
          '[{"key":"c1","label":"1 - الصنف"},{"key":"c2","label":"2 - الصنف"}]', 2);
        INSERT INTO SalaryTable VALUES (10, '1', 'جدول الاجور بالشهر', 'نظام 48 ساعة', '2002-07-01', '2010-12-31',
          '[{"key":"e1","degree":1,"ageOfWork":"1"}]', '[{"key":"k1","label":"التنفيذ 1"}]', 5);
        INSERT INTO SalaryTableCell VALUES (1, 'c1', NULL, '"d1"', 1.5, 7);
        INSERT INTO SalaryTableCell VALUES (2, 'c2', NULL, '"d2"', 2.25, 7);
        INSERT INTO SalaryTableCell VALUES (3, 'c2', NULL, '"unknown"', 9, 7);
        INSERT INTO SalaryTableCell VALUES (4, 'k1', NULL, '"e1"', 500, 10);
        INSERT INTO SalaryTableCell VALUES (5, 'k1', NULL, '"e1"', 1, NULL);
        INSERT INTO Affair VALUES (4, '32659988', 'Imen', 'Imen ARAFA', '2000-01-01', '2020-12-31', 'C2', 1, 2, 7);
        INSERT INTO Affair VALUES (5, '2024-2001', 'Salem', 'Salem OMRANE', '2000-01-01', '2020-01-03', 'التنفيذ 1', 3, 5, 10);
        """;

    private async Task<ImportReport> ImportAsync()
    {
        await using var ctx = _db.NewContext();
        return await new LegacyElectronImporter(ctx).ImportAsync(_legacyPath);
    }

    [Fact]
    public async Task Imports_agreements_tables_cells_and_affairs()
    {
        CreateLegacy(SampleData);

        var report = await ImportAsync();

        Assert.Equal((2, 2, 3, 2), (report.Agreements, report.SalaryTables, report.Cells, report.Affairs));
        await using var ctx = _db.NewContext();
        var bakeries = (await new AgreementRepository(ctx).ListAsync()).Single(a => a.Name == "المخابز");
        var table = Assert.Single(await new SalaryTableRepository(ctx).ListByAgreementAsync(bakeries.Id));
        Assert.Equal(SalaryType.Hourly, table.SalaryType);
        Assert.Null(table.ApplicationPeriod.End);
        Assert.Equal(2, table.Degrees.Single(d => d.Number == 2).YearsInDegree);
        Assert.Equal(2.25m, table.SalaryFor("2 - الصنف", 2));
    }

    [Fact]
    public async Task Affair_category_codes_are_resolved_to_the_linked_table_labels()
    {
        CreateLegacy(SampleData);

        await ImportAsync();

        await using var ctx = _db.NewContext();
        var affairs = await new AffairRepository(ctx).ListAsync();
        var imen = affairs.Single(a => a.AffairNumber == "32659988");
        var salem = affairs.Single(a => a.AffairNumber == "2024-2001");
        Assert.Equal("2 - الصنف", imen.InitialPosition.Category);
        Assert.Equal("التنفيذ 1", salem.InitialPosition.Category);
        var table = (await new SalaryTableRepository(ctx).GetAsync(imen.SalaryTableId!.Value))!;
        Assert.Equal("5", table.TableNumber);
    }

    [Fact]
    public async Task Unusable_rows_are_skipped_with_warnings()
    {
        CreateLegacy(SampleData);

        var report = await ImportAsync();

        Assert.Contains(report.Warnings, w => w.Contains("unknown", StringComparison.Ordinal));
        Assert.Contains(report.Warnings, w => w.Contains("#5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_refuses_a_database_that_already_has_data()
    {
        CreateLegacy(SampleData);
        await using (var ctx = _db.NewContext())
        {
            new AgreementRepository(ctx).Add(SectorialJointAgreement.Create("existing", null));
            await ctx.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(ImportAsync);
    }

    [Fact]
    public async Task Missing_legacy_file_is_reported()
    {
        await using var ctx = _db.NewContext();
        await Assert.ThrowsAsync<FileNotFoundException>(() => new LegacyElectronImporter(ctx).ImportAsync(_legacyPath));
    }

    /// <summary>Smoke test against the real Electron database when it is present in the repository.</summary>
    [Fact]
    public async Task Real_legacy_database_imports_without_errors()
    {
        var real = FindUp("payroll.sqlite");
        if (real is null)
        {
            return; // repository copy not available on this machine
        }

        File.Copy(real, _legacyPath, overwrite: true);

        var report = await ImportAsync();

        output.WriteLine($"agreements={report.Agreements} tables={report.SalaryTables} cells={report.Cells} affairs={report.Affairs}");
        report.Warnings.ToList().ForEach(output.WriteLine);
        Assert.True(report.Agreements > 0);
        Assert.True(report.SalaryTables > 0);
        Assert.True(report.Affairs > 0);
    }

    private static string? FindUp(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
