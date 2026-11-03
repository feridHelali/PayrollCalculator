using System.Windows;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
