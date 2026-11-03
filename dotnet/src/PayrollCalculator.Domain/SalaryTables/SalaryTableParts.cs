namespace PayrollCalculator.Domain.SalaryTables;

/// <summary>Unit of the amounts held by a salary table.</summary>
public enum SalaryType
{
    /// <summary>Amounts are monthly salaries (جدول الأجور بالشهر).</summary>
    Monthly = 0,

    /// <summary>Amounts are hourly rates (جدول الأجور بالساعة).</summary>
    Hourly = 1,
}

/// <summary>Input describing a category when rebuilding a grid.</summary>
public sealed record CategorySpec(string Key, string Label);

/// <summary>Input describing a degree when rebuilding a grid.</summary>
public sealed record DegreeSpec(string Key, int Number, int YearsInDegree);

/// <summary>Input describing a cell when rebuilding a grid.</summary>
public sealed record CellSpec(string CategoryKey, string DegreeKey, decimal Amount);

/// <summary>A row of the grid: a professional category (الصنف).</summary>
public sealed class SalaryCategory
{
    internal SalaryCategory(string key, string label, int position)
    {
        Key = key;
        Label = label;
        Position = position;
    }

    /// <summary>Stable identifier inside the table (kept across renames).</summary>
    public string Key { get; private set; }

    public string Label { get; internal set; }

    /// <summary>Display order (0-based).</summary>
    public int Position { get; internal set; }
}

/// <summary>A column of the grid: a degree (الدرجة) and the seniority spent in it before the next one.</summary>
public sealed class DegreeStep
{
    internal DegreeStep(string key, int number, int yearsInDegree)
    {
        Key = key;
        Number = number;
        YearsInDegree = yearsInDegree;
    }

    public string Key { get; private set; }

    public int Number { get; private set; }

    /// <summary>Years of effective seniority (الأقدمية الفعلية) spent in this degree before advancing.</summary>
    public int YearsInDegree { get; internal set; }
}

/// <summary>One amount of the grid at (category, degree).</summary>
public sealed class SalaryCell
{
    internal SalaryCell(string categoryKey, string degreeKey, decimal amount)
    {
        CategoryKey = categoryKey;
        DegreeKey = degreeKey;
        Amount = amount;
    }

    public string CategoryKey { get; private set; }

    public string DegreeKey { get; private set; }

    public decimal Amount { get; internal set; }
}
