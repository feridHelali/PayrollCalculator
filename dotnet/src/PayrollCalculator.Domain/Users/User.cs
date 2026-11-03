using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Users;

/// <summary>The result of one sign-in attempt.</summary>
public enum SignInOutcome
{
    Succeeded,

    /// <summary>The credentials are right, but the password is temporary and must be changed first.</summary>
    MustChangePassword,

    /// <summary>Unknown user or wrong password. Deliberately the same outcome for both.</summary>
    InvalidCredentials,

    LockedOut,
}

/// <summary>Someone who may sign in to the application.</summary>
/// <remarks>
/// The aggregate owns the lockout counter and the sign-in decision. It never sees a plaintext password:
/// the caller compares it through <see cref="IPasswordHasher"/> and passes in the result.
/// </remarks>
public sealed class User : Entity
{
    public const int MaxUserNameLength = 32;
    public const int MaxDisplayNameLength = 100;

    /// <summary>Failures in a row before the account locks.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>A timed lockout: a permanent one would let anyone block a colleague by guessing.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User(string userName, string displayName, string passwordHash)
    {
        UserName = userName;
        DisplayName = displayName;
        PasswordHash = passwordHash;
    }

    /// <summary>Sign-in identifier, stored lowercase so comparisons never depend on collation.</summary>
    public string UserName { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>A self-describing hash produced by <see cref="IPasswordHasher"/>. Never the password.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>Set by registration and by a reset: the next sign-in must choose a new password.</summary>
    public bool MustChangePassword { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? LastSignedInOn { get; private set; }

    /// <summary>Creates a user with a temporary password that must be changed at the first sign-in.</summary>
    public static User Register(string? userName, string? displayName, string temporaryPasswordHash)
    {
        if (!TryNormalizeUserName(userName, out var normalized))
        {
            throw new UserException(UserError.UserNameInvalid);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new UserException(UserError.DisplayNameRequired);
        }

        var label = displayName.Trim();
        return new User(normalized, label.Length > MaxDisplayNameLength ? label[..MaxDisplayNameLength] : label,
            Guard.Required(temporaryPasswordHash, DomainField.PasswordHash))
        {
            MustChangePassword = true,
        };
    }

    /// <summary>Lowercase ASCII letters, digits, '.', '-' and '_', at most <see cref="MaxUserNameLength"/> characters.</summary>
    public static bool TryNormalizeUserName(string? userName, out string normalized)
    {
        normalized = (userName ?? string.Empty).Trim().ToLowerInvariant();
        return normalized.Length is > 0 and <= MaxUserNameLength
               && normalized.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_');
    }

    public bool IsLockedAt(DateTimeOffset now) => LockedUntil is { } until && until > now;

    /// <summary>Records an attempt whose password check the caller has already made.</summary>
    public SignInOutcome RecordSignIn(bool passwordMatched, DateTimeOffset now)
    {
        if (IsLockedAt(now))
        {
            return SignInOutcome.LockedOut;
        }

        if (!passwordMatched)
        {
            // Counting restarts after an expired lockout.
            if (LockedUntil is not null)
            {
                FailedAttempts = 0;
                LockedUntil = null;
            }

            FailedAttempts++;
            if (FailedAttempts >= MaxFailedAttempts)
            {
                LockedUntil = now + LockoutDuration;
                return SignInOutcome.LockedOut;
            }

            return SignInOutcome.InvalidCredentials;
        }

        FailedAttempts = 0;
        LockedUntil = null;
        LastSignedInOn = now;
        return MustChangePassword ? SignInOutcome.MustChangePassword : SignInOutcome.Succeeded;
    }

    /// <summary>The user's own change: the new password is permanent. The policy is checked before, where the plaintext exists.</summary>
    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        PasswordHash = Guard.Required(newPasswordHash, DomainField.PasswordHash);
        MustChangePassword = false;
        FailedAttempts = 0;
        LockedUntil = null;
        LastSignedInOn = now;
    }

    /// <summary>An administrator's reset: a temporary password, to be changed at the next sign-in. Also unlocks.</summary>
    public void ResetPassword(string temporaryPasswordHash)
    {
        PasswordHash = Guard.Required(temporaryPasswordHash, DomainField.PasswordHash);
        MustChangePassword = true;
        FailedAttempts = 0;
        LockedUntil = null;
    }
}

/// <summary>Hashes and verifies passwords. The algorithm is an infrastructure concern.</summary>
public interface IPasswordHasher
{
    /// <summary>A self-describing hash: algorithm, parameters, salt and digest.</summary>
    string Hash(string password);

    /// <summary>False for a wrong password or a malformed hash; never throws. Must be constant-time.</summary>
    bool Verify(string password, string storedHash);
}

public interface IUserRepository : IRepository<User>
{
    /// <param name="normalizedUserName">A name already normalised by <see cref="User.TryNormalizeUserName"/>.</param>
    Task<User?> FindByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
