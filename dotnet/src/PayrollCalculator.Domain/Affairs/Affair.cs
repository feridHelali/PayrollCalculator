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

/// <summary>
/// Aggregate root: a legal case (القضية) for a laid-off claimant whose salary must be
/// reconstructed month by month over the employment period.
/// </summary>
public sealed class Affair : Entity
{
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
