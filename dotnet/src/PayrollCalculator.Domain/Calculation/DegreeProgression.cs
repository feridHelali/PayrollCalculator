using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Domain.Calculation;

/// <summary>Strategy deciding which degree an employee holds after some seniority.</summary>
public interface IDegreeProgressionPolicy
{
    /// <returns>The degree held, or null when <paramref name="initialDegree"/> is not in the grid.</returns>
    DegreeStep? DegreeAt(IReadOnlyList<DegreeStep> degrees, int initialDegree, int monthsOfSeniority);
}

/// <summary>
/// Default rule: the employee starts at the initial degree and moves to the next degree after
/// spending <see cref="DegreeStep.YearsInDegree"/> years in the current one. The last degree is a ceiling.
/// </summary>
public sealed class SeniorityDegreeProgression : IDegreeProgressionPolicy
{
    public DegreeStep? DegreeAt(IReadOnlyList<DegreeStep> degrees, int initialDegree, int monthsOfSeniority)
    {
        var ordered = degrees.OrderBy(d => d.Number).ToList();
        var index = ordered.FindIndex(d => d.Number == initialDegree);
        if (index < 0)
        {
            return null;
        }

        var remaining = monthsOfSeniority;
        while (index < ordered.Count - 1 && remaining >= ordered[index].YearsInDegree * 12)
        {
            remaining -= ordered[index].YearsInDegree * 12;
            index++;
        }

        return ordered[index];
    }
}
