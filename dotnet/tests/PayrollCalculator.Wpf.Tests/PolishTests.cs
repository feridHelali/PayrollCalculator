using PayrollCalculator.Application.Common;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Tests;

public sealed class PolishTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private static bool HasArabic(string text) => text.Any(c => c is >= '؀' and <= 'ۿ');

    [Fact]
    public void Every_domain_error_and_field_has_an_arabic_text()
    {
        foreach (var error in Enum.GetValues<DomainError>())
        {
            Assert.True(HasArabic(Labels.DomainErrorText(new DomainException(error, DomainField.Claimant, "x"))), error.ToString());
        }

        foreach (var field in Enum.GetValues<DomainField>().Where(f => f != DomainField.None))
        {
            Assert.True(HasArabic(Labels.FieldText(field)), field.ToString());
        }
    }

    [Fact]
    public void Messages_name_the_field_and_the_value()
    {
        Assert.Equal($"يجب إدخال {Labels.AgreementName}", Labels.DomainErrorText(new DomainException(DomainError.Required, DomainField.AgreementName)));
        Assert.Contains("C1", Labels.DomainErrorText(new DomainException(DomainError.Duplicate, DomainField.CategoryLabel, "C1")), StringComparison.Ordinal);
        Assert.Equal(Labels.NotFound, Labels.ErrorText(new NotFoundException("Agreement", 3)));
    }

    [Fact]
    public async Task Screens_show_rule_violations_in_arabic()
    {
        var agreements = _app.Get<AgreementsViewModel>();
        await agreements.LoadAsync();
        agreements.NewCommand.Execute(null);
        agreements.EditName = "  ";

        await agreements.SaveCommand.ExecuteAsync(null);

        Assert.Equal($"يجب إدخال {Labels.AgreementName}", Assert.Single(_app.Dialogs.Errors));
    }

    private async Task<string> SignedInAsync()
    {
        await _app.Get<IServiceRunner>().RunAsync<UserSeeder, bool>(s => s.SeedAsync());
        var login = _app.Get<LoginViewModel>();
        login.UserName = UserSeeder.DefaultUserName;
        await login.SignInAsync(UserSeeder.DefaultPassword);
        await login.ChangePasswordAsync(UserSeeder.DefaultPassword, "First@2026", "First@2026");
        return "First@2026";
    }

    [Fact]
    public async Task Signed_in_user_changes_their_own_password()
    {
        var current = await SignedInAsync();
        var page = _app.Get<ChangePasswordViewModel>();

        Assert.True(await page.ChangeAsync(current, "Second@2026", "Second@2026"));
        Assert.Equal(Labels.PasswordChanged, Assert.Single(_app.Dialogs.Infos));
        Assert.True(_app.Get<CurrentUser>().IsAuthenticated);

        _app.Get<CurrentUser>().SignOut();
        var login = _app.Get<LoginViewModel>();
        login.UserName = "admin";
        Assert.True(await login.SignInAsync("Second@2026"));
    }

    [Fact]
    public async Task Own_password_change_reports_errors_in_arabic()
    {
        await SignedInAsync();
        var page = _app.Get<ChangePasswordViewModel>();

        Assert.False(await page.ChangeAsync("wrong", "Second@2026", "Second@2026"));
        Assert.Equal(Labels.WrongCurrentPassword, page.ErrorMessage);

        Assert.False(await page.ChangeAsync("First@2026", "Second@2026", "Other@2026"));
        Assert.Equal(Labels.PasswordConfirmationMismatch, page.ErrorMessage);
        Assert.True(_app.Get<CurrentUser>().IsAuthenticated);
    }

    [Fact]
    public async Task Change_password_page_is_reached_from_the_shell()
    {
        await SignedInAsync();
        var shell = _app.Get<MainViewModel>();

        await shell.ShowChangePasswordCommand.ExecuteAsync(null);

        Assert.IsType<ChangePasswordViewModel>(shell.CurrentPage);
        Assert.Equal("admin", ((ChangePasswordViewModel)shell.CurrentPage!).UserName);
    }
}
