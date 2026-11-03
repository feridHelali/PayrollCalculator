using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Ledger;

/// <summary>One component of one month: its rate, how it was valued, its amount, and the events that set it.</summary>
public sealed record LedgerEntry(
    YearMonth Month,
    ComponentCode Component,
    string Name,
    CategoryCode Category,
    int Degree,
    PayUnit Unit,
    Money? Rate,
    Percentage? Percentage,
    decimal Quantity,
    decimal Proration,
    Money? Amount,
    IReadOnlyList<Guid> Causes,
    LedgerIssue Issue)
{
    /// <summary>What must differ from the previous month for a row to count as a change (the amount alone does not: proration).</summary>
    internal (Money?, Percentage?, PayUnit, CategoryCode, int, decimal, LedgerIssue) Signature =>
        (Rate, Percentage, Unit, Category, Degree, Quantity, Issue);
}

/// <summary>Consecutive months of a component with the same values, collapsed into one period (for the concise print).</summary>
public sealed record LedgerRun(
    ComponentCode Component,
    string Name,
    YearMonth From,
    YearMonth To,
    int Months,
    CategoryCode Category,
    int Degree,
    PayUnit Unit,
    Money? Rate,
    Percentage? Percentage,
    LedgerIssue Issue,
    Money Total,
    IReadOnlyList<Guid> Causes);

/// <summary>Which entries to keep. Every criterion left null keeps everything.</summary>
public sealed record LedgerFilter(
    IReadOnlyCollection<ComponentCode>? Components = null,
    YearMonth? From = null,
    YearMonth? To = null,
    bool IssuesOnly = false)
{
    public bool Keeps(LedgerEntry entry) =>
        (Components is null || Components.Contains(entry.Component))
        && (From is not { } from || entry.Month >= from)
        && (To is not { } to || entry.Month <= to)
        && (!IssuesOnly || entry.Issue != LedgerIssue.None);
}

/// <summary>
/// Read model: the month-by-month valuation of an affair, ordered by month then component (base first).
/// Built by <see cref="PayLedgerBuilder"/>; the views (<see cref="Filter"/>, <see cref="ChangesOnly"/>, <see cref="Collapse"/>)
/// return new ledgers or runs and never change this one.
/// </summary>
public sealed class PayLedger
{
    private readonly Rulebook _rules;

