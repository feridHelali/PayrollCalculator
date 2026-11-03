using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.Tests;

public class AgreementServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly AgreementService _service;

    public AgreementServiceTests() => _service = new AgreementService(_store.Agreements, _store);

    [Fact]
    public async Task Create_persists_and_returns_the_new_id()
    {
        var id = await _service.CreateAsync(new AgreementDto(0, "النسيج", null));

        Assert.True(id > 0);
        Assert.Equal(1, _store.SaveCount);
        var list = await _service.ListAsync();
        Assert.Equal("النسيج", Assert.Single(list).Name);
    }

    [Fact]
    public async Task Update_changes_name_and_description()
    {
        var id = await _service.CreateAsync(new AgreementDto(0, "A", null));

        await _service.UpdateAsync(new AgreementDto(id, "B", "d"));

        var updated = await _service.GetAsync(id);
        Assert.Equal((id, "B", "d", false), (updated.Id, updated.Name, updated.Description, updated.IsSeeded));
        Assert.StartsWith("custom_", updated.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_unknown_agreement_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(new AgreementDto(99, "B", null)));
    }

    [Fact]
    public async Task Invalid_name_surfaces_the_domain_rule()
    {
        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(new AgreementDto(0, " ", null)));
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Delete_removes_the_agreement()
    {
        var id = await _service.CreateAsync(new AgreementDto(0, "A", null));

        await _service.DeleteAsync(id);

        Assert.Empty(await _service.ListAsync());
    }

    [Fact]
    public async Task List_is_sorted_by_name()
    {
        await _service.CreateAsync(new AgreementDto(0, "B", null));
        await _service.CreateAsync(new AgreementDto(0, "A", null));

        Assert.Equal(["A", "B"], (await _service.ListAsync()).Select(a => a.Name));
    }

    private async Task<int> SeedAsync(string code, string name, int order)
    {
        _store.Agreements.Add(SectorialJointAgreement.FromSeed(code, name, order));
        await _store.SaveChangesAsync();
        return _store.Agreements.Items.Single(a => a.Code == code).Id;
    }

    [Fact]
    public async Task Seeded_agreements_come_first_in_their_shipped_order()
    {
        await _service.CreateAsync(new AgreementDto(0, "A", null));
        await SeedAsync("textile", "الاتفاقية المشتركة القطاعية للنسيج", 2);
        await SeedAsync("vetements_tisses_et_prets_a_porter", "الاتفاقية المشتركة القطاعية لصناعة الملابس المنسوجة والجاهزة", 1);

        var list = await _service.ListAsync();

        Assert.Equal(["vetements_tisses_et_prets_a_porter", "textile"], list.Take(2).Select(a => a.Code));
        Assert.True(list[0].IsSeeded);
        Assert.Equal("A", list[2].Name);
    }

    [Fact]
    public async Task A_seeded_agreement_can_be_neither_changed_nor_deleted()
    {
        var id = await SeedAsync("textile", "الاتفاقية المشتركة القطاعية للنسيج", 2);

        var update = await Assert.ThrowsAsync<DomainException>(() => _service.UpdateAsync(new AgreementDto(id, "x", null)));
        var delete = await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(id));

        Assert.Equal((DomainError.ReadOnlySeed, DomainError.ReadOnlySeed), (update.Error, delete.Error));
        Assert.Single(await _service.ListAsync());
    }
}
