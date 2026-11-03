using System.Runtime.InteropServices;
using PayrollCalculator.Infrastructure;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>About page: credits, and the information support needs (version, build, runtime, database).</summary>
/// <remarks>
/// <see cref="SupportText"/> is a block meant to be pasted into an e-mail. It deliberately leaves out the user
/// name and anything else about who is signed in.
/// </remarks>
public sealed class AboutViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, DatabaseOptions database)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.About;

    public ProductInfo Product { get; } = ProductInfo.Current;

    public string DatabasePath { get; } = database.DatabasePath;

    public string Runtime { get; } = RuntimeInformation.FrameworkDescription;

    public string OperatingSystem { get; } = RuntimeInformation.OSDescription;

    public string SupportText => string.Join(
        Environment.NewLine,
        Product.Name + " " + Product.DisplayVersion,
        "Idea: " + Labels.IdeaBy,
        "Development: " + Labels.DevelopedBy,
        "Runtime: " + Runtime,
        "OS: " + OperatingSystem,
        "Database: " + DatabasePath);
}
