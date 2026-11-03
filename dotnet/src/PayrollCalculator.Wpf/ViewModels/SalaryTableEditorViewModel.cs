using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>A grid column: one degree.</summary>
public sealed partial class DegreeColumnViewModel : ObservableObject
{
    public DegreeColumnViewModel(string key, int number, int yearsInDegree)
    {
        Key = key;
        Number = number;
        YearsInDegree = yearsInDegree;
    }

    public string Key { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Header))]
    public partial int Number { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Header))]
    public partial int YearsInDegree { get; set; }

    public string Header => string.Create(CultureInfo.InvariantCulture, $"{Labels.Degree} {Number} ({YearsInDegree})");
}

/// <summary>A grid row: one category with its amount per degree key (bound as <c>[degreeKey]</c>).</summary>
public sealed partial class CategoryRowViewModel : ObservableObject
{
    private readonly Dictionary<string, decimal?> _amounts = [];

    public CategoryRowViewModel(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }

    [ObservableProperty]
    public partial string Label { get; set; }

    public decimal? this[string degreeKey]
    {
        get => _amounts.GetValueOrDefault(degreeKey);
        set
        {
            _amounts[degreeKey] = value;
            OnPropertyChanged("Item[]");
        }
    }

    public IEnumerable<KeyValuePair<string, decimal>> Amounts =>
        _amounts.Where(a => a.Value is not null).Select(a => KeyValuePair.Create(a.Key, a.Value!.Value));

    internal void Forget(string degreeKey)
    {
        if (_amounts.Remove(degreeKey))
        {
            OnPropertyChanged("Item[]");
        }
    }
}

