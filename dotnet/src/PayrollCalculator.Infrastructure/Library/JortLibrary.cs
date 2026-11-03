using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayrollCalculator.Infrastructure.Library;

/// <summary>How far a file of the library has been identified.</summary>
public enum EntryStatus
{
    /// <summary>Only the file is known (hash, pages): someone has to open it and say what text it is.</summary>
    Unidentified,

    /// <summary>Part of the reference is known (for example from the file name).</summary>
    Partial,

    /// <summary>Kind, number, date and JORT reference are filled in and checked.</summary>
    Identified,
}

public sealed record JortIssue(int Number, DateOnly Date);

/// <summary>One PDF of the library and what is known about the text it holds (07.2).</summary>
public sealed record LibraryEntry
{
    /// <summary>Stable key, also the file name without ".pdf"; seed files cite sources by this key.</summary>
    public required string Key { get; init; }

    public required string Sha256 { get; init; }

    public required long Bytes { get; init; }

    public int? Pages { get; init; }

    public EntryStatus Status { get; init; }

    /// <summary>A <c>LegalSourceKind</c> name, when known.</summary>
    public string? Kind { get; init; }

    public string? Number { get; init; }

    public string? Title { get; init; }

    public DateOnly? SignedOn { get; init; }

    public JortIssue? Jort { get; init; }

    /// <summary>Agreement codes the text applies to.</summary>
    public IReadOnlyList<string> Agreements { get; init; } = [];

    /// <summary>"ar", "fr", or both.</summary>
    public string? Language { get; init; }

    public string? Notes { get; init; }

    [JsonIgnore]
    public string FileName => Key + ".pdf";
}

/// <summary>What a scan found that does not match the manifest.</summary>
public sealed record LibraryProblem(string Key, string Problem);

/// <summary>
/// The JORT library: <c>files/*.pdf</c>, <c>inbox/</c> for new files, and <c>manifest.json</c> describing each file.
/// The manifest is the source of truth for what a file is; the files are checked against it by SHA-256.
/// </summary>
public sealed class JortLibrary
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep Arabic readable in reviews
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly Func<string, int?> _countPages;

    /// <param name="root">The library folder (for example <c>data/jort</c>).</param>
    /// <param name="countPages">Reads the page count of a PDF; null when it cannot.</param>
    public JortLibrary(string root, Func<string, int?> countPages)
    {
        Root = root;
        _countPages = countPages;
    }

    public string Root { get; }

    public string FilesFolder => Path.Combine(Root, "files");

    public string InboxFolder => Path.Combine(Root, "inbox");

    public string ManifestPath => Path.Combine(Root, "manifest.json");

    public IReadOnlyList<LibraryEntry> Load() =>
        File.Exists(ManifestPath)
            ? JsonSerializer.Deserialize<List<LibraryEntry>>(File.ReadAllText(ManifestPath), Json) ?? []
            : [];

    public void Save(IEnumerable<LibraryEntry> entries) =>
        File.WriteAllText(ManifestPath, JsonSerializer.Serialize(entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToList(), Json) + "\n");

    /// <summary>
    /// Brings the manifest up to date: files moved from the inbox (renamed <c>unidentified-{hash}</c> unless a file with the
    /// same content is already there, in which case the duplicate is reported and left in the inbox), new files added as
    /// unidentified, hashes and page counts refreshed. Metadata typed in the manifest is kept.
    /// </summary>
    /// <returns>What could not be reconciled automatically.</returns>
    public IReadOnlyList<LibraryProblem> Catalogue()
    {
        Directory.CreateDirectory(FilesFolder);
        Directory.CreateDirectory(InboxFolder);
        var problems = new List<LibraryProblem>();
        var entries = Load().ToDictionary(e => e.Key, StringComparer.Ordinal);
        var byHash = Directory.EnumerateFiles(FilesFolder, "*.pdf").ToDictionary(Hash, f => f, StringComparer.Ordinal);

        foreach (var incoming in Directory.EnumerateFiles(InboxFolder, "*.pdf"))
        {
            var hash = Hash(incoming);
            if (byHash.TryGetValue(hash, out var existing))
            {
                problems.Add(new LibraryProblem(Path.GetFileNameWithoutExtension(existing), $"inbox/{Path.GetFileName(incoming)} is a copy of it"));
                continue;
            }

            var target = Path.Combine(FilesFolder, string.Create(CultureInfo.InvariantCulture, $"unidentified-{hash[..12]}.pdf"));
            File.Move(incoming, target);
            byHash[hash] = target;
        }

        foreach (var (hash, file) in byHash)
        {
            var key = Path.GetFileNameWithoutExtension(file);
            var bytes = new FileInfo(file).Length;
            entries[key] = entries.TryGetValue(key, out var known)
                ? known with { Sha256 = hash, Bytes = bytes, Pages = _countPages(file) ?? known.Pages }
                : new LibraryEntry { Key = key, Sha256 = hash, Bytes = bytes, Pages = _countPages(file), Status = EntryStatus.Unidentified };
        }

        foreach (var orphan in entries.Keys.Where(k => !File.Exists(Path.Combine(FilesFolder, k + ".pdf"))).ToList())
        {
            problems.Add(new LibraryProblem(orphan, "in the manifest but the file is missing"));
        }

        Save(entries.Values);
        return problems;
    }

    /// <summary>Checks the files against the manifest without changing anything (run by the tests and by <c>seed verify</c>).</summary>
    public IReadOnlyList<LibraryProblem> Verify()
    {
        var problems = new List<LibraryProblem>();
        var entries = Load();
        foreach (var duplicate in entries.GroupBy(e => e.Key, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            problems.Add(new LibraryProblem(duplicate.Key, "key used twice"));
        }

        foreach (var duplicate in entries.GroupBy(e => e.Sha256, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            problems.Add(new LibraryProblem(string.Join(", ", duplicate.Select(e => e.Key)), "same content"));
        }

        foreach (var entry in entries)
        {
            var file = Path.Combine(FilesFolder, entry.FileName);
            if (!File.Exists(file))
            {
                problems.Add(new LibraryProblem(entry.Key, "file missing"));
            }
            else if (Hash(file) != entry.Sha256)
            {
                problems.Add(new LibraryProblem(entry.Key, "file changed since it was catalogued"));
            }

            if (entry.Status == EntryStatus.Identified && (entry.Kind is null || entry.Number is null || entry.SignedOn is null))
            {
                problems.Add(new LibraryProblem(entry.Key, "marked identified but kind, number or date is missing"));
            }
        }

        var known = entries.Select(e => e.FileName).ToHashSet(StringComparer.Ordinal);
        foreach (var file in Directory.Exists(FilesFolder) ? Directory.EnumerateFiles(FilesFolder, "*.pdf") : [])
        {
            if (!known.Contains(Path.GetFileName(file)))
            {
                problems.Add(new LibraryProblem(Path.GetFileNameWithoutExtension(file), "file not in the manifest (run catalogue)"));
            }
        }

        return problems;
    }

    public static string Hash(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
