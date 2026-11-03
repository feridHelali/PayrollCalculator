using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Tests;

/// <summary>Every rule violation carries a code and the field concerned, so the UI can word it (in Arabic).</summary>
public class DomainErrorTests
{
    private static DateRange Period => new(new DateOnly(2000, 1, 1), new DateOnly(2001, 12, 31));

    private static DomainException Fails(Action action) => Assert.ThrowsAny<DomainException>(action);

    [Fact]
    public void Missing_values_name_their_field()
    {
        var agreement = Fails(() => SectorialJointAgreement.Create(" ", null));
        Assert.Equal(DomainError.Required, agreement.Error);
        Assert.Equal(DomainField.AgreementName, agreement.Field);

        var claimant = Fails(() => Affair.Open("12", "", " ", Period, 1, null, "A", 1));
        Assert.Equal((DomainError.Required, DomainField.Claimant), (claimant.Error, claimant.Field));

        var table = Fails(() => SalaryTable.Create(1, " ", SalaryType.Monthly, "", Period));
        Assert.Equal((DomainError.Required, DomainField.TableNumber), (table.Error, table.Field));
    }

    [Fact]
    public void Number_rules_name_their_field()
    {
        var degree = Fails(() => _ = new ProfessionalPosition("A", 0));
        Assert.Equal((DomainError.MustBePositive, DomainField.ProfessionalDegree), (degree.Error, degree.Field));

        var table = SalaryTable.Create(1, "4", SalaryType.Monthly, "", Period);
        var years = Fails(() => table.AddDegree(1, -1));
        Assert.Equal((DomainError.CannotBeNegative, DomainField.YearsInDegree), (years.Error, years.Field));
    }

    [Fact]
    public void Periods_have_their_own_codes()
    {
        Assert.Equal(DomainError.EndBeforeStart,
            Fails(() => _ = new DateRange(new DateOnly(2001, 1, 1), new DateOnly(2000, 1, 1))).Error);

        var open = Fails(() => Affair.Open("12", "", "X", DateRange.From(new DateOnly(2000, 1, 1)), 1, null, "A", 1));
        Assert.Equal((DomainError.PeriodMustEnd, DomainField.EmploymentPeriod), (open.Error, open.Field));
    }

    [Fact]
    public void Duplicates_carry_the_offending_value()
    {
        var table = SalaryTable.Create(1, "4", SalaryType.Monthly, "", Period);
        table.AddDegree(1, 1);
        table.AddCategory("C1");

        var degree = Fails(() => table.AddDegree(1, 2));
        Assert.Equal((DomainError.Duplicate, DomainField.Degree, "1"), (degree.Error, degree.Field, degree.Detail));

        var category = Fails(() => table.AddCategory(" C1 "));
        Assert.Equal((DomainError.Duplicate, DomainField.CategoryLabel, "C1"), (category.Error, category.Field, category.Detail));
    }

    [Fact]
    public void Messages_stay_readable_for_logs()
    {
        var ex = Fails(() => SectorialJointAgreement.Create("", null));
        Assert.Contains("AgreementName", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Required", ex.Message, StringComparison.Ordinal);
    }
}
