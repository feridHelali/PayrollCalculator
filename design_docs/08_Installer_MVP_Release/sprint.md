# Sprint 08: installer and MVP release for the domain expert

## Goal

Send a first version (MVP / proof of concept) to the domain expert, who tests it and approves the calculation.
The expert can also gather all the agreements and their PDFs, for the ingestion pipeline that comes later.

**Done means:** the expert gets one file, `PayrollCalculator-Setup-1.0.0.exe`. It installs without admin rights and
without installing anything else (no .NET, no runtime, no SQL server). The app then starts on the seeded data: 20
agreements, the grids and primes of clothing and travel agencies. The database is the file `data\payroll.db` beside the
app.

## Decisions

| # | Question | Decision |
|---|---|---|
| D1 | Runtime | **Self-contained** publish for `win-x64`: the .NET runtime, WPF and the native SQLite library ship with the app. Nothing to install. |
| D2 | Where the data lives | **`{app}\data\payroll.db`**, beside `PayrollCalculator.exe`. `PAYROLL_DB_PATH` still overrides it. If that folder can't be written (for example a copy in `Program Files`), the app falls back to `%LOCALAPPDATA%\PayrollCalculator\data` and says so on the About page. |
| D3 | Installer | **Inno Setup 6** (replaces the Velopack choice of sprint 07, D5). It needs nothing on the target machine. It puts `data\` right beside the exe, where Velopack would install into a `current\` folder replaced at each update. It is already on the build machine, and it has an Arabic wizard. Auto-update isn't needed for an expert release; Velopack can come back for that later. |
| D4 | Install scope | **Per user, no admin**: `%LOCALAPPDATA%\Programs\PayrollCalculator`. Start-menu shortcut, optional desktop shortcut, uninstaller in Windows Settings. |
| D5 | Uninstall and upgrade | Installing a newer version over an older one **keeps `data\`**. Uninstalling also keeps it by default: it holds the expert's affairs. |
| D6 | Before a schema upgrade | When a migration is pending, the app first copies the database to `data\backups\payroll-{date}-{version}.db`. |
| D7 | Also shipped | A **portable zip** (the same folder, no installer) for machines where running a setup is blocked. |
| D8 | Signing | **Unsigned** for the MVP: Windows SmartScreen shows "Windows protected your PC" → "More info" → "Run anyway". The expert guide says so. A code-signing certificate is for a public release. |
| D9 | Not shipped | No PDFs, no legacy sample, no test data. The reference data is inside the app (the migration's seed). |

## Sub-sprints

| # | Work | Checked by |
|---|---|---|
| 08.1 | **Data folder beside the app.** `AppPaths` resolves the data folder (env override → `{app}\data` → fallback when not writable). `run.ps1` keeps the dev database in `%LOCALAPPDATA%` by passing `PAYROLL_DB_PATH`. The About page shows the database path. | Unit tests for the resolution order and the fallback |
| 08.2 | **Backup before migrating** (D6). | Test: a pending migration makes a copy; an up-to-date database makes none |
| 08.3 | **Self-contained publish.** `win-x64`, self-contained, ReadyToRun, no single file (WPF and the native SQLite stay plain files), invariant globalization off (Arabic). | `publish.ps1` output starts on a machine without .NET |
| 08.4 | **Installer.** `installer/PayrollCalculator.iss`: Arabic and French wizard, per user, shortcuts, keeps `data\`, version from `Directory.Build.props`. | `release.ps1` builds Setup and zip; install, start, uninstall |
| 08.5 | **Release script.** `release.ps1`: tests → publish → installer → portable zip → SHA-256 file, everything in `artifacts/release/{version}/`. | One command produces the three files |
| 08.6 | **Expert pack.** An Arabic guide: install, first sign-in (`admin` / `Admin@2026`, then change it), what is seeded, what to check, known limits, how to report. Release notes. | Read through |
| 08.7 | **Clean-machine test.** Windows Sandbox (no .NET): install offline, sign in, open a seeded grid, build a ledger, export the CSV, uninstall. | Checklist ticked |

## Known limits of the MVP (to state in the guide)

- Only two agreements have grids and primes: clothing (rider 18, 2024–2026) and travel agencies (1996–1998, 2017, 2022, 2023). The other 18 have only their name and grid types.
- Travel agencies: riders 1–8 and 10 are missing, so the 1999–2017 months use the last known amounts.
- The values were read from page images. The expert checks them against the JORT.
- The output is the gross salary, month by month. Nothing else.

| 08.8 | **Licence serial** (added at the product owner's request). The app asks for a serial once, before the sign-in. `tools/generate-serials.ps1` makes N serials, keeps them in the private, git-ignored `licences/`, and appends only their SHA-256 to `Infrastructure/Licensing/serials.sha256`, which is embedded in the app. No secret in the app or the repository, so a serial can't be forged. The activation is kept in `data\license.json`; the About page shows the masked serial. | Unit tests (format, look-alike letters, script and app give the same hash, unknown serial refused), activation view model test |

### The licence: what it does and doesn't do

- It does: refuse a copy started without one of the issued serials; let you trace which serial you gave to whom.
- It doesn't: bind a serial to one machine (the same serial works on several), or work as copy protection against
  someone who patches the program. Both need online activation, out of scope for an expert release.
- A new batch: `.\tools\generate-serials.ps1 -Count 10 -Note "..."`, then rebuild and release. Older serials stay valid;
  to revoke one, delete its line from `serials.sha256` and release.

## Progress

- **08.1 done.** `DatabaseOptions.Resolve`: `PAYROLL_DB_PATH`, else `{app}\data\payroll.db`, else the per-user folder
  when the app folder can't be written (shown on the About page). `run.ps1` keeps the dev database in `%LOCALAPPDATA%`.
- **08.2 done.** `MigrateDatabaseAsync` backs up an existing database before applying a pending migration (SQLite
  backup API, WAL included) and returns the path.
- **08.3–08.5 done.** `release.ps1`: tests → self-contained ReadyToRun publish → Inno Setup → portable zip → expert
  guide → `SHA256SUMS.txt`, in `artifacts/release/<version>/`. Setup 49 MB, zip 68 MB.
- **08.6 done.** `expert_guide_ar.md` (copied into the release folder as `دليل_الخبير.md`).
- **08.7 partly.** On the build machine: silent install into a temp folder → the app starts, creates `data\payroll.db`
  beside the exe with the seed, waits for the serial → silent uninstall removes the program and keeps `data\`. Still to
  do: the same on a clean Windows Sandbox (no .NET), and the full manual checklist.
- **08.8 done.** 100 serials generated (`licences/serials-20261104-170901.csv`, private).
