using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.Allowances;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>List of the primes and allowances, filterable by agreement.</summary>
public sealed partial class AllowancesViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Allowances;

    /// <summary>Filter options; the first entry (Id 0) means "all agreements".</summary>
    public ObservableCollection<AgreementDto> AgreementFilter { get; } = [];

    public ObservableCollection<AllowanceSummaryDto> Items { get; } = [];

    [ObservableProperty]
    public partial AgreementDto? SelectedFilter { get; set; }

    [ObservableProperty]
    public partial AllowanceSummaryDto? SelectedItem { get; set; }

    public Task LoadAsync(int? agreementId = null) => TryAsync(async () =>
    {
        var agreements = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        AgreementFilter.Clear();
        AgreementFilter.Add(new AgreementDto(0, Labels.AllAgreements, null));
        foreach (var a in agreements)
        {
            AgreementFilter.Add(a);
        }

        SelectedFilter = AgreementFilter.FirstOrDefault(a => a.Id == agreementId) ?? AgreementFilter[0];
        await ReloadAsync().ConfigureAwait(true);
    });

    partial void OnSelectedFilterChanged(AgreementDto? value) => _ = TryAsync(ReloadAsync);

    [RelayCommand]
    private Task NewAsync() =>
        navigator.GoAsync<AllowanceEditorViewModel>(e => e.LoadAsync(null, SelectedFilter is { Id: > 0 } f ? f.Id : null));

    [RelayCommand]
    private Task EditAsync() =>
        SelectedItem is { } a ? navigator.GoAsync<AllowanceEditorViewModel>(e => e.LoadAsync(a.Id)) : Task.CompletedTask;

    [RelayCommand]
    private async Task DeleteAsync(AllowanceSummaryDto? row)
    {
        if ((row ?? SelectedItem) is not { } selected || !Dialogs.Confirm(Labels.ConfirmDelete))
        {
            return;
        }

        await TryAsync(async () =>
        {
            await Runner.RunAsync<AllowanceService>(s => s.DeleteAsync(selected.Id)).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
        }, Labels.Deleted).ConfigureAwait(true);
    }

    private async Task ReloadAsync()
    {
        int? filter = SelectedFilter is { Id: > 0 } f ? f.Id : null;
        var rows = await Runner.RunAsync<AllowanceService, IReadOnlyList<AllowanceSummaryDto>>(s => s.ListAsync(filter)).ConfigureAwait(true);
        Items.Clear();
        foreach (var row in rows)
        {
            Items.Add(row);
        }
    }
}

/// <summary>One dated amount of a prime, as edited in the grid.</summary>
public sealed partial class AllowanceRateRowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial DateTime? EffectiveFrom { get; set; }

    [ObservableProperty]
    public partial decimal? Value { get; set; }

    [ObservableProperty]
    public partial string? Source { get; set; }
}

/// <summary>Editor of one prime: what it is, who gets it, and its amounts over time.</summary>
public sealed partial class AllowanceEditorViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Allowance;

    public IReadOnlyList<AllowanceUnit> Units { get; } = Enum.GetValues<AllowanceUnit>();

    public ObservableCollection<AgreementDto> Agreements { get; } = [];

    public ObservableCollection<AllowanceRateRowViewModel> Rates { get; } = [];

    [ObservableProperty]
    public partial int Id { get; private set; }

    [ObservableProperty]
    public partial int? SelectedAgreementId { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AllowanceUnit Unit { get; set; }

    [ObservableProperty]
    public partial string? AppliesTo { get; set; }

    [ObservableProperty]
    public partial string? Conditions { get; set; }

    public Task LoadAsync(int? id, int? agreementId = null) => TryAsync(async () =>
    {
        var agreements = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Agreements.Clear();
        foreach (var a in agreements)
        {
            Agreements.Add(a);
        }

        if (id is not { } allowanceId)
        {
            SelectedAgreementId = agreementId;
            return;
        }

        var dto = await Runner.RunAsync<AllowanceService, AllowanceDto>(s => s.GetAsync(allowanceId)).ConfigureAwait(true);
        Id = dto.Id;
        SelectedAgreementId = dto.AgreementId;
        Name = dto.Name;
        Unit = dto.Unit;
        AppliesTo = dto.AppliesTo;
        Conditions = dto.Conditions;
        Rates.Clear();
        foreach (var r in dto.Rates)
        {
            Rates.Add(new AllowanceRateRowViewModel { EffectiveFrom = ToDateTime(r.EffectiveFrom), Value = r.Value, Source = r.Source });
        }
    });

    [RelayCommand]
    private void AddRate() =>
        Rates.Add(new AllowanceRateRowViewModel { Source = Rates.LastOrDefault()?.Source }); // same text as the previous line, most often

    [RelayCommand]
    private void RemoveRate(AllowanceRateRowViewModel? row)
    {
        if (row is not null && Dialogs.Confirm(Labels.ConfirmDelete))
        {
            Rates.Remove(row);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedAgreementId is not { } agreementId)
        {
            Dialogs.ShowError(Labels.SelectAgreement);
            return;
        }

        if (Rates.Any(r => r.EffectiveFrom is null || r.Value is null))
        {
            Dialogs.ShowError(Labels.RateIncomplete);
            return;
        }

        var dto = new AllowanceDto(Id, agreementId, Name, Unit, AppliesTo, Conditions,
            Rates.Select(r => new AllowanceRateDto(ToDate(r.EffectiveFrom)!.Value, r.Value!.Value, r.Source)).ToList());
        if (await TryAsync(async () =>
                Id = await Runner.RunAsync<AllowanceService, int>(s => s.SaveAsync(dto)).ConfigureAwait(true),
                Labels.Saved).ConfigureAwait(true))
        {
            await navigator.GoAsync<AllowancesViewModel>(p => p.LoadAsync(agreementId)).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private Task BackAsync() => navigator.GoAsync<AllowancesViewModel>(p => p.LoadAsync(SelectedAgreementId));
}
