using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.Common;
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

        Assert.Equal(new AgreementDto(id, "B", "d"), await _service.GetAsync(id));
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
}
