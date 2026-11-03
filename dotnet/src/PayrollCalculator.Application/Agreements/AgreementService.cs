using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.Agreements;

/// <summary>A kind of salary grid of an agreement, offered when creating a grid.</summary>
public sealed record GridKindDto(string Code, string Name, PayUnit Unit, int DefaultWeeklyHours);

/// <param name="Code">Stable code (read only); null when creating.</param>
/// <param name="IsSeeded">Shipped with the product, so read only (D4).</param>
/// <param name="GridKinds">The salary grid kinds the agreement defines (read only).</param>
public sealed record AgreementDto(int Id, string Name, string? Description, string? Code = null, bool IsSeeded = false,
    IReadOnlyList<GridKindDto>? GridKinds = null);

/// <summary>Use cases for sectoral joint agreements.</summary>
public sealed class AgreementService(IAgreementRepository agreements, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<AgreementDto>> ListAsync(CancellationToken ct = default) =>
        (await agreements.ListAsync(ct).ConfigureAwait(false))
            .OrderBy(a => a.SortOrder ?? int.MaxValue)
            .ThenBy(a => a.Name, StringComparer.CurrentCulture)
            .Select(ToDto)
            .ToList();

    public async Task<AgreementDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await Load(id, ct).ConfigureAwait(false));

    public async Task<int> CreateAsync(AgreementDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var agreement = SectorialJointAgreement.Create(dto.Name, dto.Description);
        agreements.Add(agreement);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return agreement.Id;
    }

    public async Task UpdateAsync(AgreementDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var agreement = await Load(dto.Id, ct).ConfigureAwait(false);
        agreement.Rename(dto.Name);
        agreement.ChangeDescription(dto.Description);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Deletes the agreement; its salary tables and affairs are deleted with it (cascade).</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var agreement = await Load(id, ct).ConfigureAwait(false);
        agreement.EnsureEditable();
        agreements.Remove(agreement);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private Task<SectorialJointAgreement> Load(int id, CancellationToken ct) =>
        NotFoundException.OrThrow(agreements.GetAsync(id, ct), "Agreement", id);

    private static AgreementDto ToDto(SectorialJointAgreement a) => new(a.Id, a.Name, a.Description, a.Code, a.IsSeeded,
        a.GridKinds.Select(k => new GridKindDto(k.Code, k.Name, k.Unit, k.DefaultWeeklyHours)).ToList());
}
