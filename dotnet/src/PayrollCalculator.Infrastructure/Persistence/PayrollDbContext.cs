using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Infrastructure.Persistence;

public sealed class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<SectorialJointAgreement> Agreements => Set<SectorialJointAgreement>();

    public DbSet<SalaryTable> SalaryTables => Set<SalaryTable>();

    public DbSet<Affair> Affairs => Set<Affair>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayrollDbContext).Assembly);
    }
}
