using System.Windows;
using System.Windows.Input;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

/// <summary>Modal activation window. <c>DialogResult</c> is true once a serial is accepted.</summary>
public partial class ActivationWindow : Window
{
    private readonly ActivationViewModel _viewModel;

    public ActivationWindow(ActivationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += (_, _) => SerialBox.Focus();
    }

    private async void OnActivate(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.ActivateAsync().ConfigureAwait(true))
        {
            DialogResult = true;
        }
        else
        {
            SerialBox.Focus();
            SerialBox.SelectAll();
        }
    }

    private void OnSerialKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnActivate(sender, e);
        }
    }

    private void OnQuit(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
