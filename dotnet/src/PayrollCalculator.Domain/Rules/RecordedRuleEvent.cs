using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Rules;

/// <summary>Where a rule event stands in its review. Only approved events reach the rulebook.</summary>
public enum RuleEventStatus
{
    /// <summary>Proposed (typed in, or read from a PDF by the MCP server); can still be revised or withdrawn.</summary>
    Draft = 0,

    /// <summary>Checked by a person; it applies, and it can no longer change.</summary>
    Approved = 1,

    /// <summary>Refused at review; kept for the record.</summary>
    Rejected = 2,

    /// <summary>Was approved, then withdrawn (for example replaced by a corrected event); kept so old snapshots reproduce.</summary>
    Retracted = 3,
}

/// <summary>
/// Aggregate root: a rule event of an agreement, with its review status and who recorded and reviewed it.
/// The event itself is immutable; a draft can be replaced by a revised one, an approved one only retracted.
/// </summary>
public sealed class RecordedRuleEvent
{
    private RuleEvent _event = null!;

    private RecordedRuleEvent(int agreementId, RuleEvent ruleEvent, string recordedBy, DateTime recordedAt)
    {
        Id = ruleEvent.Id;
        AgreementId = agreementId;
        Event = ruleEvent;
        RecordedBy = recordedBy;
        RecordedAt = recordedAt;
    }

    // Required by EF Core.
    private RecordedRuleEvent()
    {
        RecordedBy = string.Empty;
    }

    public Guid Id { get; private set; }

    public int AgreementId { get; private set; }

    public RuleEvent Event
    {
        get => _event;
        private set
        {
            _event = value;
            Kind = value.Kind;
            EffectiveFrom = value.EffectiveFrom;
            LegalSourceId = value.Source.LegalSourceId;
        }
    }

    /// <summary>Copies of the event's kind, date and source, kept as columns to query and to link the source.</summary>
    public RuleEventKind Kind { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }

    public int LegalSourceId { get; private set; }

    public RuleEventStatus Status { get; private set; }

    /// <summary>Who recorded it: a user name, or the tool that proposed it (for example "mcp", "migration").</summary>
    public string RecordedBy { get; private set; }

    /// <summary>UTC.</summary>
    public DateTime RecordedAt { get; private set; }

    public string? ReviewedBy { get; private set; }

    /// <summary>UTC.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Why it was rejected or retracted.</summary>
    public string? Note { get; private set; }

    public bool IsInForce => Status == RuleEventStatus.Approved;

    public static RecordedRuleEvent Draft(int agreementId, RuleEvent ruleEvent, string recordedBy, DateTime recordedAt)
    {
        ArgumentNullException.ThrowIfNull(ruleEvent);
        return new(Guard.Positive(agreementId, DomainField.Agreement), ruleEvent, Guard.Required(recordedBy, DomainField.Reviewer), recordedAt);
    }

    /// <summary>Replaces a draft by a corrected version of the same event (same id).</summary>
    public void Revise(RuleEvent revised)
    {
        ArgumentNullException.ThrowIfNull(revised);
        if (Status != RuleEventStatus.Draft)
        {
            throw new DomainException(DomainError.ImmutableOnceApproved, DomainField.RuleEvent, Id.ToString());
        }

        if (revised.Id != Id)
        {
            throw new DomainException(DomainError.Unknown, DomainField.RuleEvent, revised.Id.ToString());
        }

        Event = revised;
    }

    public void Approve(string reviewer, DateTime at) => Review(RuleEventStatus.Draft, RuleEventStatus.Approved, reviewer, at, null);

    public void Reject(string reviewer, DateTime at, string? note) => Review(RuleEventStatus.Draft, RuleEventStatus.Rejected, reviewer, at, note);

    public void Retract(string reviewer, DateTime at, string? note) => Review(RuleEventStatus.Approved, RuleEventStatus.Retracted, reviewer, at, note);

    private void Review(RuleEventStatus from, RuleEventStatus to, string reviewer, DateTime at, string? note)
    {
        if (Status != from)
        {
            throw new DomainException(DomainError.InvalidStatusTransition, DomainField.RuleEvent, $"{Status} → {to}");
        }

        ReviewedBy = Guard.Required(reviewer, DomainField.Reviewer);
        ReviewedAt = at;
        Note = Guard.Optional(note);
        Status = to;
    }
}

/// <summary>Port: the rule events of the agreements, whatever their status.</summary>
public interface IRuleEventStore
{
    /// <returns>The agreement's events in recorded order.</returns>
    Task<IReadOnlyList<RecordedRuleEvent>> ListAsync(int agreementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecordedRuleEvent>> ListAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<RecordedRuleEvent?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(RecordedRuleEvent recorded);

    /// <summary>Withdraws a draft. The store refuses to remove any other status.</summary>
    void Remove(RecordedRuleEvent recorded);
}

public static class RecordedRuleEvents
{
    /// <summary>The rulebook of the approved events, applied in recorded order for events of the same day and kind.</summary>
    public static Rulebook ToRulebook(this IEnumerable<RecordedRuleEvent> recorded, bool approvedOnly = true)
    {
        ArgumentNullException.ThrowIfNull(recorded);
        return Rulebook.From(recorded
            .Where(r => !approvedOnly || r.IsInForce)
            .OrderBy(r => r.RecordedAt)
            .ThenBy(r => r.Id)
            .Select(r => r.Event));
    }
}
