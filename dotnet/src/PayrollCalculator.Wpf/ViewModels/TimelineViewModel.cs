using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>Month-by-month base salary of an affair, with CSV export.</summary>
public sealed partial class TimelineViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    private SalaryTimeline? _timeline;

    public override string Title => Labels.Timeline;

    [ObservableProperty]
    public partial AffairDto? Affair { get; private set; }

    public IReadOnlyList<MonthlySalaryLine> Lines => _timeline?.Lines ?? [];

    public decimal TotalMonthlyAmount => _timeline?.TotalMonthlyAmount ?? 0m;

    public int UnresolvedMonths => _timeline?.UnresolvedMonths ?? 0;

    public Task LoadAsync(int affairId) => TryAsync(async () =>
    {
        Affair = await Runner.RunAsync<AffairService, AffairDto>(s => s.GetAsync(affairId)).ConfigureAwait(true);
        _timeline = await Runner.RunAsync<AffairService, SalaryTimeline>(s => s.CalculateTimelineAsync(affairId)).ConfigureAwait(true);
        OnPropertyChanged(nameof(Lines));
        OnPropertyChanged(nameof(TotalMonthlyAmount));
        OnPropertyChanged(nameof(UnresolvedMonths));
    });

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (_timeline is null || Affair is null)
        {
            return;
        }

        var path = Dialogs.SaveFile(Labels.CsvFilter, $"affair-{Affair.AffairNumber}.csv");
        if (path is null)
        {
            return;
        }

        var csv = TimelineCsv.Write(_timeline);
        await TryAsync(() => File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)), Labels.Saved)
            .ConfigureAwait(true);
    }

    [RelayCommand]
    private Task BackAsync() => navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync());
}
