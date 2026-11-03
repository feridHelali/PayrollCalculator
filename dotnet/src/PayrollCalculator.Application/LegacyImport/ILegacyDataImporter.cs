namespace PayrollCalculator.Application.LegacyImport;

/// <summary>Outcome of a legacy import: what was imported and what was skipped or adjusted.</summary>
public sealed record ImportReport(int Agreements, int SalaryTables, int Cells, int Affairs, IReadOnlyList<string> Warnings);

/// <summary>Imports the data of the former Electron application (payroll.sqlite) into an empty database.</summary>
public interface ILegacyDataImporter
{
    Task<ImportReport> ImportAsync(string legacyDatabasePath, CancellationToken cancellationToken = default);
}
