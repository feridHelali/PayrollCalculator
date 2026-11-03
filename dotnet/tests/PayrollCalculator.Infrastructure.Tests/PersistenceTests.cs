using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<int> SeedAgreementAsync(string name = "Boulangeries")
    {
        await using var ctx = _db.NewContext();
        var agreement = SectorialJointAgreement.Create(name, "desc");
        new AgreementRepository(ctx).Add(agreement);
        await ctx.SaveChangesAsync();
        return agreement.Id;
    }

    private async Task<int> SeedTableAsync(int agreementId)
    {
        await using var ctx = _db.NewContext();
        var table = SalaryTable.Create(agreementId, "4", SalaryType.Hourly, "Ouvriers",
            new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2022, 12, 31)));
        var c1 = table.AddCategory("C1");
        table.AddCategory("C2");
        var d1 = table.AddDegree(1, 1);
        table.AddDegree(2, 2);
        table.SetSalary(c1.Key, d1.Key, 1.875m);
        new SalaryTableRepository(ctx).Add(table);
        await ctx.SaveChangesAsync();
        return table.Id;
    }

    private async Task<int> SeedAffairAsync(int agreementId, int? tableId)
    {
        await using var ctx = _db.NewContext();
        var affair = Affair.Open("2024-1", "T", "Salem",
            new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2021, 6, 30)), agreementId, tableId, "C1", 1);
        new AffairRepository(ctx).Add(affair);
        await ctx.SaveChangesAsync();
        return affair.Id;
    }

    [Fact]
    public async Task Agreement_round_trips()
    {
        var id = await SeedAgreementAsync("النسيج");

        await using var ctx = _db.NewContext();
        var loaded = await new AgreementRepository(ctx).GetAsync(id);

        Assert.Equal("النسيج", loaded!.Name);
        Assert.Equal("desc", loaded.Description);
    }

    [Fact]
    public async Task Salary_table_round_trips_with_its_grid()
    {
        var tableId = await SeedTableAsync(await SeedAgreementAsync());

        await using var ctx = _db.NewContext();
        var table = (await new SalaryTableRepository(ctx).GetAsync(tableId))!;

        Assert.Equal(SalaryType.Hourly, table.SalaryType);
        Assert.Equal(new DateOnly(2022, 12, 31), table.ApplicationPeriod.End);
        Assert.Equal(["C1", "C2"], table.Categories.Select(c => c.Label));
        Assert.Equal([1, 2], table.Degrees.Select(d => d.Number));
        Assert.Equal(1.875m, table.SalaryFor("C1", 1));
    }

    [Fact]
    public async Task Rebuilt_grid_is_persisted()
    {
        var tableId = await SeedTableAsync(await SeedAgreementAsync());

        await using (var ctx = _db.NewContext())
        {
            var table = (await new SalaryTableRepository(ctx).GetAsync(tableId))!;
            table.RebuildGrid([new("x", "X")], [new("y", 3, 4)], [new("x", "y", 99m)]);
            await ctx.SaveChangesAsync();
        }

        await using var check = _db.NewContext();
        var reloaded = (await new SalaryTableRepository(check).GetAsync(tableId))!;
        Assert.Equal("X", Assert.Single(reloaded.Categories).Label);
        Assert.Equal(4, Assert.Single(reloaded.Degrees).YearsInDegree);
        Assert.Equal(99m, Assert.Single(reloaded.Cells).Amount);
    }

    [Fact]
    public async Task List_by_agreement_filters()
    {
        var a = await SeedAgreementAsync("A");
        var b = await SeedAgreementAsync("B");
        await SeedTableAsync(a);

        await using var ctx = _db.NewContext();
        var repo = new SalaryTableRepository(ctx);

        Assert.Single(await repo.ListByAgreementAsync(a));
        Assert.Empty(await repo.ListByAgreementAsync(b));
    }

    [Fact]
    public async Task Affair_round_trips_with_value_objects()
    {
        var agreementId = await SeedAgreementAsync();
        var tableId = await SeedTableAsync(agreementId);
        var affairId = await SeedAffairAsync(agreementId, tableId);

        await using var ctx = _db.NewContext();
        var affair = (await new AffairRepository(ctx).GetAsync(affairId))!;

        Assert.Equal(new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2021, 6, 30)), affair.EmploymentPeriod);
        Assert.Equal(new ProfessionalPosition("C1", 1), affair.InitialPosition);
        Assert.Equal(tableId, affair.SalaryTableId);
    }

    [Fact]
    public async Task Deleting_an_agreement_cascades_to_tables_and_affairs()
    {
        var agreementId = await SeedAgreementAsync();
        await SeedAffairAsync(agreementId, await SeedTableAsync(agreementId));

        await using (var ctx = _db.NewContext())
        {
            var repo = new AgreementRepository(ctx);
            repo.Remove((await repo.GetAsync(agreementId))!);
            await ctx.SaveChangesAsync();
        }

        await using var check = _db.NewContext();
        Assert.Empty(await new SalaryTableRepository(check).ListAsync());
        Assert.Empty(await new AffairRepository(check).ListAsync());
    }

    [Fact]
    public async Task Deleting_a_table_unlinks_its_affairs()
    {
        var agreementId = await SeedAgreementAsync();
        var tableId = await SeedTableAsync(agreementId);
        var affairId = await SeedAffairAsync(agreementId, tableId);

        await using (var ctx = _db.NewContext())
        {
            var repo = new SalaryTableRepository(ctx);
            repo.Remove((await repo.GetAsync(tableId))!);
            await ctx.SaveChangesAsync();
        }

        await using var check = _db.NewContext();
        Assert.Null((await new AffairRepository(check).GetAsync(affairId))!.SalaryTableId);
    }
}
