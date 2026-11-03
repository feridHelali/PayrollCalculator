using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Domain.Tests;

public class PasswordPolicyTests
{
    private readonly PasswordPolicy _policy = PasswordPolicy.Default;

    [Theory]
    [InlineData("Admin@2026")]
    [InlineData("salaire2026!")]
    [InlineData("ABCdef123")]
    [InlineData("كلمة-سر-2026")] // Arabic letters count as letters; '-' and digits complete the classes.
    public void Acceptable_passwords(string password)
    {
        Assert.Empty(_policy.Check(password, "admin"));
    }

    [Fact]
    public void Too_short()
    {
        Assert.Contains(PasswordViolation.TooShort, _policy.Check("Ab1!", "admin"));
    }

    [Theory]
    [InlineData("abcdefghij")]
    [InlineData("1234567890")]
    [InlineData("abcdef1234")]
    public void Too_simple(string password)
    {
        Assert.Equal([PasswordViolation.TooSimple], _policy.Check(password, "admin"));
    }

    [Fact]
    public void Must_differ_from_the_user_name()
    {
        Assert.Contains(PasswordViolation.SameAsUserName, _policy.Check("Lazhar.2026", "lazhar.2026"));
    }

    [Fact]
    public void Null_is_too_short_and_too_simple()
    {
        Assert.Equal([PasswordViolation.TooShort, PasswordViolation.TooSimple], _policy.Check(null, "admin"));
    }

    [Fact]
    public void Enforce_throws_with_every_violation()
    {
        var ex = Assert.Throws<UserException>(() => _policy.Enforce("abc", "admin"));
        Assert.Equal(UserError.PasswordRejected, ex.Error);
        Assert.Equal([PasswordViolation.TooShort, PasswordViolation.TooSimple], ex.Violations);
    }
}
