# Three approaches compared: the Electron app, the .NET event model, Gemini's pipeline

**Question.** Which way of building the product gets us to *a released app holding every agreement, whose results
hold up in court*, at the lowest cost and risk?

**Criteria**, in order of weight:

1. **Correctness and traceability in court**: every amount comes from a cited text, and every month can be explained.
2. **Data-entry throughput**: about 16 PDFs today and dozens of grids, mostly scanned Arabic.
3. **Time to a first release.**
4. **Maintainability**: one developer, plus LLM assistants.
5. **Distribution**: nothing to install beforehand (D5).

---

## 1. The original Electron / React / TypeORM app

**What it got right**

- **Grid entry as a spreadsheet** (`SalaryTableSheet`) is the fastest way for a person to type a grid, and the clearest
  way to check one against a PDF. This should be kept, as the editor and as the review screen.
- Electron also needs no runtime on the user's machine, which meets D5.
- It had the right core entities: agreement, grid, affair, and the actual paid salary (`ActualMonthSalary`), which is
  the arrears idea Gemini raises again.

**What blocks it**

- **A grid is a state with an end date, not a dated change.** Gaps between versions and overlaps have no answer
  (design review C1), and a retroactive text can't be represented.
- **No source, no versioning.** An amount can't be traced to a JORT page, which is disqualifying in court.
- **Categories matched by label**, degrees and categories in JSON columns, `synchronize: true` with no migrations. The
  data model drifts, and every user's database differs.
- **The calculation was never finished.** `ActualMonthSalary` exists, but no algorithm produced a month-by-month
  result. The .NET port had to write the first real calculator.
- Each feature crosses seven layers (entity → service → IPC → preload → typing → thunk → page), which is slow to change.

**Verdict.** Keep the spreadsheet editing experience and the "paid salary" entity. Drop the model.

---

## 2. My approach: the .NET event model (sprints 05–07)

**What it gets right**

- **It is built for court work.** Each rule is a dated, cited event that a person approves. The ledger has one row per
  month and component, each with its causes. A snapshot is sealed with a hash and rebuilds the same result after any
  correction. A retroactive text just sorts earlier.
- **It exists and it is tested.** 298 tests, 0 warnings. On the real data it matches the first calculator for all
  443 months, and every difference elsewhere is explained.
- **Updates are safe by design.** Approved events never change, so shipping a new seed only adds events (R4).
- **It meets D5:** published self-contained, with Velopack.

**What I criticise in it**

- **Too much plan, no real data yet.** Three sprint documents, an MCP server of 16 sub-sprints, and not one real
  agreement entered from a PDF. The **golden case (05.1)**, the one real career checked by the domain expert, is
  still missing. Until it exists, the engine is proven consistent with the old code, not correct against the law.
  **This is the biggest risk in the project.**
- **The 10 hotspot questions are still open** (mid-month changes, proration, reclassification, absences, ceilings,
  minimum wage). The defaults are reasonable but unconfirmed, and some will change results.
- **The MCP server (sprint 06) is likely oversized for 16 PDFs.** A full connector with resources, prompts, an
  evaluation set and packaging is right for hundreds of texts or for other users. For one library of about 16 files,
  a lighter loop gives the same result sooner (see the recommendation).
- **It is heavy for one developer**: event sourcing, a review workflow, a JSON format written by hand, snapshots.
  Each piece is justified by the court requirement, but the whole only pays off once the screens exist (Phase D).
- **The court's real output, arrears, isn't there yet.** The ledger shows what was owed. The court also needs
  owed minus paid. That is sprint 08, and it should move up.

**Verdict.** Keep the model: it is the only one of the three that is traceable, and it is already built. Cut the
pipeline around it down to size, and get real data in front of the expert sooner.

---

## 3. Gemini's pipeline (OCR → LLM → JSON → database)

**What it gets right** (all taken into sprint 07: G2–G5, G7, G9)

- Monotonicity as a misreading check. Two dates per text. A stable code per prime. Per-day primes.
- Above all, **arrears month by month, line by line**, because a global total gets rejected.

**What I criticise in it**

- **It assumes the pipeline is "ready"**: "Puisque votre pipeline d'extraction est prêt". It isn't. The reply opens
  by agreeing ("excellente approche") instead of checking.
