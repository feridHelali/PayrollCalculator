using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Migration;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Ledger;

/// <summary>
/// Domain service: the month-by-month ledger of an affair (base salary and each chosen prime) from the data the screens
/// edit today, the salary tables and the primes. It translates them into rule events and lets the
/// <see cref="PayLedgerBuilder"/> value them, so the calculation is the one of the event model (sprint 05).
/// </summary>
/// <remarks>
/// The events are built in memory only and cite a placeholder source; the sources stay on the primes' rates and the
/// tables. The work regime is the one of the affair's salary table (<see cref="SalaryTables.SalaryTable.WeeklyHours"/>):
/// see <see cref="WorkRegimeProfile.For"/> for the hours and working days of a full month.
/// </remarks>
public static class AffairLedgerBuilder
{
    private static readonly Citation InMemory = new(1, 1);

    /// <summary>The ledger component of a prime.</summary>
    public static ComponentCode CodeOf(Allowance allowance)
    {
        ArgumentNullException.ThrowIfNull(allowance);
        return new ComponentCode("PRIME_" + allowance.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <param name="affair">The affair; it must be linked to one of <paramref name="tables"/>.</param>
    /// <param name="tables">The salary tables of the affair's agreement (every version of every table number).</param>
    /// <param name="allowances">The primes that apply to this employee.</param>
    /// <returns>Null when the affair has no salary table, so no career can be built.</returns>
    public static PayLedger? Build(Affair affair, IReadOnlyCollection<SalaryTables.SalaryTable> tables, IEnumerable<Allowance> allowances,
        LedgerSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(affair);
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentNullException.ThrowIfNull(allowances);
        var table = tables.FirstOrDefault(t => t.Id == affair.SalaryTableId);
        var warnings = new List<TranslationWarning>();
        if (table is null)
        {
            return null;
        }

        var family = tables.Where(t => t.AgreementId == table!.AgreementId).ToList();
        var first = new[] { family.Min(t => t.ApplicationPeriod.Start), affair.EmploymentPeriod.Start }.Min();
        var regime = WorkRegimeProfile.For(table!.WeeklyHours);
        List<RuleEvent> events =
        [
            .. LegacyTranslator.RuleEvents(family, InMemory, warnings),
            .. family.Select(t => t.WeeklyHours).Append(table.WeeklyHours).Distinct().Select(WorkRegimeProfile.For)
                .Select(r => new WorkingTimeChanged(first, InMemory, r.Code, r.HoursPerMonth)),
            .. allowances.SelectMany(a => AllowanceEvents(a, regime)),
        ];

        var career = LegacyTranslator.CareerEvents(affair, table, warnings, regime.Code)!;
        return (settings ?? LedgerSettings.Default).Build(Career.From(career), Rulebook.From(events));
    }

    /// <summary>A prime becomes one introduction (its first rate) followed by one revaluation per later rate.</summary>
    public static IEnumerable<RuleEvent> AllowanceEvents(Allowance allowance, WorkRegimeProfile regime)
    {
        ArgumentNullException.ThrowIfNull(regime);
        ArgumentNullException.ThrowIfNull(allowance);
        var code = CodeOf(allowance);
        var rates = allowance.Rates;
        for (var i = 0; i < rates.Count; i++)
        {
            var basis = Basis(allowance.Unit, rates[i].Value, regime);
            yield return i == 0
                ? new AllowanceIntroduced(rates[i].EffectiveFrom, InMemory, code, allowance.Name, basis)
                : new AllowanceRevalued(rates[i].EffectiveFrom, InMemory, code, basis);
        }
    }

    private static AllowanceBasis Basis(AllowanceUnit unit, decimal value, WorkRegimeProfile regime) => unit switch
    {
        AllowanceUnit.PerMonth => new FixedMonthly(Money.FromDinars(value)),
        AllowanceUnit.PerHour => new PerHour(Money.FromDinars(value)),
        AllowanceUnit.PerDayWorked => new PerDayWorked(Money.FromDinars(value), regime.WorkingDaysPerMonth),
        AllowanceUnit.PercentOfBase => new PercentOfBase(new Percentage(value)),
        _ => throw new DomainException(DomainError.Unknown, DomainField.Unit, unit.ToString()),
    };
}

/// <summary>
/// The hours and working days of a full month for a weekly regime. Hours: weekly hours × 52 ÷ 12 (48 h → 208,
/// 40 h → 173.333…). Working days: 26 for a six-day week (48 h), 22 for a five-day week (40 h). Other regimes are prorated
/// on 48 h = 26 days. These are assumptions to confirm with the expert; an agreement that states otherwise wins.
/// </summary>
public sealed record WorkRegimeProfile(RegimeCode Code, decimal HoursPerMonth, decimal WorkingDaysPerMonth)
{
    public static WorkRegimeProfile For(int weeklyHours) => weeklyHours switch
    {
        48 => new(new RegimeCode("H48"), 208m, 26m),
        40 => new(new RegimeCode("H40"), 40m * 52m / 12m, 22m),
        _ => new(new RegimeCode("H" + weeklyHours.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            weeklyHours * 52m / 12m, decimal.Round(weeklyHours * 26m / 48m, 2)),
    };
}
