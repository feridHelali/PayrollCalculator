using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayrollCalculator.Application;
using PayrollCalculator.Infrastructure;
using PayrollCalculator.Infrastructure.Persistence;
using PayrollCalculator.Wpf;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.Tests;

/// <summary>Full composition root (Application + EF Core on in-memory SQLite) with a scripted dialog service.</summary>
internal sealed class TestApp : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public TestApp()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddPersistence(o => o.UseSqlite(_connection));
        services.AddPresentation();
        services.AddSingleton<IDialogService>(Dialogs);
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database.Migrate();
    }

    public FakeDialogs Dialogs { get; } = new();

    public T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}

internal sealed class FakeDialogs : IDialogService
{
    public bool ConfirmAnswer { get; set; } = true;

    public string? FileToOpen { get; set; }

    public string? FileToSave { get; set; }

    public List<string> Errors { get; } = [];

    public List<string> Infos { get; } = [];

    public bool Confirm(string message) => ConfirmAnswer;

    public void ShowError(string message) => Errors.Add(message);

    public void ShowInfo(string message) => Infos.Add(message);

    public string? OpenFile(string filter) => FileToOpen;

    public string? SaveFile(string filter, string defaultFileName) => FileToSave;
}
