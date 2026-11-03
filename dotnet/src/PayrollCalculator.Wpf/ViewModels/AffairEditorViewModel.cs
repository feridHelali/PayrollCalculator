using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>Editor for one affair; the table and category pickers follow the selected agreement/table.</summary>
public sealed partial class AffairEditorViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Affair;

    public ObservableCollection<AgreementDto> Agreements { get; } = [];

    public ObservableCollection<SalaryTableSummaryDto> Tables { get; } = [];

    public ObservableCollection<string> CategoryOptions { get; } = [];

    public ObservableCollection<int> DegreeOptions { get; } = [];

    /// <summary>The latest lookup triggered by a selection change (awaitable by tests).</summary>
    public Task PendingLoad { get; private set; } = Task.CompletedTask;

    [ObservableProperty]
    public partial int Id { get; private set; }

    [ObservableProperty]
    public partial string AffairNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AffairTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Claimant { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial int? SelectedAgreementId { get; set; }

    [ObservableProperty]
    public partial int? SelectedTableId { get; set; }

    [ObservableProperty]
    public partial string InitialCategory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int InitialDegree { get; set; } = 1;

    public Task LoadAsync(int? id) => TryAsync(async () =>
    {
        var agreements = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Agreements.Clear();
        foreach (var a in agreements)
        {
            Agreements.Add(a);
        }

        if (id is not { } affairId)
        {
            return;
        }

        var dto = await Runner.RunAsync<AffairService, AffairDto>(s => s.GetAsync(affairId)).ConfigureAwait(true);
        Id = dto.Id;
        AffairNumber = dto.AffairNumber;
        AffairTitle = dto.Title;
        Claimant = dto.Claimant;
        StartDate = ToDateTime(dto.StartDate);
        EndDate = ToDateTime(dto.EndDate);
        SelectedAgreementId = dto.AgreementId;
        await PendingLoad.ConfigureAwait(true);
        SelectedTableId = dto.SalaryTableId;
        await PendingLoad.ConfigureAwait(true);
        InitialCategory = dto.InitialCategory;
        InitialDegree = dto.InitialDegree;
    });

    partial void OnSelectedAgreementIdChanged(int? value) => PendingLoad = TryAsync(() => LoadTablesAsync(value));

    partial void OnSelectedTableIdChanged(int? value) => PendingLoad = TryAsync(() => LoadAxesAsync(value));

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAgreementId is not { } agreementId)
        {
            Dialogs.ShowError(Labels.SelectAgreement);
            return;
        }

        if (ToDate(StartDate) is not { } start || ToDate(EndDate) is not { } end)
        {
            Dialogs.ShowError(Labels.DatesRequired);
            return;
        }

        var dto = new AffairDto(Id, AffairNumber, AffairTitle, Claimant, start, end, agreementId, SelectedTableId, InitialCategory, InitialDegree);
        if (await TryAsync(async () =>
                Id = await Runner.RunAsync<AffairService, int>(s => s.SaveAsync(dto)).ConfigureAwait(true),
                Labels.Saved).ConfigureAwait(true))
        {
            await navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync()).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private Task TimelineAsync() =>
        Id > 0 ? navigator.GoAsync<TimelineViewModel>(t => t.LoadAsync(Id)) : Task.CompletedTask;

    [RelayCommand]
    private Task BackAsync() => navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync());

    private async Task LoadTablesAsync(int? agreementId)
    {
        var rows = agreementId is { } id
            ? await Runner.RunAsync<SalaryTableService, IReadOnlyList<SalaryTableSummaryDto>>(s => s.ListAsync(id)).ConfigureAwait(true)
            : [];
        Tables.Clear();
        foreach (var row in rows)
        {
            Tables.Add(row);
        }

        if (SelectedTableId is { } selected && rows.All(r => r.Id != selected))
        {
            SelectedTableId = null;
        }
    }

    private async Task LoadAxesAsync(int? tableId)
    {
        CategoryOptions.Clear();
        DegreeOptions.Clear();
        if (tableId is not { } id)
        {
            return;
        }

        var table = await Runner.RunAsync<SalaryTableService, SalaryTableDto>(s => s.GetAsync(id)).ConfigureAwait(true);
        foreach (var c in table.Categories)
        {
            CategoryOptions.Add(c.Label);
        }

        foreach (var d in table.Degrees)
        {
            DegreeOptions.Add(d.Number);
        }
    }
}
