using System.Collections.Immutable;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;

namespace PayrollCalculator.Domain.Rules;

/// <summary>The kinds of rule events. The order is also the order in which events of the same day apply.</summary>
public enum RuleEventKind
{
    GridLineDeclared = 0,
    WorkingTimeChanged = 1,
    CareerRuleChanged = 2,
    SalaryGridEnacted = 3,
    SalaryGridRevalued = 4,
    AllowanceIntroduced = 5,
    AllowanceRevalued = 6,
    AllowanceAbolished = 7,
}

/// <summary>
/// A change of the rules, written in a legal text and effective from a date. Events are immutable facts:
/// each one knows how it changes the rules in force (<see cref="ApplyTo"/>), so a new kind of factor is a new subtype.
/// </summary>
public abstract record RuleEvent
{
    protected RuleEvent(DateOnly effectiveFrom, Citation source, Guid? id)
    {
        ArgumentNullException.ThrowIfNull(source);
        Id = id ?? Guid.CreateVersion7();
        EffectiveFrom = effectiveFrom;
        Source = source;
    }

    public Guid Id { get; }

    /// <summary>The day the rule starts to apply (can be earlier than its publication).</summary>
    public DateOnly EffectiveFrom { get; }

    public Citation Source { get; }

    public abstract RuleEventKind Kind { get; }

    /// <summary>Applies the event to the rules in force; records warnings in <paramref name="warnings"/>.</summary>
    internal abstract PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings);

    private protected static GridLineRules RequireLine(PayRules rules, GridLineCode code) =>
        rules.Line(code) ?? throw new DomainException(DomainError.Undeclared, DomainField.GridLine, code.Value);

    private protected static AllowanceRule RequireAllowance(PayRules rules, ComponentCode code) =>
        rules.Allowance(code) ?? throw new DomainException(DomainError.Undeclared, DomainField.Component, code.Value);
}

/// <summary>Declares (or amends) a line of grids: its name, the unit of its amounts and its categories.</summary>
public sealed record GridLineDeclared : RuleEvent
{
    public GridLineDeclared(DateOnly effectiveFrom, Citation source, GridLineCode line, string name, PayUnit unit,
        IEnumerable<CategoryDefinition> categories, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        ArgumentNullException.ThrowIfNull(categories);
        Line = line;
        Name = Guard.Required(name, DomainField.GridLine);
        Unit = unit;
        Categories = categories.ToImmutableArray();
        if (Categories.Count == 0)
        {
            throw new DomainException(DomainError.Required, DomainField.CategoryCode);
        }

        var duplicate = Categories.GroupBy(c => c.Code).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException(DomainError.Duplicate, DomainField.CategoryCode, duplicate.Key.Value);
        }
    }

    public GridLineCode Line { get; }

    public string Name { get; }

    public PayUnit Unit { get; }

    public IReadOnlyList<CategoryDefinition> Categories { get; }

    public override RuleEventKind Kind => RuleEventKind.GridLineDeclared;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings)
    {
        var existing = rules.Line(Line);
        var line = existing is null
            ? new GridLineRules(Line, Name, Unit, Categories, Id)
            : existing with { Name = Name, Unit = Unit, Categories = Categories, DeclaredBy = Id };
        return rules.WithLine(line);
    }
}

/// <summary>A whole new grid for a line, replacing the one in force.</summary>
public sealed record SalaryGridEnacted : RuleEvent
{
    public SalaryGridEnacted(DateOnly effectiveFrom, Citation source, GridLineCode line, IEnumerable<GridCell> cells, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        Line = line;
        Cells = GridCells.Validate(cells);
    }

    public GridLineCode Line { get; }

    public IReadOnlyList<GridCell> Cells { get; }

