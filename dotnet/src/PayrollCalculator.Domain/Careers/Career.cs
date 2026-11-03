using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Domain.Careers;

/// <summary>A fact of the employee's career, at a date.</summary>
public abstract record CareerEvent
{
    protected CareerEvent(DateOnly date, Guid? id)
    {
        Id = id ?? Guid.CreateVersion7();
        Date = date;
    }

    public Guid Id { get; }

    public DateOnly Date { get; }
}

/// <summary>The hiring: where the employee starts in the grids.</summary>
public sealed record Hired : CareerEvent
{
    public Hired(DateOnly date, GridLineCode line, CategoryCode category, int degree, RegimeCode regime,
        int monthsAlreadyInDegree = 0, Guid? id = null)
        : base(date, id)
    {
        Line = line;
        Category = category;
        Degree = Guard.Positive(degree, DomainField.ProfessionalDegree);
        Regime = regime;
        MonthsAlreadyInDegree = Guard.NotNegative(monthsAlreadyInDegree, DomainField.MonthsInDegree);
    }

    public GridLineCode Line { get; }

    public CategoryCode Category { get; }

    public int Degree { get; }

    public RegimeCode Regime { get; }

    /// <summary>Time already spent in the initial degree (for example with a previous employer), in months.</summary>
    public int MonthsAlreadyInDegree { get; }
}

/// <summary>A move to another category (and degree), for example a promotion.</summary>
public sealed record Reclassified : CareerEvent
{
    public Reclassified(DateOnly date, CategoryCode category, int degree, Guid? id = null) : base(date, id)
    {
        Category = category;
        Degree = Guard.Positive(degree, DomainField.ProfessionalDegree);
    }

    public CategoryCode Category { get; }

    public int Degree { get; }
}

/// <summary>A degree proven by a document, replacing the computed one from that date.</summary>
public sealed record DegreeOverridden : CareerEvent
{
    public DegreeOverridden(DateOnly date, int degree, Guid? id = null) : base(date, id) =>
        Degree = Guard.Positive(degree, DomainField.ProfessionalDegree);

    public int Degree { get; }
}

/// <summary>The employee moves to another work regime.</summary>
public sealed record RegimeChanged : CareerEvent
{
    public RegimeChanged(DateOnly date, RegimeCode regime, Guid? id = null) : base(date, id) => Regime = regime;

    public RegimeCode Regime { get; }
}

/// <summary>A period without pay (and, by default, without seniority).</summary>
public sealed record UnpaidAbsence : CareerEvent
{
    public UnpaidAbsence(DateRange period, Guid? id = null)
        : base((period ?? throw new DomainException(DomainError.PeriodRequired, DomainField.CareerEvent)).Start, id) =>
        Period = period.IsOpenEnded ? throw new DomainException(DomainError.PeriodMustEnd, DomainField.CareerEvent) : period;

    public DateRange Period { get; }
}

/// <summary>The end of the employment (the layoff): the last day reconstructed.</summary>
public sealed record Separated : CareerEvent
{
    public Separated(DateOnly date, Guid? id = null) : base(date, id) { }
}

/// <summary>
/// Choices the agreements may make differently (see the sprint 05 questions 5 and 8). The defaults are assumptions to confirm.
/// </summary>
public sealed record CareerOptions(bool ResetTimeInDegreeOnReclassification = true, bool AbsenceSuspendsSeniority = true)
{
    public static CareerOptions Default { get; } = new();
}

/// <summary>The employee's position for one month, as used to value it.</summary>
/// <param name="ReferenceDay">The first day worked in the month: its rules and position apply to the whole month.</param>
public sealed record MonthPosition(
    YearMonth Month,
    DateOnly ReferenceDay,
    GridLineCode Line,
    CategoryCode Category,
    int Degree,
    RegimeCode Regime,
    int PaidDays,
    int DaysInMonth,
    int YearsOfService);

/// <summary>
/// Aggregate (part of an affair): the career events of the employee, validated as a whole.
/// <see cref="Positions"/> walks the months, applying the career events and the career rules in force.
/// </summary>
public sealed class Career
{
    private readonly List<CareerEvent> _events;

    private Career(Hired hiring, Separated? separation, List<CareerEvent> events)
    {
        Hiring = hiring;
        Separation = separation;
        _events = events;
    }

    public Hired Hiring { get; }

    public Separated? Separation { get; }

    public IReadOnlyList<CareerEvent> Events => _events;

    public DateRange Period => new(Hiring.Date, Separation?.Date);

