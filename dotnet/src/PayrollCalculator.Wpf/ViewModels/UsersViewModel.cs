using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>User management: the list, adding a user, resetting a password.</summary>
/// <remarks>
/// The temporary password is an ordinary visible field on purpose: the administrator has to read it to the
/// user, and it stops working once the user has chosen their own.
/// </remarks>
public sealed partial class UsersViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Users;

    public ObservableCollection<UserDto> Users { get; } = [];

    [ObservableProperty]
    public partial UserDto? SelectedUser { get; set; }

    [ObservableProperty]
    public partial string NewUserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TempPassword { get; set; } = string.Empty;

    public Task LoadAsync() => TryAsync(() => ReloadAsync(null));

    [RelayCommand]
    private void GeneratePassword() => TempPassword = TemporaryPassword.Generate();

    [RelayCommand]
    private async Task AddAsync() => await TryAsync(async () =>
    {
        var id = await Runner.RunAsync<UserService, int>(s => s.CreateAsync(NewUserName, NewDisplayName, TempPassword)).ConfigureAwait(true);
        NewUserName = NewDisplayName = TempPassword = string.Empty;
        await ReloadAsync(id).ConfigureAwait(true);
    }, Labels.UserAdded).ConfigureAwait(true);

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is not { } selected)
        {
            Dialogs.ShowError(Labels.SelectUser);
            return;
        }

        if (!Dialogs.Confirm(Labels.ConfirmReset))
        {
            return;
        }

        await TryAsync(async () =>
        {
            await Runner.RunAsync<UserService>(s => s.ResetPasswordAsync(selected.Id, TempPassword)).ConfigureAwait(true);
            TempPassword = string.Empty;
            await ReloadAsync(selected.Id).ConfigureAwait(true);
        }, Labels.PasswordWasReset).ConfigureAwait(true);
    }

    private async Task ReloadAsync(int? select)
    {
        var items = await Runner.RunAsync<UserService, IReadOnlyList<UserDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Users.Clear();
        foreach (var item in items)
        {
            Users.Add(item);
        }

        SelectedUser = Users.FirstOrDefault(u => u.Id == select);
    }
}
