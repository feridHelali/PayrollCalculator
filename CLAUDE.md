# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Desktop app that rebuilds, month by month, the **gross salary only** (salaire brut: grid base salary + applicable primes)
over long periods for Tunisia, according to sectoral joint agreements (Conventions Collectives Sectorielles /
الاتفاقيات القطاعية المشتركة). Out of scope: net pay, CNSS, IRPP, legal interest, severance, paid-vs-owed arrears. The results are used in legal proceedings after a
layoff. The UI is in **Arabic, RTL**. The product spec is `design_docs/00_Inception/spec_2025.md`.

## Layout

- `dotnet/`: the application (.NET 10, WPF, EF Core SQLite, DDD/MVVM/TDD). Start with `dotnet/README.md` for its
  architecture and the business-rule assumptions.
- `design_docs/`: specs, the agreement PDFs, sprint notes. `02_Legacy_Electron_Archive/electron-app/` is the former
  Electron app, kept for reference only. Don't build or change it.
- The 20 sectoral agreements are seeded by the single migration `InitialCreate` (EF `HasData`, list in
  `Infrastructure/Persistence/AgreementSeed.cs`). `data/jort/`: the JORT PDF library (`files/`, `inbox/`, `manifest.json`), used
  to build the seed, not shipped. Plan: `design_docs/07_Reference_Data_Seed/sprint.md`.
- `dotnet/tests/fixtures/legacy-sample.sqlite`: the old Electron sample data (test fixture only; the app no longer uses it).

## Commands (from `dotnet/`)

- `dotnet build`: warnings are errors (`Directory.Build.props`).
- `dotnet test`, or one test class with `dotnet test tests/PayrollCalculator.Domain.Tests --filter "FullyQualifiedName~SalaryTimeline"`.
- `.\run.ps1 [-Reset] [-DbPath <file>] [-ImportLegacy] [-Release]`: build and run the WPF app (`-Reset` deletes the database first).
- `dotnet run --project tools/PayrollCalculator.Seed -- catalogue | library-verify | text <key> [page]`: the PDF library.
- `dotnet ef migrations add <Name> -p src/PayrollCalculator.Infrastructure -s src/PayrollCalculator.Infrastructure -o Persistence/Migrations`

## Conventions

- Write the tests first, in the test project that matches the layer (Domain / Application / Infrastructure / Wpf).
- Domain logic belongs in the Domain aggregates and services. Application services orchestrate the use cases and return DTOs. The WPF view models call them through `IServiceRunner`, one DI scope per call.
- Change the schema only through EF migrations.
- Put user-facing strings in `src/PayrollCalculator.Wpf/Resources/Labels.cs`, in Arabic. The domain raises codes (`DomainException.Error`/`Field`/`Detail`, `UserError`), never UI text; `Labels.ErrorText` translates them, and a test requires an Arabic text for every code.
- Styles and colors live in `src/PayrollCalculator.Wpf/Themes/Theme.xaml`. Give buttons an icon with `c:Ui.Icon="{x:Static c:Icons.X}"` (Segoe Fluent glyphs in `Controls/Ui.cs`).
- Start-up: splash (`Views/SplashWindow`, real stages) → sign-in window → main window (maximized). On an empty database the seeded account is `admin` / `Admin@2026` (forced change at the first sign-in).
- The version (1.0.0) is set only in `dotnet/Directory.Build.props`; `Services/ProductInfo` reads it for the splash and the About page.
