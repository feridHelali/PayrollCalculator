using System.Windows;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        FitToScreen();
    }

    /// <summary>
    /// Opens maximized on the work area (the screen minus the taskbar). The restored size is 90 % of that area,
    /// and the minimum size never exceeds it, so the window fits on a 1366 × 768 laptop as well as a large monitor.
    /// </summary>
    private void FitToScreen()
    {
        var area = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, area.Width);
        MinHeight = Math.Min(MinHeight, area.Height);
        Width = Math.Max(MinWidth, area.Width * 0.9);
        Height = Math.Max(MinHeight, area.Height * 0.9);
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowState = WindowState.Maximized;
    }
}
