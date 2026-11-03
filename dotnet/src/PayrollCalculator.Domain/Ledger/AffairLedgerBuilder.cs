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
/// tables. Work regime: 48 hours a week, so 208 hours and 26 working days in a full month (to adapt when a 40-hour
/// regime is entered).
/// </remarks>
public static class AffairLedgerBuilder
{
    public const decimal HoursPerMonth48 = 208m;

    public const decimal WorkingDaysPerMonth48 = 26m;

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
        if (LegacyTranslator.CareerEvents(affair, table, warnings) is not { } careerEvents)
        {
            return null;
        }

        var family = tables.Where(t => t.AgreementId == table!.AgreementId).ToList();
        var first = new[] { family.Min(t => t.ApplicationPeriod.Start), affair.EmploymentPeriod.Start }.Min();
        List<RuleEvent> events =
        [
            .. LegacyTranslator.RuleEvents(family, InMemory, warnings),
            new WorkingTimeChanged(first, InMemory, LegacyTranslator.Regime, HoursPerMonth48),
            .. allowances.SelectMany(AllowanceEvents),
        ];

        return (settings ?? LedgerSettings.Default).Build(Career.From(careerEvents), Rulebook.From(events));
    }

    /// <summary>A prime becomes one introduction (its first rate) followed by one revaluation per later rate.</summary>
    public static IEnumerable<RuleEvent> AllowanceEvents(Allowance allowance)
    {
        ArgumentNullException.ThrowIfNull(allowance);
        var code = CodeOf(allowance);
        var rates = allowance.Rates;
        for (var i = 0; i < rates.Count; i++)
        {
            var basis = Basis(allowance.Unit, rates[i].Value);
            yield return i == 0
                ? new AllowanceIntroduced(rates[i].EffectiveFrom, InMemory, code, allowance.Name, basis)
                : new AllowanceRevalued(rates[i].EffectiveFrom, InMemory, code, basis);
        }
    }

    private static AllowanceBasis Basis(AllowanceUnit unit, decimal value) => unit switch
    {
        AllowanceUnit.PerMonth => new FixedMonthly(Money.FromDinars(value)),
        AllowanceUnit.PerHour => new PerHour(Money.FromDinars(value)),
        AllowanceUnit.PerDayWorked => new PerDayWorked(Money.FromDinars(value), WorkingDaysPerMonth48),
        AllowanceUnit.PercentOfBase => new PercentOfBase(new Percentage(value)),
        _ => throw new DomainException(DomainError.Unknown, DomainField.Unit, unit.ToString()),
    };
}
