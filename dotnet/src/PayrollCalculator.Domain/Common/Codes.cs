namespace PayrollCalculator.Domain.Common;

// Stable codes, compared by value (ordinal). A label can change from one text to the next; a code does not.

/// <summary>A line of salary grids that succeed each other over time (for example "WORKERS").</summary>
public readonly record struct GridLineCode
{
    public GridLineCode(string value) => Value = Guard.Required(value, DomainField.GridLine);

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>A professional category of a grid line (a row), independent of its label.</summary>
public readonly record struct CategoryCode
{
    public CategoryCode(string value) => Value = Guard.Required(value, DomainField.CategoryCode);

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>A work regime (for example "H48" or "H40").</summary>
public readonly record struct RegimeCode
{
    public RegimeCode(string value) => Value = Guard.Required(value, DomainField.Regime);

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>A component of the salary: the base, or a prime or allowance.</summary>
public readonly record struct ComponentCode
{
    public ComponentCode(string value) => Value = Guard.Required(value, DomainField.Component);

    /// <summary>The base salary read from the grid.</summary>
    public static ComponentCode Base { get; } = new("BASE");

    public string Value { get; }

    public bool IsBase => this == Base;

    public override string ToString() => Value;
}
