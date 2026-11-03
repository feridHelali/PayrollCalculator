## Re implement the existing in C#(WPF), Sqlite

1. Current State: Not Completed Electron App that Compute and Track Salaries Hour/day rate over many years forlegal use after layoff
2. Objective : Re-implement the app on C# dotnet10, WPF
3. Using TDD, DDD, MVVM and best OO practice, Principle and patterns
4. back up legacy code in design Docs as archive
5. Scaffold, Migrate

---

## Sprint outcome (2026-11-03)

| # | Item | Status |
|---|---|---|
| 2 | .NET 10 WPF solution in `dotnet/`: Domain / Application / Infrastructure / Wpf plus 4 test projects | Done |
| 3 | TDD: tests written before the code in each layer (91 tests, all green). DDD: aggregates, value objects, domain service, strategy, repositories and unit of work. MVVM: CommunityToolkit.Mvvm with DI | Done |
| 4 | Legacy Electron sources moved (`git mv`) to `design_docs/02_Legacy_Electron_Archive/electron-app/`. The repository root now holds only `dotnet/`, `design_docs/` and the legacy `payroll.sqlite` | Done |
| 5 | Scaffold and migrate: EF Core `InitialCreate` migration, applied automatically at startup. The `payroll.sqlite` data is imported with `LegacyElectronImporter` (4 agreements, 6 tables, 85 cells, 5 affairs) | Done |
| + | New feature: month-by-month salary timeline per affair (the engine the Electron app never had), with CSV export | Done |

### Legacy data issues found by the importer
- Affairs #4 and #5 reference a salary table that belongs to another agreement. They were imported without a table link and need fixing in the UI.
- Affairs use the codes `C1`/`C2` as categories. These were mapped to the n-th category label of their table.

### Backlog (next sprint)
- Have a domain expert validate the degree-progression rule (see `dotnet/README.md`, "Business rules").
- Converting hourly rates to monthly amounts (hours per month by work regime).
- Translate domain error messages, which are currently English, to Arabic for the UI.
- Arabic DatePicker placeholder, application icon, installer (MSIX or Velopack).