    public override RuleEventKind Kind => RuleEventKind.SalaryGridEnacted;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings)
    {
        var line = RequireLine(rules, Line);
        if (Cells.FirstOrDefault(c => !line.HasCategory(c.Category)) is { } unknown)
        {
            throw new DomainException(DomainError.Unknown, DomainField.CategoryCode, unknown.Category.Value);
        }

        var grid = new SalaryGrid(Cells.ToImmutableDictionary(c => c.Key, c => new GridValue(c.Amount, Id)));
        foreach (var category in line.Categories)
        {
            foreach (var degree in grid.Degrees.Where(d => grid.Cell(category.Code, d) is null))
            {
                warnings.Add(new RuleWarning(RuleWarningKind.GridIncomplete, Id, new GridKey(category.Code, degree).ToString()));
            }
        }

        return rules.WithLine(line with { Grid = grid });
    }
}

/// <summary>A raise of the grid in force (percentage or fixed amount), optionally with the amounts the text publishes.</summary>
public sealed record SalaryGridRevalued : RuleEvent
{
    public SalaryGridRevalued(DateOnly effectiveFrom, Citation source, GridLineCode line, RevaluationRule rule,
        RoundingRule rounding, IEnumerable<GridCell>? published = null, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Line = line;
        Rule = rule;
        Rounding = rounding;
        Published = published is null ? [] : GridCells.Validate(published, allowEmpty: true);
    }

    public GridLineCode Line { get; }

    public RevaluationRule Rule { get; }

    public RoundingRule Rounding { get; }

    /// <summary>Amounts written in the text; they override the computed ones.</summary>
    public IReadOnlyList<GridCell> Published { get; }

    public override RuleEventKind Kind => RuleEventKind.SalaryGridRevalued;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings)
    {
        var line = RequireLine(rules, Line);
        var grid = line.Grid ?? throw new DomainException(DomainError.NotApplicable, DomainField.GridLine, Line.Value);
        if (Rule.NamedCategories.FirstOrDefault(c => !line.HasCategory(c)) is { Value: not null } unknown)
        {
            throw new DomainException(DomainError.Unknown, DomainField.CategoryCode, unknown.Value);
        }

        if (Published.FirstOrDefault(p => !grid.Cells.ContainsKey(p.Key)) is { } stray)
        {
            throw new DomainException(DomainError.Unknown, DomainField.GridCell, stray.Key.ToString());
        }

        var published = Published.ToDictionary(p => p.Key, p => p.Amount);
        var cells = grid.Cells.ToImmutableDictionary(pair => pair.Key, pair =>
        {
            var computed = Rule.Apply(pair.Key.Category, pair.Value.Amount, Rounding);
            if (published.TryGetValue(pair.Key, out var written))
            {
                if (computed is { } c && c != written)
                {
                    warnings.Add(new RuleWarning(RuleWarningKind.PublishedDiffersFromComputed, Id, pair.Key.ToString(), c, written));
                }

                return new GridValue(written, Id);
            }

            return computed is { } amount ? new GridValue(amount, Id) : pair.Value;
        });

        return rules.WithLine(line with { Grid = new SalaryGrid(cells) });
    }
}

/// <summary>The months to spend in each degree of a line before moving to the next one. A degree without a duration is a ceiling.</summary>
public sealed record CareerRuleChanged : RuleEvent
{
    public CareerRuleChanged(DateOnly effectiveFrom, Citation source, GridLineCode line, IReadOnlyDictionary<int, int> monthsInDegree, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        ArgumentNullException.ThrowIfNull(monthsInDegree);
        Line = line;
        foreach (var (degree, months) in monthsInDegree)
        {
            Guard.Positive(degree, DomainField.Degree);
            Guard.Positive(months, DomainField.MonthsInDegree);
        }

        MonthsInDegree = monthsInDegree.ToImmutableDictionary();
    }

    public GridLineCode Line { get; }

    public IReadOnlyDictionary<int, int> MonthsInDegree { get; }

    public override RuleEventKind Kind => RuleEventKind.CareerRuleChanged;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings) =>
        rules.WithLine(RequireLine(rules, Line) with { CareerRule = new CareerRule(MonthsInDegree.ToImmutableDictionary(), Id) });
}

