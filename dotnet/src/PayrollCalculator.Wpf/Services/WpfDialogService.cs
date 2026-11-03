using System.Windows;
using Microsoft.Win32;
using PayrollCalculator.Wpf.Resources;

namespace PayrollCalculator.Wpf.Services;

public sealed class WpfDialogService : IDialogService
{
    private const MessageBoxOptions Rtl = MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign;

    public bool Confirm(string message) =>
        MessageBox.Show(message, Labels.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, Rtl)
        == MessageBoxResult.Yes;

    public void ShowError(string message) =>
        MessageBox.Show(message, Labels.Error, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, Rtl);

    public void ShowInfo(string message) =>
        MessageBox.Show(message, Labels.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, Rtl);

    public string? OpenFile(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog { Filter = filter, FileName = defaultFileName };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
