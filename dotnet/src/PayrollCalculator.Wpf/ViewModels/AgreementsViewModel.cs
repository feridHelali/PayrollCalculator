using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>List of agreements with an inline editor.</summary>
public sealed partial class AgreementsViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.Agreements;

    public ObservableCollection<AgreementDto> Agreements { get; } = [];

    [ObservableProperty]
    public partial AgreementDto? SelectedAgreement { get; set; }

    [ObservableProperty]
    public partial int EditId { get; set; }

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? EditDescription { get; set; }

    public Task LoadAsync() => TryAsync(ReloadAsync);

    partial void OnSelectedAgreementChanged(AgreementDto? value)
    {
        EditId = value?.Id ?? 0;
        EditName = value?.Name ?? string.Empty;
        EditDescription = value?.Description;
    }

    [RelayCommand]
    private void New() => SelectedAgreement = null;

    [RelayCommand]
    private async Task SaveAsync() => await TryAsync(async () =>
    {
        var dto = new AgreementDto(EditId, EditName, EditDescription);
        var id = EditId == 0
            ? await Runner.RunAsync<AgreementService, int>(s => s.CreateAsync(dto)).ConfigureAwait(true)
            : await Runner.RunAsync<AgreementService, int>(async s => { await s.UpdateAsync(dto).ConfigureAwait(true); return dto.Id; }).ConfigureAwait(true);
        await ReloadAsync().ConfigureAwait(true);
        SelectedAgreement = Agreements.FirstOrDefault(a => a.Id == id);
    }, Labels.Saved).ConfigureAwait(true);

    [RelayCommand]
    private async Task DeleteAsync(AgreementDto? row)
    {
        if ((row ?? SelectedAgreement) is not { } selected || !Dialogs.Confirm(Labels.ConfirmDeleteAgreement))
        {
            return;
        }

        await TryAsync(async () =>
        {
            await Runner.RunAsync<AgreementService>(s => s.DeleteAsync(selected.Id)).ConfigureAwait(true);
            await ReloadAsync().ConfigureAwait(true);
        }, Labels.Deleted).ConfigureAwait(true);
    }

    private async Task ReloadAsync()
    {
        var items = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Agreements.Clear();
        foreach (var item in items)
        {
            Agreements.Add(item);
        }
    }
}
