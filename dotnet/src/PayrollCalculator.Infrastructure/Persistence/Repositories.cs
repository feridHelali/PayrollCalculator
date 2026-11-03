using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Infrastructure.Persistence;

public abstract class EfRepository<T>(PayrollDbContext context) : IRepository<T> where T : Entity
{
    protected PayrollDbContext Context { get; } = context;

    protected DbSet<T> Set => Context.Set<T>();

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<T?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(T aggregate) => Set.Add(aggregate);

    public void Remove(T aggregate) => Set.Remove(aggregate);
}

public sealed class AgreementRepository(PayrollDbContext context)
    : EfRepository<SectorialJointAgreement>(context), IAgreementRepository;

public sealed class AffairRepository(PayrollDbContext context)
    : EfRepository<Affair>(context), IAffairRepository;

public sealed class SalaryTableRepository(PayrollDbContext context)
    : EfRepository<SalaryTable>(context), ISalaryTableRepository
{
    public async Task<IReadOnlyList<SalaryTable>> ListByAgreementAsync(int agreementId, CancellationToken cancellationToken = default) =>
        await Set.Where(t => t.AgreementId == agreementId).ToListAsync(cancellationToken).ConfigureAwait(false);
}
