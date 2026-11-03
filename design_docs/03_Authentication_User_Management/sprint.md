## Sprint 03: Authentication, user management and modern theme

1. Context: the .NET 10 / WPF re-implementation (sprint 01) opens straight onto the data, with no sign-in. The data is legal evidence, so access must be controlled.
2. Objective: add a simple sign-in, user management (**add a user** and **reset a password**, nothing else), a seeded first account, and a modern visual theme.
3. Out of scope, on purpose: roles and permissions (RBAC), deactivating or deleting users, an access log. Every signed-in user can manage users.
4. Approach: TDD, DDD, MVVM, SOLID, as in sprint 01. The reference implementation is the Alfa ERP (`D:\53_AlfaComputers_ERP_System`, sprints 08 and 11), minus its RBAC.

---

## Sub-sprints

| # | Sub-sprint | Layer | Deliverables |
|---|---|---|---|
| 03.1 | User domain | Domain | `User` aggregate (normalised user name, lockout, forced password change), `PasswordPolicy`, `UserException` with `UserError` codes, `IPasswordHasher` and `IUserRepository` ports |
| 03.2 | Authentication and user use cases | Application | `AuthenticationService` (sign in, change password), `UserService` (list, add, reset), `CurrentUser` session, `UserSeeder`, `TemporaryPassword` generator |
| 03.3 | Persistence and hashing | Infrastructure | `Pbkdf2PasswordHasher`, `Users` table, `AddUsers` EF migration, `UserRepository` |
| 03.4 | Sign-in flow | Wpf | `LoginWindow` (sign in, then forced password change in the same window), start-up sequence, sign-out |
| 03.5 | User management screen | Wpf | `UsersView`: list, add with a temporary password, reset a password |
| 03.6 | Modern theme | Wpf | Brand palette from the AC logo, rounded buttons and inputs, side menu with hover and active-page highlight, Segoe Fluent icons on the menu and actions, logo on the sign-in window and the side menu |

## Design decisions

### Security (taken from the ERP's sprint 08)
- **Passwords are hashed with PBKDF2-HMAC-SHA256 at 600 000 iterations** (OWASP), from the BCL with no package. The stored form is versioned (`pbkdf2-sha256$600000$salt$hash`), so the algorithm can change later without a password reset.
- **The same message for an unknown user and a wrong password**, and a hash is computed even for an unknown user. Otherwise the response time reveals which user names exist.
- **Lockout is temporary:** 15 minutes after 5 failures in a row. A permanent lockout would let anyone block a colleague. The `User` aggregate owns the counter and it is persisted, so restarting the app does not reset it.
- **The password never becomes a bindable property.** `PasswordBox` values are passed to the view model as method arguments and do not outlive the call.
- **Password policy:** at least 8 characters, 3 of the 4 character classes (lower, upper, digit, symbol), and not equal to the user name. There is no forced expiry and no reuse history (NIST SP 800-63B).

### The seeded account
An empty database gets `admin` / `Admin@2026`, created with *must change password*. The published password cannot be used to work: the sign-in window asks for a new one before the app opens. This is the difference between a documented first credential and a back door.

### Temporary passwords
Adding a user and resetting a password both set a **temporary** password that the user must change at their next sign-in. The administrator can type it or generate one. It is shown in clear text on purpose, because the administrator has to pass it on.

### Errors are codes, not text
The domain raises `UserException` with a `UserError` code (and the `PasswordViolation` list), and the sign-in returns a `SignInOutcome`. The WPF layer turns these into Arabic text (`Labels`). The domain stays free of UI strings. This starts the "Arabic domain errors" backlog item from sprint 01, for the new code.

### Theme (inspired by the ERP's sprint 11)
- **Native icons first:** Segoe Fluent Icons (Windows 11) with Segoe MDL2 Assets as the fallback. No icon package and no image files.
- The brand gradient (violet, magenta, amber, sampled from the AC logo) is kept for the logo panel and the active-page marker. Data screens stay neutral.
- The existing style keys (`PrimaryButton`, `NavButton`, `PageTitle`, ...) are kept, so the views only gain icons.

## Sprint outcome (2026-11-03)

| # | Sub-sprint | Status | Tests added |
|---|---|---|---|
| 03.1 | User domain: `Domain/Users` (`User`, `PasswordPolicy`, `UserException`, ports) | Done | 25 domain |
| 03.2 | Use cases: `Application/Users` (`AuthenticationService`, `UserService`, `CurrentUser`, `UserSeeder`, `TemporaryPassword`) | Done | 24 application |
| 03.3 | `Infrastructure/Security/Pbkdf2PasswordHasher`, `Users` table, migration `AddUsers` | Done | 11 infrastructure (real SQLite) |
| 03.4 | `LoginWindow` + `LoginViewModel`, start-up sequence and sign-out in `App.xaml.cs` | Done | 8 view-model tests, |
| 03.5 | `UsersView` + `UsersViewModel` (add, generate a temporary password, reset) | Done | covering 03.4 and 03.5 |
| 03.6 | `Themes/Theme.xaml`, `Controls/Ui.cs` (icon and active attached properties, `Icons` glyphs), `Assets/logo.png`, icons on every action | Done | checked visually |

| 03.7 | Added during the sprint: splash screen, version **1.0.0**, About page, main window fitted to the screen | Done | 5 view-model tests |

### 03.7: splash, version, About, window size
- **Version 1.0.0** is set once in `dotnet/Directory.Build.props` (with company, product and copyright). `ProductInfo` reads it from the assembly; the SDK appends the git commit, shown as `1.0.0 (f8beb31)` so support knows the exact build.
- **Splash** (`SplashWindow`): logo, name, description, version, and a bar driven by the real start-up stages (`StartupStage`: database, accounts, legacy import, ready). It stays at least 1.5 s after it is first drawn, so it never just flashes. On a start-up error it closes and the error is shown.
- **About** (حول البرنامج), a page in the side menu: idea **Lazhar Lataoui**, development Ferid HELALI (AlfaComputers), built with Claude Code, version and copyright, and a support block (version, runtime, OS, database path) with a "copy" button. The copied block leaves out who is signed in.
- **Main window** opens maximized on the work area. Its restored size is 90 % of the work area and its minimum size never exceeds it, so it fits a 1366 × 768 laptop.

Totals: **164 tests** (91 before the sprint), 0 warnings. The app was driven through UI Automation on a new database: sign in as the seeded admin, the forced change refuses a weak password with the Arabic rule list, the change succeeds, the main window opens on Affairs, the users screen highlights its menu entry, and signing out returns to the sign-in window.

### Found and fixed during the visual check
- `Admin@2026` rendered as `2026@Admin` inside the Arabic hint. WPF ignores the LRE/PDF control characters, so the credentials now sit in their own left-to-right text block.
- On the password-change step, the error box pushed the form over the header. The window is now taller and the form is top-aligned.

### Backlog
- A "change my password" entry for a signed-in user (today it goes through a reset by another user, or a forced change).
- Deactivating a user (deliberately left out; would need the "never the last user" rule).
- Translating the remaining English domain errors (agreements, tables, affairs) the same way as `UserError`.

## Definition of done
- Tests written first in each layer. `dotnet build` with 0 warnings and `dotnet test` all green.
- The app starts on the sign-in window. `admin` / `Admin@2026` forces a password change and then opens the app.
- From the users screen: add a user, reset a password, and sign in as that user.
