using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Views;

/// <summary>Modal sign-in window. <c>DialogResult</c> is true once a session is open.</summary>
/// <remarks>
/// Passwords are read straight from the boxes and handed to the view model as arguments, then the boxes are
/// cleared: the plaintext is never stored in a field or a bindable property.
/// </remarks>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += (_, _) => (string.IsNullOrEmpty(viewModel.UserName) ? (Control)UserNameBox : PasswordBox).Focus();
    }

    private async void OnSignIn(object sender, RoutedEventArgs e)
    {
        var ok = await _viewModel.SignInAsync(PasswordBox.Password).ConfigureAwait(true);
        PasswordBox.Clear();
        if (ok)
        {
            DialogResult = true;
        }
        else if (_viewModel.IsChangingPassword)
        {
            CurrentPasswordBox.Focus();
        }
        else
        {
            PasswordBox.Focus();
        }
    }

    private async void OnChangePassword(object sender, RoutedEventArgs e)
    {
        var ok = await _viewModel.ChangePasswordAsync(
            CurrentPasswordBox.Password, NewPasswordBox.Password, ConfirmPasswordBox.Password).ConfigureAwait(true);
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
        if (ok)
        {
            CurrentPasswordBox.Clear();
            DialogResult = true;
        }
        else
        {
            NewPasswordBox.Focus();
        }
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnSignIn(sender, e);
        }
    }

    private void OnConfirmKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnChangePassword(sender, e);
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
