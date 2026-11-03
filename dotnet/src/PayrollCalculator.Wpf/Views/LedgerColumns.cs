using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

/// <summary>
/// Attached behaviour: one read-only DataGrid column per ledger component (the base and each prime), bound to the row
/// indexer <c>[code]</c> and inserted before the columns declared in XAML after <see cref="InsertAtProperty"/>.
/// </summary>
public static class LedgerColumns
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
        "Columns", typeof(ObservableCollection<LedgerColumnViewModel>), typeof(LedgerColumns), new PropertyMetadata(null, OnColumnsChanged));

    /// <summary>Index at which the generated columns are inserted (the fixed columns before it stay first).</summary>
    public static readonly DependencyProperty InsertAtProperty = DependencyProperty.RegisterAttached(
        "InsertAt", typeof(int), typeof(LedgerColumns), new PropertyMetadata(0));

    private static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
        "Handler", typeof(NotifyCollectionChangedEventHandler), typeof(LedgerColumns));

    public static ObservableCollection<LedgerColumnViewModel>? GetColumns(DependencyObject obj) =>
        (ObservableCollection<LedgerColumnViewModel>?)obj?.GetValue(ColumnsProperty);

    public static void SetColumns(DependencyObject obj, ObservableCollection<LedgerColumnViewModel>? value) => obj?.SetValue(ColumnsProperty, value);

    public static int GetInsertAt(DependencyObject obj) => (int)(obj?.GetValue(InsertAtProperty) ?? 0);

    public static void SetInsertAt(DependencyObject obj, int value) => obj?.SetValue(InsertAtProperty, value);

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if (e.OldValue is ObservableCollection<LedgerColumnViewModel> old && grid.GetValue(HandlerProperty) is NotifyCollectionChangedEventHandler oldHandler)
        {
            old.CollectionChanged -= oldHandler;
        }

        if (e.NewValue is ObservableCollection<LedgerColumnViewModel> columns)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => Rebuild(grid, columns);
            grid.SetValue(HandlerProperty, handler);
            columns.CollectionChanged += handler;
            Rebuild(grid, columns);
        }
    }

    private static void Rebuild(DataGrid grid, IEnumerable<LedgerColumnViewModel> columns)
    {
        foreach (var generated in grid.Columns.Where(c => c.GetValue(FrameworkElement.TagProperty) is LedgerColumnViewModel).ToList())
        {
            grid.Columns.Remove(generated);
        }

        var at = Math.Min(GetInsertAt(grid), grid.Columns.Count);
        foreach (var column in columns)
        {
            var generated = new DataGridTextColumn
            {
                Header = column.Header,
                MinWidth = 90,
                Binding = new Binding($"[{column.Code}]") { StringFormat = "0.000", TargetNullValue = "—" },
            };
            generated.SetValue(FrameworkElement.TagProperty, column);
            grid.Columns.Insert(at++, generated);
        }
    }
}
