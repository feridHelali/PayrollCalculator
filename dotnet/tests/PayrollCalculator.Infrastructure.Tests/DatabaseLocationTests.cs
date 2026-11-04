using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>Sprint 08: the database lives in data\ beside the app, and an upgrade is preceded by a backup.</summary>
public sealed class DatabaseLocationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "payroll-location-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void The_database_is_in_the_data_folder_beside_the_app()
    {
        var options = DatabaseOptions.Resolve(null, Path.Combine(_root, "app"), Path.Combine(_root, "local"));

        Assert.Equal(Path.Combine(_root, "app", "data", "payroll.db"), options.DatabasePath);
        Assert.False(options.IsFallback);
        Assert.True(Directory.Exists(Path.Combine(_root, "app", "data")));
    }

    [Fact]
    public void The_environment_variable_wins_and_a_read_only_app_folder_falls_back_to_the_user_folder()
    {
        var custom = DatabaseOptions.Resolve(Path.Combine(_root, "x.db"), Path.Combine(_root, "app"), Path.Combine(_root, "local"));
        var fallback = DatabaseOptions.Resolve(" ", Path.Combine(_root, "app"), Path.Combine(_root, "local"), canWrite: _ => false);

        Assert.Equal(Path.Combine(_root, "x.db"), custom.DatabasePath);
        Assert.Equal((Path.Combine(_root, "local", "PayrollCalculator", "data", "payroll.db"), true), (fallback.DatabasePath, fallback.IsFallback));
    }

    [Fact]
    public async Task A_new_database_is_created_without_a_backup_and_an_upgrade_is_backed_up_first()
    {
        var options = new DatabaseOptions { DatabasePath = Path.Combine(_root, "data", "payroll.db") };
        await using var services = new ServiceCollection().AddInfrastructure(options).BuildServiceProvider();

        Assert.Null(await services.MigrateDatabaseAsync());
        Assert.Null(await services.MigrateDatabaseAsync()); // up to date: nothing to back up

        // Pretend the database comes from an older version: one applied migration that isn't the current one.
        await using (var connection = new SqliteConnection($"Data Source={options.DatabasePath};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE __EFMigrationsHistory SET MigrationId = '20000101000000_Old'";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAnyAsync<Exception>(() => services.MigrateDatabaseAsync()); // the schema exists already
        var backup = Assert.Single(Directory.GetFiles(Path.Combine(_root, "data", "backups"), "payroll-*.db"));
        Assert.True(new FileInfo(backup).Length > 0);
    }
}
