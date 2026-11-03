using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Infrastructure.Persistence.Events;

public sealed class LegalSourceRepository(PayrollDbContext context) : EfRepository<LegalSource>(context), ILegalSourceRepository
{
    public Task<LegalSource?> FindAsync(LegalSourceKind kind, string number, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(s => s.Kind == kind && s.Number == number, cancellationToken);
}

public sealed class RuleEventStore(PayrollDbContext context) : IRuleEventStore
{
    private DbSet<RecordedRuleEvent> Set => context.Set<RecordedRuleEvent>();

    public async Task<IReadOnlyList<RecordedRuleEvent>> ListAsync(int agreementId, CancellationToken cancellationToken = default) =>
        InRecordedOrder(await Set.Where(e => e.AgreementId == agreementId).ToListAsync(cancellationToken).ConfigureAwait(false));

    public async Task<IReadOnlyList<RecordedRuleEvent>> ListAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        InRecordedOrder(await Set.Where(e => ids.Contains(e.Id)).ToListAsync(cancellationToken).ConfigureAwait(false));

    public Task<RecordedRuleEvent?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(RecordedRuleEvent recorded) => Set.Add(recorded);

    public void Remove(RecordedRuleEvent recorded)
    {
        ArgumentNullException.ThrowIfNull(recorded);
        if (recorded.Status != RuleEventStatus.Draft)
        {
            throw new DomainException(DomainError.ImmutableOnceApproved, DomainField.RuleEvent, recorded.Id.ToString());
        }

        Set.Remove(recorded);
    }

    // SQLite cannot order by these columns in SQL; the lists are small.
    private static List<RecordedRuleEvent> InRecordedOrder(List<RecordedRuleEvent> events) =>
        events.OrderBy(e => e.RecordedAt).ThenBy(e => e.Id).ToList();

    /// <summary>
    /// Last line of defence, run before every save: only a draft changes or disappears; an approved event can only be
    /// retracted; a rejected or retracted one never changes. The aggregate enforces the same rules.
    /// </summary>
    internal static void GuardImmutability(ChangeTracker tracker)
    {
        string[] review = [nameof(RecordedRuleEvent.Status), nameof(RecordedRuleEvent.ReviewedBy), nameof(RecordedRuleEvent.ReviewedAt), nameof(RecordedRuleEvent.Note)];
        foreach (var entry in tracker.Entries<RecordedRuleEvent>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var original = entry.OriginalValues.GetValue<RuleEventStatus>(nameof(RecordedRuleEvent.Status));
            var allowed = original switch
            {
                RuleEventStatus.Draft => true,
                RuleEventStatus.Approved => entry.State == EntityState.Modified
                    && entry.Properties.Where(p => p.IsModified).All(p => review.Contains(p.Metadata.Name))
                    && entry.Entity.Status == RuleEventStatus.Retracted,
                _ => false,
            };

            if (!allowed)
            {
                throw new DomainException(DomainError.ImmutableOnceApproved, DomainField.RuleEvent, entry.Entity.Id.ToString());
            }
        }
    }
}

public sealed class CareerEventStore(PayrollDbContext context) : ICareerEventStore
{
    private DbSet<CareerEventRow> Set => context.Set<CareerEventRow>();

    public async Task<IReadOnlyList<CareerEvent>> ListAsync(int affairId, CancellationToken cancellationToken = default)
    {
        var rows = await Set.Where(r => r.AffairId == affairId).OrderBy(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(r => EventJson.DeserializeCareerEvent(r.Payload)).ToList();
    }

    public void Add(int affairId, CareerEvent careerEvent)
    {
        ArgumentNullException.ThrowIfNull(careerEvent);
        Set.Add(new CareerEventRow
        {
            EventId = careerEvent.Id,
            AffairId = affairId,
            Kind = EventJson.CareerKind(careerEvent),
            Date = careerEvent.Date,
            Payload = EventJson.Serialize(careerEvent),
        });
    }

    public async Task<bool> RemoveAsync(int affairId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var row = await Set.FirstOrDefaultAsync(r => r.AffairId == affairId && r.EventId == eventId, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        Set.Remove(row);
        return true;
    }
}

public sealed class LedgerSnapshotRepository(PayrollDbContext context) : EfRepository<LedgerSnapshot>(context), ILedgerSnapshotRepository
{
    public async Task<IReadOnlyList<LedgerSnapshot>> ListByAffairAsync(int affairId, CancellationToken cancellationToken = default) =>
        await Set.Where(s => s.AffairId == affairId).OrderBy(s => s.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
}

/// <summary>SHA-256 over the stored form of the settings and events, in the order they were applied.</summary>
public sealed class Sha256LedgerFingerprint : ILedgerFingerprint
{
    public string Of(LedgerSettings settings, IEnumerable<RuleEvent> ruleEvents, IEnumerable<CareerEvent> careerEvents)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(ruleEvents);
        ArgumentNullException.ThrowIfNull(careerEvents);
        var text = new StringBuilder()
            .Append("settings:").Append(settings.Proration).Append('|').Append(settings.Rounding).Append('|')
            .Append(settings.ResetTimeInDegreeOnReclassification).Append('|').Append(settings.AbsenceSuspendsSeniority).Append('\n');
        foreach (var e in ruleEvents)
        {
            text.Append("rule:").Append(EventJson.Serialize(e)).Append('\n');
        }

        foreach (var e in careerEvents)
        {
            text.Append("career:").Append(EventJson.Serialize(e)).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
