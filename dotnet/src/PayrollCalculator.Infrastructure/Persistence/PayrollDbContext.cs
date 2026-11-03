using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Infrastructure.Persistence.Events;

namespace PayrollCalculator.Infrastructure.Persistence;

public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<SectorialJointAgreement> Agreements => Set<SectorialJointAgreement>();

    public DbSet<SalaryTable> SalaryTables => Set<SalaryTable>();

    public DbSet<Affair> Affairs => Set<Affair>();

    public DbSet<Allowance> Allowances => Set<Allowance>();

    public DbSet<User> Users => Set<User>();

    public DbSet<LegalSource> LegalSources => Set<LegalSource>();

    public DbSet<RecordedRuleEvent> RuleEvents => Set<RecordedRuleEvent>();

    public DbSet<LedgerSnapshot> LedgerSnapshots => Set<LedgerSnapshot>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RuleEventStore.GuardImmutability(ChangeTracker);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RuleEventStore.GuardImmutability(ChangeTracker);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayrollDbContext).Assembly);
    }
}
