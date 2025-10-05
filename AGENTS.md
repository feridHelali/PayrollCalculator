# PayrollCalculator - Agent Guidelines

## Commands
- **Test**: `yarn test` (all tests), `yarn test <file>` (single test), `yarn test:watch` (watch mode)
- **Build**: `yarn build` (production), `yarn build:main` (main process), `yarn build:renderer` (renderer process)
- **Start**: `yarn start` (Electron Forge), `yarn electron` (run built app)
- **Package**: `yarn package` (package app), `yarn make` (create installers)

## Architecture
- **Electron app** with TypeScript, React, Redux Toolkit, Chakra UI
- **Main process** (`src/main/`): Electron main, preload, services, DB layer
- **Renderer process** (`src/renderer/`): React UI (components, pages, redux, styles)
- **Database**: SQLite via TypeORM (better-sqlite3), entities in `src/main/db/entities/`
- **State**: Redux slices in `src/renderer/redux/`, uses thunks for IPC calls to main process
- **IPC**: Window API defined in `global.d.ts`, exposed via preload script
- **Tests**: Jest + React Testing Library in `__tests__/`, uses `.spec.ts(x)` naming

## Code Style
- **TypeScript**: Strict mode enabled, decorators for TypeORM entities
- **Components**: Functional components with React.FC, hooks (useState, useSelector, useDispatch)
- **Imports**: React first, then third-party, then local (types, utils, components)
- **Naming**: PascalCase for components/types, camelCase for functions/variables
- **UI**: Chakra UI components, props use shorthand (e.g., `h`, `bg`, `p`)
- **Redux**: Async logic with createAsyncThunk, slice reducers for sync state updates
