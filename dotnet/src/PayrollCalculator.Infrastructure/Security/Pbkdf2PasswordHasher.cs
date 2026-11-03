using System.Globalization;
using System.Security.Cryptography;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Infrastructure.Security;

/// <summary>PBKDF2-HMAC-SHA256 password hashing, from the BCL (no package).</summary>
/// <remarks>
/// <para>Stored form: <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>. Each hash names its
/// algorithm and parameters, so a stronger algorithm (Argon2id) or more iterations can be introduced later
/// without resetting anyone's password.</para>
/// <para>600 000 iterations is the OWASP recommendation for PBKDF2-HMAC-SHA256.</para>
/// </remarks>
public sealed class Pbkdf2PasswordHasher(int iterations = Pbkdf2PasswordHasher.DefaultIterations) : IPasswordHasher
{
    public const int DefaultIterations = 600_000;

    private const string AlgorithmId = "pbkdf2-sha256";
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const char Separator = '$';

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        return string.Join(
            Separator,
            AlgorithmId,
            iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(Derive(password, salt, iterations)));
    }

    public bool Verify(string password, string storedHash)
    {
        if (password is null || !TryParse(storedHash, out var storedIterations, out var salt, out var expected))
        {
            return false;
        }

        // Constant-time: an early exit on the first different byte would leak how much of a guess was right.
        return CryptographicOperations.FixedTimeEquals(Derive(password, salt, storedIterations), expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashBytes);

    private static bool TryParse(string? stored, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = hash = [];
        var parts = stored?.Split(Separator);
        if (parts is not { Length: 4 } || parts[0] != AlgorithmId
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length == HashBytes;
    }
}
