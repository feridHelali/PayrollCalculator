using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Users;

/// <summary>What went wrong with a user operation. The UI turns each code into a message; the domain holds no UI text.</summary>
public enum UserError
{
    UserNameInvalid,
    DisplayNameRequired,
    UserNameTaken,
    PasswordRejected,
    WrongCurrentPassword,
    PasswordUnchanged,
    AccountLocked,
    PasswordConfirmationMismatch,
}

/// <summary>A rule about one password that a candidate breaks.</summary>
public enum PasswordViolation
{
    TooShort,
    TooSimple,
    SameAsUserName,
}

/// <summary>A user or password rule was broken. Carries a code rather than a sentence.</summary>
public sealed class UserException : DomainException
{
    public UserException(UserError error, IReadOnlyList<PasswordViolation>? violations = null)
        : base(Describe(error, violations))
    {
        Error = error;
        Violations = violations ?? [];
    }

    public UserException() : this(UserError.UserNameInvalid) { }

    public UserException(string message) : base(message) => Violations = [];

    public UserException(string message, Exception innerException) : base(message, innerException) => Violations = [];

    /// <summary>The user-specific code; it takes the place of <see cref="DomainException.Error"/> (left <c>Unspecified</c>).</summary>
    public new UserError Error { get; }

    /// <summary>Filled when <see cref="Error"/> is <see cref="UserError.PasswordRejected"/>.</summary>
    public IReadOnlyList<PasswordViolation> Violations { get; }

    private static string Describe(UserError error, IReadOnlyList<PasswordViolation>? violations) =>
        violations is { Count: > 0 } ? $"{error}: {string.Join(", ", violations)}" : error.ToString();
}
