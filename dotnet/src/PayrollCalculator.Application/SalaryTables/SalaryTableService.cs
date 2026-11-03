using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.SalaryTables;

public sealed record CategoryDto(string Key, string Label);

public sealed record DegreeDto(string Key, int Number, int YearsInDegree);

public sealed record CellDto(string CategoryKey, string DegreeKey, decimal Amount);

/// <summary>Full salary table, as edited in the sheet.</summary>
public sealed record SalaryTableDto(
    int Id,
    int AgreementId,
    string TableNumber,
    SalaryType SalaryType,
    string ConcernedEmployees,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<DegreeDto> Degrees,
    IReadOnlyList<CellDto> Cells);

/// <summary>Row of the salary tables list.</summary>
public sealed record SalaryTableSummaryDto(
    int Id,
    int AgreementId,
    string AgreementName,
    string TableNumber,
    SalaryType SalaryType,
    string ConcernedEmployees,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <summary>Use cases for salary tables.</summary>
public sealed class SalaryTableService(ISalaryTableRepository tables, IAgreementRepository agreements, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<SalaryTableSummaryDto>> ListAsync(int? agreementId = null, CancellationToken ct = default)
    {
        var rows = agreementId is { } id
            ? await tables.ListByAgreementAsync(id, ct).ConfigureAwait(false)
            : await tables.ListAsync(ct).ConfigureAwait(false);
        var names = (await agreements.ListAsync(ct).ConfigureAwait(false)).ToDictionary(a => a.Id, a => a.Name);

        return rows
            .OrderBy(t => names.GetValueOrDefault(t.AgreementId), StringComparer.CurrentCulture)
            .ThenBy(t => t.TableNumber, StringComparer.CurrentCulture)
            .ThenBy(t => t.ApplicationPeriod.Start)
            .Select(t => new SalaryTableSummaryDto(t.Id, t.AgreementId, names.GetValueOrDefault(t.AgreementId) ?? string.Empty,
                t.TableNumber, t.SalaryType, t.ConcernedEmployees, t.ApplicationPeriod.Start, t.ApplicationPeriod.End))
            .ToList();
    }

    public async Task<SalaryTableDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await Load(id, ct).ConfigureAwait(false));

    /// <summary>Creates (Id == 0) or updates a table, replacing its grid with the one in <paramref name="dto"/>.</summary>
    public async Task<int> SaveAsync(SalaryTableDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = await NotFoundException.OrThrow(agreements.GetAsync(dto.AgreementId, ct), "Agreement", dto.AgreementId).ConfigureAwait(false);
        var period = new DateRange(dto.StartDate, dto.EndDate);

        SalaryTable table;
        if (dto.Id == 0)
        {
            table = SalaryTable.Create(dto.AgreementId, dto.TableNumber, dto.SalaryType, dto.ConcernedEmployees, period);
            RebuildGrid(table, dto);
            tables.Add(table);
        }
        else
        {
            table = await Load(dto.Id, ct).ConfigureAwait(false);
            if (table.AgreementId != dto.AgreementId)
            {
                throw new DomainException("A salary table cannot be moved to another agreement.");
            }

            table.Describe(dto.TableNumber, dto.SalaryType, dto.ConcernedEmployees, period);
            RebuildGrid(table, dto);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return table.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        tables.Remove(await Load(id, ct).ConfigureAwait(false));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void RebuildGrid(SalaryTable table, SalaryTableDto dto) =>
        table.RebuildGrid(
            dto.Categories.Select(c => new CategorySpec(c.Key, c.Label)),
            dto.Degrees.Select(d => new DegreeSpec(d.Key, d.Number, d.YearsInDegree)),
            dto.Cells.Select(c => new CellSpec(c.CategoryKey, c.DegreeKey, c.Amount)));

    private Task<SalaryTable> Load(int id, CancellationToken ct) =>
        NotFoundException.OrThrow(tables.GetAsync(id, ct), "Salary table", id);

    private static SalaryTableDto ToDto(SalaryTable t) => new(
        t.Id, t.AgreementId, t.TableNumber, t.SalaryType, t.ConcernedEmployees,
        t.ApplicationPeriod.Start, t.ApplicationPeriod.End,
        t.Categories.Select(c => new CategoryDto(c.Key, c.Label)).ToList(),
        t.Degrees.Select(d => new DegreeDto(d.Key, d.Number, d.YearsInDegree)).ToList(),
        t.Cells.Select(c => new CellDto(c.CategoryKey, c.DegreeKey, c.Amount)).ToList());
}
