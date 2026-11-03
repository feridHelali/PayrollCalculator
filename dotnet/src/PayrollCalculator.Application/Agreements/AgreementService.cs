using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.Agreements;

public sealed record AgreementDto(int Id, string Name, string? Description);

/// <summary>Use cases for sectoral joint agreements.</summary>
public sealed class AgreementService(IAgreementRepository agreements, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<AgreementDto>> ListAsync(CancellationToken ct = default) =>
        (await agreements.ListAsync(ct).ConfigureAwait(false))
            .OrderBy(a => a.Name, StringComparer.CurrentCulture)
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
        agreements.Remove(await Load(id, ct).ConfigureAwait(false));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private Task<SectorialJointAgreement> Load(int id, CancellationToken ct) =>
        NotFoundException.OrThrow(agreements.GetAsync(id, ct), "Agreement", id);

    private static AgreementDto ToDto(SectorialJointAgreement a) => new(a.Id, a.Name, a.Description);
}
