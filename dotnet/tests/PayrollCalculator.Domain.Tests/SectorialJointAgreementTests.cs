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
}
