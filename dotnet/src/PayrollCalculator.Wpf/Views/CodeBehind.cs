using System.Windows.Controls;
using System.Windows.Input;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

public partial class AgreementsView : UserControl
{
    public AgreementsView() => InitializeComponent();
}

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();
}

/// <summary>Passes the three passwords to the view model as arguments, then clears the boxes.</summary>
public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView() => InitializeComponent();

    private async void OnChange(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not ChangePasswordViewModel page)
        {
            return;
        }

        var ok = await page.ChangeAsync(CurrentPasswordBox.Password, NewPasswordBox.Password, ConfirmPasswordBox.Password)
            .ConfigureAwait(true);
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
        if (ok)
        {
            CurrentPasswordBox.Clear();
        }
    }
}

public partial class AboutView : UserControl
{
    public AboutView() => InitializeComponent();

    private void OnCopyInfo(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AboutViewModel about)
        {
            System.Windows.Clipboard.SetText(about.SupportText);
            if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel shell)
            {
                shell.Status.Message = PayrollCalculator.Wpf.Resources.Labels.InfoCopied;
            }
        }
    }
}

public partial class SalaryTablesView : UserControl
{
    public SalaryTablesView() => InitializeComponent();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e) =>
        (DataContext as SalaryTablesViewModel)?.EditCommand.Execute(null);
}

public partial class AllowancesView : UserControl
{
    public AllowancesView() => InitializeComponent();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e) =>
        (DataContext as AllowancesViewModel)?.EditCommand.Execute(null);
}

public partial class AllowanceEditorView : UserControl
{
    public AllowanceEditorView() => InitializeComponent();
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
