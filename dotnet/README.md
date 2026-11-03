# PayrollCalculator (.NET 10 / WPF)

This is the C# re-implementation of the Electron application, which is archived in `design_docs/02_Legacy_Electron_Archive/`.
It rebuilds, month by month, the base salary a laid-off employee was owed over their employment. The calculation
follows the salary grids of Tunisian sectoral joint agreements, and the result is used in legal proceedings.

## Commands (run from `dotnet/`)

```
dotnet build                                   # warnings are errors
dotnet test                                    # all 4 test projects
dotnet test tests/PayrollCalculator.Domain.Tests --filter "FullyQualifiedName~SalaryTimeline"
dotnet run --project src/PayrollCalculator.Wpf
dotnet run --project src/PayrollCalculator.Wpf -- --import-legacy ..\payroll.sqlite   # one-shot migration into an empty DB
dotnet ef migrations add <Name> -p src/PayrollCalculator.Infrastructure -s src/PayrollCalculator.Infrastructure -o Persistence/Migrations
```

- The database is created and migrated on startup at `%LOCALAPPDATA%\PayrollCalculator\payroll.db`. Set the
  `PAYROLL_DB_PATH` environment variable to use another file.

## Architecture (DDD, layered)

| Project | Role | Depends on |
|---|---|---|
| `Domain` | Aggregates `SectorialJointAgreement`, `SalaryTable` (categories × degrees grid) and `Affair`; value objects `DateRange`, `YearMonth` and `ProfessionalPosition`; the `SalaryTimelineCalculator` domain service and the `IDegreeProgressionPolicy` strategy; repository interfaces | nothing |
| `Application` | Use-case services (`AgreementService`, `SalaryTableService`, `AffairService`), DTOs, `TimelineCsv`, `ILegacyDataImporter` port | Domain |
| `Infrastructure` | EF Core SQLite `PayrollDbContext` (owned collections for the grid, complex types for value objects), repositories, migrations, `LegacyElectronImporter` | Application, Domain |
| `Wpf` | MVVM (CommunityToolkit.Mvvm) with an RTL Arabic UI; view-model-first navigation (`Navigator` + DataTemplates in `App.xaml`); `IServiceRunner` runs each use case in a fresh DI scope (one DbContext per operation) | Application, Infrastructure |

The tests mirror the layers. Domain and Application tests are pure unit tests that use in-memory fakes. Infrastructure
tests use real SQLite in memory with the migrations applied, and include an import of the repository's real
`payroll.sqlite`. The Wpf tests drive the view models through the full stack.

## Business rules and assumptions to validate

1. **Degree progression** (`SeniorityDegreeProgression`): the employee starts at the initial degree. They move to the next
   degree after spending that degree's `YearsInDegree` (the legacy `ageOfWork`, الأقدمية الفعلية) in it. The last degree is a ceiling.
   **This rule should be validated against the agreements.** Swap the `IDegreeProgressionPolicy` implementation if it is wrong.
2. **Grid versions over time**: an affair references one salary table. For each month, the calculator uses the version of
   that grid in force on that date: same agreement and same `TableNumber`, latest application start date that covers the date.
3. **Gaps are reported, never guessed.** A month with no applicable table, an unknown category or degree, or an empty cell
   has no amount and carries a `TimelineIssue`.
4. **Hourly grids** hold hourly rates, so `TotalMonthlyAmount` only sums the months valued with a monthly grid.
   Converting hourly rates to monthly amounts (hours per month) is not implemented.
5. The category is matched by its **label**, which lets it carry across grid versions. The importer converts legacy
   `C1`/`C2` codes to the n-th category label of the linked table.
