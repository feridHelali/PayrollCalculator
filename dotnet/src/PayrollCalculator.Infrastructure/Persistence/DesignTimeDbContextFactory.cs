using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PayrollCalculator.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> to create migrations; the runtime path comes from <see cref="DatabaseOptions"/>.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PayrollDbContext>
{
    public PayrollDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PayrollDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
