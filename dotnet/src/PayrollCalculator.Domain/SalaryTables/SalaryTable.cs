using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.SalaryTables;

/// <summary>
/// Aggregate root: a salary grid (categories x degrees) of an agreement, valid for an application period.
/// Several versions of the same <see cref="TableNumber"/> succeed each other over time.
/// </summary>
public sealed class SalaryTable : Entity
{
    private readonly List<SalaryCategory> _categories = [];
    private readonly List<DegreeStep> _degrees = [];
    private readonly List<SalaryCell> _cells = [];

    private SalaryTable(int agreementId, string tableNumber, SalaryType salaryType, string concernedEmployees, DateRange applicationPeriod)
    {
        AgreementId = agreementId;
        TableNumber = tableNumber;
        SalaryType = salaryType;
        ConcernedEmployees = concernedEmployees;
        ApplicationPeriod = applicationPeriod;
    }

    // Required by EF Core to materialize the owned DateRange.
    private SalaryTable()
    {
        TableNumber = string.Empty;
        ConcernedEmployees = string.Empty;
        ApplicationPeriod = DateRange.From(DateOnly.MinValue);
    }

    public int AgreementId { get; private set; }

    /// <summary>Number of the grid in the agreement (رقم الجدول); shared by successive versions.</summary>
    public string TableNumber { get; private set; }

    public SalaryType SalaryType { get; private set; }

    public string ConcernedEmployees { get; private set; }

    public DateRange ApplicationPeriod { get; private set; }

    /// <summary>The work regime the grid is written for, in hours per week (the texts say «نظام عمل 48 ساعة في الأسبوع»).</summary>
    public int WeeklyHours { get; private set; } = DefaultWeeklyHours;

    public const int DefaultWeeklyHours = 48;

    public IReadOnlyList<SalaryCategory> Categories => _categories.OrderBy(c => c.Position).ToList();

    public IReadOnlyList<DegreeStep> Degrees => _degrees.OrderBy(d => d.Number).ToList();

    public IReadOnlyList<SalaryCell> Cells => _cells;

    public static SalaryTable Create(int agreementId, string tableNumber, SalaryType salaryType, string concernedEmployees, DateRange applicationPeriod) =>
        new(Guard.Positive(agreementId, DomainField.Agreement),
            Guard.Required(tableNumber, DomainField.TableNumber),
            salaryType,
            Guard.Optional(concernedEmployees) ?? string.Empty,
            applicationPeriod ?? throw new DomainException(DomainError.PeriodRequired, DomainField.ApplicationPeriod));

    public void Describe(string tableNumber, SalaryType salaryType, string concernedEmployees, DateRange applicationPeriod)
    {
        TableNumber = Guard.Required(tableNumber, DomainField.TableNumber);
        SalaryType = salaryType;
        ConcernedEmployees = Guard.Optional(concernedEmployees) ?? string.Empty;
        ApplicationPeriod = applicationPeriod ?? throw new DomainException(DomainError.PeriodRequired, DomainField.ApplicationPeriod);
    }

    public void ChangeWorkingTime(int weeklyHours) =>
        WeeklyHours = weeklyHours is >= 1 and <= 60
            ? weeklyHours
            : throw new DomainException(DomainError.OutOfRange, DomainField.WeeklyHours, weeklyHours.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public bool IsApplicableOn(DateOnly date) => ApplicationPeriod.Contains(date);

    /// <summary>
    /// Replaces the whole grid (as edited in a sheet). Categories keep the given order; keys are preserved
    /// so cells can reference them. Invariants are checked as each part is added.
    /// </summary>
    public void RebuildGrid(IEnumerable<CategorySpec> categories, IEnumerable<DegreeSpec> degrees, IEnumerable<CellSpec> cells)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(degrees);
        ArgumentNullException.ThrowIfNull(cells);

        _cells.Clear();
        _categories.Clear();
        _degrees.Clear();

        foreach (var category in categories)
        {
            AddCategory(category.Label, Guard.Required(category.Key, DomainField.CategoryKey));
        }

        foreach (var degree in degrees)
        {
            AddDegree(degree.Number, degree.YearsInDegree, Guard.Required(degree.Key, DomainField.DegreeKey));
        }

        foreach (var cell in cells)
        {
            SetSalary(cell.CategoryKey, cell.DegreeKey, cell.Amount);
        }
    }

    // ---- categories (rows) ----

