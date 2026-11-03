using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.Allowances;

public sealed record AllowanceRateDto(DateOnly EffectiveFrom, decimal Value, string? Source);

/// <summary>A prime as edited in the form.</summary>
public sealed record AllowanceDto(
    int Id,
    int AgreementId,
    string Name,
    AllowanceUnit Unit,
    string? AppliesTo,
    string? Conditions,
    IReadOnlyList<AllowanceRateDto> Rates);

/// <summary>Row of the primes list; <see cref="CurrentValue"/> is the amount in force today.</summary>
public sealed record AllowanceSummaryDto(
    int Id,
    int AgreementId,
    string AgreementName,
    string Name,
    AllowanceUnit Unit,
    string? AppliesTo,
    decimal? CurrentValue,
    DateOnly? FirstDate,
    int RateCount);

/// <summary>Use cases for the primes and allowances of the agreements.</summary>
public sealed class AllowanceService(IAllowanceRepository allowances, IAgreementRepository agreements, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<IReadOnlyList<AllowanceSummaryDto>> ListAsync(int? agreementId = null, CancellationToken ct = default)
    {
        var names = (await agreements.ListAsync(ct).ConfigureAwait(false)).ToDictionary(a => a.Id, a => a.Name);
        var today = DateOnly.FromDateTime(clock.GetLocalNow().Date);
        return (await allowances.ListByAgreementAsync(agreementId, ct).ConfigureAwait(false))
            .OrderBy(a => names.GetValueOrDefault(a.AgreementId), StringComparer.CurrentCulture)
            .ThenBy(a => a.Name, StringComparer.CurrentCulture)
            .Select(a => new AllowanceSummaryDto(a.Id, a.AgreementId, names.GetValueOrDefault(a.AgreementId) ?? string.Empty, a.Name, a.Unit,
                a.AppliesTo, a.RateOn(today)?.Value, a.Rates.Count > 0 ? a.Rates[0].EffectiveFrom : null, a.Rates.Count))
            .ToList();
    }

    public async Task<AllowanceDto> GetAsync(int id, CancellationToken ct = default)
    {
        var a = await Load(id, ct).ConfigureAwait(false);
        return new AllowanceDto(a.Id, a.AgreementId, a.Name, a.Unit, a.AppliesTo, a.Conditions,
            a.Rates.Select(r => new AllowanceRateDto(r.EffectiveFrom, r.Value, r.Source)).ToList());
    }

    /// <returns>The id of the saved prime.</returns>
    public async Task<int> SaveAsync(AllowanceDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        _ = await NotFoundException.OrThrow(agreements.GetAsync(dto.AgreementId, ct), "Agreement", dto.AgreementId).ConfigureAwait(false);
        Allowance allowance;
        if (dto.Id == 0)
        {
            allowance = Allowance.Create(dto.AgreementId, dto.Name, dto.Unit, dto.AppliesTo, dto.Conditions);
            allowances.Add(allowance);
        }
        else
        {
            allowance = await Load(dto.Id, ct).ConfigureAwait(false);
            if (allowance.AgreementId != dto.AgreementId)
            {
                throw new DomainException(DomainError.TableAgreementLocked, DomainField.Agreement);
            }
        }

        allowance.ReplaceRates([]); // so that a change of unit is checked against the new amounts only
        allowance.Describe(dto.Name, dto.Unit, dto.AppliesTo, dto.Conditions);
        allowance.ReplaceRates(dto.Rates.Select(r => new AllowanceRateSpec(r.EffectiveFrom, r.Value, r.Source)));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return allowance.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        allowances.Remove(await Load(id, ct).ConfigureAwait(false));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private Task<Allowance> Load(int id, CancellationToken ct) =>
        NotFoundException.OrThrow(allowances.GetAsync(id, ct), "Allowance", id);
}
