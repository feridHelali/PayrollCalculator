using System.Collections.Immutable;
using System.Globalization;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Rules;

/// <summary>A category of a grid line: a stable code and the label the texts give it.</summary>
public sealed record CategoryDefinition
{
    public CategoryDefinition(CategoryCode code, string label)
    {
        Code = code;
        Label = Guard.Required(label, DomainField.CategoryLabel);
    }

    public CategoryCode Code { get; }

    public string Label { get; }
}

/// <summary>Position of a cell in a grid: a category (row) and a degree (column).</summary>
public readonly record struct GridKey(CategoryCode Category, int Degree)
{
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Category}/{Degree}");
}

/// <summary>One amount of a grid as written in a text.</summary>
public sealed record GridCell
{
    public GridCell(CategoryCode category, int degree, Money amount)
    {
        Category = category;
        Degree = Guard.Positive(degree, DomainField.Degree);
        Amount = amount >= Money.Zero ? amount : throw new DomainException(DomainError.CannotBeNegative, DomainField.Amount);
    }

    public CategoryCode Category { get; }

    public int Degree { get; }

    public Money Amount { get; }

    public GridKey Key => new(Category, Degree);
}

/// <summary>An amount in force and the event that set it.</summary>
public sealed record GridValue(Money Amount, Guid SetBy);

/// <summary>A grid in force: the amounts by category and degree.</summary>
public sealed record SalaryGrid(ImmutableDictionary<GridKey, GridValue> Cells)
{
    public GridValue? Cell(CategoryCode category, int degree) => Cells.GetValueOrDefault(new GridKey(category, degree));

    public Money? AmountFor(CategoryCode category, int degree) => Cell(category, degree)?.Amount;

    /// <summary>The degrees the grid has, in increasing order.</summary>
    public IReadOnlyList<int> Degrees => Cells.Keys.Select(k => k.Degree).Distinct().Order().ToList();

    public int MaxDegree => Cells.Keys.Max(k => k.Degree);
}

/// <summary>How many months an employee spends in each degree before moving to the next one.</summary>
public sealed record CareerRule(ImmutableDictionary<int, int> MonthsInDegree, Guid SetBy)
{
    /// <returns>The months to spend in <paramref name="degree"/>, or null when the rule gives none (no further progression).</returns>
    public int? MonthsIn(int degree) => MonthsInDegree.TryGetValue(degree, out var months) ? months : null;
}

/// <summary>A work regime in force and the event that set it.</summary>
public sealed record RegimeRule(WorkRegime Regime, Guid SetBy);

/// <summary>An allowance in force, how it is valued, and the event that last set it.</summary>
public sealed record AllowanceRule(ComponentCode Code, string Name, AllowanceBasis Basis, Guid SetBy);

/// <summary>A grid line in force: its definition, its grid and its career rule.</summary>
public sealed record GridLineRules(
    GridLineCode Code,
    string Name,
    PayUnit Unit,
    IReadOnlyList<CategoryDefinition> Categories,
    Guid DeclaredBy,
    SalaryGrid? Grid = null,
    CareerRule? CareerRule = null)
{
    public bool HasCategory(CategoryCode code) => Categories.Any(c => c.Code == code);
}
