using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.8: a career is a stream of events; the degree advances cumulatively and never goes down.</summary>
public class CareerTests
{
    private static Hired Hire(DateOnly on, int degree = 1, int monthsAlreadyInDegree = 0, string category = "C1") =>
        new(on, Workers, new CategoryCode(category), degree, H48, monthsAlreadyInDegree);

    private static Rulebook Rules(params RuleEvent[] extra) =>
        Rulebook.From([Declare(D(1990)), EnactFull(D(1990), 300m), CareerRule(D(1990), (1, 24), (2, 36)), .. extra]);

    private static MonthPosition At(IReadOnlyList<MonthPosition> positions, int year, int month) =>
        positions.Single(p => p.Month == new YearMonth(year, month));

    private static IReadOnlyList<MonthPosition> Positions(Rulebook rules, params CareerEvent[] events) =>
        Career.From(events).Positions(rules);

    [Fact]
    public void A_career_starts_with_a_hiring()
    {
        var none = Assert.Throws<DomainException>(() => Career.From([]));
        Assert.Equal(DomainError.CareerMustStartWithHiring, none.Error);

        var first = Assert.Throws<DomainException>(() => Career.From([new RegimeChanged(D(1999), H48), Hire(D(2000))]));
        Assert.Equal(DomainError.CareerMustStartWithHiring, first.Error);

        var twice = Assert.Throws<DomainException>(() => Career.From([Hire(D(2000)), Hire(D(2001))]));
        Assert.Equal((DomainError.Duplicate, DomainField.CareerEvent), (twice.Error, twice.Field));
    }

    [Fact]
    public void Nothing_happens_after_the_separation()
    {
        var after = Assert.Throws<DomainException>(() =>
            Career.From([Hire(D(2000)), new Separated(D(2005)), new Reclassified(D(2006), C2, 1)]));
        Assert.Equal((DomainError.OutsideCareer, DomainField.CareerEvent), (after.Error, after.Field));

        var twice = Assert.Throws<DomainException>(() => Career.From([Hire(D(2000)), new Separated(D(2005)), new Separated(D(2005))]));
        Assert.Equal((DomainError.Duplicate, DomainField.CareerEvent), (twice.Error, twice.Field));

        var absence = Assert.Throws<DomainException>(() =>
            Career.From([Hire(D(2000)), new Separated(D(2005)), new UnpaidAbsence(new DateRange(D(2004, 12, 1), D(2005, 1, 31)))]));
        Assert.Equal(DomainError.OutsideCareer, absence.Error);
    }

    [Fact]
    public void An_open_career_cannot_be_valued()
    {
        var career = Career.From([Hire(D(2000))]);

        Assert.True(career.Period.IsOpenEnded);
        var error = Assert.Throws<DomainException>(() => career.Positions(Rules()));
        Assert.Equal((DomainError.PeriodMustEnd, DomainField.EmploymentPeriod), (error.Error, error.Field));
    }

    [Fact]
    public void The_degree_advances_after_the_months_of_the_career_rule_and_the_last_degree_is_a_ceiling()
    {
        var positions = Positions(Rules(), Hire(D(2000)), new Separated(D(2010, 12, 31)));

        Assert.Equal(1, At(positions, 2001, 12).Degree);
        Assert.Equal(2, At(positions, 2002, 1).Degree);  // 24 months in degree 1
        Assert.Equal(2, At(positions, 2004, 12).Degree);
        Assert.Equal(3, At(positions, 2005, 1).Degree);  // 36 months in degree 2
        Assert.Equal(3, At(positions, 2010, 12).Degree); // no further degree in the grid
        Assert.Equal(132, positions.Count);
    }

    [Fact]
    public void The_degree_never_goes_down_when_the_career_rule_changes()
    {
        // Sprint 01 recomputed the degree from the hiring with each month's durations: 36 months < 120 sent it back to 1.
        var rules = Rules(CareerRule(D(2003), (1, 120), (2, 120)));
        var positions = Positions(rules, Hire(D(2000)), new Separated(D(2004, 12, 31)));

        Assert.Equal(2, At(positions, 2002, 12).Degree);
        Assert.Equal(2, At(positions, 2003, 1).Degree);
        Assert.True(positions.Zip(positions.Skip(1)).All(p => p.Second.Degree >= p.First.Degree));
    }

