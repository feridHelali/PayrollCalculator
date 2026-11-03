using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Affairs;

/// <summary>Category label and degree held by the employee.</summary>
public sealed record ProfessionalPosition
{
    public ProfessionalPosition(string category, int degree)
    {
        Category = Guard.Required(category, DomainField.ProfessionalCategory);
        Degree = Guard.Positive(degree, DomainField.ProfessionalDegree);
    }

    public string Category { get; }

    public int Degree { get; }
}

/// <summary>A gross monthly pay actually received, from a date until the next step (from the payslips).</summary>
public sealed class PaidSalaryStep
{
    internal PaidSalaryStep(DateOnly from, decimal grossAmount, string? note)
    {
        From = from;
        GrossAmount = grossAmount;
        Note = note;
    }

    public DateOnly From { get; private set; }

    /// <summary>Gross pay for a full month, in dinars (three decimals at most).</summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>Where it comes from, for example "fiche de paie de mars 2001".</summary>
    public string? Note { get; private set; }
}

/// <summary>Input describing a paid step when editing an affair.</summary>
public sealed record PaidSalaryStepSpec(DateOnly From, decimal GrossAmount, string? Note);

/// <summary>
/// Aggregate root: a legal case (القضية) for a laid-off claimant whose salary must be
/// reconstructed month by month over the employment period.
/// </summary>
public sealed class Affair : Entity
{
    private readonly List<PaidSalaryStep> _paidSalary = [];

    private Affair(string affairNumber, string title, string claimant, DateRange employmentPeriod,
        int agreementId, int? salaryTableId, ProfessionalPosition initialPosition)
    {
        AffairNumber = affairNumber;
        Title = title;
        Claimant = claimant;
        EmploymentPeriod = employmentPeriod;
        AgreementId = agreementId;
        SalaryTableId = salaryTableId;
        InitialPosition = initialPosition;
    }

    // Required by EF Core to materialize owned value objects.
    private Affair()
    {
        AffairNumber = Title = Claimant = string.Empty;
        EmploymentPeriod = DateRange.From(DateOnly.MinValue);
        InitialPosition = new ProfessionalPosition("-", 1);
    }

    public string AffairNumber { get; private set; }

    public string Title { get; private set; }

    public string Claimant { get; private set; }

    public DateRange EmploymentPeriod { get; private set; }

    public int AgreementId { get; private set; }

    /// <summary>Reference salary table; its <c>TableNumber</c> identifies the grid family to follow over time.</summary>
    public int? SalaryTableId { get; private set; }

    public ProfessionalPosition InitialPosition { get; private set; }

    /// <summary>What the employee was actually paid (gross), as steps by date; compared with what was owed (arrears).</summary>
    public IReadOnlyList<PaidSalaryStep> PaidSalary => _paidSalary.OrderBy(s => s.From).ToList();

    /// <summary>Replaces the paid steps. Dates must be distinct; amounts are gross dinars with three decimals at most.</summary>
    public void ReplacePaidSalary(IEnumerable<PaidSalaryStepSpec> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var list = steps.ToList();
        if (list.GroupBy(s => s.From).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
        {
            throw new DomainException(DomainError.Duplicate, DomainField.EffectiveDate,
                duplicate.Key.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture));
        }

        foreach (var step in list)
        {
            Guard.NotNegative(step.GrossAmount, DomainField.Amount);
            if (decimal.Round(step.GrossAmount, 3) != step.GrossAmount)
            {
                throw new DomainException(DomainError.OutOfRange, DomainField.Amount, step.GrossAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        _paidSalary.Clear();
        _paidSalary.AddRange(list.Select(s => new PaidSalaryStep(s.From, s.GrossAmount, Guard.Optional(s.Note))));
    }

    /// <returns>The gross pay of a full month in force on <paramref name="day"/>, or null when nothing is known yet.</returns>
    public decimal? PaidOn(DateOnly day) => _paidSalary.Where(s => s.From <= day).MaxBy(s => s.From)?.GrossAmount;

    public static Affair Open(string affairNumber, string title, string claimant, DateRange employmentPeriod,
        int agreementId, int? salaryTableId, string initialCategory, int initialDegree)
    {
        var affair = new Affair(
            Guard.Required(affairNumber, DomainField.AffairNumber),
            Guard.Optional(title) ?? string.Empty,
            Guard.Required(claimant, DomainField.Claimant),
            ValidPeriod(employmentPeriod),
            Guard.Positive(agreementId, DomainField.Agreement),
            salaryTableId,
            new ProfessionalPosition(initialCategory, initialDegree));
        return affair;
    }

    public void Describe(string affairNumber, string title, string claimant)
    {
        AffairNumber = Guard.Required(affairNumber, DomainField.AffairNumber);
        Title = Guard.Optional(title) ?? string.Empty;
        Claimant = Guard.Required(claimant, DomainField.Claimant);
    }

    public void ChangeEmploymentPeriod(DateRange period) => EmploymentPeriod = ValidPeriod(period);

    public void ChangeInitialPosition(string category, int degree) => InitialPosition = new ProfessionalPosition(category, degree);

    /// <summary>Moves the affair to another agreement; the salary table belonged to the old one, so it is unlinked.</summary>
    public void ChangeAgreement(int agreementId)
    {
        if (agreementId == AgreementId)
        {
            return;
        }

        AgreementId = Guard.Positive(agreementId, DomainField.Agreement);
        SalaryTableId = null;
    }

    public void LinkSalaryTable(int? salaryTableId) => SalaryTableId = salaryTableId;

    private static DateRange ValidPeriod(DateRange? period) => period switch
    {
        null => throw new DomainException(DomainError.PeriodRequired, DomainField.EmploymentPeriod),
        { IsOpenEnded: true } => throw new DomainException(DomainError.PeriodMustEnd, DomainField.EmploymentPeriod),
        _ => period,
    };
}

public interface IAffairRepository : IRepository<Affair>;
