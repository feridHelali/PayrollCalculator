using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>List of affairs (القضايا).</summary>
public sealed partial class AffairsViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Affairs;

    public ObservableCollection<AffairSummaryDto> Affairs { get; } = [];

    [ObservableProperty]
    public partial AffairSummaryDto? SelectedAffair { get; set; }

    public Task LoadAsync() => TryAsync(async () =>
    {
        var rows = await Runner.RunAsync<AffairService, IReadOnlyList<AffairSummaryDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Affairs.Clear();
        foreach (var row in rows)
        {
            Affairs.Add(row);
        }
    });

    [RelayCommand]
    private Task NewAsync() => navigator.GoAsync<AffairEditorViewModel>(e => e.LoadAsync(null));

    [RelayCommand]
    private Task EditAsync() =>
        SelectedAffair is { } a ? navigator.GoAsync<AffairEditorViewModel>(e => e.LoadAsync(a.Id)) : Task.CompletedTask;

    [RelayCommand]
    private Task TimelineAsync() =>
        SelectedAffair is { } a ? navigator.GoAsync<TimelineViewModel>(t => t.LoadAsync(a.Id)) : Task.CompletedTask;

    [RelayCommand]
    private Task LedgerAsync() =>
        SelectedAffair is { } a ? navigator.GoAsync<LedgerViewModel>(l => l.LoadAsync(a.Id)) : Task.CompletedTask;

    [RelayCommand]
    private async Task DeleteAsync(AffairSummaryDto? row)
    {
        if ((row ?? SelectedAffair) is not { } selected || !Dialogs.Confirm(Labels.ConfirmDelete))
        {
            return;
        }

        if (await TryAsync(() => Runner.RunAsync<AffairService>(s => s.DeleteAsync(selected.Id)), Labels.Deleted).ConfigureAwait(true))
        {
            Affairs.Remove(selected);
        }
    }
}
