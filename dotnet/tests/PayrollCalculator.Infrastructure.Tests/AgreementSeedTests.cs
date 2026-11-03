using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>The initial migration seeds the 20 sectoral agreements (Arabic labels, stable codes).</summary>
public sealed class AgreementSeedTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task A_new_database_holds_the_twenty_agreements_in_order()
    {
        await using var ctx = _db.NewContext();
        var agreements = await ctx.Agreements.OrderBy(a => a.SortOrder).ToListAsync();

        Assert.Equal(20, agreements.Count);
        Assert.All(agreements, a => Assert.Equal(DataOrigin.Seed, a.Origin));
        Assert.Equal(("vetements_tisses_et_prets_a_porter", "الاتفاقية المشتركة القطاعية لصناعة الملابس المنسوجة والجاهزة"), (agreements[0].Code, agreements[0].Name));
        Assert.Equal("commerce_et_distribution", agreements[18].Code);
        Assert.All(agreements, a => Assert.StartsWith("الاتفاقية المشتركة القطاعية", a.Name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_agreement_added_by_a_user_gets_an_id_after_the_seeded_ones()
    {
        int id;
        await using (var ctx = _db.NewContext())
        {
            var agreement = SectorialJointAgreement.Create("اتفاقية أخرى", null);
            new AgreementRepository(ctx).Add(agreement);
            await ctx.SaveChangesAsync();
            id = agreement.Id;
        }

        await using var check = _db.NewContext();
        Assert.True(id > 20);
        Assert.Equal(DataOrigin.User, (await new AgreementRepository(check).GetAsync(id))!.Origin);
        Assert.NotNull(await new AgreementRepository(check).FindByCodeAsync("textile"));
    }

    [Fact]
    public async Task Each_agreement_comes_with_its_salary_grid_kinds()
    {
        await using var ctx = _db.NewContext();
        var agreements = await ctx.Agreements.ToListAsync();

        Assert.Equal(37, agreements.Sum(a => a.GridKinds.Count));
        Assert.All(agreements, a => Assert.InRange(a.GridKinds.Count, 1, 2));
        var textile = agreements.Single(a => a.Code == "textile");
        var hourly = textile.GridKinds.Single(k => k.Code == "horaire_ouvriers");
        Assert.Equal((PayUnit.Hourly, 48, "جدول الأجور الخاص بالعملة (ساعي)"), (hourly.Unit, hourly.DefaultWeeklyHours, hourly.Name));
        Assert.Equal((PayUnit.Monthly, 40), (agreements.Single(a => a.Code == "banques_et_institutions_financieres").GridKinds[0].Unit,
            agreements.Single(a => a.Code == "banques_et_institutions_financieres").GridKinds[0].DefaultWeeklyHours));
    }
}
