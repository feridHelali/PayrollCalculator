using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>The sign-in window: sign in, then (for a temporary password) choose a new one, in the same window.</summary>
/// <remarks>
/// Passwords are never properties. <c>PasswordBox</c> deliberately has no bindable value, so the plaintext does
/// not live as long as the view model; the view passes each password as an argument and clears the box.
/// </remarks>
public sealed partial class LoginViewModel(IServiceRunner runner) : ObservableObject
{
    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Second step: the password was temporary and must be replaced.</summary>
    [ObservableProperty]
    public partial bool IsChangingPassword { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>True on the run that created the default account: its credentials are shown under the form.</summary>
    [ObservableProperty]
    public partial bool IsFirstRun { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    /// <returns>True when the session is open and the main window can be shown.</returns>
    public Task<bool> SignInAsync(string password) => RunAsync(async () =>
    {
        var outcome = await runner.RunAsync<AuthenticationService, SignInOutcome>(a => a.SignInAsync(UserName, password)).ConfigureAwait(true);
        if (outcome == SignInOutcome.MustChangePassword)
        {
            IsChangingPassword = true;
            return false;
        }

        ErrorMessage = Labels.SignInOutcomeText(outcome);
        return outcome == SignInOutcome.Succeeded;
    });

    /// <returns>True when the password was replaced and the session is open.</returns>
    public Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, string confirmation) => RunAsync(async () =>
    {
        await runner.RunAsync<AuthenticationService>(a => a.ChangePasswordAsync(UserName, currentPassword, newPassword, confirmation)).ConfigureAwait(true);
        return true;
    });

    [RelayCommand]
    private void BackToSignIn()
    {
        IsChangingPassword = false;
        ErrorMessage = string.Empty;
    }

    private async Task<bool> RunAsync(Func<Task<bool>> action)
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            return await action().ConfigureAwait(true);
        }
        catch (UserException ex)
        {
            ErrorMessage = Labels.UserErrorText(ex);
            return false;
        }
        catch (Exception ex) when (ex is DbUpdateException or IOException or InvalidOperationException)
        {
            ErrorMessage = $"{Labels.Error}: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
