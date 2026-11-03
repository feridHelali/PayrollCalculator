using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Tests;

public class AffairTests
{
    private static Affair NewAffair() => Affair.Open(
        affairNumber: "2024-001",
        title: "Dossier",
        claimant: "Salem",
        employmentPeriod: new DateRange(new DateOnly(2000, 1, 1), new DateOnly(2020, 12, 31)),
        agreementId: 1,
        salaryTableId: 10,
        initialCategory: "C1",
        initialDegree: 1);

    [Fact]
    public void Open_sets_all_fields()
    {
        var affair = NewAffair();
        Assert.Equal("2024-001", affair.AffairNumber);
        Assert.Equal(10, affair.SalaryTableId);
        Assert.Equal(new ProfessionalPosition("C1", 1), affair.InitialPosition);
    }

    [Fact]
    public void Employment_period_must_be_closed()
    {
        Assert.Throws<DomainException>(() => Affair.Open("1", "t", "c",
            DateRange.From(new DateOnly(2000, 1, 1)), 1, null, "C1", 1));
    }

    [Theory]
    [InlineData("", "t", "c")]
    [InlineData("1", "t", " ")]
    public void Number_and_claimant_are_required(string number, string title, string claimant)
    {
        Assert.Throws<DomainException>(() => Affair.Open(number, title, claimant,
            new DateRange(new DateOnly(2000, 1, 1), new DateOnly(2001, 1, 1)), 1, null, "C1", 1));
    }

    [Fact]
    public void Initial_degree_must_be_positive()
    {
        Assert.Throws<DomainException>(() => new ProfessionalPosition("C1", 0));
    }

    [Fact]
    public void Changing_the_agreement_unlinks_the_salary_table()
    {
        var affair = NewAffair();
        affair.ChangeAgreement(2);
        Assert.Equal(2, affair.AgreementId);
        Assert.Null(affair.SalaryTableId);
    }

    private static Affair Paid() =>
        Affair.Open("1", "", "Salem", new DateRange(new DateOnly(2000, 1, 1), new DateOnly(2010, 12, 31)), 1, null, "C1", 1);

    [Fact]
    public void A_paid_step_applies_from_its_date_until_the_next()
    {
        var affair = Paid();
        affair.ReplacePaidSalary([new PaidSalaryStepSpec(new DateOnly(2003, 7, 1), 500m, null), new PaidSalaryStepSpec(new DateOnly(2000, 1, 1), 400m, "fiche")]);

        Assert.Equal([400m, 500m], affair.PaidSalary.Select(s => s.GrossAmount));
        Assert.Null(affair.PaidOn(new DateOnly(1999, 12, 31)));
        Assert.Equal(400m, affair.PaidOn(new DateOnly(2003, 6, 30)));
        Assert.Equal(500m, affair.PaidOn(new DateOnly(2003, 7, 1)));
    }

    [Fact]
    public void Paid_steps_need_distinct_dates_and_amounts_in_millimes()
    {
        var affair = Paid();

        Assert.Throws<DomainException>(() => affair.ReplacePaidSalary(
            [new PaidSalaryStepSpec(new DateOnly(2001, 1, 1), 1m, null), new PaidSalaryStepSpec(new DateOnly(2001, 1, 1), 2m, null)]));
        Assert.Throws<DomainException>(() => affair.ReplacePaidSalary([new PaidSalaryStepSpec(new DateOnly(2001, 1, 1), 1.0005m, null)]));
        Assert.Throws<DomainException>(() => affair.ReplacePaidSalary([new PaidSalaryStepSpec(new DateOnly(2001, 1, 1), -1m, null)]));
    }
}
