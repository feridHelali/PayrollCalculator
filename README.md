# حاسبة الأجور · PayrollCalculator

A Windows desktop app that rebuilds, month by month, the **gross salary** (salaire brut) an employee was owed under
the Tunisian sectoral joint agreements (الاتفاقيات المشتركة القطاعية): the base salary from the agreement's grids plus
the applicable primes. The result supports legal proceedings after a layoff. The interface is in Arabic, right to left.

The gross salary is the only output. Net pay, social security (CNSS), income tax (IRPP), legal interest, severance and
comparisons with what was actually paid are out of scope.

**Status:** version 1.0.0, an MVP for the domain expert to test and approve
([sprint 08](design_docs/08_Installer_MVP_Release/sprint.md)).

## What the app does

- **Agreements:** the 20 sectoral agreements, with their Arabic names and their kinds of salary grid.
- **Salary grids** (categories × degrees, hourly or monthly, 48 h or 40 h a week), dated versions that follow each
  other. Each grid can be imported from a CSV file.
- **Primes:** dated amounts per month, per hour, per day worked, or as a percentage of the base, each with its JORT
  source.
- **Affairs:** an employee, their period of employment, agreement, grid, category and starting degree.
- **Monthly ledger:** base salary and each chosen prime, month by month, with degree progression, partial months
  and totals. CSV export.
- **Users:** sign-in, roles, forced password change, lockout. **Licence:** a serial asked once per installation.

### Data shipped with the app

| Agreement | Grids | Primes |
|---|---|---|
| Woven and ready-to-wear clothing (rider 18) | 6: hourly and monthly, 2024, 2025, 2026 | 4 |
| Travel agencies (1997 agreement, riders 9 and 11) | 6: 1996, 1997, 1998, 2017, 2022, 2023 | 7 |

The other 18 agreements have only their name and grid kinds so far. The values were read from JORT page images and
are still to be checked by the expert. Shipped rows are read-only in the app.

## Install (end users)

Run `PayrollCalculator-Setup-<version>.exe`. It needs no admin rights and nothing else on the machine: the .NET
runtime ships inside. Then enter your serial number, and sign in with `admin` / `Admin@2026` (you choose a new
password at once).

The data lives in the `data` folder beside the program: `payroll.db`, `backups\`, `license.json`. Upgrading or
uninstalling keeps it. The Arabic guide for the expert is
[`design_docs/08_Installer_MVP_Release/expert_guide_ar.md`](design_docs/08_Installer_MVP_Release/expert_guide_ar.md).

## Develop

Needs the .NET 10 SDK on Windows. From `dotnet/`:

```powershell
dotnet build                 # warnings are errors
dotnet test                  # Domain, Application, Infrastructure and WPF tests
.\run.ps1                    # build and run; -Reset deletes the dev database first
```

The development database is `%LOCALAPPDATA%\PayrollCalculator\payroll.db`. The architecture (DDD, MVVM, EF Core
SQLite, the event-based ledger) and the business assumptions are in [`dotnet/README.md`](dotnet/README.md).
Conventions for contributors and AI assistants are in [`CLAUDE.md`](CLAUDE.md).

## Release

```powershell
cd dotnet
.\release.ps1                # tests, self-contained publish, installer, portable zip, checksums
```

The output goes to `dotnet/artifacts/release/<version>/` (not in git). This needs [Inno Setup 6](https://jrsoftware.org/isinfo.php)
on the build machine only. The version is set in `dotnet/Directory.Build.props`.

**Licence serials:** `.\tools\generate-serials.ps1 -Count 100 -Note "..."` writes the serials to `licences/` at the
repository root. That folder is private and git-ignored; keep it safe, as it is the only copy. Only the serials'
SHA-256 hashes go into the app (`Infrastructure/Licensing/serials.sha256`), so rebuild for new serials to be accepted.

## Repository layout

| Path | Contents |
|---|---|
| `dotnet/` | The application: `src/` (Domain, Application, Infrastructure, Wpf), `tests/`, `tools/`, `installer/` |
| `data/seed/agreements/` | The shipped grids and primes, one folder per agreement (`seed.json` and CSV files) |
| `data/jort/` | The library of JORT PDFs used to build the seed, with `manifest.json`; not shipped |
| `design_docs/` | Specification, sprint plans and decisions, from inception to sprint 08 |
| `design_docs/02_Legacy_Electron_Archive/` | The former Electron app, kept for reference only |

## Next

- The expert's review of the MVP, and the PDFs of all the agreements and their riders.
- A pipeline that turns those PDFs into seed files (`data/seed/agreements/`).

© 2026 AlfaComputers
