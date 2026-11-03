using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Infrastructure.Persistence.Events;
using static PayrollCalculator.Infrastructure.Tests.EventStoreTests;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>05.13: a snapshot records what a handed-out ledger was built from, and rebuilds it later.</summary>
public sealed class LedgerSnapshotTests : IDisposable
{
    private static readonly Sha256LedgerFingerprint Fingerprint = new();

    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static readonly CareerEvent[] CareerEvents =
        [new Hired(new DateOnly(2020, 1, 1), Line, C1, 1, new RegimeCode("H48")), new Separated(new DateOnly(2020, 12, 31))];

    private static Career Career() => Domain.Careers.Career.From(CareerEvents);

    private async Task<List<RecordedRuleEvent>> RecordApprovedAsync(int agreementId, IEnumerable<RuleEvent> events)
    {
        await using var ctx = _db.NewContext();
        var store = new RuleEventStore(ctx);
        var recorded = new List<RecordedRuleEvent>();
        var at = Now;
        foreach (var e in events)
        {
            var r = RecordedRuleEvent.Draft(agreementId, e, "admin", at = at.AddSeconds(1));
            r.Approve("admin", at);
            store.Add(r);
            recorded.Add(r);
        }

        await ctx.SaveChangesAsync();
        return recorded;
    }

    private async Task<Rulebook> RulebookAsync(int agreementId)
    {
        await using var ctx = _db.NewContext();
        return (await new RuleEventStore(ctx).ListAsync(agreementId)).ToRulebook();
    }

    [Fact]
    public void The_same_events_and_settings_give_the_same_hash()
    {
        var rules = Rulebook.From(Rules(1));
        var other = Rulebook.From(Rules(1, 301m));

        var first = Fingerprint.Of(LedgerSettings.Default, rules.Events, Career().Events);

        Assert.Equal(64, first.Length);
        Assert.Equal(first, Fingerprint.Of(LedgerSettings.Default, rules.Events, Career().Events));
        Assert.NotEqual(first, Fingerprint.Of(LedgerSettings.Default, other.Events, Career().Events));
        Assert.NotEqual(first, Fingerprint.Of(LedgerSettings.Default with { Proration = ProrationKind.ThirtyDays }, rules.Events, Career().Events));
    }

    [Fact]
    public async Task A_snapshot_round_trips_and_reproduces_its_ledger_after_a_correction()
    {
        var (agreementId, sourceId, affairId) = await SeedAsync(_db);
        var recorded = await RecordApprovedAsync(agreementId, Rules(sourceId));
        var snapshot = LedgerSnapshot.Take(affairId, SnapshotPurpose.Print, LedgerSettings.Default, await RulebookAsync(agreementId), Career(),
            Fingerprint, "admin", Now);
        await using (var ctx = _db.NewContext())
        {
            new LedgerSnapshotRepository(ctx).Add(snapshot);
            await ctx.SaveChangesAsync();
        }

        // The correction: the grid is retracted and replaced by a corrected one.
        await using (var ctx = _db.NewContext())
        {
            (await new RuleEventStore(ctx).GetAsync(recorded[1].Id))!.Retract("admin", Now, "rectificatif");
            await ctx.SaveChangesAsync();
        }

        await RecordApprovedAsync(agreementId, [Rules(sourceId, 310m)[1]]);
        var corrected = LedgerSettings.Default.Build(Career(), await RulebookAsync(agreementId));

        await using var check = _db.NewContext();
        var loaded = Assert.Single(await new LedgerSnapshotRepository(check).ListByAffairAsync(affairId));
        var stored = await new RuleEventStore(check).ListAsync(loaded.RuleEventIds.ToList());
        var (ledger, hashMatches) = loaded.Reproduce(stored, Fingerprint);

        Assert.True(hashMatches);
        Assert.Equal((Money.FromDinars(3600m), 12, SnapshotPurpose.Print), (loaded.Total, loaded.Months, loaded.Purpose));
        Assert.Equal(loaded.Total, ledger.Total);
        Assert.Equal(Money.FromDinars(3720m), corrected.Total);
        Assert.Equal(Career().Events, loaded.CareerEvents);
    }

    [Fact]
    public async Task A_snapshot_whose_events_are_gone_cannot_be_reproduced()
    {
        var (agreementId, sourceId, affairId) = await SeedAsync(_db);
        await RecordApprovedAsync(agreementId, Rules(sourceId));
        var snapshot = LedgerSnapshot.Take(affairId, SnapshotPurpose.Export, LedgerSettings.Default, await RulebookAsync(agreementId), Career(),
            Fingerprint, "admin", Now);

        var error = Assert.Throws<DomainException>(() => snapshot.Reproduce([], Fingerprint));

        Assert.Equal(DomainError.IncompleteSnapshot, error.Error);
    }
}
