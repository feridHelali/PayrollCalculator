using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Domain.Users;

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
    : EfRepository<SectorialJointAgreement>(context), IAgreementRepository
{
    public Task<SectorialJointAgreement?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
}

public sealed class AllowanceRepository(PayrollDbContext context)
    : EfRepository<Allowance>(context), IAllowanceRepository
{
    public async Task<IReadOnlyList<Allowance>> ListByAgreementAsync(int? agreementId, CancellationToken cancellationToken = default) =>
        await Set.Where(a => agreementId == null || a.AgreementId == agreementId).ToListAsync(cancellationToken).ConfigureAwait(false);
}

public sealed class AffairRepository(PayrollDbContext context)
    : EfRepository<Affair>(context), IAffairRepository;

public sealed class UserRepository(PayrollDbContext context)
    : EfRepository<User>(context), IUserRepository
{
    public Task<User?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(u => u.UserName == normalizedUserName, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Set.AnyAsync(cancellationToken);
}

public sealed class SalaryTableRepository(PayrollDbContext context)
    : EfRepository<SalaryTable>(context), ISalaryTableRepository
{
    public async Task<IReadOnlyList<SalaryTable>> ListByAgreementAsync(int agreementId, CancellationToken cancellationToken = default) =>
        await Set.Where(t => t.AgreementId == agreementId).ToListAsync(cancellationToken).ConfigureAwait(false);
}
