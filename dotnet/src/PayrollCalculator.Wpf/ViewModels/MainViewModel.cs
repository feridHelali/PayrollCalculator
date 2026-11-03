using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.LegacyImport;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>The side-menu entry a page belongs to; drives the active-item highlight.</summary>
public enum NavSection
{
    None,
    Affairs,
    SalaryTables,
    Agreements,
    Users,
    About,
}

/// <summary>Shell: side navigation, current page, signed-in user, status bar and legacy import.</summary>
public sealed partial class MainViewModel : PageViewModel
{
    private readonly Navigator _navigator;
    private readonly CurrentUser _currentUser;

    public MainViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, Navigator navigator, CurrentUser currentUser)
        : base(runner, dialogs, status)
    {
        _navigator = navigator;
        _currentUser = currentUser;
        ((INotifyPropertyChanged)navigator).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Navigator.Current))
            {
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(ActiveSection));
                OnPropertyChanged(nameof(IsAffairsActive));
                OnPropertyChanged(nameof(IsSalaryTablesActive));
                OnPropertyChanged(nameof(IsAgreementsActive));
                OnPropertyChanged(nameof(IsUsersActive));
                OnPropertyChanged(nameof(IsAboutActive));
            }
        };
        currentUser.Changed += (_, _) => OnPropertyChanged(nameof(UserDisplayName));
    }

    public override string Title => Labels.AppTitle;

    public object? CurrentPage => _navigator.Current;

    public new StatusViewModel Status => base.Status;

    public string UserDisplayName => _currentUser.Session?.DisplayName ?? string.Empty;

    public NavSection ActiveSection => _navigator.Current switch
    {
        AffairsViewModel or AffairEditorViewModel or TimelineViewModel => NavSection.Affairs,
        SalaryTablesViewModel or SalaryTableEditorViewModel => NavSection.SalaryTables,
        AgreementsViewModel => NavSection.Agreements,
        UsersViewModel => NavSection.Users,
        AboutViewModel => NavSection.About,
        _ => NavSection.None,
    };

    public bool IsAffairsActive => ActiveSection == NavSection.Affairs;

    public bool IsSalaryTablesActive => ActiveSection == NavSection.SalaryTables;

    public bool IsAgreementsActive => ActiveSection == NavSection.Agreements;

    public bool IsUsersActive => ActiveSection == NavSection.Users;

    public bool IsAboutActive => ActiveSection == NavSection.About;

    [RelayCommand]
    private Task ShowAgreementsAsync() => _navigator.GoAsync<AgreementsViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowSalaryTablesAsync() => _navigator.GoAsync<SalaryTablesViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowAffairsAsync() => _navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowUsersAsync() => _navigator.GoAsync<UsersViewModel>(p => p.LoadAsync());

    [RelayCommand]
    private Task ShowAboutAsync() => _navigator.GoAsync<AboutViewModel>();

    [RelayCommand]
    private Task ShowChangePasswordAsync() => _navigator.GoAsync<ChangePasswordViewModel>();

    /// <summary>Closes the session; the application shell reacts by showing the sign-in window again.</summary>
    [RelayCommand]
    private void SignOut() => _currentUser.SignOut();

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
