using System.Globalization;
using PayrollCalculator.Infrastructure.Library;
using UglyToad.PdfPig;

// payroll-seed: the maintainer's tool for the reference data (sprint 07). Not shipped with the app.
//   catalogue        move inbox/*.pdf into the library and refresh data/jort/manifest.json
//   library-verify   check the files against the manifest (exit code 1 on a problem)
//   text <key> [p]   print the text layer of a library file (page p, or all), to help identify it. The JORT fonts map
//                    the digits to the glyphs ∞±≤≥¥µ∂∑∏π; they are decoded back to 0-9 (the Arabic words stay garbled)

var root = FindRepositoryRoot();
var library = new JortLibrary(Path.Combine(root, "data", "jort"), CountPages);

return args.FirstOrDefault() switch
{
    "catalogue" => Report(library.Catalogue(), library, "catalogued"),
    "library-verify" => Report(library.Verify(), library, "verified"),
    "text" when args.Length >= 2 => PrintText(library, args[1], args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : null),
    _ => Usage(),
};

static int Report(IReadOnlyList<LibraryProblem> problems, JortLibrary library, string done)
{
    var entries = library.Load();
    Console.WriteLine($"{entries.Count} files {done}: {entries.Count(e => e.Status == EntryStatus.Identified)} identified, "
        + $"{entries.Count(e => e.Status == EntryStatus.Partial)} partial, {entries.Count(e => e.Status == EntryStatus.Unidentified)} unidentified.");
    foreach (var problem in problems)
    {
        Console.Error.WriteLine($"  {problem.Key}: {problem.Problem}");
    }

    return problems.Count == 0 ? 0 : 1;
}

static int PrintText(JortLibrary library, string key, int? page)
{
    using var pdf = PdfDocument.Open(Path.Combine(library.FilesFolder, key + ".pdf"));
    foreach (var p in pdf.GetPages().Where(p => page is null || p.Number == page))
    {
        Console.WriteLine($"--- page {p.Number} ---");
        Console.WriteLine(DecodeDigits(p.Text));
    }

    return 0;
}

static string DecodeDigits(string text)
{
    const string glyphs = "∞±≤≥¥µ∂∑∏π";
    return string.Concat(text.Select(c => glyphs.IndexOf(c, StringComparison.Ordinal) is var i and >= 0 ? (char)('0' + i) : c));
}

static int? CountPages(string file)
{
    try
    {
        using var pdf = PdfDocument.Open(file);
        return pdf.NumberOfPages;
    }
#pragma warning disable CA1031 // a damaged PDF must not stop the catalogue: its page count is just unknown
    catch (Exception)
#pragma warning restore CA1031
    {
        return null;
    }
}

static string FindRepositoryRoot()
{
    for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "data", "jort")))
        {
            return dir.FullName;
        }
    }

    throw new DirectoryNotFoundException("Run payroll-seed inside the repository (no data/jort folder found above the current folder).");
}

static int Usage()
{
    Console.Error.WriteLine("usage: payroll-seed catalogue | library-verify | text <key> [page]");
    return 2;
}
