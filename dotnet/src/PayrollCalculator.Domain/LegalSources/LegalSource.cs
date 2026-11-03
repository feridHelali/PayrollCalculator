using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.LegalSources;

/// <summary>What kind of text a rule comes from.</summary>
public enum LegalSourceKind
{
    /// <summary>The sectoral joint agreement itself (الاتفاقية القطاعية المشتركة).</summary>
    Agreement = 0,

    /// <summary>A rider to the agreement (ملحق تعديلي).</summary>
    Rider = 1,

    /// <summary>A ministerial order approving a rider (قرار).</summary>
    MinisterialOrder = 2,

    /// <summary>A government decree (أمر / مرسوم), for example the minimum wage.</summary>
    Decree = 3,

    Law = 4,
}

/// <summary>An issue of the Official Journal of the Tunisian Republic (الرائد الرسمي للجمهورية التونسية).</summary>
public sealed record JortReference
{
    public JortReference(int issueNumber, DateOnly issueDate)
    {
        IssueNumber = Guard.Positive(issueNumber, DomainField.JortIssue);
        IssueDate = issueDate;
    }

    public int IssueNumber { get; }

    public DateOnly IssueDate { get; }
}

/// <summary>Aggregate root: a published legal text that rules cite.</summary>
public sealed class LegalSource : Entity
{
    private LegalSource(LegalSourceKind kind, string number, string title, DateOnly signedOn, JortReference? jort, string? fileName)
    {
        Kind = kind;
        Number = number;
        Title = title;
        SignedOn = signedOn;
        Jort = jort;
        FileName = fileName;
    }

    public LegalSourceKind Kind { get; private set; }

    /// <summary>The number of the text (for example "2024-1383" for an order, "18" for a rider).</summary>
    public string Number { get; private set; }

    public string Title { get; private set; }

    /// <summary>Date of the text (signature or order).</summary>
    public DateOnly SignedOn { get; private set; }

    /// <summary>Where it was published; unknown for an unpublished rider.</summary>
    public JortReference? Jort { get; private set; }

    /// <summary>The PDF in the library, when there is one.</summary>
    public string? FileName { get; private set; }

    public static LegalSource Register(LegalSourceKind kind, string number, string title, DateOnly signedOn,
        JortReference? jort, string? fileName) =>
        new(kind,
            Guard.Required(number, DomainField.SourceNumber),
            Guard.Required(title, DomainField.SourceTitle),
            signedOn,
            jort,
            Guard.Optional(fileName));
}

/// <summary>Where in a source a rule is written.</summary>
public sealed record Citation
{
    public Citation(int legalSourceId, int page, string? article = null)
    {
        LegalSourceId = Guard.Positive(legalSourceId, DomainField.LegalSource);
        Page = Guard.Positive(page, DomainField.Page);
        Article = Guard.Optional(article);
    }

    public int LegalSourceId { get; }

    public int Page { get; }

    /// <summary>The article (الفصل), when the text has them.</summary>
    public string? Article { get; }
}
