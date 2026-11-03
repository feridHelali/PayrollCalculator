using System.Globalization;
using System.Windows.Data;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Wpf.Resources;

namespace PayrollCalculator.Wpf.Converters;

public sealed class SalaryTypeTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Labels.SalaryTypeText(value as SalaryType?);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IssueTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TimelineIssue issue ? Labels.IssueText(issue) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Shows dates as yyyy-MM-dd regardless of the Arabic UI culture.</summary>
public sealed class DateOnlyTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
