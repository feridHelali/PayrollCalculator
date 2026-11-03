namespace PayrollCalculator.Domain.Rules;

/// <summary>
/// The history of an agreement's rules: approved events folded by effective date, so the rules in force on any day
/// can be read. Events apply by <c>(EffectiveFrom, Kind, recorded order)</c>; publication order does not matter,
/// so a retroactive text simply sorts earlier. Pure: no I/O, no clock.
/// </summary>
public sealed class Rulebook
{
    private readonly List<(DateOnly From, PayRules Rules)> _snapshots;
    private readonly Dictionary<Guid, RuleEvent> _byId;

    private Rulebook(List<RuleEvent> events, List<(DateOnly, PayRules)> snapshots, List<RuleWarning> warnings)
    {
        Events = events;
        _snapshots = snapshots;
        _byId = events.ToDictionary(e => e.Id);
        Warnings = warnings;
    }

    /// <summary>The events in the order they were applied.</summary>
    public IReadOnlyList<RuleEvent> Events { get; }

    public IReadOnlyList<RuleWarning> Warnings { get; }

    /// <summary>The days on which the rules change.</summary>
    public IReadOnlyList<DateOnly> ChangeDates => _snapshots.Select(s => s.From).ToList();

    /// <param name="events">Approved, non-retracted events, in recorded order.</param>
    public static Rulebook From(IEnumerable<RuleEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var ordered = events
            .Select((e, index) => (Event: e, Index: index))
            .OrderBy(x => x.Event.EffectiveFrom)
            .ThenBy(x => x.Event.Kind)
            .ThenBy(x => x.Index)
            .Select(x => x.Event)
            .ToList();

        var warnings = new List<RuleWarning>();
        var snapshots = new List<(DateOnly, PayRules)>();
        var rules = PayRules.Empty;
        foreach (var day in ordered.GroupBy(e => e.EffectiveFrom))
        {
            rules = day.Aggregate(rules, (current, e) => e.ApplyTo(current, warnings));
            snapshots.Add((day.Key, rules));
        }

        return new Rulebook(ordered, snapshots, warnings);
    }

    /// <summary>The rules in force on <paramref name="day"/> (empty before the first event).</summary>
    public PayRules At(DateOnly day)
    {
        int low = 0, high = _snapshots.Count - 1, found = -1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            if (_snapshots[middle].From <= day)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return found < 0 ? PayRules.Empty : _snapshots[found].Rules;
    }

    public RuleEvent? Find(Guid id) => _byId.GetValueOrDefault(id);
}
