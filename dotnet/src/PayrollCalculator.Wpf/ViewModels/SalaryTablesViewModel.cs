using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>List of salary tables, filterable by agreement.</summary>
public sealed partial class SalaryTablesViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.SalaryTables;

    /// <summary>Filter options; the first entry (Id 0) means "all agreements".</summary>
    public ObservableCollection<AgreementDto> AgreementFilter { get; } = [];

    public ObservableCollection<SalaryTableSummaryDto> Tables { get; } = [];

    [ObservableProperty]
    public partial AgreementDto? SelectedFilter { get; set; }

    [ObservableProperty]
    public partial SalaryTableSummaryDto? SelectedTable { get; set; }

    public Task LoadAsync() => TryAsync(async () =>
    {
        var agreements = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        AgreementFilter.Clear();
        AgreementFilter.Add(new AgreementDto(0, Labels.AllAgreements, null));
        foreach (var a in agreements)
        {
            AgreementFilter.Add(a);
        }

        SelectedFilter = AgreementFilter[0];
        await ReloadAsync().ConfigureAwait(true);
    });

    partial void OnSelectedFilterChanged(AgreementDto? value) => _ = TryAsync(ReloadAsync);

    [RelayCommand]
    private Task NewAsync() =>
        navigator.GoAsync<SalaryTableEditorViewModel>(e => e.LoadAsync(null, SelectedFilter is { Id: > 0 } f ? f.Id : null));

    [RelayCommand]
    private Task EditAsync() =>
        SelectedTable is { } t ? navigator.GoAsync<SalaryTableEditorViewModel>(e => e.LoadAsync(t.Id)) : Task.CompletedTask;

    [RelayCommand]
    private async Task DeleteAsync(SalaryTableSummaryDto? row)
    {
        if ((row ?? SelectedTable) is not { } selected || !Dialogs.Confirm(Labels.ConfirmDelete))
        {
            return;
        }

        await TryAsync(async () =>
        {
            await Runner.RunAsync<SalaryTableService>(s => s.DeleteAsync(selected.Id)).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
        }, Labels.Deleted).ConfigureAwait(true);
    }

    private async Task ReloadAsync()
    {
        int? filter = SelectedFilter is { Id: > 0 } f ? f.Id : null;
        var rows = await Runner.RunAsync<SalaryTableService, IReadOnlyList<SalaryTableSummaryDto>>(s => s.ListAsync(filter)).ConfigureAwait(true);
        Tables.Clear();
        foreach (var row in rows)
        {
            Tables.Add(row);
        }
    }
}
