# Legacy Electron application (archived)

`electron-app/` holds the Electron/React/TypeORM application that the .NET re-implementation in `dotnet/` replaces
(see `../01_Re_Implementation_In_C_Sharp/sprint.md`). The files were moved here with `git mv`, so
`git log --follow` still shows their history. They are kept for reference only and are not built or tested.

The legacy data file `payroll.sqlite` is still in the repository root, because the .NET app imports it:
`dotnet/run.ps1 -DbPath <new.db> -ImportLegacy`, or the "استيراد بيانات التطبيق القديم" button in the side menu.

## Running the archived app (if ever needed)

Copy `electron-app/` out of the repository, put `payroll.sqlite` next to it (TypeORM resolves the DB relative to `dist/`,
i.e. the app root), then run `yarn install`, `yarn electron-rebuild`, `yarn start:dev` and, in a second terminal,
`yarn electron`. You can also check out commit `442f131`, the last commit before the re-implementation.

## How it was built (summary)

- Two webpack builds: `src/main` (Electron main process + preload, TypeORM over `better-sqlite3`) and `src/renderer`
  (React, Chakra UI RTL, Redux Toolkit).
- Each feature crossed every layer: TypeORM entity → service → `ipcMain.handle` channel → `contextBridge` preload →
  `global.d.ts` typing → Redux `createAsyncThunk` → page and components.
- `synchronize: true`: the schema was derived from the entities, with no migrations. The salary-table grid axes were
  JSON columns (`degrees`, `categories`) on `salary_table`, and cells were a child table. `LegacyElectronImporter` in
  `dotnet/` documents every quirk of that schema.
- The tests used Jest + React Testing Library, in `__tests__/`.
