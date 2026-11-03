using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application.Tests;

public sealed class AuthenticationServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly FakeHasher _hasher = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.Zero));
    private readonly CurrentUser _currentUser = new();

    private AuthenticationService Service() =>
        new(_store.Users, _store, _hasher, PasswordPolicy.Default, _clock, _currentUser);

    private async Task<User> UserAsync(string password = "Temp@2026", bool temporary = false)
    {
        var user = User.Register("lazhar", "لزهر", _hasher.Hash(password));
        if (!temporary)
        {
            user.ChangePassword(_hasher.Hash(password), _clock.Now);
        }

        _store.Users.Add(user);
        await _store.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Right_credentials_sign_in_and_open_the_session()
    {
        var user = await UserAsync("Good@2026");

        var outcome = await Service().SignInAsync(" LAZHAR ", "Good@2026");

        Assert.Equal(SignInOutcome.Succeeded, outcome);
        Assert.True(_currentUser.IsAuthenticated);
        Assert.Equal(new UserSession(user.Id, "lazhar", "لزهر"), _currentUser.Session);
    }

    [Fact]
    public async Task Wrong_password_is_refused_and_the_attempt_is_saved()
    {
        await UserAsync("Good@2026");
        var saves = _store.SaveCount;

        var outcome = await Service().SignInAsync("lazhar", "nope");

        Assert.Equal(SignInOutcome.InvalidCredentials, outcome);
        Assert.False(_currentUser.IsAuthenticated);
        Assert.Equal(1, _store.Users.Items[0].FailedAttempts);
        Assert.Equal(saves + 1, _store.SaveCount);
    }

    [Theory]
    [InlineData("ghost")]
    [InlineData("not a valid name")]
    [InlineData(null)]
    public async Task Unknown_user_gives_the_same_answer_and_still_hashes(string? userName)
    {
        await UserAsync();
        var before = _hasher.HashCalls;

        var outcome = await Service().SignInAsync(userName, "whatever");

        Assert.Equal(SignInOutcome.InvalidCredentials, outcome);
        Assert.Equal(before + 1, _hasher.HashCalls);
    }

    [Fact]
    public async Task Temporary_password_asks_for_a_change_without_opening_the_session()
    {
        await UserAsync("Temp@2026", temporary: true);

        var outcome = await Service().SignInAsync("lazhar", "Temp@2026");

        Assert.Equal(SignInOutcome.MustChangePassword, outcome);
        Assert.False(_currentUser.IsAuthenticated);
    }

    [Fact]
    public async Task Lockout_after_five_failures()
    {
        await UserAsync("Good@2026");
        var service = Service();
        for (var i = 1; i < User.MaxFailedAttempts; i++)
        {
            await service.SignInAsync("lazhar", "bad");
        }

        Assert.Equal(SignInOutcome.LockedOut, await service.SignInAsync("lazhar", "bad"));
        Assert.Equal(SignInOutcome.LockedOut, await service.SignInAsync("lazhar", "Good@2026"));

        _clock.Advance(User.LockoutDuration);
        Assert.Equal(SignInOutcome.Succeeded, await service.SignInAsync("lazhar", "Good@2026"));
    }

    [Fact]
    public async Task Change_password_makes_it_permanent_and_opens_the_session()
    {
        await UserAsync("Temp@2026", temporary: true);

        await Service().ChangePasswordAsync("lazhar", "Temp@2026", "New@Pass2026", "New@Pass2026");

        var user = _store.Users.Items[0];
        Assert.Equal("hashed:New@Pass2026", user.PasswordHash);
        Assert.False(user.MustChangePassword);
        Assert.True(_currentUser.IsAuthenticated);
        Assert.Equal(SignInOutcome.Succeeded, await Service().SignInAsync("lazhar", "New@Pass2026"));
    }

    [Fact]
    public async Task Change_password_checks_the_current_password()
    {
        await UserAsync("Temp@2026", temporary: true);

        var ex = await Assert.ThrowsAsync<UserException>(() =>
            Service().ChangePasswordAsync("lazhar", "wrong", "New@Pass2026", "New@Pass2026"));

        Assert.Equal(UserError.WrongCurrentPassword, ex.Error);
        Assert.Equal(1, _store.Users.Items[0].FailedAttempts);
        Assert.False(_currentUser.IsAuthenticated);
    }

    [Fact]
    public async Task Change_password_refuses_a_locked_account()
    {
        var user = await UserAsync("Temp@2026", temporary: true);
        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            user.RecordSignIn(false, _clock.Now);
        }

        var ex = await Assert.ThrowsAsync<UserException>(() =>
            Service().ChangePasswordAsync("lazhar", "Temp@2026", "New@Pass2026", "New@Pass2026"));
        Assert.Equal(UserError.AccountLocked, ex.Error);
    }

    [Fact]
    public async Task Change_password_enforces_the_policy()
    {
        await UserAsync("Temp@2026", temporary: true);

        var ex = await Assert.ThrowsAsync<UserException>(() =>
            Service().ChangePasswordAsync("lazhar", "Temp@2026", "short", "short"));

        Assert.Equal(UserError.PasswordRejected, ex.Error);
        Assert.Contains(PasswordViolation.TooShort, ex.Violations);
        Assert.True(_store.Users.Items[0].MustChangePassword);
    }

    [Fact]
    public async Task Change_password_requires_the_confirmation_to_match()
    {
        await UserAsync("Temp@2026", temporary: true);

        var ex = await Assert.ThrowsAsync<UserException>(() =>
            Service().ChangePasswordAsync("lazhar", "Temp@2026", "New@Pass2026", "New@Pass2027"));
        Assert.Equal(UserError.PasswordConfirmationMismatch, ex.Error);
    }

    [Fact]
    public async Task Change_password_refuses_the_same_password()
    {
        await UserAsync("Temp@2026", temporary: true);

        var ex = await Assert.ThrowsAsync<UserException>(() =>
            Service().ChangePasswordAsync("lazhar", "Temp@2026", "Temp@2026", "Temp@2026"));
        Assert.Equal(UserError.PasswordUnchanged, ex.Error);
    }

    [Fact]
    public void Sign_out_closes_the_session_and_notifies()
    {
        var changes = 0;
        _currentUser.Changed += (_, _) => changes++;
        _currentUser.SignIn(new UserSession(1, "a", "A"));
        _currentUser.SignOut();

        Assert.False(_currentUser.IsAuthenticated);
        Assert.Equal(2, changes);
    }
}
