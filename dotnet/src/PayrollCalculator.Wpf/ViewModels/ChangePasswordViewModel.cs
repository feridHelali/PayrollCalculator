using CommunityToolkit.Mvvm.ComponentModel;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Users;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>The signed-in user changes their own password. Same rules as the forced change at sign-in.</summary>
/// <remarks>As on the sign-in window, passwords are method arguments, never properties.</remarks>
public sealed partial class ChangePasswordViewModel(
    IServiceRunner runner, IDialogService dialogs, StatusViewModel status, CurrentUser currentUser)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.ChangeMyPassword;

    public string UserName => currentUser.Session?.UserName ?? string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    /// <returns>True when the password was changed.</returns>
    public async Task<bool> ChangeAsync(string currentPassword, string newPassword, string confirmation)
    {
        ErrorMessage = string.Empty;
        try
        {
            await Runner.RunAsync<AuthenticationService>(a =>
                a.ChangePasswordAsync(UserName, currentPassword, newPassword, confirmation)).ConfigureAwait(true);
        }
        catch (UserException ex)
        {
            ErrorMessage = Labels.UserErrorText(ex);
            return false;
        }

        Status.Message = Labels.PasswordChanged;
        Dialogs.ShowInfo(Labels.PasswordChanged);
        return true;
    }
}