/// <summary>Editor for one salary table: header, axes and the amount grid.</summary>
public sealed partial class SalaryTableEditorViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status, INavigator navigator)
    : PageViewModel(runner, dialogs, status)
{
    public override string Title => Labels.SalaryTable;

    public IReadOnlyList<SalaryType> SalaryTypes { get; } = Enum.GetValues<SalaryType>();

    public ObservableCollection<AgreementDto> Agreements { get; } = [];

    /// <summary>The kinds of grids the selected agreement defines; picking one fills the salary type and the employees.</summary>
    public ObservableCollection<GridKindDto> GridKinds { get; } = [];

    public bool HasGridKinds => GridKinds.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DefaultRegimeText))]
    public partial GridKindDto? SelectedGridKind { get; set; }

    /// <summary>The default working time of the selected grid kind, for example "48 ساعة في الأسبوع".</summary>
    public string DefaultRegimeText => SelectedGridKind is { } kind
        ? string.Create(CultureInfo.InvariantCulture, $"{kind.DefaultWeeklyHours} {Labels.HoursPerWeek}")
        : string.Empty;

    private bool _filling;

    public ObservableCollection<CategoryRowViewModel> Categories { get; } = [];

    public ObservableCollection<DegreeColumnViewModel> Degrees { get; } = [];

    [ObservableProperty]
    public partial int Id { get; private set; }

    [ObservableProperty]
    public partial int? SelectedAgreementId { get; set; }

    [ObservableProperty]
    public partial string TableNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SalaryType SalaryType { get; set; }

    [ObservableProperty]
    public partial string ConcernedEmployees { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial CategoryRowViewModel? SelectedCategory { get; set; }

    public Task LoadAsync(int? id, int? agreementId = null) => TryAsync(async () =>
    {
        var agreements = await Runner.RunAsync<AgreementService, IReadOnlyList<AgreementDto>>(s => s.ListAsync()).ConfigureAwait(true);
        Agreements.Clear();
        foreach (var a in agreements)
        {
            Agreements.Add(a);
        }

        if (id is not { } tableId)
        {
            SelectedAgreementId = agreementId;
            return;
        }

        var dto = await Runner.RunAsync<SalaryTableService, SalaryTableDto>(s => s.GetAsync(tableId)).ConfigureAwait(true);
        Fill(dto);
    });

    partial void OnSelectedAgreementIdChanged(int? value)
    {
        GridKinds.Clear();
        foreach (var kind in Agreements.FirstOrDefault(a => a.Id == value)?.GridKinds ?? [])
        {
            GridKinds.Add(kind);
        }

        SelectedGridKind = GridKinds.FirstOrDefault(k => k.Name == ConcernedEmployees);
        OnPropertyChanged(nameof(HasGridKinds));
    }

    partial void OnSelectedGridKindChanged(GridKindDto? oldValue, GridKindDto? newValue)
    {
        if (_filling || newValue is null)
        {
            return;
        }

        SalaryType = newValue.Unit == PayUnit.Hourly ? SalaryType.Hourly : SalaryType.Monthly;
        // Keep what the user typed; replace only an empty field or the previous kind's name.
        if (string.IsNullOrWhiteSpace(ConcernedEmployees) || ConcernedEmployees == oldValue?.Name)
        {
            ConcernedEmployees = newValue.Name;
        }
    }

    [RelayCommand]
    private void AddCategory()
    {
        var row = new CategoryRowViewModel(Guid.NewGuid().ToString(),
            string.Create(CultureInfo.InvariantCulture, $"{Categories.Count + 1} - {Labels.Category}"));
        Categories.Add(row);
        SelectedCategory = row;
    }

    [RelayCommand]
    private void RemoveCategory(CategoryRowViewModel? row)
    {
        if ((row ?? SelectedCategory) is { } target && Dialogs.Confirm(Labels.ConfirmDelete))
        {
            Categories.Remove(target);
        }
    }

    [RelayCommand]
    private void AddDegree()
    {
        var number = Degrees.Count == 0 ? 1 : Degrees.Max(d => d.Number) + 1;
        Degrees.Add(new DegreeColumnViewModel(Guid.NewGuid().ToString(), number, yearsInDegree: number == 1 ? 1 : 2));
    }

    [RelayCommand]
    private void RemoveDegree(DegreeColumnViewModel? degree)
    {
        if (degree is null || !Dialogs.Confirm(Labels.ConfirmDelete))
        {
            return;
        }

        Degrees.Remove(degree);
        foreach (var row in Categories)
        {
            row.Forget(degree.Key);
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

        if (ToDate(StartDate) is not { } start)
        {
            Dialogs.ShowError(Labels.DatesRequired);
            return;
        }

        var degreeKeys = Degrees.Select(d => d.Key).ToHashSet();
        var dto = new SalaryTableDto(
            Id, agreementId, TableNumber, SalaryType, ConcernedEmployees, start, ToDate(EndDate),
            Categories.Select(c => new CategoryDto(c.Key, c.Label)).ToList(),
            Degrees.Select(d => new DegreeDto(d.Key, d.Number, d.YearsInDegree)).ToList(),
            Categories.SelectMany(c => c.Amounts.Where(a => degreeKeys.Contains(a.Key)).Select(a => new CellDto(c.Key, a.Key, a.Value))).ToList());

        if (await TryAsync(async () =>
                Id = await Runner.RunAsync<SalaryTableService, int>(s => s.SaveAsync(dto)).ConfigureAwait(true),
                Labels.Saved).ConfigureAwait(true))
        {
            await navigator.GoAsync<SalaryTablesViewModel>(p => p.LoadAsync()).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private Task BackAsync() => navigator.GoAsync<SalaryTablesViewModel>(p => p.LoadAsync());

    private void Fill(SalaryTableDto dto)
    {
        _filling = true;
        Id = dto.Id;
        TableNumber = dto.TableNumber;
        SalaryType = dto.SalaryType;
        ConcernedEmployees = dto.ConcernedEmployees;
        SelectedAgreementId = dto.AgreementId; // after the employees, so a matching grid kind is shown as selected
        _filling = false;
        StartDate = ToDateTime(dto.StartDate);
        EndDate = ToDateTime(dto.EndDate);

        Degrees.Clear();
        foreach (var d in dto.Degrees)
        {
            Degrees.Add(new DegreeColumnViewModel(d.Key, d.Number, d.YearsInDegree));
        }

        Categories.Clear();
        foreach (var c in dto.Categories)
        {
            var row = new CategoryRowViewModel(c.Key, c.Label);
            foreach (var cell in dto.Cells.Where(x => x.CategoryKey == c.Key))
            {
                row[cell.DegreeKey] = cell.Amount;
            }

            Categories.Add(row);
        }
    }
}
