using System.Collections.Immutable;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Rules;

/// <summary>How a raise changes the amounts of a grid (strategy).</summary>
public abstract record RevaluationRule
{
    /// <summary>The categories the rule names explicitly (checked against the grid line).</summary>
    public virtual IEnumerable<CategoryCode> NamedCategories => [];

    /// <returns>The raised amount, or null when the rule does not cover <paramref name="category"/> (unchanged).</returns>
    public abstract Money? Apply(CategoryCode category, Money current, RoundingRule rounding);
}

/// <summary>The same percentage for every cell.</summary>
public sealed record PercentForAll(Percentage Percentage) : RevaluationRule
{
    public override Money? Apply(CategoryCode category, Money current, RoundingRule rounding) => current.Increase(Percentage, rounding);
}

/// <summary>A percentage for each category (or band of categories) the text names; the others are unchanged.</summary>
public sealed record PercentByCategory : RevaluationRule
{
    public PercentByCategory(IReadOnlyDictionary<CategoryCode, Percentage> percentages)
    {
        ArgumentNullException.ThrowIfNull(percentages);
        Percentages = percentages.ToImmutableDictionary();
    }

    public IReadOnlyDictionary<CategoryCode, Percentage> Percentages { get; }

    public override IEnumerable<CategoryCode> NamedCategories => Percentages.Keys;

    public override Money? Apply(CategoryCode category, Money current, RoundingRule rounding) =>
        Percentages.TryGetValue(category, out var percentage) ? current.Increase(percentage, rounding) : null;
}

/// <summary>The same amount added to every cell.</summary>
public sealed record FixedIncrease : RevaluationRule
{
    public FixedIncrease(Money increase) =>
        Increase = increase >= Money.Zero ? increase : throw new DomainException(DomainError.CannotBeNegative, DomainField.Amount);

    public Money Increase { get; }

    public override Money? Apply(CategoryCode category, Money current, RoundingRule rounding) => current + Increase;
}

/// <summary>Why a fold result deserves a look, without being an error.</summary>
public enum RuleWarningKind
{
    /// <summary>The amount published in the text is not the one the stated percentage gives. The published one is used.</summary>
    PublishedDiffersFromComputed,

    /// <summary>A grid lacks a cell (category/degree) that the rest of the grid implies.</summary>
    GridIncomplete,

    /// <summary>
    /// An amount is lower than the one of the degree before it, in the same category: most likely a misreading.
    /// <see cref="RuleWarning.Computed"/> holds the lower degree's amount and <see cref="RuleWarning.Published"/> this one's.
    /// </summary>
    NonMonotonicGrid,
}

/// <summary>A point to review, raised while folding the events.</summary>
public sealed record RuleWarning(RuleWarningKind Kind, Guid EventId, string Subject, Money? Computed = null, Money? Published = null)
{
    /// <summary>Published minus computed.</summary>
    public Money? Difference => Computed is { } c && Published is { } p ? p - c : null;
}
