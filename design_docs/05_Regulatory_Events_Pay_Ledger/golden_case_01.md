# Golden case 01: Ste Yosra, IT lead, 1998–2008 (simulated)

**Purpose (05.1).** One career, worked out by hand, that the app must reproduce exactly from month to month. It is the
only proof that the engine is right in law, and not just consistent with the old code. This case is a **simulation**
played by the product owner as the employee. Every value still has to come from a real text, or be marked as an
assumption.

## What is known

| Fact | Value | Source |
|---|---|---|
| Employer | Ste Yosra | product owner |
| Period | September 1998 → August 2008 (10 years, 120 months) | product owner. Exact days to confirm: from 1 September 1998 to 31 August 2008? |
| Job | IT lead (responsable informatique) | product owner. A job title, not a category of a grid |
| Pay received | **Net** 400 TND at the start → 600 TND at the end | product owner. When it changed is not known |

## What this case shows straight away

Each point below is a gap in either the data or the product. Most of them would have surfaced only in court.

| # | Point | Why it matters | What is needed |
|---|---|---|---|
| Y1 | **Net is not comparable with the grids.** The grids set a **gross** base salary. Net = gross − the employee's social contributions (CNSS) − income tax (IRPP). Both rates and the tax scale changed between 1998 and 2008, through legal texts too | Comparing a net 400 with a gross grid amount overstates the arrears | **Gross** amounts from the payslips, if available (preferred). Otherwise a net → gross conversion, which is its own set of rule events (CNSS rates, IRPP scale, deductions) and its own sprint. It is not assumed here |
| Y2 | **Which agreement applies?** It depends on Ste Yosra's sector of activity, not on the job. An IT lead in an IT services company, a bank or a factory falls under different agreements, or under none | Decides every amount | Ste Yosra's main activity (its registry code, if known). If no sectoral agreement covers it, the floor is the minimum wage (SMIG / SMAG, by decree), which is question 6: then `MinimumWageChanged` becomes required |
| Y3 | **Category and degree at hiring.** "IT lead" must be placed in a category of the grid (for example cadre / agent de maîtrise / exécution) and a degree | Decides the starting cell and the progression | The category and degree on the contract or the payslips; otherwise the expert's classification, with the reason written down |
| Y4 | **The texts in force from 1998 to 2008.** The library has 2022 and 2024 texts (textile, the 2024 order) and the electronics agreement; nothing for 1998–2008 so far. The grid in force **on the hiring day** may come from a text before 1998 | Without them, the ledger reports `NoGridInForce`, not a guessed amount | The agreement and every rider or order with an effect between its base text and August 2008, added to `data/jort/` (07.2) |
| Y5 | **When did the pay go from 400 to 600?** | Arrears are computed per month | The months where the pay changed, or the payslips (a list "from month X: amount" is enough) |
| Y6 | **Primes and allowances**: which ones were paid, and which ones the agreement grants (seniority, transport, presence…) | Seniority alone (10 years) is probably the largest prime here | What the payslips show, and the agreement's prime clauses |
| Y7 | Work regime (48 h or 40 h), monthly or hourly pay | Hourly grids need the hours | From the contract or the payslips |
| Y8 | Unpaid absences, promotions, earlier service (time already in degree) | Changes the degree and the seniority | Yes/no and dates |

## The case once the gaps are filled

The career events are known except for the line, category and degree:

```text
Hired       1998-09-01  line = <agreement grid line>, category = <Y3>, degree = <Y3>, regime = <Y7>
Separated   2008-08-31
(+ Reclassified / UnpaidAbsence if Y8 says so)
```

What the expert fills in (`tests/fixtures/golden_01/expected.xlsx`), one row per month:

| Month | Category | Degree | Base owed (gross) | Each prime owed | Total owed | Gross paid | Net paid | Text cited |
|---|---|---|---|---|---|---|---|---|
| 1998-09 | … | … | … | … | … | … | 400.000 | … |
| … | | | | | | | | |
| 2008-08 | … | … | … | … | … | … | 600.000 | … |

**The acceptance test**, red until the data is entered:
- The ledger built from the seed and these career events gives the "owed" columns for every month, to the millime.
- Sprint 08 then checks "owed − paid", keeping the negative months, against the same file.

## Questions for the product owner (as the employee)

1. What was Ste Yosra's activity? (Y2)
2. Do you have the payslips, or at least the **gross** salary? If only the net, roughly from which month did it go up? (Y1, Y5)
3. Did the contract or the payslips give a category and a degree? (Y3)
4. Which primes appeared on the payslips? (Y6)
5. 48 h or 40 h a week? Any unpaid leave or promotion? (Y7, Y8)
6. Exact first and last days worked? (period)

*Until these answers come back, nothing in this case is invented. In particular, no grid amount is made up to fill a
gap: the ledger reports the gap instead.*
