using CommunityToolkit.Mvvm.ComponentModel;
using PayrollCalculator.Application.Licensing;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>The activation window, shown before the sign-in while this copy has no valid serial.</summary>
public sealed partial class ActivationViewModel(IServiceRunner runner) : ObservableObject
{
    [ObservableProperty]
    public partial string Serial { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    /// <returns>True when the copy is already activated (the window isn't needed).</returns>
    public async Task<bool> IsActivatedAsync() =>
        await runner.RunAsync<LicenseService, LicenseInfo?>(s => s.CurrentAsync()).ConfigureAwait(true) is not null;

    /// <returns>True once the serial is accepted and saved.</returns>
    public async Task<bool> ActivateAsync()
    {
        var outcome = await runner.RunAsync<LicenseService, ActivationOutcome>(s => s.ActivateAsync(Serial)).ConfigureAwait(true);
        ErrorMessage = Labels.ActivationOutcomeText(outcome);
        return outcome == ActivationOutcome.Activated;
    }
}
