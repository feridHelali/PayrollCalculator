using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Affairs;

public sealed record AffairDto(
    int Id,
    string AffairNumber,
    string Title,
    string Claimant,
    DateOnly StartDate,
    DateOnly EndDate,
    int AgreementId,
    int? SalaryTableId,
    string InitialCategory,
    int InitialDegree);

public sealed record AffairSummaryDto(
    int Id,
    string AffairNumber,
    string Title,
    string Claimant,
    DateOnly StartDate,
    DateOnly EndDate,
    string AgreementName,
    string? TableNumber);

/// <summary>Use cases for affairs (legal cases) and their salary reconstruction.</summary>
public sealed class AffairService(
    IAffairRepository affairs,
    IAgreementRepository agreements,
    ISalaryTableRepository tables,
    IUnitOfWork unitOfWork,
    SalaryTimelineCalculator calculator)
{
    public async Task<IReadOnlyList<AffairSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var agreementNames = (await agreements.ListAsync(ct).ConfigureAwait(false)).ToDictionary(a => a.Id, a => a.Name);
        var tableNumbers = (await tables.ListAsync(ct).ConfigureAwait(false)).ToDictionary(t => t.Id, t => t.TableNumber);

        return (await affairs.ListAsync(ct).ConfigureAwait(false))
            .OrderBy(a => a.AffairNumber, StringComparer.CurrentCulture)
            .Select(a => new AffairSummaryDto(a.Id, a.AffairNumber, a.Title, a.Claimant,
                a.EmploymentPeriod.Start, a.EmploymentPeriod.End!.Value,
                agreementNames.GetValueOrDefault(a.AgreementId) ?? string.Empty,
                a.SalaryTableId is { } t ? tableNumbers.GetValueOrDefault(t) : null))
            .ToList();
    }

    public async Task<AffairDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await Load(id, ct).ConfigureAwait(false));

    /// <summary>Creates (Id == 0) or updates an affair.</summary>
    public async Task<int> SaveAsync(AffairDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = await NotFoundException.OrThrow(agreements.GetAsync(dto.AgreementId, ct), "Agreement", dto.AgreementId).ConfigureAwait(false);
        await EnsureTableBelongsToAgreement(dto, ct).ConfigureAwait(false);
        var period = new DateRange(dto.StartDate, dto.EndDate);

        Affair affair;
        if (dto.Id == 0)
        {
            affair = Affair.Open(dto.AffairNumber, dto.Title, dto.Claimant, period,
                dto.AgreementId, dto.SalaryTableId, dto.InitialCategory, dto.InitialDegree);
            affairs.Add(affair);
        }
        else
        {
            affair = await Load(dto.Id, ct).ConfigureAwait(false);
            affair.Describe(dto.AffairNumber, dto.Title, dto.Claimant);
            affair.ChangeEmploymentPeriod(period);
            affair.ChangeAgreement(dto.AgreementId);
            affair.LinkSalaryTable(dto.SalaryTableId);
            affair.ChangeInitialPosition(dto.InitialCategory, dto.InitialDegree);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return affair.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        affairs.Remove(await Load(id, ct).ConfigureAwait(false));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reconstructs the affair's base salary month by month.</summary>
    public async Task<SalaryTimeline> CalculateTimelineAsync(int id, CancellationToken ct = default)
    {
        var affair = await Load(id, ct).ConfigureAwait(false);
        var candidates = await tables.ListByAgreementAsync(affair.AgreementId, ct).ConfigureAwait(false);
        return calculator.Calculate(affair, candidates);
    }

    private async Task EnsureTableBelongsToAgreement(AffairDto dto, CancellationToken ct)
    {
        if (dto.SalaryTableId is not { } tableId)
        {
            return;
        }

        var table = await NotFoundException.OrThrow(tables.GetAsync(tableId, ct), "Salary table", tableId).ConfigureAwait(false);
        if (table.AgreementId != dto.AgreementId)
        {
            throw new DomainException("The salary table does not belong to the selected agreement.");
        }
    }

    private Task<Affair> Load(int id, CancellationToken ct) =>
        NotFoundException.OrThrow(affairs.GetAsync(id, ct), "Affair", id);

    private static AffairDto ToDto(Affair a) => new(
        a.Id, a.AffairNumber, a.Title, a.Claimant, a.EmploymentPeriod.Start, a.EmploymentPeriod.End!.Value,
        a.AgreementId, a.SalaryTableId, a.InitialPosition.Category, a.InitialPosition.Degree);
}
