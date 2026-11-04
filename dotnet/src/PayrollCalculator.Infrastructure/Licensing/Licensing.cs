using System.Text.Json;
using PayrollCalculator.Application.Licensing;

namespace PayrollCalculator.Infrastructure.Licensing;

/// <summary>
/// The serial hashes embedded in this assembly (<c>Licensing/serials.sha256</c>: one lower-case hex SHA-256 per line,
/// <c>#</c> for comments), written by <c>tools/generate-serials.ps1</c>.
/// </summary>
public sealed class EmbeddedSerialRegistry : ISerialRegistry
{
    public const string ResourceName = "PayrollCalculator.Licensing.serials.sha256";

    private static readonly Lazy<HashSet<string>> Hashes = new(Load);

    public static IReadOnlyCollection<string> All => Hashes.Value;

    public bool IsKnown(string hash) => Hashes.Value.Contains(hash);

    private static HashSet<string> Load()
    {
        using var stream = typeof(EmbeddedSerialRegistry).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return [];
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length == 64 && !l.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);
    }
}

/// <summary>The activation in <c>data\license.json</c>, beside the database.</summary>
public sealed class FileLicenseStore(DatabaseOptions options) : ILicenseStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public string FilePath => Path.Combine(options.DataDirectory, "license.json");

    public async Task<LicenseRecord?> ReadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<LicenseRecord>(stream, Json, ct).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null; // a damaged file: ask for the serial again
        }
    }

    public async Task WriteAsync(LicenseRecord record, CancellationToken ct = default)
    {
        Directory.CreateDirectory(options.DataDirectory);
        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, record, Json, ct).ConfigureAwait(false);
    }
}
