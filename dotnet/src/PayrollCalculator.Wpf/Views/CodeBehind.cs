using System.Windows.Controls;
using System.Windows.Input;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

public partial class AgreementsView : UserControl
{
    public AgreementsView() => InitializeComponent();
}

public partial class SalaryTablesView : UserControl
{
    public SalaryTablesView() => InitializeComponent();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e) =>
        (DataContext as SalaryTablesViewModel)?.EditCommand.Execute(null);
}

public partial class SalaryTableEditorView : UserControl
{
    public SalaryTableEditorView() => InitializeComponent();
}

public partial class AffairsView : UserControl
{
    public AffairsView() => InitializeComponent();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e) =>
        (DataContext as AffairsViewModel)?.EditCommand.Execute(null);
}

public partial class AffairEditorView : UserControl
{
    public AffairEditorView() => InitializeComponent();
}

public partial class TimelineView : UserControl
{
    public TimelineView() => InitializeComponent();
}
