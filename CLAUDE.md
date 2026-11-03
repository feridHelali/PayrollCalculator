# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Electron desktop app that determines base salaries over long periods for Tunisia according to sectoral joint agreements (Conventions Collectives Sectorielles / الاتفاقيات القطاعية المشتركة). The UI is in **Arabic, RTL**. The full product spec is in `design_docs/00_Inception/spec_2025.md`; the original DB design is in `design_docs/00_Inception/DDL.SQL`.

## Commands

- `yarn test`: all Jest tests. `yarn test <path>` runs a single file, e.g. `yarn test __tests__/salaryTableTests/SalaryTableForm.spec.tsx`. `yarn test:watch` for watch mode.
- `yarn build` (= `build:main` + `build:renderer`): production webpack builds into `dist/`.
- **Dev loop**: `yarn start:dev` builds main/preload into `dist/` and serves the renderer on `http://localhost:9000`. Then run `yarn electron` in a second terminal. The main window loads `ELECTRON_START_URL` (default `http://localhost:9000`), so the dev server must be running.
- `yarn start` (Electron Forge), `yarn package`, `yarn make`: packaging and installers.
- After changing Electron or native deps, run `yarn electron-rebuild`. `better-sqlite3` is a native module and is marked as a webpack external in the main config.
- There is an `.eslintrc.js` but no lint script.

## Architecture

Two webpack builds:
- `webpack.main.config.js`: `src/main/main.ts` + `src/main/preload.ts` → `dist/main.js`, `dist/preload.js` (ts-loader, target `electron-main`).
- `webpack.renderer.config.js`: `src/renderer/index.tsx` → `dist/renderer.js` (babel-loader, target `web`).

### Data flow (adding a feature touches every layer)

1. **Entity**: `src/main/db/entities/*.ts` (TypeORM decorators). Register new entities in the `entities` array of `src/main/typeorm.config.ts`. `synchronize: true` means the schema is auto-altered from the entities; there are no real migrations. The DB file is `payroll.sqlite`, resolved relative to `dist/` (i.e. the repo root).
2. **Service**: `src/main/services/*Service.ts` wraps the TypeORM repository and holds the CRUD logic.
3. **IPC handler**: registered in `src/main/main.ts` with `ipcMain.handle('<kebab-channel>', ...)`, **after** `initializeDatabase()` resolves.
4. **Preload bridge**: `src/main/preload.ts` exposes `window.electronAPI.<camelName>` via `contextBridge` (`contextIsolation: true`, no node integration).
5. **Type declaration**: add the method to `ElectronAPI` (and to `MockElectronAPI`, used by tests) in `global.d.ts` at the repo root.
6. **Redux thunk**: `src/renderer/redux/<domain>/*Slice.ts` uses `createAsyncThunk` to call `window.electronAPI.*`. Slices follow a common shape (`items`, `current*`, `status`, `error`, `mode: 'create' | 'update'`). The slices are wired in `src/renderer/redux/store.ts` (keys: `agreements`, `salaryTables`, `affairs`, `status`). Use the typed hooks in `redux/redux.hooks.ts`.
7. **UI**: pages in `src/renderer/pages/`, feature components in `src/renderer/components/<domain>/`, routes in `src/renderer/App.tsx` (`/x-form` for create, `/x-form/:id` for update).

Shared DTO/prop types live in `src/types/` and are imported by both processes.

### Domain model

- `SectorialJointAgreement` 1─* `SalaryTable` 1─* `SalaryTableCell` (cells are cascade + eager loaded).
- `SalaryTable` stores its grid axes as JSON columns: `degrees` (`{key, degree, ageOfWork}[]`, which are the column headers) and `categories` (`{key, label}[]`, which are the rows). Each cell holds `professionalCategory`, a `professionalDegree` JSON value, and `salary`.
- `Affair` (a legal case for a claimant) belongs to an agreement and optionally a `SalaryTable` (`salaryTableId`). It has many `ActualMonthSalary`.
- Deleting an agreement cascades to its salary tables and affairs.

### Renderer conventions

- Editing the salary-table grid uses a local React context (`components/salaryTables/contexts/SalaryTableContext.tsx`: categories, headers and cells keyed by UUID) instead of Redux. Redux holds persisted/global state. The rationale is in `design_docs/00_Inception/using_redux_cs_useContext.md`.
- All user-facing strings come from `src/renderer/arabic.labels.ts` (`labels.*`). Add new labels there rather than hard-coding text.
- RTL is set in the Chakra theme (`styles/theme.ts`, `direction: 'rtl'`) and in `styles/rtl.css`.
- `shared/GenericLookupDialog.tsx` is the reusable lookup/picker dialog (for example, choosing a salary table from the Affair form).

## Tests

Tests use Jest (`ts-jest`, jsdom) + React Testing Library, live in `__tests__/`, use `*.spec.ts(x)` names, and are set up by `setupTests.ts`. Tests mock IPC by assigning `jest.fn()`s to `window.electronAPI` / `window.mockElectronAPI` (see `global.d.ts`). Some existing specs import outdated paths (e.g. `salaryTableSlice` vs. the actual `salaryTablesSlice.ts`), so check the imports when a suite fails to load.

## Code style (from AGENTS.md)

- Strict TypeScript; functional components typed `React.FC` with hooks.
- Import order: React, then third-party, then local (types, utils, components).
- PascalCase for components and types, camelCase for functions and variables.
- Chakra UI with shorthand style props (`h`, `bg`, `p`).
- Async logic goes in `createAsyncThunk`; synchronous updates go in slice reducers.
