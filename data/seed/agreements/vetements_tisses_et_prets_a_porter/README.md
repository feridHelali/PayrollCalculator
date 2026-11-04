# Seed: clothing agreement (الاتفاقية المشتركة القطاعية لصناعة الملابس المنسوجة والجاهزة), rider 18

**Status: seeded in the app (read-only) from `seed.json` and the CSVs of this folder. Read from the page images: still to be checked by a person against the PDF.**

## Source

- **File:** `data/jort/files/arrete-2024-1383-ar.pdf`.
- **Text:** order of the Minister of Social Affairs of 8 April 2024, approving rider 18 of the agreement.
  - Signed 29 February 2024; in force from 1 January 2024.
  - Published in JORT issue 49 of 16 April 2024, pages 3513–3523.
- **Grids:** pages 6–11 of the PDF (JORT pages 3518–3523). They are scanned images rotated a quarter turn, with no text layer.

## The six grids

| File | Grid | Unit | In force from | Size | JORT page |
|---|---|---|---|---|---|
| `rider-18_table-1.csv` | Table 1, «الأعوان الخالصون بالساعة» | hourly | 2024-01-01 | 6 categories (I–VI) × 18 degrees | 3518 |
| `rider-18_table-2.csv` | Table 2, «الأعوان الخالصون بالشهر» | monthly | 2024-01-01 | 18 categories (1أ, 1ب, 2–17) × 18 degrees | 3519 |
| `rider-18_table-3.csv` | Table 3, hourly | hourly | 2025-01-01 | 6 × 18 | 3520 |
| `rider-18_table-4.csv` | Table 4, monthly | monthly | 2025-01-01 | 18 × 18 | 3521 |
| `rider-18_table-5.csv` | Table 5, hourly | hourly | 2026-01-01 | 6 × 18 | 3522 |
| `rider-18_table-6.csv` | Table 6, monthly | monthly | 2026-01-01 | 18 × 18 | 3523 |

**Format:**
- The first line lists the degrees; the second line gives the years to spend in each one (مدة البقاء بالدرجة): 1 year for degrees 1–3, then 2 years each.
- Each following line is a category, with one amount per degree.
- A note on every grid says the amounts include the temporary complementary prime (decree 437 of 7 April 1981, raised by decree 501 of 16 March 1982).

## Checks run (all 1,296 cells)

- **Degrees:** no amount is lower than the one of the degree before it.
- **Categories:** no amount is lower than the one of the category before it, at the same degree.
- **Across years:** every 2025 cell equals the 2024 cell × 1.065, and every 2026 cell equals the 2024 cell × 1.065 × 1.07, rounded to the millime. Two exceptions, below.

## To decide: two cells printed with two decimals

| Table 2 (2024) cell | Printed | Implied by table 4 (2025 ÷ 1.065) | In the CSV |
|---|---|---|---|
| category 16, degree 4 | «1 173,63» | 1 173.635 | 1173.63 (as printed) |
| category 17, degree 2 | «1 226,31» | 1 226.315 | 1226.31 (as printed) |

The printed text is the authority, so the drafts keep the printed value. The 2025 grid shows that the last digit was
lost in printing (5 millimes per month). **Decision for the domain expert.**

## Not grids: the primes of the same rider (article 49, page 3516)

| Prime | 2024 | 2025 | 2026 | Notes |
|---|---|---|---|---|
| Presence (منحة الحضور), all workers | 7.930 | 8.445 | 9.036 | per month; includes the half-day prime of article 5 of the decree of 8 January 1948 |
| Transport (منحة النقل), execution and supervision staff | 68.693 | 73.158 | 78.279 | per month |
| Transport (منحة النقل), cadres | 76.491 | 81.462 | 87.165 | per month; so the "two columns" are two groups of categories, not two work regimes |
| Assiduity (منحة التشجيع على المواظبة), all workers | 14.050 | 14.964 | 16.011 | per month; lost by anyone absent more than once in the month without a justified reason |

**Other rules of the rider (page 3516):**
- **Promotion:** a worker promoted to a higher category keeps the same degree.
- **Moving up a degree:** each step gives a 1% raise, computed on the amount of the degree held before the step.

## In the app

The six grids and the four primes above are seeded (see `seed.json`): they appear in **جداول الأجور** and
**المنح والمنافع** on a new database, read-only. To change a value, edit the CSV or `seed.json` and add a migration.