/// <summary>Defines a work regime or changes its hours per month.</summary>
public sealed record WorkingTimeChanged : RuleEvent
{
    public WorkingTimeChanged(DateOnly effectiveFrom, Citation source, RegimeCode regime, decimal hoursPerMonth, Guid? id = null)
        : base(effectiveFrom, source, id) => Regime = new WorkRegime(regime, hoursPerMonth);

    public WorkRegime Regime { get; }

    public override RuleEventKind Kind => RuleEventKind.WorkingTimeChanged;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings) => rules.WithRegime(new RegimeRule(Regime, Id));
}

/// <summary>A new prime or allowance.</summary>
public sealed record AllowanceIntroduced : RuleEvent
{
    public AllowanceIntroduced(DateOnly effectiveFrom, Citation source, ComponentCode code, string name, AllowanceBasis basis, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        ArgumentNullException.ThrowIfNull(basis);
        Code = code.IsBase ? throw new DomainException(DomainError.OutOfRange, DomainField.Component, code.Value) : code;
        Name = Guard.Required(name, DomainField.Component);
        Basis = basis;
    }

    public ComponentCode Code { get; }

    public string Name { get; }

    public AllowanceBasis Basis { get; }

    public override RuleEventKind Kind => RuleEventKind.AllowanceIntroduced;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings) =>
        rules.Allowance(Code) is null
            ? rules.WithAllowance(new AllowanceRule(Code, Name, Basis, Id))
            : throw new DomainException(DomainError.Duplicate, DomainField.Component, Code.Value);
}

/// <summary>A new amount for an allowance: the published one, or computed from a percentage (or both, then checked).</summary>
public sealed record AllowanceRevalued : RuleEvent
{
    public AllowanceRevalued(DateOnly effectiveFrom, Citation source, ComponentCode code, AllowanceBasis? published,
        Percentage? percentage = null, RoundingRule rounding = RoundingRule.HalfUp, Guid? id = null)
        : base(effectiveFrom, source, id)
    {
        if (published is null && percentage is null)
        {
            throw new DomainException(DomainError.Required, DomainField.Amount);
        }

        Code = code;
        Published = published;
        Percentage = percentage;
        Rounding = rounding;
    }

    public ComponentCode Code { get; }

    public AllowanceBasis? Published { get; }

    public Percentage? Percentage { get; }

    public RoundingRule Rounding { get; }

    public override RuleEventKind Kind => RuleEventKind.AllowanceRevalued;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings)
    {
        var current = RequireAllowance(rules, Code);
        var computed = Percentage is { } percentage
            ? current.Basis.Increase(percentage, Rounding) ?? throw new DomainException(DomainError.NotApplicable, DomainField.Component, Code.Value)
            : null;

        if (computed is not null && Published is not null && computed != Published)
        {
            warnings.Add(new RuleWarning(RuleWarningKind.PublishedDiffersFromComputed, Id, Code.Value, computed.Amount, Published.Amount));
        }

        return rules.WithAllowance(current with { Basis = Published ?? computed!, SetBy = Id });
    }
}

/// <summary>An allowance stops being paid.</summary>
public sealed record AllowanceAbolished : RuleEvent
{
    public AllowanceAbolished(DateOnly effectiveFrom, Citation source, ComponentCode code, Guid? id = null)
        : base(effectiveFrom, source, id) => Code = code;

    public ComponentCode Code { get; }

    public override RuleEventKind Kind => RuleEventKind.AllowanceAbolished;

    internal override PayRules ApplyTo(PayRules rules, ICollection<RuleWarning> warnings)
    {
        _ = RequireAllowance(rules, Code);
        return rules.WithoutAllowance(Code);
    }
}

internal static class GridCells
{
    public static ImmutableArray<GridCell> Validate(IEnumerable<GridCell> cells, bool allowEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var list = cells.ToImmutableArray();
        if (list.Length == 0 && !allowEmpty)
        {
            throw new DomainException(DomainError.Required, DomainField.GridCell);
        }

        var duplicate = list.GroupBy(c => c.Key).FirstOrDefault(g => g.Count() > 1);
        return duplicate is null
            ? list
            : throw new DomainException(DomainError.Duplicate, DomainField.GridCell, duplicate.Key.ToString());
    }
}