    internal PayLedger(IReadOnlyList<LedgerEntry> entries, Rulebook rules)
    {
        Entries = entries;
        _rules = rules;
        Components = entries.Select(e => e.Component).Distinct()
            .OrderBy(c => c.IsBase ? 0 : 1).ThenBy(c => c.Value, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<LedgerEntry> Entries { get; }

    /// <summary>The components present, the base first and then the allowances by code.</summary>
    public IReadOnlyList<ComponentCode> Components { get; }

    /// <summary>Points to review raised while folding the rules (published amounts that differ, incomplete grids).</summary>
    public IReadOnlyList<RuleWarning> RuleWarnings => _rules.Warnings;

    public Money Total => Sum(Entries);

    public int UnresolvedEntries => Entries.Count(e => e.Issue != LedgerIssue.None);

    public bool IsComplete => UnresolvedEntries == 0;

    public Money TotalFor(ComponentCode component) => Sum(Entries.Where(e => e.Component == component));

    public IReadOnlyDictionary<int, Money> TotalsByYear() =>
        Entries.GroupBy(e => e.Month.Year).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => Sum(g));

    /// <summary>The event (and so the citation) behind a cause of an entry.</summary>
    public RuleEvent? SourceOf(Guid eventId) => _rules.Find(eventId);

    public PayLedger Filter(LedgerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return new PayLedger(Entries.Where(filter.Keeps).ToList(), _rules);
    }

    /// <summary>For each component, its first month and every month where a value (not only the prorated amount) changes.</summary>
    public PayLedger ChangesOnly()
    {
        var previous = new Dictionary<ComponentCode, LedgerEntry>();
        var changes = new List<LedgerEntry>();
        foreach (var entry in Entries)
        {
            if (!previous.TryGetValue(entry.Component, out var before) || before.Signature != entry.Signature
                || entry.Month != before.Month.Next())
            {
                changes.Add(entry);
            }

            previous[entry.Component] = entry;
        }

        return new PayLedger(changes, _rules);
    }

    /// <summary>Runs of consecutive months with the same values, by component (base first) then by period.</summary>
    public IReadOnlyList<LedgerRun> Collapse()
    {
        var runs = new List<LedgerRun>();
        foreach (var component in Components)
        {
            List<LedgerEntry> current = [];
            foreach (var entry in Entries.Where(e => e.Component == component))
            {
                if (current.Count > 0 && (current[^1].Signature != entry.Signature || entry.Month != current[^1].Month.Next()))
                {
                    runs.Add(RunOf(current));
                    current = [];
                }

                current.Add(entry);
            }

            if (current.Count > 0)
            {
                runs.Add(RunOf(current));
            }
        }

        return runs;
    }

    private static LedgerRun RunOf(List<LedgerEntry> entries)
    {
        var first = entries[0];
        return new LedgerRun(first.Component, first.Name, first.Month, entries[^1].Month, entries.Count, first.Category, first.Degree,
            first.Unit, first.Rate, first.Percentage, first.Issue, Sum(entries), entries.SelectMany(e => e.Causes).Distinct().ToList());
    }

    private static Money Sum(IEnumerable<LedgerEntry> entries) =>
        entries.Aggregate(Money.Zero, (total, e) => e.Amount is { } amount ? total + amount : total);
}

/// <summary>
/// Domain service: values each month of a career with the rules in force on its reference day.
/// The base comes from the grid cell of the employee's category and degree; each allowance in force is valued by its basis.
/// </summary>
public sealed class PayLedgerBuilder(IProrationPolicy proration, CareerOptions? options = null, RoundingRule rounding = RoundingRule.HalfUp)
{
    public PayLedger Build(Career career, Rulebook rules)
    {
        ArgumentNullException.ThrowIfNull(career);
        ArgumentNullException.ThrowIfNull(rules);

        var entries = new List<LedgerEntry>();
        foreach (var position in career.Positions(rules, options))
        {
            var inForce = rules.At(position.ReferenceDay);
            var factor = proration.Factor(position.PaidDays, position.DaysInMonth);
            var regime = inForce.Regime(position.Regime);

            var (baseValue, baseCauses) = ValueBase(inForce.Line(position.Line), position, regime, factor);
            entries.Add(Entry(position, ComponentCode.Base, inForce.Line(position.Line)?.Name ?? string.Empty, baseValue, factor, baseCauses));

            var context = new ValuationContext(baseValue.Amount, regime?.Regime.HoursPerMonth, factor, position.YearsOfService, rounding);
            foreach (var allowance in inForce.Allowances)
            {
                var value = allowance.Basis.ValueFor(context);
                List<Guid> causes = [allowance.SetBy];
                if (value.Unit == PayUnit.Hourly && regime is not null)
                {
                    causes.Add(regime.SetBy);
                }

                entries.Add(Entry(position, allowance.Code, allowance.Name, value, factor, causes));
            }
        }

        return new PayLedger(entries, rules);
    }

    private (ComponentValue Value, List<Guid> Causes) ValueBase(GridLineRules? line, MonthPosition position, RegimeRule? regime, decimal factor)
    {
        if (line?.Grid is not { } grid)
        {
            return (ComponentValue.Missing(line?.Unit ?? PayUnit.Monthly, null, LedgerIssue.NoGridInForce), []);
        }

        if (!line.HasCategory(position.Category))
        {
            return (ComponentValue.Missing(line.Unit, null, LedgerIssue.CategoryNotInGrid), []);
        }

        if (grid.Cell(position.Category, position.Degree) is not { } cell)
        {
            return (ComponentValue.Missing(line.Unit, null, LedgerIssue.SalaryMissing), []);
        }

        if (line.Unit == PayUnit.Monthly)
        {
            return (new ComponentValue(PayUnit.Monthly, cell.Amount, null, 1m, cell.Amount.Times(factor, rounding), LedgerIssue.None), [cell.SetBy]);
        }

        if (regime is null)
        {
            return (ComponentValue.Missing(PayUnit.Hourly, cell.Amount, LedgerIssue.RegimeUnknown), [cell.SetBy]);
        }

        var hours = regime.Regime.HoursPerMonth;
        return (new ComponentValue(PayUnit.Hourly, cell.Amount, null, hours, cell.Amount.Times(hours * factor, rounding), LedgerIssue.None),
            [cell.SetBy, regime.SetBy]);
    }

    private static LedgerEntry Entry(MonthPosition position, ComponentCode component, string name, ComponentValue value, decimal factor,
        IReadOnlyList<Guid> causes) =>
        new(position.Month, component, name, position.Category, position.Degree, value.Unit, value.Rate, value.Percentage,
            value.Quantity, factor, value.Amount, causes, value.Issue);
}