    public SalaryCategory AddCategory(string label, string? key = null)
    {
        var clean = Guard.Required(label, DomainField.CategoryLabel);
        EnsureUniqueLabel(clean, exceptKey: null);
        var category = new SalaryCategory(key ?? NewKey(), clean, _categories.Count == 0 ? 0 : _categories.Max(c => c.Position) + 1);
        EnsureUniqueKey(category.Key);
        _categories.Add(category);
        return category;
    }

    public void RenameCategory(string key, string label)
    {
        var clean = Guard.Required(label, DomainField.CategoryLabel);
        EnsureUniqueLabel(clean, exceptKey: key);
        Category(key).Label = clean;
    }

    public void RemoveCategory(string key)
    {
        _categories.Remove(Category(key));
        _cells.RemoveAll(c => c.CategoryKey == key);
    }

    public SalaryCategory? FindCategory(string label) =>
        _categories.FirstOrDefault(c => string.Equals(c.Label, label?.Trim(), StringComparison.Ordinal));

    // ---- degrees (columns) ----

    public DegreeStep AddDegree(int number, int yearsInDegree, string? key = null)
    {
        Guard.Positive(number, DomainField.Degree);
        Guard.NotNegative(yearsInDegree, DomainField.YearsInDegree);
        if (_degrees.Any(d => d.Number == number))
        {
            throw new DomainException(DomainError.Duplicate, DomainField.Degree, number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var degree = new DegreeStep(key ?? NewKey(), number, yearsInDegree);
        EnsureUniqueKey(degree.Key);
        _degrees.Add(degree);
        return degree;
    }

    public void ChangeYearsInDegree(string key, int yearsInDegree) =>
        Degree(key).YearsInDegree = Guard.NotNegative(yearsInDegree, DomainField.YearsInDegree);

    public void RemoveDegree(string key)
    {
        _degrees.Remove(Degree(key));
        _cells.RemoveAll(c => c.DegreeKey == key);
    }

    // ---- cells ----

    public void SetSalary(string categoryKey, string degreeKey, decimal amount)
    {
        _ = Category(categoryKey);
        _ = Degree(degreeKey);
        Guard.NotNegative(amount, DomainField.Salary);

        var cell = FindCell(categoryKey, degreeKey);
        if (cell is null)
        {
            _cells.Add(new SalaryCell(categoryKey, degreeKey, amount));
        }
        else
        {
            cell.Amount = amount;
        }
    }

    public void ClearSalary(string categoryKey, string degreeKey)
    {
        if (FindCell(categoryKey, degreeKey) is { } cell)
        {
            _cells.Remove(cell);
        }
    }

    public decimal? SalaryFor(string categoryKey, string degreeKey) => FindCell(categoryKey, degreeKey)?.Amount;

    public decimal? SalaryFor(string categoryLabel, int degreeNumber)
    {
        var category = FindCategory(categoryLabel);
        var degree = _degrees.FirstOrDefault(d => d.Number == degreeNumber);
        return category is null || degree is null ? null : SalaryFor(category.Key, degree.Key);
    }

    private SalaryCell? FindCell(string categoryKey, string degreeKey) =>
        _cells.FirstOrDefault(c => c.CategoryKey == categoryKey && c.DegreeKey == degreeKey);

    private SalaryCategory Category(string key) =>
        _categories.FirstOrDefault(c => c.Key == key) ?? throw new DomainException(DomainError.Unknown, DomainField.CategoryKey, key);

    private DegreeStep Degree(string key) =>
        _degrees.FirstOrDefault(d => d.Key == key) ?? throw new DomainException(DomainError.Unknown, DomainField.DegreeKey, key);

    private void EnsureUniqueLabel(string label, string? exceptKey)
    {
        if (_categories.Any(c => c.Key != exceptKey && string.Equals(c.Label, label, StringComparison.Ordinal)))
        {
            throw new DomainException(DomainError.Duplicate, DomainField.CategoryLabel, label);
        }
    }

    private void EnsureUniqueKey(string key)
    {
        if (_categories.Any(c => c.Key == key) || _degrees.Any(d => d.Key == key))
        {
            throw new DomainException(DomainError.Duplicate, DomainField.CategoryKey, key);
        }
    }

    private static string NewKey() => Guid.NewGuid().ToString();
}

public interface ISalaryTableRepository : IRepository<SalaryTable>
{
    Task<IReadOnlyList<SalaryTable>> ListByAgreementAsync(int agreementId, CancellationToken cancellationToken = default);
}
