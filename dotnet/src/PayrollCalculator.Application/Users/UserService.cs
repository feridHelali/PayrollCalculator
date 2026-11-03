using System.Security.Cryptography;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application.Users;

public enum UserStatus
{
    Active,
    MustChangePassword,
    Locked,
}

public sealed record UserDto(int Id, string UserName, string DisplayName, UserStatus Status, DateTimeOffset? LastSignedInOn);

/// <summary>User management: list, add, reset a password. There are no roles: any signed-in user may do this.</summary>
public sealed class UserService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    PasswordPolicy policy,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        return (await users.ListAsync(ct).ConfigureAwait(false))
            .OrderBy(u => u.UserName, StringComparer.Ordinal)
            .Select(u => new UserDto(u.Id, u.UserName, u.DisplayName, StatusOf(u, now), u.LastSignedInOn))
            .ToList();
    }

    /// <summary>Adds a user whose temporary password must be changed at the first sign-in.</summary>
    public async Task<int> CreateAsync(string? userName, string? displayName, string? temporaryPassword, CancellationToken ct = default)
    {
        if (!User.TryNormalizeUserName(userName, out var normalized))
        {
            throw new UserException(UserError.UserNameInvalid);
        }

        policy.Enforce(temporaryPassword, normalized);
        if (await users.FindByUserNameAsync(normalized, ct).ConfigureAwait(false) is not null)
        {
            throw new UserException(UserError.UserNameTaken);
        }

        var user = User.Register(normalized, displayName, hasher.Hash(temporaryPassword!));
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return user.Id;
    }

    /// <summary>Gives the user a new temporary password and unlocks the account.</summary>
    public async Task ResetPasswordAsync(int userId, string? temporaryPassword, CancellationToken ct = default)
    {
        var user = await NotFoundException.OrThrow(users.GetAsync(userId, ct), "User", userId).ConfigureAwait(false);
        policy.Enforce(temporaryPassword, user.UserName);
        user.ResetPassword(hasher.Hash(temporaryPassword!));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static UserStatus StatusOf(User user, DateTimeOffset now) =>
        user.IsLockedAt(now) ? UserStatus.Locked
        : user.MustChangePassword ? UserStatus.MustChangePassword
        : UserStatus.Active;
}

/// <summary>Random temporary passwords that always satisfy <see cref="PasswordPolicy.Default"/>.</summary>
public static class TemporaryPassword
{
    public const int Length = 12;

    // Without look-alikes (0/O, 1/l/I) because the administrator reads it out to the user.
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "@#$%&*!?";
    private const string All = Lower + Upper + Digits + Symbols;

    public static string Generate()
    {
        var chars = new char[Length];
        chars[0] = Pick(Lower);
        chars[1] = Pick(Upper);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Symbols);
        for (var i = 4; i < Length; i++)
        {
            chars[i] = Pick(All);
        }

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }

    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}

/// <summary>Creates the first account on an empty database.</summary>
/// <remarks>
/// The password is fixed and documented, and the account must change it at the first sign-in, so it
/// cannot be used to do any work. A random password written to a log would be worse: logs get sent around.
/// </remarks>
public sealed class UserSeeder(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher)
{
    public const string DefaultUserName = "admin";
    public const string DefaultPassword = "Admin@2026";
    public const string DefaultDisplayName = "مدير النظام";

    /// <returns>True when the account was created, false when users already existed.</returns>
    public async Task<bool> SeedAsync(CancellationToken ct = default)
    {
        if (await users.AnyAsync(ct).ConfigureAwait(false))
        {
            return false;
        }

        users.Add(User.Register(DefaultUserName, DefaultDisplayName, hasher.Hash(DefaultPassword)));
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