    public static Career From(IEnumerable<CareerEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var ordered = events
            .Select((e, index) => (Event: e, Index: index))
            .OrderBy(x => x.Event.Date)
            .ThenBy(x => x.Event switch { Hired => 0, Separated => 2, _ => 1 })
            .ThenBy(x => x.Index)
            .Select(x => x.Event)
            .ToList();

        if (ordered.FirstOrDefault() is not Hired hiring)
        {
            throw new DomainException(DomainError.CareerMustStartWithHiring, DomainField.CareerEvent);
        }

        if (ordered.OfType<Hired>().Count() > 1 || ordered.OfType<Separated>().Count() > 1)
        {
            throw new DomainException(DomainError.Duplicate, DomainField.CareerEvent);
        }

        var separation = ordered.OfType<Separated>().SingleOrDefault();
        if (separation is not null && ordered.Any(e => e is not Separated && LastDay(e) > separation.Date))
        {
            throw new DomainException(DomainError.OutsideCareer, DomainField.CareerEvent);
        }

        return new Career(hiring, separation, ordered);
    }

    /// <summary>The position for every month of the (closed) career.</summary>
    public IReadOnlyList<MonthPosition> Positions(Rulebook rules, CareerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        options ??= CareerOptions.Default;
        if (Separation is null)
        {
            throw new DomainException(DomainError.PeriodMustEnd, DomainField.EmploymentPeriod);
        }

        var absences = options.AbsenceSuspendsSeniority ? _events.OfType<UnpaidAbsence>().Select(a => a.Period).ToList() : [];
        var allAbsences = _events.OfType<UnpaidAbsence>().Select(a => a.Period).ToList();
        var pending = new Queue<CareerEvent>(_events.Where(e => e is not Hired and not Separated and not UnpaidAbsence));

        var category = Hiring.Category;
        var degree = Hiring.Degree;
        var regime = Hiring.Regime;
        var degreeStart = Hiring.Date.AddMonths(-Hiring.MonthsAlreadyInDegree);

        var positions = new List<MonthPosition>();
        foreach (var month in Period.Months())
        {
            var firstDay = Max(month.FirstDay, Hiring.Date);
            var lastDay = Min(month.LastDay, Separation.Date);

            while (pending.TryPeek(out var next) && next.Date <= firstDay)
            {
                switch (pending.Dequeue())
                {
                    case Reclassified r:
                        category = r.Category;
                        degree = r.Degree;
                        degreeStart = options.ResetTimeInDegreeOnReclassification ? r.Date : degreeStart;
                        break;
                    case DegreeOverridden o:
                        degree = o.Degree;
                        degreeStart = o.Date;
                        break;
                    case RegimeChanged c:
                        regime = c.Regime;
                        break;
                }
            }

            (degree, degreeStart) = Advance(rules.At(firstDay).Line(Hiring.Line), degree, degreeStart, firstDay, absences);

            var paidDays = lastDay.DayNumber - firstDay.DayNumber + 1 - DaysWithin(allAbsences, firstDay, lastDay);
            positions.Add(new MonthPosition(month, firstDay, Hiring.Line, category, degree, regime, paidDays,
                DateTime.DaysInMonth(month.Year, month.Month), YearsOfService(firstDay, absences)));
        }

        return positions;
    }

    /// <summary>Moves up while the time in the current degree (plus suspended days) has elapsed. Never moves down.</summary>
    private static (int Degree, DateOnly Start) Advance(GridLineRules? line, int degree, DateOnly start, DateOnly day, List<DateRange> absences)
    {
        while (line?.CareerRule?.MonthsIn(degree) is { } months
               && line.Grid?.Degrees.Where(d => d > degree).Cast<int?>().FirstOrDefault() is { } next)
        {
            var due = start.AddMonths(months);
            for (var suspended = 0; ;)
            {
                var now = DaysWithin(absences, start, due.AddDays(-1));
                if (now == suspended)
                {
                    break;
                }

                due = due.AddDays(now - suspended);
                suspended = now;
            }

            if (day < due)
            {
                break;
            }

            degree = next;
            start = due;
        }

        return (degree, start);
    }

    private int YearsOfService(DateOnly day, List<DateRange> absences)
    {
        var start = Hiring.Date.AddDays(DaysWithin(absences, Hiring.Date, day.AddDays(-1)));
        var years = day.Year - start.Year;
        return start.AddYears(years) > day ? years - 1 : years;
    }

    private static int DaysWithin(List<DateRange> periods, DateOnly from, DateOnly to) =>
        periods.Sum(p =>
        {
            var start = Max(p.Start, from);
            var end = Min(p.End!.Value, to);
            return end < start ? 0 : end.DayNumber - start.DayNumber + 1;
        });

    private static DateOnly LastDay(CareerEvent e) => e is UnpaidAbsence a ? a.Period.End!.Value : e.Date;

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
