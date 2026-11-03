using System.Globalization;

namespace PayrollCalculator.Domain.Common;

/// <summary>A calendar month (pay period).</summary>
public readonly record struct YearMonth : IComparable<YearMonth>
{
    public YearMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new DomainException("Month must be between 1 and 12.");
        }

        if (year is < 1 or > 9999)
        {
            throw new DomainException("Year is out of range.");
        }

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public static YearMonth Of(DateOnly date) => new(date.Year, date.Month);

    public YearMonth Next() => Month == 12 ? new YearMonth(Year + 1, 1) : new YearMonth(Year, Month + 1);

    /// <summary>Number of whole calendar months from <paramref name="earlier"/> to this month.</summary>
    public int MonthsSince(YearMonth earlier) => (Year - earlier.Year) * 12 + (Month - earlier.Month);

    public int CompareTo(YearMonth other) => MonthsSince(other);

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;

    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;

    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;

    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}");
}
