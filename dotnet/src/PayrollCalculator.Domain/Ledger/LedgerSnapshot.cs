using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Ledger;

/// <summary>Which proration policy a ledger uses (sprint 05 question 3).</summary>
public enum ProrationKind
{
    CalendarDays = 0,
    ThirtyDays = 1,
}

/// <summary>Everything besides the events that decides a ledger's amounts. Part of a snapshot, so a snapshot reproduces.</summary>
public sealed record LedgerSettings(
    ProrationKind Proration = ProrationKind.CalendarDays,
    RoundingRule Rounding = RoundingRule.HalfUp,
    bool ResetTimeInDegreeOnReclassification = true,
    bool AbsenceSuspendsSeniority = true)
{
    public static LedgerSettings Default { get; } = new();

    public CareerOptions Career => new(ResetTimeInDegreeOnReclassification, AbsenceSuspendsSeniority);

    public PayLedgerBuilder Builder() => new(
        Proration == ProrationKind.ThirtyDays ? new ThirtyDayProration() : new CalendarDaysProration(), Career, Rounding);

    public PayLedger Build(Career career, Rulebook rules) => Builder().Build(career, rules);
}

/// <summary>Port: a stable hash of what a ledger was built from (settings, rule events in applied order, career events).</summary>
public interface ILedgerFingerprint
{
    string Of(LedgerSettings settings, IEnumerable<RuleEvent> ruleEvents, IEnumerable<CareerEvent> careerEvents);
}

/// <summary>Why the snapshot was taken.</summary>
public enum SnapshotPurpose
{
    Export = 0,
    Print = 1,
}

/// <summary>
/// Aggregate root: the record of a ledger handed out (exported or printed). It keeps the ids of the rule events used
/// (approved events never change, and retracted ones are kept), a copy of the career events and the settings,
/// so the same ledger can be rebuilt later, even after corrections, and checked against its hash.
/// </summary>
public sealed class LedgerSnapshot : Entity
{
    private LedgerSnapshot(int affairId, SnapshotPurpose purpose, LedgerSettings settings, List<Guid> ruleEventIds,
        List<CareerEvent> careerEvents, string hash, Money total, int months, string takenBy, DateTime takenAt)
    {
        AffairId = affairId;
        Purpose = purpose;
        Settings = settings;
        RuleEventIds = ruleEventIds;
        CareerEvents = careerEvents;
        Hash = hash;
        Total = total;
        Months = months;
        TakenBy = takenBy;
        TakenAt = takenAt;
    }

    // Required by EF Core.
    private LedgerSnapshot()
    {
        Settings = LedgerSettings.Default;
        RuleEventIds = [];
        CareerEvents = [];
        Hash = TakenBy = string.Empty;
    }

    public int AffairId { get; private set; }

    public SnapshotPurpose Purpose { get; private set; }

    public LedgerSettings Settings { get; private set; }

    /// <summary>The rule events used, in the order they were applied.</summary>
    public IReadOnlyList<Guid> RuleEventIds { get; private set; }

    public IReadOnlyList<CareerEvent> CareerEvents { get; private set; }

    public string Hash { get; private set; }

    public Money Total { get; private set; }

    public int Months { get; private set; }

    public string TakenBy { get; private set; }

    /// <summary>UTC.</summary>
    public DateTime TakenAt { get; private set; }

    public static LedgerSnapshot Take(int affairId, SnapshotPurpose purpose, LedgerSettings settings, Rulebook rules, Career career,
        ILedgerFingerprint fingerprint, string takenBy, DateTime takenAt)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(career);
        ArgumentNullException.ThrowIfNull(fingerprint);
        var ledger = settings.Build(career, rules);
        return new LedgerSnapshot(
            Guard.Positive(affairId, DomainField.Affair),
            purpose,
            settings,
            rules.Events.Select(e => e.Id).ToList(),
            career.Events.ToList(),
            fingerprint.Of(settings, rules.Events, career.Events),
            ledger.Total,
            ledger.Entries.Select(e => e.Month).Distinct().Count(),
            Guard.Required(takenBy, DomainField.Reviewer),
            takenAt);
    }

    /// <summary>Rebuilds the ledger from the stored events (whatever their status now) and checks it against the hash.</summary>
    /// <param name="stored">At least the events of <see cref="RuleEventIds"/>.</param>
    public (PayLedger Ledger, bool HashMatches) Reproduce(IEnumerable<RecordedRuleEvent> stored, ILedgerFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(fingerprint);
        var byId = stored.ToDictionary(r => r.Id, r => r.Event);
        if (RuleEventIds.FirstOrDefault(id => !byId.ContainsKey(id)) is var missing && missing != Guid.Empty)
        {
            throw new DomainException(DomainError.IncompleteSnapshot, DomainField.LedgerSnapshot, missing.ToString());
        }

        var rules = Rulebook.From(RuleEventIds.Select(id => byId[id]));
        var career = Career.From(CareerEvents);
        var ledger = Settings.Build(career, rules);
        return (ledger, fingerprint.Of(Settings, rules.Events, career.Events) == Hash);
    }
}

public interface ILedgerSnapshotRepository : IRepository<LedgerSnapshot>
{
    Task<IReadOnlyList<LedgerSnapshot>> ListByAffairAsync(int affairId, CancellationToken cancellationToken = default);
}
