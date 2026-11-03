# Sprint 05: design review

This review covers two things: the idea proposed for sprint 05, and the domain model built in sprints 01 to 04. Each finding
says what is wrong or ambiguous, why it matters for a salary reconstruction used in court, and what the new model does about it.

## 1. Evidence from the sample texts (`design_docs/PDF Conventions`)

I read the sample PDFs before modelling.

1. **The Arabic PDFs have no usable text layer.** `pdftotext` returns the numbers but garbled glyphs instead of the Arabic
   words (the fonts have no Unicode mapping). An extractor cannot rely on the embedded text. It has to render each page
   as an image and read it, with a vision model or with OCR (Tesseract `ara`).
2. **A text is a chain of riders.** The 2024 order (`arrete2024-1383arabe-35.pdf`) lists the agreement's riders (ملاحق
   تعديلية) 1 to 17, from 1975 to 2022, then adds rider 18. Each rider is approved by a ministerial order published in the
   JORT. The textile "MAJ 17" file has the same structure. So an agreement's history is a sequence of events.
3. **One order holds several staged changes.** The 2024 order gives amounts that take effect in 2024, then 2025, then 2026
   (for example 7.930 → 8.445 → 9.036, and 14.050 → 14.964 → 16.011). One publication therefore produces several
   events, each with its own effective date.
4. **Increases are percentages, but the published amounts are what counts.** The steps match +6.5 % then +7 %. Recomputing
   them does not always give the published amount:

   | Published base | × 1.065 | Published | × 1.065 × 1.07 | Published |
   |---|---|---|---|---|
   | 76.491 | 81.462915 | **81.462** (cut) | 87.165319 | **87.165** (rounded, from the unrounded value) |
   | 106.500 | 113.422500 | **113.423** (rounded half up) | | |
   | 7.930 | 8.445450 | 8.445 | 9.036632 | **9.036** (cut, or chained from 8.445) |

   One rounding rule cannot reproduce every published value. **The published amount is the authority. The percentage is
   only a check, or the fallback when the text gives no amount.** The model keeps both and reports any difference.
