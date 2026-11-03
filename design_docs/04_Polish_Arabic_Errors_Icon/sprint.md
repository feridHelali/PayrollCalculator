## Sprint 04: Arabic errors, own password change, date picker, application icon

1. Context: the backlogs of sprints 01 and 03 hold small items that need no decision from a domain expert.
2. Objective: finish the Arabic UI and close those items.
3. Out of scope: the installer (needs a choice between MSIX and Velopack), the hourly-to-monthly conversion and the degree-progression rule (both need the agreements' rules), and deactivating users.

---

## Sub-sprints

| # | Sub-sprint | Layer | Status | Tests added |
|---|---|---|---|---|
| 04.1 | **Error codes everywhere.** `DomainException` carries `DomainError` (what), `DomainField` (which value) and `Detail` (the offending value); `Guard` takes a `DomainField`. No English sentence is shown to the user any more | Domain, Application | Done | 5 domain |
| 04.2 | **Arabic error texts.** `Labels.ErrorText` turns any expected exception (user, domain, not found, database) into Arabic; `PageViewModel` uses it | Wpf | Done | 3 view-model |
| 04.3 | **Change my password.** A page reached from the side menu, under the signed-in user. Same rules as the forced change: current password checked (a wrong one counts as a failed sign-in), policy, confirmation | Wpf | Done | 3 view-model |
| 04.4 | **Date picker.** Arabic placeholder (`يوم/شهر/سنة`) instead of the Windows one ("Sélectionner une date" on a French Windows) | Wpf theme | Done | checked visually |
| 04.5 | **Application icon.** `Assets/app.ico` (16 to 256 px, generated from the logo) is the exe, taskbar and shortcut icon | Wpf | Done | checked: icon extracted from the exe |

Totals: **175 tests** (164 before), 0 warnings.

## Design notes
- **Codes in the domain, words in the UI.** The domain does not know the user's language. `Exception.Message` stays an English sentence built from the codes (`Required (AgreementName)`), for logs and support. `UserException` keeps its own `UserError` code, which takes the place of `DomainException.Error`.
- **A test guards the translations:** every `DomainError` and every `DomainField` must produce Arabic text, so a new code without a translation fails the build's tests.
- Example: an empty agreement name shows «يجب إدخال اسم الاتفاقية القطاعية المشتركة»; a duplicate category shows «الصنف «C1» موجود مسبقا».

## Backlog
- Installer (MSIX or Velopack: to choose).
- Hourly-to-monthly conversion and validation of the degree rule (domain expert).
- Deactivating a user (with a "never the last user" rule).
- The combo boxes still have the stock Windows look; they could get the rounded theme.
