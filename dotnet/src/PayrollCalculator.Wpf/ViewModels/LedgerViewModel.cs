using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Application.Allowances;
using PayrollCalculator.Domain.Allowances;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>A prime of the affair's agreement, ticked when it applies to the employee.</summary>
public sealed partial class LedgerPrimeViewModel(int id, string name, AllowanceUnit unit, string? appliesTo) : ObservableObject
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public AllowanceUnit Unit { get; } = unit;

    public string? AppliesTo { get; } = appliesTo;

    public string Text => string.IsNullOrWhiteSpace(AppliesTo) ? Name : $"{Name} ({AppliesTo})";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>A generated column of the ledger grid, bound to the row indexer <c>[Code]</c>.</summary>
public sealed record LedgerColumnViewModel(string Code, string Header);

/// <summary>One month of the ledger; <c>this[code]</c> gives the amount of a column.</summary>
public sealed class LedgerRowViewModel(LedgerRowDto row)
{
    public string Month { get; } = row.Month.ToString();

    public string Category { get; } = row.Category;

    public int Degree { get; } = row.Degree;

    public decimal? BaseRate { get; } = row.BaseRate;

    /// <summary>Share of the month paid, for example 0.5484 for 17 of 31 days.</summary>
    public string Proration { get; } = row.Proration == 1m ? string.Empty : row.Proration.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    public decimal Total { get; } = row.Total;

    public LedgerIssue Issue { get; } = row.Issue;

    public decimal? this[string code] => row.Amounts.TryGetValue(code, out var amount) ? amount : null;
}

/// <summary>
/// The monthly ledger of an affair: base salary from its grids, plus the primes ticked for the employee, with totals
/// and a CSV export. The calculation is the event model's (sprint 05).
/// </summary>
public sealed partial class LedgerViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    private AffairLedgerDto? _ledger;

    public override string Title => Labels.Ledger;

    [ObservableProperty]
    public partial AffairDto? Affair { get; private set; }

    public ObservableCollection<LedgerPrimeViewModel> Primes { get; } = [];

    public ObservableCollection<LedgerColumnViewModel> Columns { get; } = [];

    public ObservableCollection<LedgerRowViewModel> Rows { get; } = [];

    /// <summary>Totals of the columns, in the order of <see cref="Columns"/>, as "name: amount".</summary>
    public ObservableCollection<string> Totals { get; } = [];

    [ObservableProperty]
    public partial decimal GrandTotal { get; private set; }

    [ObservableProperty]
    public partial int UnresolvedMonths { get; private set; }

    public bool HasPrimes => Primes.Count > 0;

    public Task LoadAsync(int affairId) => TryAsync(async () =>
    {
        Affair = await Runner.RunAsync<AffairService, AffairDto>(s => s.GetAsync(affairId)).ConfigureAwait(true);
        var primes = await Runner.RunAsync<AllowanceService, IReadOnlyList<AllowanceSummaryDto>>(s => s.ListAsync(Affair.AgreementId))
            .ConfigureAwait(true);
        Primes.Clear();
        foreach (var p in primes)
        {
            // Preselect the primes for everyone; the ones for a group (cadres, drivers...) are left to the expert.
            Primes.Add(new LedgerPrimeViewModel(p.Id, p.Name, p.Unit, p.AppliesTo) { IsSelected = string.IsNullOrWhiteSpace(p.AppliesTo) });
        }

        OnPropertyChanged(nameof(HasPrimes));
        await CalculateCoreAsync().ConfigureAwait(true);
    });

    [RelayCommand]
    private async Task CalculateAsync() => await TryAsync(CalculateCoreAsync, Labels.LedgerCalculated).ConfigureAwait(true);

    private async Task CalculateCoreAsync()
    {
        if (Affair is null)
        {
            return;
        }

        var ids = Primes.Where(p => p.IsSelected).Select(p => p.Id).ToList();
        _ledger = await Runner.RunAsync<LedgerService, AffairLedgerDto>(s => s.BuildAsync(Affair.Id, ids)).ConfigureAwait(true);

        Columns.Clear();
        foreach (var c in _ledger.Columns)
        {
            Columns.Add(new LedgerColumnViewModel(c.Code, c.Code == AffairLedgerDto.BaseCode ? Labels.BaseSalary : c.Name));
        }

        Rows.Clear();
        foreach (var r in _ledger.Rows)
        {
            Rows.Add(new LedgerRowViewModel(r));
        }

        Totals.Clear();
        foreach (var c in Columns)
        {
            Totals.Add($"{c.Header}: {_ledger.Totals[c.Code]:0.000}");
        }

        GrandTotal = _ledger.GrandTotal;
        UnresolvedMonths = _ledger.UnresolvedMonths;
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (_ledger is null || Affair is null || Dialogs.SaveFile(Labels.CsvFilter, $"ledger-{Affair.AffairNumber}.csv") is not { } path)
        {
            return;
        }

        var csv = LedgerCsv.Write(_ledger, Labels.BaseSalary);
        await TryAsync(() => File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)), Labels.Saved)
            .ConfigureAwait(true);
    }

    [RelayCommand]
    private Task BackAsync() => navigator.GoAsync<AffairsViewModel>(p => p.LoadAsync());
}
