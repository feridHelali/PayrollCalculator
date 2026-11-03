using PayrollCalculator.Application.Users;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Tests;

public sealed class SecurityViewModelTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private Task<bool> SeedAsync() => _app.Get<IServiceRunner>().RunAsync<UserSeeder, bool>(s => s.SeedAsync());

    private async Task SignedInAsync()
    {
        await SeedAsync();
        var login = _app.Get<LoginViewModel>();
        login.UserName = UserSeeder.DefaultUserName;
        await login.SignInAsync(UserSeeder.DefaultPassword);
        Assert.True(await login.ChangePasswordAsync(UserSeeder.DefaultPassword, "Nouveau@2026", "Nouveau@2026"));
    }

    [Fact]
    public async Task Seeded_admin_must_change_the_password_before_entering()
    {
        await SeedAsync();
        var login = _app.Get<LoginViewModel>();
        login.UserName = "admin";

        Assert.False(await login.SignInAsync(UserSeeder.DefaultPassword));
        Assert.True(login.IsChangingPassword);
        Assert.False(_app.Get<CurrentUser>().IsAuthenticated);

        Assert.True(await login.ChangePasswordAsync(UserSeeder.DefaultPassword, "Nouveau@2026", "Nouveau@2026"));
        Assert.True(_app.Get<CurrentUser>().IsAuthenticated);
        Assert.False(login.HasError);
    }

    [Fact]
    public async Task Wrong_password_shows_one_arabic_message_for_both_cases()
    {
        await SeedAsync();
        var login = _app.Get<LoginViewModel>();

        login.UserName = "admin";
        Assert.False(await login.SignInAsync("wrong"));
        var wrongPassword = login.ErrorMessage;

        login.UserName = "ghost";
        Assert.False(await login.SignInAsync("wrong"));

        Assert.True(login.HasError);
        Assert.Equal(Labels.InvalidCredentials, wrongPassword);
        Assert.Equal(wrongPassword, login.ErrorMessage);
        Assert.False(login.IsChangingPassword);
    }

    [Fact]
    public async Task Password_change_errors_are_listed_in_arabic()
    {
        await SeedAsync();
        var login = _app.Get<LoginViewModel>();
        login.UserName = "admin";
        await login.SignInAsync(UserSeeder.DefaultPassword);

        Assert.False(await login.ChangePasswordAsync(UserSeeder.DefaultPassword, "abc", "abc"));

        Assert.Contains(Labels.PasswordTooShort, login.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains(Labels.PasswordTooSimple, login.ErrorMessage, StringComparison.Ordinal);
        Assert.True(login.IsChangingPassword);

        login.BackToSignInCommand.Execute(null);
        Assert.False(login.IsChangingPassword);
        Assert.False(login.HasError);
    }

    [Fact]
    public async Task Users_screen_adds_a_user_who_can_then_sign_in()
    {
        await SignedInAsync();
        var users = _app.Get<UsersViewModel>();
        await users.LoadAsync();

        users.NewUserName = "Sami";
        users.NewDisplayName = "سامي";
        users.GeneratePasswordCommand.Execute(null);
        var temporary = users.TempPassword;
        await users.AddCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        Assert.Equal(["admin", "sami"], users.Users.Select(u => u.UserName));
        Assert.Equal("sami", users.SelectedUser?.UserName);
        Assert.Equal(UserStatus.MustChangePassword, users.SelectedUser?.Status);
        Assert.Equal(string.Empty, users.NewUserName);

        var login = _app.Get<LoginViewModel>();
        login.UserName = "sami";
        await login.SignInAsync(temporary);
        Assert.True(login.IsChangingPassword);
        Assert.True(await login.ChangePasswordAsync(temporary, "Sami@Pass26", "Sami@Pass26"));
        Assert.Equal("سامي", _app.Get<CurrentUser>().Session?.DisplayName);
    }

    [Fact]
    public async Task Users_screen_shows_rule_violations_as_dialog_errors()
    {
        await SignedInAsync();
        var users = _app.Get<UsersViewModel>();
        await users.LoadAsync();

        users.NewUserName = "admin";
        users.NewDisplayName = "x";
        users.TempPassword = "Temp@2026x";
        await users.AddCommand.ExecuteAsync(null);

        Assert.Equal(Labels.UserNameTaken, Assert.Single(_app.Dialogs.Errors));
        Assert.Single(users.Users);
    }

    [Fact]
    public async Task Users_screen_resets_a_password()
    {
        await SignedInAsync();
        var users = _app.Get<UsersViewModel>();
        await users.LoadAsync();
        users.SelectedUser = users.Users[0];
        users.TempPassword = "Reset@2026";

        await users.ResetPasswordCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        Assert.Equal(UserStatus.MustChangePassword, users.Users[0].Status);
        var login = _app.Get<LoginViewModel>();
        login.UserName = "admin";
        await login.SignInAsync("Reset@2026");
        Assert.True(login.IsChangingPassword);
    }

    [Fact]
    public async Task Reset_without_a_selection_explains_why()
    {
        await SignedInAsync();
        var users = _app.Get<UsersViewModel>();
        await users.LoadAsync();
        users.SelectedUser = null;

        await users.ResetPasswordCommand.ExecuteAsync(null);

        Assert.Equal(Labels.SelectUser, Assert.Single(_app.Dialogs.Errors));
    }

    [Fact]
    public async Task Shell_tracks_the_active_section_the_user_and_sign_out()
    {
        await SignedInAsync();
        var shell = _app.Get<MainViewModel>();
        Assert.Equal(UserSeeder.DefaultDisplayName, shell.UserDisplayName);

        await shell.ShowUsersCommand.ExecuteAsync(null);
        Assert.Equal(NavSection.Users, shell.ActiveSection);
        Assert.True(shell.IsUsersActive);
        Assert.False(shell.IsAffairsActive);

        await shell.ShowAgreementsCommand.ExecuteAsync(null);
        Assert.Equal(NavSection.Agreements, shell.ActiveSection);

        shell.SignOutCommand.Execute(null);
        Assert.False(_app.Get<CurrentUser>().IsAuthenticated);
        Assert.Equal(string.Empty, shell.UserDisplayName);
    }
}
