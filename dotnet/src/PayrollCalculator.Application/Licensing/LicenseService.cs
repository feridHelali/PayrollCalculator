using System.Security.Cryptography;
using System.Text;

namespace PayrollCalculator.Application.Licensing;

/// <summary>
/// A serial number (رقم الترخيص): 20 characters of Crockford base 32 (no I, L, O or U), shown as
/// <c>XXXXX-XXXXX-XXXXX-XXXXX</c>, so 100 random bits.
/// </summary>
/// <remarks>
/// The app never holds the serials, only their SHA-256 (<see cref="Hash"/>): <c>tools/generate-serials.ps1</c> makes
/// them, keeps the list for the publisher and appends the hashes to the list shipped with the app. Nothing in the app
/// or the repository lets anyone make a valid serial.
/// </remarks>
public static class SerialKey
{
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int Length = 20;

    /// <summary>Prefix hashed with the serial; must match <c>tools/generate-serials.ps1</c>.</summary>
    public const string HashPrefix = "PayrollCalculator.serial.v1:";

    /// <returns>The 20 characters, upper case, without separators (I and L read as 1, O as 0); null when it can't be a serial.</returns>
    public static string? Normalize(string? input)
    {
        if (input is null)
        {
            return null;
        }

        var builder = new StringBuilder(Length);
        foreach (var c in input.ToUpperInvariant())
        {
            if (c is '-' or ' ' or '\t')
            {
                continue;
            }

            var mapped = c switch
            {
                'I' or 'L' => '1',
                'O' => '0',
                _ => c,
            };
            if (!Alphabet.Contains(mapped, StringComparison.Ordinal))
            {
                return null;
            }

            builder.Append(mapped);
        }

        return builder.Length == Length ? builder.ToString() : null;
    }

    /// <summary>Lower-case hex SHA-256 of <see cref="HashPrefix"/> + the normalized serial.</summary>
    public static string Hash(string normalized) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(HashPrefix + normalized)));

    /// <summary><c>XXXXX-XXXXX-XXXXX-XXXXX</c>.</summary>
    public static string Format(string normalized) =>
        string.Join('-', Enumerable.Range(0, Length / 5).Select(i => normalized.Substring(i * 5, 5)));

    /// <summary>Only the last group is shown on screen: <c>*****-*****-*****-XXXXX</c>.</summary>
    public static string Mask(string normalized) => "*****-*****-*****-" + normalized[^5..];
}

/// <summary>The hashes of the serials shipped with this version.</summary>
public interface ISerialRegistry
{
    bool IsKnown(string hash);
}

/// <summary>The activation of this copy, kept beside the database.</summary>
public sealed record LicenseRecord(string Serial, DateTimeOffset ActivatedAt);

public interface ILicenseStore
{
    Task<LicenseRecord?> ReadAsync(CancellationToken ct = default);

    Task WriteAsync(LicenseRecord record, CancellationToken ct = default);
}

public enum ActivationOutcome
{
    Activated,

    /// <summary>Not 20 characters of the serial alphabet.</summary>
    InvalidFormat,

    /// <summary>Well formed, but not one of the serials issued for this version.</summary>
    Unknown,
}

/// <summary>What the About page shows.</summary>
public sealed record LicenseInfo(string MaskedSerial, DateTimeOffset ActivatedAt);

/// <summary>Use cases of the licence: is this copy activated, activate it with a serial.</summary>
public sealed class LicenseService(ISerialRegistry registry, ILicenseStore store, TimeProvider clock)
{
    /// <returns>The activation, or null when the copy isn't activated (no licence, or a serial no longer issued).</returns>
    public async Task<LicenseInfo?> CurrentAsync(CancellationToken ct = default)
    {
        var record = await store.ReadAsync(ct).ConfigureAwait(false);
        return record is not null && SerialKey.Normalize(record.Serial) is { } serial && registry.IsKnown(SerialKey.Hash(serial))
            ? new LicenseInfo(SerialKey.Mask(serial), record.ActivatedAt)
            : null;
    }

    public async Task<ActivationOutcome> ActivateAsync(string? input, CancellationToken ct = default)
    {
        if (SerialKey.Normalize(input) is not { } serial)
        {
            return ActivationOutcome.InvalidFormat;
        }

        if (!registry.IsKnown(SerialKey.Hash(serial)))
        {
            return ActivationOutcome.Unknown;
        }

        await store.WriteAsync(new LicenseRecord(SerialKey.Format(serial), clock.GetUtcNow()), ct).ConfigureAwait(false);
        return ActivationOutcome.Activated;
    }
}
