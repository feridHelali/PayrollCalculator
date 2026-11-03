using System.Collections.Immutable;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Rules;

/// <summary>The rules in force on a given day: grid lines (with their grid and career rule), allowances and regimes. Immutable.</summary>
public sealed class PayRules
{
    private PayRules(
        ImmutableDictionary<GridLineCode, GridLineRules> lines,
        ImmutableSortedDictionary<string, AllowanceRule> allowances,
        ImmutableDictionary<RegimeCode, RegimeRule> regimes)
    {
        Lines = lines;
        AllowancesByCode = allowances;
        Regimes = regimes;
    }

    public static PayRules Empty { get; } = new(
        ImmutableDictionary<GridLineCode, GridLineRules>.Empty,
        ImmutableSortedDictionary.Create<string, AllowanceRule>(StringComparer.Ordinal),
        ImmutableDictionary<RegimeCode, RegimeRule>.Empty);

    public ImmutableDictionary<GridLineCode, GridLineRules> Lines { get; }

    public ImmutableDictionary<RegimeCode, RegimeRule> Regimes { get; }

    /// <summary>The allowances in force, ordered by code.</summary>
    public IReadOnlyList<AllowanceRule> Allowances => AllowancesByCode.Values.ToList();

    private ImmutableSortedDictionary<string, AllowanceRule> AllowancesByCode { get; }

    public GridLineRules? Line(GridLineCode code) => Lines.GetValueOrDefault(code);

    public AllowanceRule? Allowance(ComponentCode code) => AllowancesByCode.GetValueOrDefault(code.Value);

    public RegimeRule? Regime(RegimeCode code) => Regimes.GetValueOrDefault(code);

    internal PayRules WithLine(GridLineRules line) => new(Lines.SetItem(line.Code, line), AllowancesByCode, Regimes);

    internal PayRules WithAllowance(AllowanceRule allowance) => new(Lines, AllowancesByCode.SetItem(allowance.Code.Value, allowance), Regimes);

    internal PayRules WithoutAllowance(ComponentCode code) => new(Lines, AllowancesByCode.Remove(code.Value), Regimes);

    internal PayRules WithRegime(RegimeRule regime) => new(Lines, AllowancesByCode, Regimes.SetItem(regime.Regime.Code, regime));
}
