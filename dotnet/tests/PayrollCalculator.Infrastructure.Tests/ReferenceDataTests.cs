using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Infrastructure.Library;
using PayrollCalculator.Infrastructure.LegacyImport;
using PayrollCalculator.Infrastructure.Persistence;
using PayrollCalculator.Infrastructure.Seeding;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>Sprint 07: the shipped agreements (07.1 and the seed list) and the PDF library (07.2).</summary>
public sealed class ReferenceDataTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<SeedReport> SeedAsync(IReadOnlyList<SeedAgreement>? agreements = null)
    {
        await using var ctx = _db.NewContext();
        var seeder = new ReferenceDataSeeder(ctx);
        return agreements is null ? await seeder.SeedAsync() : await seeder.SeedAsync(agreements);
    }

    [Fact]
    public void The_build_ships_the_twenty_agreements_with_unique_codes_and_arabic_names()
    {
        var shipped = ReferenceDataSeeder.ShippedAgreements();

        Assert.Equal(20, shipped.Count);
        Assert.Equal(Enumerable.Range(1, 20), shipped.Select(a => a.Id));
        Assert.All(shipped, a => Assert.StartsWith("الاتفاقية المشتركة القطاعية", a.Label, StringComparison.Ordinal));
        Assert.Contains(shipped, a => a.Slug == "commerce_et_distribution");
        _ = shipped.Select(a => SectorialJointAgreement.FromSeed(a.Slug, a.Label, a.Id)).ToList(); // every code is a valid slug
    }

    [Fact]
    public async Task Seeding_is_idempotent_and_matched_by_code()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        Assert.Equal((20, 0, 0), (first.Added, first.Updated, first.Unchanged));
        Assert.Equal((0, 0, 20), (second.Added, second.Updated, second.Unchanged));
        await using var ctx = _db.NewContext();
        Assert.All(await ctx.Agreements.ToListAsync(), a => Assert.Equal(DataOrigin.Seed, a.Origin));
    }

    [Fact]
    public async Task A_newer_release_renames_a_seeded_agreement_and_leaves_user_ones_alone()
    {
        await SeedAsync([new SeedAgreement(1, "قديم", "textile")]);
        await using (var ctx = _db.NewContext())
        {
            ctx.Agreements.Add(SectorialJointAgreement.Create("اتفاقية خاصة", null));
            await ctx.SaveChangesAsync();
        }

        var report = await SeedAsync([new SeedAgreement(2, "الاتفاقية المشتركة القطاعية للنسيج", "textile")]);

        Assert.Equal((0, 1), (report.Added, report.Updated));
        await using var check = _db.NewContext();
        var textile = await new AgreementRepository(check).FindByCodeAsync("textile");
        Assert.Equal(("الاتفاقية المشتركة القطاعية للنسيج", (int?)2), (textile!.Name, textile.SortOrder));
        Assert.Single(await check.Agreements.Where(a => a.Origin == DataOrigin.User).ToListAsync());
    }

    [Fact]
    public void A_seed_list_with_a_code_used_twice_is_refused()
    {
        using var json = new MemoryStream("""[{"id":1,"label":"a","slug":"x_y"},{"id":2,"label":"b","slug":"x_y"}]"""u8.ToArray());

        Assert.Throws<InvalidDataException>(() => ReferenceDataSeeder.Parse(json));
    }

    [Fact]
    public async Task Agreements_entered_before_sprint_07_get_a_unique_user_code_when_migrating()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<PayrollDbContext>().UseSqlite(connection).Options;
        await using (var ctx = new PayrollDbContext(options))
        {
            await ctx.GetService<IMigrator>().MigrateAsync("20261103184514_AddRuleEvents");
            await ctx.Database.ExecuteSqlRawAsync("INSERT INTO Agreements (Name, Description) VALUES ('A', NULL), ('B', NULL);");
        }

        await using var migrated = new PayrollDbContext(options);
        await migrated.Database.MigrateAsync();
        var agreements = await migrated.Agreements.OrderBy(a => a.Id).ToListAsync();

        Assert.Equal(["custom_1", "custom_2"], agreements.Select(a => a.Code));
        Assert.All(agreements, a => Assert.Equal(DataOrigin.User, a.Origin));
    }

    [Fact]
    public async Task The_legacy_import_still_works_on_a_seeded_database()
    {
        await SeedAsync();
        var legacy = Path.Combine(Path.GetTempPath(), $"legacy-{Guid.NewGuid():N}.sqlite");
        try
        {
            await using (var conn = new SqliteConnection($"Data Source={legacy}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = LegacyImporterTests.LegacySchema + LegacyImporterTests.SampleData;
                await cmd.ExecuteNonQueryAsync();
            }

            await using var ctx = _db.NewContext();
            var report = await new LegacyElectronImporter(ctx).ImportAsync(legacy);

            Assert.Equal(2, report.Agreements);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(legacy);
        }
    }

    /// <summary>07.2: the repository's library matches its manifest (no missing, changed, duplicate or uncatalogued file).</summary>
    [Fact]
    public void The_pdf_library_matches_its_manifest()
    {
        var manifest = LegacyImporterTests.FindUp(Path.Combine("data", "jort", "manifest.json"));
        Assert.NotNull(manifest);
        var library = new JortLibrary(Path.GetDirectoryName(manifest)!, _ => null);

        Assert.Empty(library.Verify());
        Assert.Equal(15, library.Load().Count);
    }
}
