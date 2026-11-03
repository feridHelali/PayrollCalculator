using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application.Users;

/// <summary>Sign-in and the user's own password change.</summary>
/// <remarks>
/// The password comparison happens here because it needs <see cref="IPasswordHasher"/>; everything that
/// follows from it (counter, lockout, forced change) is decided by the <see cref="User"/> aggregate.
/// Every attempt is saved, otherwise restarting the app would reset the lockout.
/// </remarks>
public sealed class AuthenticationService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    PasswordPolicy policy,
    TimeProvider clock,
    CurrentUser currentUser)
{
    /// <summary>Opens the session only on <see cref="SignInOutcome.Succeeded"/>.</summary>
    public async Task<SignInOutcome> SignInAsync(string? userName, string? password, CancellationToken ct = default)
    {
        var candidate = password ?? string.Empty;
        var user = await FindAsync(userName, ct).ConfigureAwait(false);
        if (user is null)
        {
            // Hash anyway: answering faster for an unknown name would reveal which names exist.
            hasher.Hash(candidate);
            return SignInOutcome.InvalidCredentials;
        }

        var outcome = user.RecordSignIn(hasher.Verify(candidate, user.PasswordHash), clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        if (outcome == SignInOutcome.Succeeded)
        {
            currentUser.SignIn(ToSession(user));
        }

        return outcome;
    }

    /// <summary>
    /// Replaces the user's password (temporary or not) after checking the current one, then opens the session.
    /// A wrong current password counts as a failed sign-in, so this form cannot be used to guess passwords.
    /// </summary>
    /// <exception cref="UserException">Mismatched confirmation, wrong current password, locked account, policy, or unchanged password.</exception>
    public async Task ChangePasswordAsync(
        string? userName, string? currentPassword, string? newPassword, string? confirmation, CancellationToken ct = default)
    {
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            throw new UserException(UserError.PasswordConfirmationMismatch);
        }

        var current = currentPassword ?? string.Empty;
        var user = await FindAsync(userName, ct).ConfigureAwait(false);
        if (user is null)
        {
            hasher.Hash(current);
            throw new UserException(UserError.WrongCurrentPassword);
        }

        var now = clock.GetUtcNow();
        var outcome = user.RecordSignIn(hasher.Verify(current, user.PasswordHash), now);
        if (outcome is SignInOutcome.InvalidCredentials or SignInOutcome.LockedOut)
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            throw new UserException(outcome == SignInOutcome.LockedOut ? UserError.AccountLocked : UserError.WrongCurrentPassword);
        }

        policy.Enforce(newPassword, user.UserName);
        if (string.Equals(current, newPassword, StringComparison.Ordinal))
        {
            // Compared on the plaintext: salted hashes of the same password always differ.
            throw new UserException(UserError.PasswordUnchanged);
        }

        user.ChangePassword(hasher.Hash(newPassword!), now);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        currentUser.SignIn(ToSession(user));
    }

    private async Task<User?> FindAsync(string? userName, CancellationToken ct) =>
        User.TryNormalizeUserName(userName, out var normalized)
            ? await users.FindByUserNameAsync(normalized, ct).ConfigureAwait(false)
            : null;

    private static UserSession ToSession(User user) => new(user.Id, user.UserName, user.DisplayName);
}
