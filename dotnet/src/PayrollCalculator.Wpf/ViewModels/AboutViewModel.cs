using System.Globalization;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using PayrollCalculator.Application.Licensing;
using PayrollCalculator.Infrastructure;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>About page: credits, and the information support needs (version, build, runtime, database).</summary>
/// <remarks>
/// <see cref="SupportText"/> is a block meant to be pasted into an e-mail. It deliberately leaves out the user
/// name and anything else about who is signed in.
/// </remarks>
public sealed partial class AboutViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, DatabaseOptions database)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.About;

    public ProductInfo Product { get; } = ProductInfo.Current;

    public string DatabasePath { get; } = database.DatabasePath;

    /// <summary>Shown when the app folder can't be written and the data went to the user folder.</summary>
    public string? DataNote { get; } = database.IsFallback ? Labels.DataFallback : null;

    /// <summary>The masked serial and the activation date, or «غير مفعل».</summary>
    [ObservableProperty]
    public partial string LicenseText { get; private set; } = Labels.NotActivated;

    public Task LoadAsync() => TryAsync(async () =>
    {
        var license = await Runner.RunAsync<LicenseService, LicenseInfo?>(s => s.CurrentAsync()).ConfigureAwait(true);
        LicenseText = license is null
            ? Labels.NotActivated
            : license.MaskedSerial + "  (" + license.ActivatedAt.LocalDateTime.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ")";
        OnPropertyChanged(nameof(SupportText));
    });

    public string Runtime { get; } = RuntimeInformation.FrameworkDescription;

    public string OperatingSystem { get; } = RuntimeInformation.OSDescription;

    public string SupportText => string.Join(
        Environment.NewLine,
        Product.Name + " " + Product.DisplayVersion,
        "Idea: " + Labels.IdeaBy,
        "Development: " + Labels.DevelopedBy,
        "Runtime: " + Runtime,
        "OS: " + OperatingSystem,
        "Database: " + DatabasePath,
        "Licence: " + LicenseText);
}
