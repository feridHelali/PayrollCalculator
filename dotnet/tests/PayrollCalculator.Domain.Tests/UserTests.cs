using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Domain.Tests;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.FromHours(1));

    private static User NewUser() => User.Register("  Lazhar.B ", " لزهر ", "hash-1");

    [Fact]
    public void Register_normalises_the_user_name_and_forces_a_password_change()
    {
        var user = NewUser();

        Assert.Equal("lazhar.b", user.UserName);
        Assert.Equal("لزهر", user.DisplayName);
        Assert.Equal("hash-1", user.PasswordHash);
        Assert.True(user.MustChangePassword);
        Assert.Equal(0, user.FailedAttempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData("لزهر")]
    [InlineData("a_very_long_user_name_that_is_over_32")]
    public void Invalid_user_names_are_rejected(string userName)
    {
        var ex = Assert.Throws<UserException>(() => User.Register(userName, "x", "h"));
        Assert.Equal(UserError.UserNameInvalid, ex.Error);
    }

    [Fact]
    public void Display_name_is_required()
    {
        var ex = Assert.Throws<UserException>(() => User.Register("ali", " ", "h"));
        Assert.Equal(UserError.DisplayNameRequired, ex.Error);
    }

    [Fact]
    public void TryNormalizeUserName_never_throws()
    {
        Assert.True(User.TryNormalizeUserName(" Admin ", out var normalized));
        Assert.Equal("admin", normalized);
        Assert.False(User.TryNormalizeUserName("bad name", out _));
        Assert.False(User.TryNormalizeUserName(null, out _));
    }

    [Fact]
    public void A_correct_password_on_a_temporary_account_requires_a_change()
    {
        var user = NewUser();
        Assert.Equal(SignInOutcome.MustChangePassword, user.RecordSignIn(passwordMatched: true, Now));
        Assert.Equal(Now, user.LastSignedInOn);
    }

    [Fact]
    public void A_correct_password_succeeds_once_the_password_was_changed()
    {
        var user = NewUser();
        user.ChangePassword("hash-2", Now);

        Assert.False(user.MustChangePassword);
        Assert.Equal(SignInOutcome.Succeeded, user.RecordSignIn(true, Now));
    }

    [Fact]
    public void Five_failures_in_a_row_lock_the_account_for_fifteen_minutes()
    {
        var user = NewUser();
        for (var i = 1; i < User.MaxFailedAttempts; i++)
        {
            Assert.Equal(SignInOutcome.InvalidCredentials, user.RecordSignIn(false, Now));
        }

        Assert.Equal(SignInOutcome.LockedOut, user.RecordSignIn(false, Now));
        Assert.True(user.IsLockedAt(Now));

        // Even the right password is refused while locked.
        Assert.Equal(SignInOutcome.LockedOut, user.RecordSignIn(true, Now.AddMinutes(14)));

        // The lockout is temporary.
        Assert.False(user.IsLockedAt(Now + User.LockoutDuration));
        Assert.Equal(SignInOutcome.MustChangePassword, user.RecordSignIn(true, Now + User.LockoutDuration));
        Assert.Equal(0, user.FailedAttempts);
    }

    [Fact]
    public void A_success_resets_the_failure_counter()
    {
        var user = NewUser();
        user.RecordSignIn(false, Now);
        user.RecordSignIn(false, Now);
        user.RecordSignIn(true, Now);

        Assert.Equal(0, user.FailedAttempts);
    }

    [Fact]
    public void Reset_sets_a_temporary_password_and_unlocks()
    {
        var user = NewUser();
        user.ChangePassword("hash-2", Now);
        for (var i = 0; i < User.MaxFailedAttempts; i++)
        {
            user.RecordSignIn(false, Now);
        }

        user.ResetPassword("hash-3");

        Assert.Equal("hash-3", user.PasswordHash);
        Assert.True(user.MustChangePassword);
        Assert.False(user.IsLockedAt(Now));
        Assert.Equal(0, user.FailedAttempts);
    }

    [Fact]
    public void Hashes_are_required()
    {
        Assert.ThrowsAny<Common.DomainException>(() => User.Register("ali", "Ali", " "));
        var user = NewUser();
        Assert.ThrowsAny<Common.DomainException>(() => user.ChangePassword("", Now));
        Assert.ThrowsAny<Common.DomainException>(() => user.ResetPassword(""));
    }
}
