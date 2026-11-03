using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayrollCalculator.Application;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Infrastructure;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;
using PayrollCalculator.Wpf.Views;

namespace PayrollCalculator.Wpf;

/// <summary>Composition root and start-up sequence: splash (database → first account → import) → sign-in → main window.</summary>
/// <remarks>
/// <c>ShutdownMode</c> is <c>OnExplicitShutdown</c> while no main window is on screen: the sign-in window opens and
/// closes before (and, after a sign-out, instead of) the main window, and the default mode would end the process
/// as soon as it closed.
/// </remarks>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private bool _isFirstRun;

    private IServiceProvider Services => _host!.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;
        var splash = new SplashWindow();
        splash.Show();

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
            splash.Report(StartupStage.Database);
            await Services.MigrateDatabaseAsync().ConfigureAwait(true);

            splash.Report(StartupStage.Accounts);
            await using (var scope = Services.CreateAsyncScope())
            {
                if (await scope.ServiceProvider.GetRequiredService<UserSeeder>().SeedAsync().ConfigureAwait(true))
                {
                    _isFirstRun = true;
                }
            }

            if (LegacyPathArgument(e.Args) is { } legacyPath)
            {
                splash.Report(StartupStage.LegacyImport);
                importNotice = await ImportLegacyAsync(legacyPath).ConfigureAwait(true);
            }

            await splash.CompleteAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            splash.Close();
            MessageBox.Show(ex.Message, Labels.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        if (!SignIn())
        {
            Shutdown();
            return;
        }

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        await ShowMainWindowAsync().ConfigureAwait(true);
        if (importNotice is not null)
        {
            Services.GetRequiredService<StatusViewModel>().Message = importNotice;
        }

        Services.GetRequiredService<CurrentUser>().Changed += OnSessionChanged;
    }

    /// <summary>Shows the modal sign-in window. True once a session is open.</summary>
    private bool SignIn()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var viewModel = Services.GetRequiredService<LoginViewModel>();
        viewModel.IsFirstRun = _isFirstRun;
        var signedIn = new LoginWindow(viewModel).ShowDialog() == true;
        if (signedIn)
        {
            _isFirstRun = false;
        }

        return signedIn;
    }

    private async Task ShowMainWindowAsync()
    {
        MainWindow!.Show();
        MainWindow.Activate();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        await Services.GetRequiredService<MainViewModel>().ShowAffairsCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    /// <summary>After a sign-out: hide the data, ask for credentials again, or quit.</summary>
    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        if (Services.GetRequiredService<CurrentUser>().IsAuthenticated)
        {
            return;
        }

        MainWindow!.Hide();
        if (SignIn())
        {
            await ShowMainWindowAsync().ConfigureAwait(true);
        }
        else
        {
            Shutdown();
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
        await using var scope = Services.CreateAsyncScope();
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
