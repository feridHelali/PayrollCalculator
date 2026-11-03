using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Infrastructure.Security;
using PayrollCalculator.Infrastructure.LegacyImport;
using PayrollCalculator.Infrastructure.Persistence;
using PayrollCalculator.Infrastructure.Persistence.Events;

namespace PayrollCalculator.Infrastructure;

/// <summary>Where the application database lives.</summary>
public sealed class DatabaseOptions
{
    public string DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PayrollCalculator", "payroll.db");
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton(options);
        return services.AddPersistence(o => o.UseSqlite($"Data Source={options.DatabasePath}"));
    }

    /// <summary>Registers the DbContext (with the given provider setup), repositories and importer.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<PayrollDbContext>(configure);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PayrollDbContext>());
        services.AddScoped<IAgreementRepository, AgreementRepository>();
        services.AddScoped<ISalaryTableRepository, SalaryTableRepository>();
        services.AddScoped<IAffairRepository, AffairRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILegalSourceRepository, LegalSourceRepository>();
        services.AddScoped<IRuleEventStore, RuleEventStore>();
        services.AddScoped<ICareerEventStore, CareerEventStore>();
        services.AddScoped<ILedgerSnapshotRepository, LedgerSnapshotRepository>();
        services.AddSingleton<ILedgerFingerprint>(new Sha256LedgerFingerprint());
        services.AddScoped<LegacyEventMigration>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ILegacyDataImporter, LegacyElectronImporter>();
        services.AddSingleton<IPasswordHasher>(new Pbkdf2PasswordHasher());
        return services;
    }

    /// <summary>Creates the database folder and applies pending migrations (which also seed the agreements).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var options = services.GetRequiredService<DatabaseOptions>();
        Directory.CreateDirectory(Path.GetDirectoryName(options.DatabasePath)!);
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database.MigrateAsync(ct).ConfigureAwait(false);
    }
}
