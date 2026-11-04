using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.Migration;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Infrastructure.LegacyImport;
using PayrollCalculator.Infrastructure.Persistence.Events;
using Xunit.Abstractions;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>05.12: the existing data, migrated to events, gives the same base salaries as the first calculator.</summary>
public sealed class EventMigrationTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly string _legacyPath = Path.Combine(Path.GetTempPath(), $"legacy-{Guid.NewGuid():N}.sqlite");

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_legacyPath);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);
    }

    private async Task ImportAsync(string? realFile)
    {
        if (realFile is null)
        {
            await using var conn = new SqliteConnection($"Data Source={_legacyPath}");
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = LegacyImporterTests.LegacySchema + LegacyImporterTests.SampleData;
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            File.Copy(realFile, _legacyPath, overwrite: true);
        }

        await using var ctx = _db.NewContext();
        await new LegacyElectronImporter(ctx).ImportAsync(_legacyPath);
    }

    private async Task<EventMigrationReport> MigrateAsync()
    {
        await using var ctx = _db.NewContext();
        return await new LegacyEventMigration(ctx, new FixedClock()).MigrateAsync();
    }

    /// <summary>Why a month may differ between the two calculators (the documented fixes).</summary>
    private enum Difference
    {
        None,

        /// <summary>C1: after a version's end date (or in a gap) the old calculator had no table; the grid now stays in force.</summary>
        GridStaysInForce,

        /// <summary>The new model treats a degree with 0 years as a ceiling (the old one skipped through it).</summary>
        ZeroYearsCeiling,

        /// <summary>C7: seniority now counts from the hiring day, not from its month.</summary>
        SeniorityFromHiringDay,

        Unexplained,
    }

    /// <summary>Compares, month by month, the old timeline with the base of the new ledger, and explains each difference.</summary>
    private async Task<Dictionary<Difference, int>> CompareAsync()
    {
        await using var ctx = _db.NewContext();
        var tables = await ctx.SalaryTables.ToListAsync();
        var store = new RuleEventStore(ctx);
        var careers = new CareerEventStore(ctx);
        var legacy = new SalaryTimelineCalculator(new SeniorityDegreeProgression());
        var counts = Enum.GetValues<Difference>().ToDictionary(d => d, _ => 0);

        foreach (var affair in await ctx.Affairs.ToListAsync())
        {
            if (tables.FirstOrDefault(t => t.Id == affair.SalaryTableId) is not { } table)
            {
                continue;
            }

            var timeline = legacy.Calculate(affair, tables);
            var rules = (await store.ListAsync(table.AgreementId)).ToRulebook();
            var ledger = LedgerSettings.Default.Build(Career.From(await careers.ListAsync(affair.Id)), rules);
            var bases = ledger.Entries.Where(e => e.Component.IsBase).ToDictionary(e => e.Month);

            foreach (var old in timeline.Lines)
            {
                var entry = bases[old.Period];
                var family = tables.Where(t => t.AgreementId == table.AgreementId && t.TableNumber == table.TableNumber).ToList();
                var difference = Explain(old, entry, table, family, affair.InitialPosition.Degree, affair.EmploymentPeriod.Start);
                counts[difference]++;
                if (difference == Difference.Unexplained)
                {
                    output.WriteLine($"{affair.AffairNumber} {old.Period}: old {old.Degree}/{old.Amount}/{old.Issue}, new {entry.Degree}/{entry.Rate}/{entry.Issue}");
                }
            }
        }

        foreach (var (difference, count) in counts)
        {
            output.WriteLine($"{difference}: {count} months");
        }

        return counts;
    }

    private static Difference Explain(MonthlySalaryLine old, LedgerEntry entry, SalaryTable table, List<SalaryTable> family, int initialDegree,
        DateOnly hired)
    {
        var sameIssue = (old.Issue, entry.Issue) switch
        {
            (TimelineIssue.None, LedgerIssue.None) => old.Degree == entry.Degree && old.Amount == entry.Rate!.Value.Dinars,
            (TimelineIssue.NoApplicableTable, LedgerIssue.NoGridInForce) => true,
            (TimelineIssue.CategoryNotFound, LedgerIssue.CategoryNotInGrid) => true,
            (TimelineIssue.SalaryMissing or TimelineIssue.DegreeNotFound, LedgerIssue.SalaryMissing) => old.Degree == entry.Degree,
            _ => false,
        };
        if (sameIssue)
        {
            return Difference.None;
        }

        if (old.Issue == TimelineIssue.NoApplicableTable && entry.Issue != LedgerIssue.NoGridInForce
            && family.Any(t => t.ApplicationPeriod.End is { } end && end < old.Period.FirstDay))
        {
            return Difference.GridStaysInForce;
        }

        // Where the degrees differ, the amount must still be the grid's amount for the new degree (checked by the ledger tests),
        // and the difference must come from one of the documented changes.
        var zeroBelowOld = table.Degrees.Any(d => d.YearsInDegree == 0 && d.Number >= initialDegree && d.Number < old.Degree);
        if (zeroBelowOld && entry.Degree < old.Degree)
        {
            return Difference.ZeroYearsCeiling;
        }

        return hired.Day != 1 && entry.Degree == old.Degree - 1 ? Difference.SeniorityFromHiringDay : Difference.Unexplained;
    }

    [Fact]
    public async Task The_sample_data_migrates_with_the_same_base_salaries()
    {
        await ImportAsync(null);

        var report = await MigrateAsync();
        var counts = await CompareAsync();

        Assert.Equal((2 + 2, 2), (report.Agreements, report.Careers)); // the sample agreements, plus the two with seeded grids
        Assert.Empty(report.Failures);
        Assert.Equal(0, counts[Difference.Unexplained]);
        Assert.True(counts[Difference.None] > 0);
    }

    [Fact]
    public async Task The_migration_is_idempotent()
    {
        await ImportAsync(null);
        var first = await MigrateAsync();

        var second = await MigrateAsync();

        Assert.True(first.RuleEvents > 0);
        Assert.Equal((0, 0, 0), (second.Agreements, second.RuleEvents, second.Careers));
        await using var ctx = _db.NewContext();
        Assert.Equal(first.RuleEvents, await ctx.RuleEvents.CountAsync());
        Assert.All(await ctx.RuleEvents.ToListAsync(), e => Assert.Equal((RuleEventStatus.Approved, LegacyEventMigration.Actor), (e.Status, e.ReviewedBy)));
        Assert.Equal(first.Agreements, await ctx.LegalSources.CountAsync()); // one placeholder source per agreement
    }

    /// <summary>The parity test on the real Electron data (the sample kept as <c>tests/fixtures/legacy-sample.sqlite</c>).</summary>
    [Fact]
    public async Task The_real_data_migrates_with_only_documented_differences()
    {
        var real = LegacyImporterTests.FindUp(Path.Combine("fixtures", "legacy-sample.sqlite"));
        Assert.NotNull(real);

        await ImportAsync(real);
        var report = await MigrateAsync();
        report.Warnings.ToList().ForEach(w => output.WriteLine($"{w.Note}: {w.Subject}"));
        report.Failures.ToList().ForEach(output.WriteLine);

        var counts = await CompareAsync();

        Assert.Empty(report.Failures);
        Assert.True(report.Careers > 0);
        Assert.Equal(0, counts[Difference.Unexplained]);
    }
}
