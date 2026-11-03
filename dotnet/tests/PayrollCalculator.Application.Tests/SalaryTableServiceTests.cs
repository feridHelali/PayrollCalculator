using PayrollCalculator.Application.Common;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Tests;

public class SalaryTableServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly SalaryTableService _service;
    private readonly int _agreementId;

    public SalaryTableServiceTests()
    {
        _service = new SalaryTableService(_store.SalaryTables, _store.Agreements, _store);
        _store.Agreements.Add(SectorialJointAgreement.Create("Boulangeries", null));
        _store.SaveChangesAsync().GetAwaiter().GetResult();
        _agreementId = _store.Agreements.Items[0].Id;
    }

    private SalaryTableDto NewDto(int id = 0) => new(
        id, _agreementId, "4", SalaryType.Monthly, "Administratifs",
        new DateOnly(2023, 1, 1), null,
        [new CategoryDto("c1", "C1"), new CategoryDto("c2", "C2")],
        [new DegreeDto("d1", 1, 1), new DegreeDto("d2", 2, 2)],
        [new CellDto("c1", "d1", 400m), new CellDto("c2", "d2", 550m)]);

    [Fact]
    public async Task Save_new_table_creates_the_full_grid()
    {
        var id = await _service.SaveAsync(NewDto());

        var saved = await _service.GetAsync(id);
        Assert.Equal(["C1", "C2"], saved.Categories.Select(c => c.Label));
        Assert.Equal([1, 2], saved.Degrees.Select(d => d.Number));
        Assert.Equal(550m, saved.Cells.Single(c => c.CategoryKey == "c2").Amount);
    }

    [Fact]
    public async Task Save_existing_table_replaces_structure_and_cells()
    {
        var id = await _service.SaveAsync(NewDto());
        var edited = NewDto(id) with
        {
            TableNumber = "5",
            Categories = [new CategoryDto("c2", "C2 renamed"), new CategoryDto("c3", "C3")],
            Degrees = [new DegreeDto("d1", 1, 3)],
            Cells = [new CellDto("c3", "d1", 10m)],
        };

        await _service.SaveAsync(edited);

        var saved = await _service.GetAsync(id);
        Assert.Equal("5", saved.TableNumber);
        Assert.Equal(["C2 renamed", "C3"], saved.Categories.Select(c => c.Label));
        Assert.Equal(3, Assert.Single(saved.Degrees).YearsInDegree);
        Assert.Equal(new CellDto("c3", "d1", 10m), Assert.Single(saved.Cells));
        Assert.Single(_store.SalaryTables.Items);
    }

    [Fact]
    public async Task Save_for_unknown_agreement_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SaveAsync(NewDto() with { AgreementId = 42 }));
    }

    [Fact]
    public async Task Cell_pointing_to_unknown_category_is_rejected()
    {
        var dto = NewDto() with { Cells = [new CellDto("zz", "d1", 1m)] };
        await Assert.ThrowsAsync<DomainException>(() => _service.SaveAsync(dto));
        Assert.Empty(_store.SalaryTables.Items);
    }

    [Fact]
    public async Task List_can_be_filtered_by_agreement_and_carries_its_name()
    {
        await _service.SaveAsync(NewDto());

        var all = await _service.ListAsync();
        var none = await _service.ListAsync(agreementId: 999);

        Assert.Equal("Boulangeries", Assert.Single(all).AgreementName);
        Assert.Empty(none);
    }

    [Fact]
    public async Task Delete_removes_the_table()
    {
        var id = await _service.SaveAsync(NewDto());
        await _service.DeleteAsync(id);
        Assert.Empty(_store.SalaryTables.Items);
    }
}
