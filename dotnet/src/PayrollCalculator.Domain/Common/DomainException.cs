namespace PayrollCalculator.Domain.Common;

/// <summary>Which rule was broken. The UI words each code (in Arabic); the domain holds no UI text.</summary>
public enum DomainError
{
    Unspecified,
    Required,
    MustBePositive,
    CannotBeNegative,
    EndBeforeStart,
    PeriodRequired,
    PeriodMustEnd,
    OpenEndedRange,
    OutOfRange,
    Duplicate,
    Unknown,
    IdentityAlreadyAssigned,
    TableNotInAgreement,
    TableAgreementLocked,
    CareerMustStartWithHiring,
    OutsideCareer,
    NotApplicable,
    Undeclared,
    InvalidStatusTransition,
    ImmutableOnceApproved,
    IncompleteSnapshot,
    ReadOnlySeed,
    InvalidImportFile,
}

/// <summary>The value a rule applies to.</summary>
public enum DomainField
{
    None,
    Id,
    AgreementName,
    Agreement,
    AffairNumber,
    Claimant,
    EmploymentPeriod,
    ProfessionalCategory,
    ProfessionalDegree,
    TableNumber,
    ApplicationPeriod,
    CategoryLabel,
    CategoryKey,
    Degree,
    DegreeKey,
    YearsInDegree,
    Salary,
    Month,
    Year,
    PasswordHash,
    SalaryTable,
    Amount,
    Percentage,
    HoursPerMonth,
    Regime,
    GridLine,
    CategoryCode,
    Component,
    SourceNumber,
    SourceTitle,
    JortIssue,
    LegalSource,
    Page,
    MonthsInDegree,
    GridCell,
    CareerEvent,
    RuleEvent,
    Reviewer,
    LedgerSnapshot,
    Affair,
    DaysWorked,
    Unit,
    AgreementCode,
    ImportFile,
}

/// <summary>Raised when an operation would break a business invariant.</summary>
/// <remarks>
/// <see cref="Error"/>, <see cref="Field"/> and <see cref="Detail"/> say what happened in a form the UI can translate.
/// <see cref="Exception.Message"/> stays an English sentence built from them, for logs and support.
/// </remarks>
public class DomainException : Exception
{
    public DomainException(DomainError error, DomainField field = DomainField.None, string? detail = null)
        : base(Describe(error, field, detail))
    {
        Error = error;
        Field = field;
        Detail = detail;
    }

    public DomainException(string message) : base(message) { }

    public DomainException() { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }

    public DomainError Error { get; }

    public DomainField Field { get; }

    /// <summary>The offending value, when it helps (a duplicate label, a degree number).</summary>
    public string? Detail { get; }

    private static string Describe(DomainError error, DomainField field, string? detail) =>
        $"{error}" + (field == DomainField.None ? string.Empty : $" ({field})") + (detail is null ? string.Empty : $": {detail}");
}
