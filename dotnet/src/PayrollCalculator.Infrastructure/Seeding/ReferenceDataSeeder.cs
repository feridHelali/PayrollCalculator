using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Infrastructure.Persistence;

namespace PayrollCalculator.Infrastructure.Seeding;

/// <summary>One agreement of <c>data/seed/agreements.json</c>, in the file's own field names.</summary>
/// <param name="Id">Display order.</param>
/// <param name="Label">Official Arabic name.</param>
/// <param name="Slug">Stable code.</param>
public sealed record SeedAgreement(int Id, string Label, string Slug);

/// <summary>What a seeding run changed.</summary>
public sealed record SeedReport(int Added, int Updated, int Unchanged);

/// <summary>
/// Applies the reference data shipped with the product (sprint 07): for now the list of agreements. Idempotent and
/// matched by code (R2): a missing agreement is added, a seeded one gets the shipped name and order, and nothing
/// entered by a user is touched. Seeded agreements that a release drops are kept, since affairs may use them.
/// </summary>
public sealed class ReferenceDataSeeder(PayrollDbContext db)
{
    private const string AgreementsResource = "seed.agreements.json";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>The agreements shipped in this build (embedded from <c>data/seed/agreements.json</c>).</summary>
    public static IReadOnlyList<SeedAgreement> ShippedAgreements()
    {
        using var stream = typeof(ReferenceDataSeeder).Assembly.GetManifestResourceStream(AgreementsResource)
            ?? throw new InvalidOperationException($"The build does not embed {AgreementsResource}.");
        return Parse(stream);
    }

    public static IReadOnlyList<SeedAgreement> Parse(Stream json)
    {
        var list = JsonSerializer.Deserialize<List<SeedAgreement>>(json, Json) ?? [];
        var duplicate = list.GroupBy(a => a.Slug, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        return duplicate is null ? list : throw new InvalidDataException($"The agreement code '{duplicate.Key}' is used twice.");
    }

    public Task<SeedReport> SeedAsync(CancellationToken cancellationToken = default) => SeedAsync(ShippedAgreements(), cancellationToken);

    public async Task<SeedReport> SeedAsync(IReadOnlyList<SeedAgreement> agreements, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agreements);
        var existing = await db.Agreements.ToDictionaryAsync(a => a.Code, StringComparer.Ordinal, cancellationToken).ConfigureAwait(false);
        int added = 0, updated = 0, unchanged = 0;
        foreach (var seed in agreements)
        {
            if (!existing.TryGetValue(seed.Slug, out var agreement))
            {
                db.Agreements.Add(SectorialJointAgreement.FromSeed(seed.Slug, seed.Label, seed.Id));
                added++;
            }
            else if (agreement.Name != seed.Label.Trim() || agreement.SortOrder != seed.Id)
            {
                agreement.UpdateFromSeed(seed.Label, seed.Id);
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SeedReport(added, updated, unchanged);
    }
}
