using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Infrastructure.Persistence;
using PayrollCalculator.Infrastructure.Persistence.Events;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>05.11: legal sources, rule events and career events in the database.</summary>
public sealed class EventStoreTests : IDisposable
{
    internal static readonly GridLineCode Line = new("WORKERS");
    internal static readonly CategoryCode C1 = new("C1");
    internal static readonly DateTime Now = new(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    internal static async Task<(int AgreementId, int SourceId, int AffairId)> SeedAsync(TestDatabase db)
    {
        await using var ctx = db.NewContext();
        var agreement = SectorialJointAgreement.Create("المخابز", null);
        new AgreementRepository(ctx).Add(agreement);
        var source = LegalSource.Register(LegalSourceKind.MinisterialOrder, "2024-1383", "قرار وزير الشؤون الاجتماعية",
            new DateOnly(2024, 7, 2), new JortReference(83, new DateOnly(2024, 7, 9)), "jort-2024-083.pdf");
        new LegalSourceRepository(ctx).Add(source);
        await ctx.SaveChangesAsync();
        var affair = Affair.Open("2024-1", "", "Salem", new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)), agreement.Id, null, "C1", 1);
        new AffairRepository(ctx).Add(affair);
        await ctx.SaveChangesAsync();
        return (agreement.Id, source.Id, affair.Id);
    }

    internal static RuleEvent[] Rules(int sourceId, decimal amount = 300m, DateOnly? from = null) =>
    [
        new GridLineDeclared(new DateOnly(2000, 1, 1), new Citation(sourceId, 1), Line, "عمال", PayUnit.Monthly, [new CategoryDefinition(C1, "صنف 1")]),
        new SalaryGridEnacted(from ?? new DateOnly(2000, 1, 1), new Citation(sourceId, 2), Line, [new GridCell(C1, 1, Money.FromDinars(amount))]),
    ];

    private async Task<Guid> RecordAsync(int agreementId, RuleEvent e, bool approve)
    {
        await using var ctx = _db.NewContext();
        var recorded = RecordedRuleEvent.Draft(agreementId, e, "mcp", Now);
        if (approve)
        {
            recorded.Approve("admin", Now);
        }

        new RuleEventStore(ctx).Add(recorded);
        await ctx.SaveChangesAsync();
        return recorded.Id;
    }

    [Fact]
    public async Task A_legal_source_round_trips_with_its_jort_reference()
    {
        var (_, sourceId, _) = await SeedAsync(_db);

        await using var ctx = _db.NewContext();
        var repo = new LegalSourceRepository(ctx);
        var source = (await repo.GetAsync(sourceId))!;

        Assert.Equal(new JortReference(83, new DateOnly(2024, 7, 9)), source.Jort);
        Assert.Equal("jort-2024-083.pdf", source.FileName);
        Assert.Same(source, await repo.FindAsync(LegalSourceKind.MinisterialOrder, "2024-1383"));
    }

    [Fact]
    public async Task A_rule_event_round_trips_with_its_status_and_columns()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        var enacted = Rules(sourceId)[1];
        var id = await RecordAsync(agreementId, enacted, approve: false);

        await using var ctx = _db.NewContext();
        var loaded = (await new RuleEventStore(ctx).GetAsync(id))!;

        Assert.Equal((RuleEventStatus.Draft, RuleEventKind.SalaryGridEnacted, sourceId, "mcp"), (loaded.Status, loaded.Kind, loaded.LegalSourceId, loaded.RecordedBy));
        Assert.Equal(EventJson.Serialize(enacted), EventJson.Serialize(loaded.Event));
    }

    [Fact]
    public async Task The_stored_events_fold_into_a_rulebook_of_the_approved_ones()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        var rules = Rules(sourceId);
        await RecordAsync(agreementId, rules[0], approve: true);
        await RecordAsync(agreementId, rules[1], approve: true);
        await RecordAsync(agreementId, Rules(sourceId, 999m, new DateOnly(2001, 1, 1))[1], approve: false);

        await using var ctx = _db.NewContext();
        var rulebook = (await new RuleEventStore(ctx).ListAsync(agreementId)).ToRulebook();

        Assert.Equal(Money.FromDinars(300m), rulebook.At(new DateOnly(2005, 1, 1)).Line(Line)!.Grid!.AmountFor(C1, 1));
    }

    [Fact]
    public async Task A_draft_can_be_revised_and_withdrawn()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        var first = Rules(sourceId)[1];
        var id = await RecordAsync(agreementId, first, approve: false);

        await using (var ctx = _db.NewContext())
        {
            var recorded = (await new RuleEventStore(ctx).GetAsync(id))!;
            recorded.Revise(new SalaryGridEnacted(first.EffectiveFrom, new Citation(sourceId, 3), Line, [new GridCell(C1, 1, Money.FromDinars(310m))], id));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.NewContext())
        {
            var store = new RuleEventStore(ctx);
            var revised = (await store.GetAsync(id))!;
            Assert.Equal(3, revised.Event.Source.Page);
            store.Remove(revised);
            await ctx.SaveChangesAsync();
        }

        await using var check = _db.NewContext();
        Assert.Null(await new RuleEventStore(check).GetAsync(id));
    }

    [Fact]
    public async Task The_store_refuses_to_change_or_remove_an_approved_event()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        var id = await RecordAsync(agreementId, Rules(sourceId)[1], approve: true);
        var other = Rules(sourceId, 999m)[1];

        await using (var ctx = _db.NewContext())
        {
            var store = new RuleEventStore(ctx);
            var approved = (await store.GetAsync(id))!;
            Assert.Throws<DomainException>(() => store.Remove(approved));

            // Bypassing the aggregate does not help: the context checks before saving.
            ctx.Entry(approved).Property(e => e.Event).CurrentValue = other;
            var error = await Assert.ThrowsAsync<DomainException>(() => ctx.SaveChangesAsync());
            Assert.Equal(DomainError.ImmutableOnceApproved, error.Error);
        }

        await using (var ctx = _db.NewContext())
        {
            var approved = (await new RuleEventStore(ctx).GetAsync(id))!;
            ctx.Remove(approved);
            await Assert.ThrowsAsync<DomainException>(() => ctx.SaveChangesAsync());
        }

        await using var check = _db.NewContext();
        Assert.Equal(Money.FromDinars(300m), ((SalaryGridEnacted)(await new RuleEventStore(check).GetAsync(id))!.Event).Cells[0].Amount);
    }

    [Fact]
    public async Task An_approved_event_can_be_retracted_and_then_never_changes()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        var id = await RecordAsync(agreementId, Rules(sourceId)[1], approve: true);

        await using (var ctx = _db.NewContext())
        {
            (await new RuleEventStore(ctx).GetAsync(id))!.Retract("admin", Now, "rectificatif");
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.NewContext())
        {
            var retracted = (await new RuleEventStore(ctx).GetAsync(id))!;
            Assert.Equal((RuleEventStatus.Retracted, "rectificatif"), (retracted.Status, retracted.Note));
            ctx.Entry(retracted).Property(e => e.Note).CurrentValue = "autre";
            await Assert.ThrowsAsync<DomainException>(() => ctx.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task A_legal_source_cited_by_an_event_cannot_be_deleted()
    {
        var (agreementId, sourceId, _) = await SeedAsync(_db);
        await RecordAsync(agreementId, Rules(sourceId)[0], approve: false);

        await using var ctx = _db.NewContext();
        var repo = new LegalSourceRepository(ctx);
        repo.Remove((await repo.GetAsync(sourceId))!);

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Career_events_round_trip_in_recorded_order()
    {
        var (_, _, affairId) = await SeedAsync(_db);
        CareerEvent[] events =
        [
            new Hired(new DateOnly(2020, 1, 1), Line, C1, 1, new RegimeCode("H48")),
            new Separated(new DateOnly(2020, 12, 31)),
            new Reclassified(new DateOnly(2020, 6, 1), new CategoryCode("C2"), 1),
        ];

        await using (var ctx = _db.NewContext())
        {
            var store = new CareerEventStore(ctx);
            foreach (var e in events)
            {
                store.Add(affairId, e);
            }

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.NewContext())
        {
            var store = new CareerEventStore(ctx);
            Assert.Equal(events, await store.ListAsync(affairId));
            Assert.True(await store.RemoveAsync(affairId, events[2].Id));
            Assert.False(await store.RemoveAsync(affairId + 1, events[0].Id));
            await ctx.SaveChangesAsync();
        }

        await using var check = _db.NewContext();
        var career = Career.From(await new CareerEventStore(check).ListAsync(affairId));
        Assert.Equal(2, career.Events.Count);
    }
}
