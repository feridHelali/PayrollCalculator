# PayrollCalculator (.NET 10 / WPF)

This is the C# re-implementation of the Electron application, which is archived in `design_docs/02_Legacy_Electron_Archive/`.
It rebuilds, month by month, the gross salary (salaire brut: base salary from the grid plus the applicable primes) a
laid-off employee was owed over their employment. The calculation follows the salary grids of Tunisian sectoral joint
agreements, and the result is used in legal proceedings. The gross is the only target: no net pay, CNSS, IRPP, legal
interest or severance, and no comparison with what was actually paid.

## Commands (run from `dotnet/`)

```
dotnet build                                   # warnings are errors
dotnet test                                    # all 4 test projects
dotnet test tests/PayrollCalculator.Domain.Tests --filter "FullyQualifiedName~SalaryTimeline"
dotnet run --project src/PayrollCalculator.Wpf
.\run.ps1 -Reset                               # delete the dev database; it is recreated from the seed on startup
dotnet run --project src/PayrollCalculator.Wpf -- --import-legacy tests\fixtures\legacy-sample.sqlite   # the old Electron sample
dotnet run --project tools/PayrollCalculator.Seed -- catalogue   # PDF library: file data/jort/inbox/*.pdf, refresh the manifest
dotnet run --project tools/PayrollCalculator.Seed -- library-verify | text <key> [page]
dotnet ef migrations add <Name> -p src/PayrollCalculator.Infrastructure -s src/PayrollCalculator.Infrastructure -o Persistence/Migrations
```

- The database is created and migrated on startup at `%LOCALAPPDATA%\PayrollCalculator\payroll.db`. Set the
  `PAYROLL_DB_PATH` environment variable to use another file.
- **Monthly ledger (الكشف الشهري للأجر والمنح, from the affairs list).** `AffairLedgerBuilder` (Domain) turns the affair's
  salary tables and the primes ticked for the employee into rule events and values them with `PayLedgerBuilder`: base
  from the grid, monthly primes prorated, per-day primes × working days, percentages of the base. The work regime is
  the grid's weekly hours (48 h: 208 h and 26 days a month; 40 h: 173.333 h and 22 days). CSV export with one column
  per prime.
- **One migration, `InitialCreate`**, creates the schema and seeds, with EF `HasData`:
  - the 20 sectoral agreements (Arabic label, stable code, display order) from `Infrastructure/Persistence/AgreementSeed.cs`;
  - the salary grids and primes of `data/seed/agreements/{code}/` (a `seed.json` plus one CSV per grid, embedded in
    the Infrastructure assembly and read by `ReferenceSeed.cs`). Today: clothing (rider 18, 6 grids, 4 primes) and
    travel agencies (6 grids from 1996 to 2023, 7 primes).

  Seeded rows are read-only in the app (no save, no delete). Editing a seed file changes the model: add a migration. The migrations were squashed in sprint 07, so a database created before that must be reset
  (`.\run.ps1 -Reset`). The JORT PDFs live in `data/jort/files`, described by `data/jort/manifest.json`;
  they are not shipped with the app.

## Sign-in and users

- Version **1.0.0** (set in `Directory.Build.props`). The app shows a splash screen, then a sign-in window; the About page (حول البرنامج) shows the version and credits (idea: Lazhar Lataoui).
- On an empty database it creates **`admin` / `Admin@2026`**, which must
  change its password at the first sign-in (the published password cannot be used to work).
- The users screen (المستخدمون) adds users and resets passwords. Both set a temporary password that the user must
  change at their next sign-in. There are no roles: every signed-in user can manage users.
- Passwords are hashed with PBKDF2-HMAC-SHA256 (600 000 iterations). 5 failures in a row lock the account for 15
  minutes. Policy: at least 8 characters and 3 of lower/upper/digit/symbol. Design notes are in
  `design_docs/03_Authentication_User_Management/sprint.md`.

## Architecture (DDD, layered)

| Project | Role | Depends on |
|---|---|---|
| `Domain` | Aggregates `SectorialJointAgreement`, `SalaryTable` (categories × degrees grid), `Affair` and `User` (lockout, forced password change); `PasswordPolicy`; value objects `DateRange`, `YearMonth` and `ProfessionalPosition`; the `SalaryTimelineCalculator` domain service and the `IDegreeProgressionPolicy` strategy; repository interfaces. Since sprint 05: `Money` (millimes), `LegalSources`, `Rules` (dated, cited rule events folded by `Rulebook`), `Careers` (career events), `Ledger` (`PayLedgerBuilder`, `PayLedger` and its views, `LedgerSnapshot`) and `Migration` (`LegacyTranslator`), not yet wired to the app | nothing |
| `Application` | Use-case services (`AgreementService`, `SalaryTableService`, `AffairService`, `AuthenticationService`, `UserService`), `CurrentUser` session, `UserSeeder`, DTOs, `TimelineCsv`, `ILegacyDataImporter` port | Domain |
| `Infrastructure` | EF Core SQLite `PayrollDbContext` (owned collections for the grid, complex types for value objects), repositories, migrations, `LegacyElectronImporter`, `Pbkdf2PasswordHasher`. Since sprint 05: the event store (`Persistence/Events`: versioned JSON payloads, `RuleEventStore` that refuses changes to approved events, `CareerEventStore`, `LedgerSnapshotRepository`, `Sha256LedgerFingerprint`) and `LegacyEventMigration` | Application, Domain |
| `Wpf` | MVVM (CommunityToolkit.Mvvm) with an RTL Arabic UI; `LoginWindow` before the shell; theme in `Themes/Theme.xaml` (icons via `Controls/Ui.cs`); view-model-first navigation (`Navigator` + DataTemplates in `App.xaml`); `IServiceRunner` runs each use case in a fresh DI scope (one DbContext per operation) | Application, Infrastructure |

The tests mirror the layers. Domain and Application tests are pure unit tests that use in-memory fakes. Infrastructure
tests use real SQLite in memory with the migrations applied, and include an import of the Electron sample
(`tests/fixtures/legacy-sample.sqlite`) and a check of the PDF library against its manifest. The Wpf tests drive the view models through the full stack.

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
