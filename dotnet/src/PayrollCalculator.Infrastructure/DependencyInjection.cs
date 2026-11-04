using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Allowances;
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
    public const string FileName = "payroll.db";

    public string DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PayrollCalculator", "data", FileName);

    /// <summary>True when the folder beside the app could not be written and the per-user folder is used instead.</summary>
    public bool IsFallback { get; set; }

    /// <summary>The data folder: the database, its backups and the licence.</summary>
    public string DataDirectory => Path.GetDirectoryName(DatabasePath)!;

    /// <summary>
    /// The database of an installed or portable copy: <paramref name="overridePath"/> (<c>PAYROLL_DB_PATH</c>) when given,
    /// else <c>{app}\data\payroll.db</c>, else, when that folder can't be written (a copy under Program Files),
    /// <c>%LOCALAPPDATA%\PayrollCalculator\data\payroll.db</c>.
    /// </summary>
    public static DatabaseOptions Resolve(string? overridePath, string appDirectory, string localAppData, Func<string, bool>? canWrite = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return new DatabaseOptions { DatabasePath = Path.GetFullPath(overridePath) };
        }

        var beside = Path.Combine(appDirectory, "data");
        return (canWrite ?? CanWrite)(beside)
            ? new DatabaseOptions { DatabasePath = Path.Combine(beside, FileName) }
            : new DatabaseOptions { DatabasePath = Path.Combine(localAppData, "PayrollCalculator", "data", FileName), IsFallback = true };
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probe = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton(options);
        services.AddSingleton<Application.Licensing.ISerialRegistry, Licensing.EmbeddedSerialRegistry>();
        services.AddSingleton<Application.Licensing.ILicenseStore, Licensing.FileLicenseStore>();
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
        services.AddScoped<IAllowanceRepository, AllowanceRepository>();
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
    /// <summary>
    /// Creates or upgrades the database. Before upgrading an existing database (a migration pending on a database that
    /// already has some), it copies it to <c>data\backups\payroll-{yyyyMMdd-HHmmss}.db</c>.
    /// </summary>
    /// <returns>The backup made, or null.</returns>
    public static async Task<string?> MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var options = services.GetRequiredService<DatabaseOptions>();
        Directory.CreateDirectory(options.DataDirectory);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database;
        string? backup = null;
        if (File.Exists(options.DatabasePath)
            && (await db.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).Any()
            && (await db.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).Any())
        {
            backup = Backup(options);
        }

        await db.MigrateAsync(ct).ConfigureAwait(false);
        return backup;
    }

    private static string Backup(DatabaseOptions options)
    {
        var folder = Path.Combine(options.DataDirectory, "backups");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, $"payroll-{DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)}.db");
        using var source = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={options.DatabasePath};Pooling=False");
        using var copy = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={target};Pooling=False");
        source.Open();
        copy.Open();
        source.BackupDatabase(copy); // consistent copy, WAL included
        return target;
    }
}