    [Fact]
    public void A_change_during_a_month_applies_from_the_next_month()
    {
        var positions = Positions(Rules(), Hire(D(2000, 1, 15)), new Separated(D(2002, 12, 31)));

        Assert.Equal(1, At(positions, 2002, 1).Degree); // the 24 months end on 15 January 2002
        Assert.Equal(2, At(positions, 2002, 2).Degree);
    }

    [Fact]
    public void Months_already_spent_in_the_degree_at_hiring_count()
    {
        var positions = Positions(Rules(), Hire(D(2000), monthsAlreadyInDegree: 12), new Separated(D(2001, 12, 31)));

        Assert.Equal(1, At(positions, 2000, 12).Degree);
        Assert.Equal(2, At(positions, 2001, 1).Degree);
    }

    [Fact]
    public void A_reclassification_sets_the_category_and_degree_and_restarts_the_time_in_degree()
    {
        var positions = Positions(Rules(), Hire(D(2000)), new Reclassified(D(2001, 3), C2, 1), new Separated(D(2004, 12, 31)));

        Assert.Equal((C1, 1), (At(positions, 2001, 2).Category, At(positions, 2001, 2).Degree));
        Assert.Equal((C2, 1), (At(positions, 2001, 3).Category, At(positions, 2001, 3).Degree));
        Assert.Equal(1, At(positions, 2003, 2).Degree);
        Assert.Equal(2, At(positions, 2003, 3).Degree);
    }

    [Fact]
    public void A_degree_override_replaces_the_computed_degree()
    {
        var positions = Positions(Rules(), Hire(D(2000)), new DegreeOverridden(D(2000, 6), 3), new Separated(D(2000, 12, 31)));

        Assert.Equal(1, At(positions, 2000, 5).Degree);
        Assert.Equal(3, At(positions, 2000, 6).Degree);
    }

    [Fact]
    public void An_unpaid_absence_is_not_paid_and_suspends_the_time_in_degree()
    {
        var positions = Positions(Rules(), Hire(D(2000)), new UnpaidAbsence(new DateRange(D(2001, 1, 1), D(2001, 1, 31))),
            new Separated(D(2002, 12, 31)));

        Assert.Equal(0, At(positions, 2001, 1).PaidDays);
        Assert.Equal(28, At(positions, 2001, 2).PaidDays);
        Assert.Equal(1, At(positions, 2002, 1).Degree); // 24 months + 31 days → 1 February 2002
        Assert.Equal(2, At(positions, 2002, 2).Degree);
    }

    [Fact]
    public void The_options_can_keep_the_time_in_degree_and_ignore_absences()
    {
        var options = new CareerOptions(ResetTimeInDegreeOnReclassification: false, AbsenceSuspendsSeniority: false);
        var career = Career.From([Hire(D(2000)), new Reclassified(D(2001), C2, 1),
            new UnpaidAbsence(new DateRange(D(2001, 6, 1), D(2001, 6, 30))), new Separated(D(2002, 12, 31))]);
        var positions = career.Positions(Rules(), options);

        Assert.Equal(2, At(positions, 2002, 1).Degree);
    }

    [Fact]
    public void Paid_days_count_the_first_and_last_months_from_and_to_the_day()
    {
        var positions = Positions(Rules(), Hire(D(2000, 1, 15)), new Separated(D(2000, 3, 10)));

        Assert.Equal([17, 29, 10], positions.Select(p => p.PaidDays));
        Assert.Equal([31, 29, 31], positions.Select(p => p.DaysInMonth));
    }

    [Fact]
    public void Years_of_service_are_whole_years_since_the_hiring()
    {
        var positions = Positions(Rules(), Hire(D(2000, 3, 1)), new Separated(D(2005, 12, 31)));

        Assert.Equal(4, At(positions, 2005, 2).YearsOfService);
        Assert.Equal(5, At(positions, 2005, 3).YearsOfService);
    }

    [Fact]
    public void The_regime_follows_regime_changes()
    {
        var h40 = new RegimeCode("H40");
        var positions = Positions(Rules(), Hire(D(2000)), new RegimeChanged(D(2000, 4), h40), new Separated(D(2000, 6, 30)));

        Assert.Equal(H48, At(positions, 2000, 3).Regime);
        Assert.Equal(h40, At(positions, 2000, 4).Regime);
    }
}
