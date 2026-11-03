## Sprint 05: regulatory events and the pay ledger

1. **Context.** Today the user types every version of every grid by hand. The calculation reconstructs only the base
   salary, has no link to the legal texts, and assumes a career fixed at hiring. In practice, every factor that sets a
   salary comes from texts published in the JORT, and each text changes the rules from a given date.
2. **Objective.** Model each change in the rules as a **dated, cited event**. Rebuild an affair's gross salary month by
   month (base, primes and allowances) by folding those events. Show the result as a **ledger** the user can filter,
   export (xlsx, csv) and print in a concise form. The events are typed in the app here; sprint 06 adds an **MCP server**
   that reads them from the JORT PDFs and proposes them for a person to review.
3. **Read first.** [`design_review.md`](design_review.md) (inconsistencies in the idea and in the current model) and
   [`event_storming.md`](event_storming.md) (events, policies, proposed model and the domain expert's questions).
4. **Out of scope.** Comparing owed and paid amounts (arrears), legal interest, the installer. Any of these can follow.

---

## Phases and sub-sprints

The status of every sub-sprint is **To do**. Each one is written test first, in the test project of its layer. The "First
tests" column gives the scenarios that drive the code.

### Phase A: discovery (no production code)

| # | Sub-sprint | Output | First tests |
|---|---|---|---|
| 05.0 | **Event-storming workshop.** Go through `event_storming.md` with the domain expert, answer the 10 hotspots, and fix the vocabulary (Arabic / French / code) | Decisions recorded in this file; `ubiquitous_language.md` | — |
| 05.1 | **Golden case.** One real affair, worked out by hand by the expert from a real agreement (textile, 2015–2026): career, components, expected amounts by month, in a spreadsheet | `tests/fixtures/golden_textile.xlsx` plus the cited texts | The end-to-end acceptance test (red until 05.9) |

### Phase B: domain kernel (`PayrollCalculator.Domain`)

| # | Sub-sprint | Content | First tests |
|---|---|---|---|
| 05.2 | **Money and units** | `Money` (millimes, `long`), `RoundingRule` (HalfUp, Truncate), `PayUnit`, `WorkRegime`, `Percentage` | 76.491 × 6.5 % → 81.463 (HalfUp) / 81.462 (Truncate); no `double` anywhere; adding millimes is exact |
| 05.3 | **Legal sources and citations** | `LegalSource` (kind, number, JORT issue and date, file), `Citation` (source, page, article) | A citation needs a source and a page; the JORT reference is formatted in Arabic in `Labels` |
| 05.4 | **Rule events** | `RuleEvent` hierarchy: `GridLineDeclared`, `SalaryGridEnacted`, `SalaryGridRevalued`, `AllowanceIntroduced` / `Revalued` / `Abolished`, `CareerRuleChanged`, `WorkingTimeChanged`, (`MinimumWageChanged`). Invariants at creation | A grid with a missing cell is rejected, or kept and flagged; a revaluation of an unknown category is rejected; an amount must be ≥ 0 |
| 05.5 | **Rulebook fold** | `Rulebook.From(events).At(day)` → `PayRules`; ordering by `(EffectiveFrom, kind, sequence)`; supersession; retroactivity | An enactment in 2024 replaces the one from 2022; a retroactive order published in 2025 with effect from 2024 changes 2024; two events on the same day apply in a fixed order; a retracted event is ignored |
| 05.6 | **Revaluation rules** | `RevaluationRule` (percentage for all, percentage per category band, fixed amount), rounding, **published amounts override**, the published-vs-computed check | +6.5 % then +7 % gives 87.165 from the unrounded value; a published 81.462 beats a computed 81.463, with a warning |
| 05.7 | **Allowances** | `AllowanceBasis` strategy: fixed monthly, per hour, per day, percentage of base; seniority brackets | A percentage-of-base allowance follows a raise of the base without any new event; an abolished allowance disappears from that date |
| 05.8 | **Career** | `CareerEvent` hierarchy (`Hired`, `Reclassified`, `DegreeOverridden`, `RegimeChanged`, `UnpaidAbsence`, `Separated`); `Career.At(day, rules)` with cumulative progression | The degree never goes down when the durations change (C4); a reclassification resets or keeps the seniority as answered in 05.0; an absence suspends the seniority (❓) |
| 05.9 | **Pay ledger** | `PayLedgerBuilder` → `PayLedger` of `LedgerEntry`; `IProrationPolicy`; hourly → monthly conversion through the regime; `LedgerIssue`; causes and citations on each entry | First and last months prorated; an hourly grid converted with 208 h; a missing cell gives an issue, not a zero; **the golden case passes** |
| 05.10 | **Ledger views** | `ChangesOnly()`, `Collapse()` (runs of identical months), `Filter(component, period, issuesOnly)`, totals per component and per year | Three months with the same values collapse into one row "from–to"; the filters can be combined |

### Phase C: persistence and migration (`PayrollCalculator.Infrastructure`)

| # | Sub-sprint | Content | First tests |
|---|---|---|---|
| 05.11 | **Event store** | Tables `LegalSources`, `RuleEvents` (type, schema version, JSON payload, status Draft / Approved / Rejected / Retracted, effective and published dates, citation), `CareerEvents`; EF migration; repositories | Round trip of each event type; an approved event cannot be updated (the repository refuses); JSON schema versioning |
| 05.12 | **Migrating existing data** | Each `SalaryTable` version → `GridLineDeclared` + `SalaryGridEnacted` (end dates dropped and checked); `TableNumber` → grid line; category labels → codes; `Affair.InitialPosition` → `Hired`; legacy import still works | Parity test: on the existing data the new ledger gives the same base amounts as `SalaryTimelineCalculator` (apart from the documented fixes C1, C4 and C7). Once parity holds, the old calculator and tables are removed |
| 05.13 | **Ledger snapshots** | `LedgerSnapshots` table (affair, event ids, hash, date, user) taken on export and print | The same events give the same hash; a snapshot can be reproduced after a correction |

### Phase D: application and UI (`Application`, `Wpf`)

| # | Sub-sprint | Content | First tests |
|---|---|---|---|
| 05.14 | **Application services** | `LegalSourceService`, `RuleEventService` (draft, approve, reject, retract), `CareerService`, `PayLedgerService.BuildAsync(affairId, filter)` → DTOs | Only approved events reach the ledger; approving needs a signed-in user, recorded as the reviewer |
| 05.15 | **Agreement timeline screen** | For one agreement: its events as a timeline, filtered by kind; an editor for each type (the current grid editor reused for enactments); "revalue by %" with a preview and a published amount per cell | View-model tests: the preview shows the computed amount, the published amount and the difference |
| 05.16 | **Career on the affair screen** | The "sector and start date" wizard creates `Hired`; then a list of the career events | Opening an affair without a grid line is rejected with an Arabic message |
| 05.17 | **Ledger screen** | A grid with a row per month and a column per component (pivot view) plus a detailed view; filters (component, period, changes only, issues only); frozen headers; the citation in a cell tooltip; changed values highlighted | Filter view-model tests; rendering checked visually (UI Automation) |
| 05.18 | **CSV and XLSX export** | CSV in UTF-8 with a BOM (so Excel shows the Arabic correctly), `;` separator; XLSX with ClosedXML (MIT licence), right-to-left sheet, `0.000` format, one sheet for the ledger, one for the citations | Export tests read the file back: headers, millimes, RTL flag |
| 05.19 | **Concise print** | WPF `FixedDocument` with a preview: affair header, collapsed periods table, totals, JORT citations as footnotes, snapshot hash in the footer; A4 portrait, Arabic | A document-building test (page count, rows); a visual check of the preview |

### Phase E: JORT MCP server (moved to sprint 06)

The MCP server, its connector and the review inbox now have their own sprint:
[`../06_JORT_MCP_Server_Connector/sprint.md`](../06_JORT_MCP_Server_Connector/sprint.md). Sprint 05 provides what that
sprint writes into: the rule events, the Draft / Approved status and the application services (05.4, 05.11 and 05.14).

### Phase F: closing

| # | Sub-sprint | Content |
|---|---|---|
| 05.26 | Update the docs (`dotnet/README.md`, `CLAUDE.md`), remove the old code, note the final number of tests |

---

## Delivery order

Phase B (05.2 → 05.10) is valuable on its own, and it can be tested from the golden case alone. I suggest two
deliverables:

1. **05.0 – 05.13**: the new model, with the data migrated and the ledger correct (no new screen yet).
2. **05.14 – 05.19**: the ledger screens, the export and the print.

## Definition of done

- The golden case passes end to end.
- Every ledger amount can be traced to one or more approved events and their JORT citation.
- No monetary amount is a `double`, and rounding happens only where an event or a policy says so.
- Only approved events reach the ledger (Draft events, which sprint 06 will create from the PDFs, never do).
- Warnings are errors, all tests pass, and all new UI text is in `Labels` (Arabic).

## Risks

| Risk | Mitigation |
|---|---|
| The rules (proration, seniority, primes) are not known yet | 05.0 and 05.1 come first; each rule is a strategy (`IProrationPolicy`, `IDegreeProgressionPolicy`, `IAllowanceBasis`) that can be swapped without touching the rest |
| Misreads of the scans (OCR or vision model) | Covered in sprint 06 (mandatory human review, validation tools); here, the published-vs-computed check (05.6) |
| Migrating existing data changes results | A parity test (05.12) with every difference explained; the old calculator stays until parity holds |
| The size of the sprint | Two deliverables (above), each of them usable |

## Decisions (filled in during 05.0)

| # | Question (see `event_storming.md` §5) | Answer |
|---|---|---|
| 1 | Components in the claim | |
| 2 | The two columns of the 2024 order | |
| 3 | Partial-month proration | |
| 4 | Seniority prime rule | |
| 5 | Degree progression | |
| 6 | SMIG as a floor | |
| 7 | Hours per month per regime | |
| 8 | Effect of an unpaid absence on seniority | |
| 9 | Owed vs paid | |
| 10 | MCP: local PDFs or IORT; which client | Moved to sprint 06 |
