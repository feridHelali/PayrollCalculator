using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.3: every rule cites the text it comes from.</summary>
public class LegalSourceTests
{
    [Fact]
    public void A_source_records_its_kind_number_and_jort_issue()
    {
        var source = LegalSource.Register(LegalSourceKind.MinisterialOrder, "2024-1383", "قرار وزير الشؤون الاجتماعية",
            new DateOnly(2024, 8, 8), new JortReference(49, new DateOnly(2024, 8, 16)), "arrete2024-1383arabe-35.pdf");

        Assert.Equal(LegalSourceKind.MinisterialOrder, source.Kind);
        Assert.Equal("2024-1383", source.Number);
        Assert.Equal(49, source.Jort!.IssueNumber);
        Assert.Equal("arrete2024-1383arabe-35.pdf", source.FileName);
    }

    [Fact]
    public void A_source_needs_a_number_and_a_title()
    {
        var number = Assert.Throws<DomainException>(() =>
            LegalSource.Register(LegalSourceKind.Rider, " ", "x", new DateOnly(2022, 1, 1), null, null));
        Assert.Equal((DomainError.Required, DomainField.SourceNumber), (number.Error, number.Field));

        var title = Assert.Throws<DomainException>(() =>
            LegalSource.Register(LegalSourceKind.Rider, "17", "", new DateOnly(2022, 1, 1), null, null));
        Assert.Equal((DomainError.Required, DomainField.SourceTitle), (title.Error, title.Field));
    }

    [Fact]
    public void A_jort_issue_number_must_be_positive()
    {
        var error = Assert.Throws<DomainException>(() => new JortReference(0, new DateOnly(2024, 8, 16)));
        Assert.Equal((DomainError.MustBePositive, DomainField.JortIssue), (error.Error, error.Field));
    }

    [Fact]
    public void A_citation_needs_a_source_and_a_page()
    {
        var citation = new Citation(3, 2, "الفصل 2");
        Assert.Equal((3, 2, "الفصل 2"), (citation.LegalSourceId, citation.Page, citation.Article));
        Assert.Null(new Citation(3, 1, "  ").Article);

        var source = Assert.Throws<DomainException>(() => new Citation(0, 1));
        Assert.Equal((DomainError.MustBePositive, DomainField.LegalSource), (source.Error, source.Field));

        var page = Assert.Throws<DomainException>(() => new Citation(1, 0));
        Assert.Equal((DomainError.MustBePositive, DomainField.Page), (page.Error, page.Field));
    }
}
