using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayrollCalculator.Application;
using PayrollCalculator.Infrastructure;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf;

/// <summary>Composition root.</summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        var options = new DatabaseOptions();
        if (Environment.GetEnvironmentVariable("PAYROLL_DB_PATH") is { Length: > 0 } customPath)
        {
            options.DatabasePath = Path.GetFullPath(customPath);
        }

        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Services
            .AddApplication()
            .AddInfrastructure(options)
            .AddPresentation()
            .AddSingleton<MainWindow>();
        _host = builder.Build();

        string? importNotice = null;
        try
        {
            await _host.Services.MigrateDatabaseAsync().ConfigureAwait(true);
            if (LegacyPathArgument(e.Args) is { } legacyPath)
            {
                importNotice = await ImportLegacyAsync(legacyPath).ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            MessageBox.Show(ex.Message, Labels.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        await _host.Services.GetRequiredService<MainViewModel>().ShowAffairsCommand.ExecuteAsync(null).ConfigureAwait(true);
        if (importNotice is not null)
        {
            _host.Services.GetRequiredService<StatusViewModel>().Message = importNotice;
        }
    }

    /// <summary>Supports <c>--import-legacy &lt;path-to-payroll.sqlite&gt;</c> for a one-shot migration from the Electron app.</summary>
    private static string? LegacyPathArgument(string[] args)
    {
        var index = Array.IndexOf(args, "--import-legacy");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private async Task<string> ImportLegacyAsync(string legacyPath)
    {
        await using var scope = _host!.Services.CreateAsyncScope();
        var report = await scope.ServiceProvider.GetRequiredService<PayrollCalculator.Application.LegacyImport.ILegacyDataImporter>()
            .ImportAsync(legacyPath).ConfigureAwait(true);
        return $"{Labels.ImportDone}: {report.Agreements} / {report.SalaryTables} / {report.Affairs} ({report.Warnings.Count} ⚠)";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, Labels.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
