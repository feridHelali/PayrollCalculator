using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Affairs;

/// <summary>Category label and degree held by the employee.</summary>
public sealed record ProfessionalPosition
{
    public ProfessionalPosition(string category, int degree)
    {
        Category = Guard.Required(category, "Professional category");
        Degree = Guard.Positive(degree, "Professional degree");
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
            Guard.Required(affairNumber, "Affair number"),
            Guard.Optional(title) ?? string.Empty,
            Guard.Required(claimant, "Claimant"),
            ValidPeriod(employmentPeriod),
            Guard.Positive(agreementId, "Agreement"),
            salaryTableId,
            new ProfessionalPosition(initialCategory, initialDegree));
        return affair;
    }

    public void Describe(string affairNumber, string title, string claimant)
    {
        AffairNumber = Guard.Required(affairNumber, "Affair number");
        Title = Guard.Optional(title) ?? string.Empty;
        Claimant = Guard.Required(claimant, "Claimant");
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

        AgreementId = Guard.Positive(agreementId, "Agreement");
        SalaryTableId = null;
    }

    public void LinkSalaryTable(int? salaryTableId) => SalaryTableId = salaryTableId;

    private static DateRange ValidPeriod(DateRange? period) => period switch
    {
        null => throw new DomainException("Employment period is required."),
        { IsOpenEnded: true } => throw new DomainException("Employment period must have an end date."),
        _ => period,
    };
}

public interface IAffairRepository : IRepository<Affair>;
