using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

/// <summary>
/// Attached behaviour: keeps one editable DataGrid column per degree, bound to the row indexer <c>[degreeKey]</c>.
/// Columns declared in XAML (the category label) are left untouched.
/// </summary>
public static class MatrixColumns
{
    public static readonly DependencyProperty DegreesProperty = DependencyProperty.RegisterAttached(
        "Degrees", typeof(ObservableCollection<DegreeColumnViewModel>), typeof(MatrixColumns),
        new PropertyMetadata(null, OnDegreesChanged));

    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
        "Handler", typeof(NotifyCollectionChangedEventHandler), typeof(MatrixColumns));

    public static ObservableCollection<DegreeColumnViewModel>? GetDegrees(DependencyObject obj) =>
        (ObservableCollection<DegreeColumnViewModel>?)obj?.GetValue(DegreesProperty);

    public static void SetDegrees(DependencyObject obj, ObservableCollection<DegreeColumnViewModel>? value) =>
        obj?.SetValue(DegreesProperty, value);

    private static void OnDegreesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if (e.OldValue is ObservableCollection<DegreeColumnViewModel> old && grid.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler oldHandler)
        {
            old.CollectionChanged -= oldHandler;
        }

        if (e.NewValue is ObservableCollection<DegreeColumnViewModel> degrees)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => Rebuild(grid, degrees);
            grid.SetValue(HandlerProperty, handler);
            degrees.CollectionChanged += handler;
            Rebuild(grid, degrees);
        }
    }

    private static void Rebuild(DataGrid grid, IEnumerable<DegreeColumnViewModel> degrees)
    {
        foreach (var generated in grid.Columns.Where(c => c.GetValue(FrameworkElement.TagProperty) is DegreeColumnViewModel).ToList())
        {
            grid.Columns.Remove(generated);
        }

        foreach (var degree in degrees.OrderBy(x => x.Number))
        {
            var header = new TextBlock { DataContext = degree };
            header.SetBinding(TextBlock.TextProperty, new Binding(nameof(DegreeColumnViewModel.Header)));
            var column = new DataGridTextColumn
            {
                Header = header,
                MinWidth = 90,
                Binding = new Binding($"[{degree.Key}]")
                {
                    TargetNullValue = string.Empty,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                },
            };
            column.SetValue(FrameworkElement.TagProperty, degree);
            grid.Columns.Add(column);

            // Re-sort the columns when a degree number is edited.
            PropertyChangedEventHandler? onNumber = null;
            onNumber = (_, args) =>
            {
                if (args.PropertyName == nameof(DegreeColumnViewModel.Number) && grid.Columns.Contains(column))
                {
                    degree.PropertyChanged -= onNumber;
                    Rebuild(grid, GetDegrees(grid) ?? []);
                }
            };
            degree.PropertyChanged += onNumber;
        }
    }
}