- **OCR then LLM is the weaker chain for these PDFs.** Arabic OCR on scanned JORT tables is poor (design review §1).
  A vision model reading the **page image** directly and returning cells avoids one error-prone step. OCR is only
  a cross-check of the digits.
- **The output format can't be traced**: no page, no article, one effective date and one regime per text, and a
  string `"A_VERIFIER"` in a number field. A wrong digit would reach the database with no way to find its source.
- **No human approval step**, from OCR straight to the database. In legal use that is the step that can't be skipped.
- **The arrears code is wrong for court use:**
  - It **drops the months where the difference is negative**, so an overpayment disappears, and opposing counsel
    would find it.
  - It merges base and primes into one number.
  - It uses `decimal` money with a currency instead of exact millimes.
  - It doesn't cite.
- Its prompt contradicts itself: "STRICTEMENT SUPÉRIEUR ou ÉGAL".

**Verdict.** Its ideas are good; its implementation isn't usable as is.

---

## Scores

Each approach is scored from 1 (worst) to 5 (best) on each criterion.

| Criterion (weight) | Electron app | .NET events (as planned) | Gemini pipeline | **Recommended hybrid** |
|---|---|---|---|---|
| Court traceability (×3) | 1 | 5 | 2 | **5** |
| Data-entry throughput (×2) | 3 (manual) | 2 (MCP not built) | 4 (if it worked) | **4** |
| Time to first release (×2) | 2 (no calculator) | 2 (large plan) | 3 | **4** |
| Maintainability (×1) | 2 | 3 | 3 | **4** |
| No prerequisite (×1) | 5 | 5 | n/a | **5** |
| **Weighted total (/45)** | 20 | 31 | — | **40** |

---

## Recommendation: the hybrid

Keep the .NET event model (the trace and the tests). Use Gemini's ideas (checks, catalogue, arrears). Keep the old
app's best part (the spreadsheet editor). Replace the full MCP server with a **light seed loop** for now.

### The light seed loop (replaces sprint 06 for the first release)

1. `render_page`: the cited pages of each PDF as PNG. This is 06.1–06.4, small and needed anyway for citations.
2. An LLM with vision (Claude in Claude Code, or any client) reads the page images and **writes the seed JSON
   directly** in `data/seed/agreements/{CODE}.json`, in the `EventJson` format, **with the page and article for every
   event**. One event per effective date (G3); prime codes come from the catalogue (G4).
3. `seed verify` (07.4) checks:
   - every grid folds, with no error;
   - amounts don't decrease from one degree to the next (G2);
   - published amounts agree with the stated percentages;
   - every cited page exists;
   - every code is catalogued.
4. **A person reviews the git diff of the JSON next to the page image.** The pull request is the approval: it
   records who approved and when, in `approvedBy`. This fits D4, since seed rows are read-only in the app.
5. `seed build` → `payroll.seed.db` → the installer.

**Why this is better for now.** It needs about 4 small sub-sprints instead of 16. It works with any LLM client.
Every change is reviewable in git, which a court-facing product needs. The full MCP server (sprint 06) stays
planned, for when the library grows or other people enter texts. Its domain-side checks are the same code as
`seed verify`, so nothing is thrown away.

### Revised order

| Step | What | Why first |
|---|---|---|
| 1 | **05.1 golden case** with the expert, and answers to the 10 hotspot questions | The only proof the engine is right in law |
| 2 | 07.5 reset, 07.2 library, 07.3 seed format, 07.11 catalogue, 07.12 per-day basis and monotonicity | Cheap, unblocks data entry |
| 3 | Light seed loop: `render_page` plus `seed verify` / `build`, then enter the real agreements | Real data, early |
| 4 | Sprint 05 Phase D: the ledger screens, the CSV/XLSX export and the print, with the spreadsheet editor kept for review | A usable product |
| 5 | **Sprint 08: arrears** (paid amounts per month and component; arrears = ledger − paid, negative months kept) | What the court actually asks for |
| 6 | 07.6–07.9: first run, updates, Velopack self-contained installer | Release |
| Later | Sprint 06, full MCP server and connector | When the volume justifies it |

### Answers

- Light seed loop instead of sprint 06 for now: **yes** (07.13).
- Golden case: a first one is under way (`../05_Regulatory_Events_Pay_Ledger/golden_case_01.md`).
- PDFs are **not** shipped (D3 changed): the app shows full JORT references instead (sprint 07, R7).
