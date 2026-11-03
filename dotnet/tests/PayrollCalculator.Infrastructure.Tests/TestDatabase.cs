using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>A migrated, private in-memory SQLite database. Each <see cref="NewContext"/> is a fresh unit of work.</summary>
internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.Migrate();
    }

    public PayrollDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PayrollDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
