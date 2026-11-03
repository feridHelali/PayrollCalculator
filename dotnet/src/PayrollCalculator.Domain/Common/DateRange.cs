namespace PayrollCalculator.Domain.Common;

/// <summary>An inclusive date interval; <see cref="End"/> is null when the range is open-ended.</summary>
public sealed record DateRange
{
    public DateRange(DateOnly start, DateOnly? end)
    {
        if (end is { } e && e < start)
        {
            throw new DomainException("End date cannot be before start date.");
        }

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly? End { get; }

    public bool IsOpenEnded => End is null;

    public static DateRange From(DateOnly start) => new(start, null);

    public bool Contains(DateOnly date) => date >= Start && (End is null || date <= End);

    /// <summary>Every calendar month touched by this (closed) range, in order.</summary>
    public IEnumerable<YearMonth> Months()
    {
        if (End is not { } end)
        {
            throw new DomainException("Cannot enumerate the months of an open-ended range.");
        }

        var last = YearMonth.Of(end);
        for (var month = YearMonth.Of(Start); month <= last; month = month.Next())
        {
            yield return month;
        }
    }
}
