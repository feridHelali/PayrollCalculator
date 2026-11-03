using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Tests;

public class AffairServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly AffairService _service;
    private readonly int _agreementId;
    private readonly int _otherAgreementId;
    private readonly int _tableId;

    public AffairServiceTests()
    {
        _service = new AffairService(_store.Affairs, _store.Agreements, _store.SalaryTables, _store,
            new SalaryTimelineCalculator(new SeniorityDegreeProgression()));

        _store.Agreements.Add(SectorialJointAgreement.Create("Boulangeries", null));
        _store.Agreements.Add(SectorialJointAgreement.Create("Textile", null));
        _store.SaveChangesAsync().GetAwaiter().GetResult();
        _agreementId = _store.Agreements.Items[0].Id;
        _otherAgreementId = _store.Agreements.Items[1].Id;

        var table = SalaryTable.Create(_agreementId, "4", SalaryType.Monthly, "x", DateRange.From(new DateOnly(2000, 1, 1)));
        var c1 = table.AddCategory("C1");
        var d1 = table.AddDegree(1, 1);
        table.SetSalary(c1.Key, d1.Key, 300m);
        _store.SalaryTables.Add(table);
        _store.SaveChangesAsync().GetAwaiter().GetResult();
        _tableId = table.Id;
    }

    private AffairDto NewDto(int id = 0) => new(
        id, "2024-001", "Licenciement", "Salem", new DateOnly(2020, 1, 1), new DateOnly(2020, 3, 31),
        _agreementId, _tableId, "C1", 1);

    [Fact]
    public async Task Save_new_affair_then_read_it_back()
    {
        var id = await _service.SaveAsync(NewDto());

        Assert.Equal(NewDto(id), await _service.GetAsync(id));
    }

    [Fact]
    public async Task Save_existing_affair_updates_it()
    {
        var id = await _service.SaveAsync(NewDto());

        await _service.SaveAsync(NewDto(id) with { Claimant = "Imen", InitialDegree = 1, EndDate = new DateOnly(2021, 1, 31) });

        var saved = await _service.GetAsync(id);
        Assert.Equal("Imen", saved.Claimant);
        Assert.Equal(new DateOnly(2021, 1, 31), saved.EndDate);
    }

    [Fact]
    public async Task Salary_table_must_belong_to_the_affair_agreement()
    {
        var dto = NewDto() with { AgreementId = _otherAgreementId };
        await Assert.ThrowsAsync<DomainException>(() => _service.SaveAsync(dto));
    }

    [Fact]
    public async Task Unknown_agreement_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SaveAsync(NewDto() with { AgreementId = 999 }));
    }

    [Fact]
    public async Task List_shows_agreement_name_and_table_number()
    {
        await _service.SaveAsync(NewDto());

        var row = Assert.Single(await _service.ListAsync());

        Assert.Equal("Boulangeries", row.AgreementName);
        Assert.Equal("4", row.TableNumber);
    }

    [Fact]
    public async Task Timeline_is_computed_from_the_agreement_tables()
    {
        var id = await _service.SaveAsync(NewDto());

        var timeline = await _service.CalculateTimelineAsync(id);

        Assert.Equal(3, timeline.Lines.Count);
        Assert.Equal(900m, timeline.TotalMonthlyAmount);
        Assert.True(timeline.IsComplete);
    }

    [Fact]
    public async Task Delete_removes_the_affair()
    {
        var id = await _service.SaveAsync(NewDto());
        await _service.DeleteAsync(id);
        Assert.Empty(await _service.ListAsync());
    }
}