5. **Some amounts come in two columns** (76.491 / 68.693, 81.186 / 75.089). These are probably two work regimes (48 h and
   40 h), or two groups of categories. This is still an open question (see the sprint's questions).
6. **Some texts state the increase as a percentage by group of categories** (Gmail `7.pdf`: 11.60 % / 9.67 % for
   categories 1–2, …). In that case the revaluation event carries a percentage for each band, and there is no grid.

## 2. The proposed idea

| # | Point in the proposal | Issue | Decision |
|---|---|---|---|
| P1 | "Model the grids as events" | A grid is **state**. The event is its **enactment**: a grid published at a date, by a source, effective from a date. If the two are mixed up, every month would need its own "grid event". | Events: `SalaryGridEnacted`, `SalaryGridRevalued`, … State: the `PayRules` in force in a month, computed by folding the events. |
| P2 | "The MCP server generates the grids and fills the cells" | The values come from OCR or a language model reading a scan, and the results are used in court. If the MCP writes directly into the data the calculation uses, an unchecked misread becomes a figure in a court filing. | The MCP only **proposes Draft events** with a citation (file, page). A person reviews them against the page image and approves them. Only approved events are used in calculations. |
| P3 | "Raises update the primes over time" | Raises also apply to the **salary grids**. Some primes are not amounts at all but **rules** (for example a seniority prime as a percentage of the base). A raise does not change those; it changes the base they are computed from. | Each allowance has a `Basis`: a fixed amount, a percentage of the base, or an hourly rate. A revaluation event changes amounts. Percentage-based components are **derived** each month. |
| P4 | "The user picks a sector and a start date" | That is not enough to compute a salary. The calculation also needs the category and degree at hiring, the end date (layoff), the work regime (40 h or 48 h), which grid applies (workers, supervisors or managers), and any reclassification or unpaid absence during the career. A sector can also map to more than one agreement. | The case becomes a stream of **career events** (`Hired`, `Reclassified`, `RegimeChanged`, `UnpaidAbsence`, `Separated`). The first-screen "sector + date" becomes a hiring wizard that produces the `Hired` event. |
| P5 | "Collect all the values by month and apply all the updates" | The order matters. Events must be applied by **effective date**, not publication date: a 2024 order can take effect retroactively. Two events on the same day need a defined order, and an event can later be retracted. | Folding sorts by `(EffectiveFrom, Kind order, Sequence)`. Publication date and effective date are separate fields. A correction is a `Retracted` marker plus a new event; an approved event is never edited. |
| P6 | "The log is a grid of all factors" | The log is a **read model**. It is computed from the events and is not where the truth is stored. But a figure filed in court must be **reproducible** even after the rules are corrected. | The ledger is projected on demand. When the user prints or exports it, the app saves a **snapshot**: the approved event ids, a hash and the date. The report can then be produced again identically later. |
| P7 | Hourly and monthly grids in the same log | Hourly rates and monthly amounts cannot be added up. | Each entry keeps its rate **and its unit**. A separate `MonthlyAmount` column holds the rate converted with the regime's hours per month, and only that column is totalled. |

## 3. The current model (sprints 01–04)

| # | Where | Issue | Effect | Fix in sprint 05 |
|---|---|---|---|---|
| C1 | `SalaryTable.ApplicationPeriod` has an end date | Under labour law, a grid stays in force **until a later text replaces it**. The end dates the user types create artificial gaps (`NoApplicableTable`), and they can also make two versions overlap. Nothing prevents overlaps; the calculator silently picks the one that starts last. | Gaps or ambiguous months | A grid has only `EffectiveFrom`. It is superseded by the next enactment. |
| C2 | A grid's versions are linked by the `TableNumber` string | Table numbers can change from one rider to the next, and a typo breaks the chain. | Months fall back to "no table" | A stable `GridLine` id (for example "workers' grid") carries all its versions. |
| C3 | Categories are matched by **label** (`Ordinal`) from one version to the next | A category renamed or retyped in a later rider ("C1" vs "C 1", or Arabic vs French) no longer matches. | `CategoryNotFound` for the rest of the career | Categories get stable codes on the grid line, and each version gives labels to those codes. A reclassification is an explicit event. |
| C4 | `YearsInDegree` is stored in **each table version** | The career rule belongs to the agreement, not to the salary values. If two versions have different durations, `SeniorityDegreeProgression` recomputes the degree from scratch every month with that month's table, so the degree can **go down**. | Non-monotonic careers | A separate `CareerRuleChanged` event. Progression is **cumulative** (the degree reached is kept, and the time in the current degree carries on). A test checks the degree never decreases. |
| C5 | `Affair.InitialPosition` is fixed | It cannot express a promotion, a reclassification, or a career that starts mid-grid. | Wrong amounts after a promotion | Career events (P4). |
| C6 | `Affair` holds both `AgreementId` and `SalaryTableId` | The same information is stored twice, and the two can disagree (the table belongs to an agreement). `ChangeAgreement` unlinks the table to compensate. | Inconsistent state is possible | `Hired` references the `GridLine`. The agreement follows from it. |
| C7 | Seniority counts whole calendar months from the start month | It ignores the day of the month, and the first and last months are counted as full. | Overpays the first and last months; the degree can change one month early | `ProrationPolicy` (calendar days, or a 30-day base: open question). Seniority is counted in days and then converted to months. |
| C8 | Only the base salary | The claim covers the **gross salary**: base plus primes and allowances. | The figure is incomplete | `Allowance*` events, and the ledger lists one entry per component. |
| C9 | The user types every grid version by hand, with no **source** | Nothing ties a figure back to its JORT reference, and a percentage raise forces the user to type every cell again. | Errors that cannot be traced, slow data entry | `LegalSource` and `Citation` on every event. `SalaryGridRevalued` by percentage, with a preview, and the published amount can override each cell. |
| C10 | Results are not versioned | If a grid is corrected, every past result changes silently. | A filed figure can no longer be reproduced | Ledger snapshots (P6). |
| C11 | `decimal` amounts with no rounding rule | Tunisian dinar amounts have 3 decimals (millimes), and computed raises need a rounding rule. | Differences of ±1 millime from the published amounts | A `Money` value object in millimes, a `RoundingRule` per event, and a published-vs-computed check (§1.4). |
| C12 | `TotalMonthlyAmount` skips hourly months | The total looks complete when it is not. | A misleading total | P7: everything is converted to monthly amounts, and a month that cannot be converted is listed as an issue. |
| C13 | No legal minimum wage | If a grid amount is below the legal minimum wage (SMIG), the minimum applies. | Possibly underpays | An optional `MinimumWageChanged` event (decree) and a floor in the ledger (to confirm). |

## 4. What stays

- The aggregates `SectorialJointAgreement` and `User`, and the value objects `YearMonth`, `DateRange` and the error codes.
- The rule "**gaps are reported, never guessed**" (`TimelineIssue`). It becomes `LedgerIssue`.
- The strategy pattern for career progression (`IDegreeProgressionPolicy`). It now applies to a cumulative career state.
- The current grid editor, reused to edit a `SalaryGridEnacted` event.
- The existing data: each table version becomes a `SalaryGridEnacted` event (§ migration, sub-sprint 05.11).
