using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Application.Allowances;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>The initial migration also seeds the grids and primes of data/seed/agreements (clothing rider 18, travel agencies).</summary>
public sealed class ReferenceSeedTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static async Task<int> AgreementIdAsync(PayrollDbContext ctx, string code) =>
        (await ctx.Agreements.SingleAsync(a => a.Code == code)).Id;

    [Fact]
    public async Task A_new_database_holds_the_seeded_grids_with_their_cells()
    {
        await using var ctx = _db.NewContext();
        var tables = await ctx.SalaryTables.ToListAsync();
        var clothing = await AgreementIdAsync(ctx, "vetements_tisses_et_prets_a_porter");
        var travel = await AgreementIdAsync(ctx, "agences_de_voyages");

        Assert.All(tables, t => Assert.Equal(DataOrigin.Seed, t.Origin));
        Assert.Equal(6, tables.Count(t => t.AgreementId == clothing));
        Assert.Equal(6, tables.Count(t => t.AgreementId == travel));
        Assert.Equal(1296, tables.Where(t => t.AgreementId == clothing).Sum(t => t.Cells.Count)); // the README's 1,296 cells

        var hourly2024 = tables.Single(t => t.AgreementId == clothing && t.TableNumber == "1" && t.ApplicationPeriod.Start == new DateOnly(2024, 1, 1));
        Assert.Equal((SalaryType.Hourly, 48, 6, 18), (hourly2024.SalaryType, hourly2024.WeeklyHours, hourly2024.Categories.Count, hourly2024.Degrees.Count));
        Assert.Equal([1, 1, 1, 2], hourly2024.Degrees.Take(4).Select(d => d.YearsInDegree));

        var travel2022 = tables.Single(t => t.AgreementId == travel && t.ApplicationPeriod.Start == new DateOnly(2022, 5, 1));
        Assert.Equal(504.153m, travel2022.SalaryFor("1", 1));
        Assert.Equal(1006.238m, travel2022.SalaryFor("8", 7));
        Assert.Equal(
            [new DateOnly(1996, 5, 1), new DateOnly(1997, 5, 1), new DateOnly(1998, 5, 1), new DateOnly(2017, 5, 1), new DateOnly(2022, 5, 1), new DateOnly(2023, 5, 1)],
            tables.Where(t => t.AgreementId == travel).Select(t => t.ApplicationPeriod.Start).Order());
    }

    [Fact]
    public async Task A_new_database_holds_the_seeded_primes_with_their_sources()
    {
        await using var ctx = _db.NewContext();
        var primes = await ctx.Allowances.ToListAsync();
        var travel = await AgreementIdAsync(ctx, "agences_de_voyages");

        Assert.Equal(11, primes.Count);
        Assert.All(primes, p => Assert.Equal(DataOrigin.Seed, p.Origin));
        Assert.All(primes.SelectMany(p => p.Rates), r => Assert.False(string.IsNullOrWhiteSpace(r.Source)));
        var presence = primes.Single(p => p.AgreementId == travel && p.Unit == AllowanceUnit.PerDayWorked);
        Assert.Equal([0.103m, 0.424m, 0.452m, 0.481m], presence.Rates.Select(r => r.Value));
        Assert.Equal(0.452m, presence.RateOn(new DateOnly(2022, 12, 31))!.Value);
    }

    [Fact]
    public async Task Seeded_grids_and_primes_can_be_neither_changed_nor_deleted_but_a_user_table_gets_a_later_id()
    {
        await using var ctx = _db.NewContext();
        var tables = new SalaryTableService(new SalaryTableRepository(ctx), new AgreementRepository(ctx), ctx);
        var primes = new AllowanceService(new AllowanceRepository(ctx), new AgreementRepository(ctx), ctx, TimeProvider.System);
        var table = await tables.GetAsync(1);
        var prime = await primes.GetAsync(1);
        Assert.True(table.IsSeeded && prime.IsSeeded);

        var errors = new[]
        {
            await Assert.ThrowsAsync<DomainException>(() => tables.SaveAsync(table with { ConcernedEmployees = "x" })),
            await Assert.ThrowsAsync<DomainException>(() => tables.DeleteAsync(table.Id)),
            await Assert.ThrowsAsync<DomainException>(() => primes.SaveAsync(prime with { Name = "x" })),
            await Assert.ThrowsAsync<DomainException>(() => primes.DeleteAsync(prime.Id)),
        };
        Assert.All(errors, e => Assert.Equal(DomainError.ReadOnlySeed, e.Error));

        var id = await tables.SaveAsync(table with { Id = 0, TableNumber = "9" });
        Assert.True(id > 12);
        Assert.False((await tables.GetAsync(id)).IsSeeded);
    }
}
