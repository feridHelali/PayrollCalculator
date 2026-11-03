using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

public class SectorialJointAgreementTests
{
    [Fact]
    public void Create_trims_name_and_keeps_description()
    {
        var agreement = SectorialJointAgreement.Create("  النسيج  ", "desc");
        Assert.Equal("النسيج", agreement.Name);
        Assert.Equal("desc", agreement.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string name)
    {
        Assert.Throws<DomainException>(() => SectorialJointAgreement.Create(name, null));
    }

    [Fact]
    public void Rename_validates_the_new_name()
    {
        var agreement = SectorialJointAgreement.Create("A", null);
        Assert.Throws<DomainException>(() => agreement.Rename(" "));
        agreement.Rename("B");
        Assert.Equal("B", agreement.Name);
    }

    [Fact]
    public void A_user_agreement_gets_a_generated_code_and_stays_editable()
    {
        var agreement = SectorialJointAgreement.Create("A", null);

        Assert.Matches("^custom_[0-9a-f]{8}$", agreement.Code);
        Assert.Equal((DataOrigin.User, (int?)null), (agreement.Origin, agreement.SortOrder));
        agreement.EnsureEditable();
    }

    [Fact]
    public void A_seeded_agreement_is_read_only_except_for_a_seed_update()
    {
        var agreement = SectorialJointAgreement.FromSeed("agences_de_voyages", "الاتفاقية المشتركة القطاعية لوكالات الأسفار", 6);

        Assert.Equal(DomainError.ReadOnlySeed, Assert.Throws<DomainException>(() => agreement.Rename("x")).Error);
        Assert.Throws<DomainException>(() => agreement.ChangeDescription("x"));
        agreement.UpdateFromSeed("اسم جديد", 7);
        Assert.Equal(("اسم جديد", 7), (agreement.Name, agreement.SortOrder));
        Assert.Throws<DomainException>(() => SectorialJointAgreement.Create("A", null).UpdateFromSeed("B", 1));
    }

    [Theory]
    [InlineData("Textile")]
    [InlineData("textile-17")]
    [InlineData("1textile")]
    [InlineData("t")]
    public void A_seed_code_is_a_lowercase_slug(string code) =>
        Assert.Equal(DomainError.OutOfRange, Assert.Throws<DomainException>(() => SectorialJointAgreement.FromSeed(code, "x", 1)).Error);
}
