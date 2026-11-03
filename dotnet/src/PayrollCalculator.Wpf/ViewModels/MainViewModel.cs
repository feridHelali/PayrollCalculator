using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>Shell: side navigation, current page, status bar and legacy import.</summary>
public sealed partial class MainViewModel : PageViewModel
{
    private readonly Navigator _navigator;

    public MainViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, Navigator navigator)
        : base(runner, dialogs, status)
    {
        _navigator = navigator;
        ((INotifyPropertyChanged)navigator).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Navigator.Current))
            {
                OnPropertyChanged(nameof(CurrentPage));
            }
        };
    }

    public override string Title => Labels.AppTitle;

    public object? CurrentPage => _navigator.Current;

    public new StatusViewModel Status => base.Status;

    [RelayCommand]
    private Task ShowAgreementsAsync() => _navigator.GoAsync<AgreementsViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowSalaryTablesAsync() => _navigator.GoAsync<SalaryTablesViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowAffairsAsync() => _navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private async Task ImportLegacyAsync()
    {
        var path = Dialogs.OpenFile(Labels.LegacyDbFilter);
        if (path is null)
        {
            return;
        }

        ImportReport? report = null;
        var ok = await TryAsync(async () =>
            report = await Runner.RunAsync<ILegacyDataImporter, ImportReport>(i => i.ImportAsync(path)).ConfigureAwait(true),
            Labels.ImportDone).ConfigureAwait(true);
        if (!ok || report is null)
        {
            return;
        }

        var summary = $"{Labels.ImportDone}\n{Labels.Agreements}: {report.Agreements}\n{Labels.SalaryTables}: {report.SalaryTables} ({report.Cells})\n{Labels.Affairs}: {report.Affairs}";
        if (report.Warnings.Count > 0)
        {
            summary += "\n\n" + string.Join('\n', report.Warnings);
        }

        Dialogs.ShowInfo(summary);
        await ShowAffairsAsync().ConfigureAwait(true);
    }
}
