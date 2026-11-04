# Seed: travel agencies agreement (الاتفاقية المشتركة القطاعية لوكالات الأسفار)

**Status: seeded in the app (read-only) from `seed.json` and the CSVs of this folder. Read from the page images: still to be checked by a person against the PDF.**

## The agreement and its riders (from the recitals of rider 11)

| Text | Signed | Approved by order of | JORT |
|---|---|---|---|
| Agreement | 22 April 1997 | 15 May 1997 | n° 41, 23 May 1997 (file `unidentified-00`) |
| Rider 1 | 30 June 1999 | 14 July 1999 | n° 59, 23 July 1999 |
| Rider 2 | 14 November 2002 | 25 November 2002 | n° 101, 13 December 2002 |
| Rider 3 | 29 December 2005 | 17 January 2006 | n° 7, 24 January 2006 |
| Rider 4 | 28 January 2009 | 17 February 2009 | n° 16, 24 February 2009 |
| Rider 5 | 30 March 2012 | 17 May 2012 | n° 40, 22 May 2012 |
| Rider 6 | 27 February 2013 | 8 March 2013 | n° 22, 15 March 2013 |
| Rider 7 | 22 July 2014 | 18 August 2014 | n° 68, 22 August 2014 |
| Rider 8 | 17 October 2016 | 22 November 2016 | n° 96, 25 November 2016 |
| Rider 9 | 11 July 2017 | 31 July 2017 | n° 62, 4 August 2017 (file `unidentified-09`) |
| Rider 10 | 31 October 2018 | 30 November 2018 | n° 99, 11 December 2018 (not in the library: `unidentified-10` is from the same issue but for another agreement) |
| Rider 11 | 14 September 2022 | 20 October 2022 | n° 120, 8 November 2022 (file `unidentified-11`) |

To rebuild salaries from 1997 on, the library still needs the riders whose grids are missing: 1–8 and 10.

## Grids (monthly, 48-hour week, 10 categories × 13 degrees)

Categories 1–5 are execution staff (التنفيذ), 6–8 supervision staff (التسيير), 9–10 cadres (الإطارات). Time in
degree: 1 year for degree 1, 2 years for degrees 2–7, 3 years for degrees 8–12; degree 13 is the last. In the CSV, `0`
stands for the «-» of the text.

| File | In force from | Source |
|---|---|---|
| `agreement-1997_table-1.csv` | 1996-05-01 | agreement of 22 April 1997, table 1, JORT 41/1997 p. 934 |
| `agreement-1997_table-2.csv` | 1997-05-01 | same, table 2 |
| `agreement-1997_table-3.csv` | 1998-05-01 | same, table 3 |
| `rider-09_table-1.csv` | 2017-05-01 | rider 9, JORT 62/2017 p. 2496 |
| `rider-11_table-1.csv` | 2022-05-01 | rider 11, table 1, JORT 120/2022 p. 3266 |
| `rider-11_table-2.csv` | 2023-05-01 | rider 11, table 2, JORT 120/2022 p. 3267 |

**Checks:**
- **The 1997 tables** are printed in millimes with no separator (175032 = 175.032). In each category, every degree adds the same step: 1.500, 1.600, 1.700, 2.100, 2.600, 3.000, 3.500, 4.400, 4.800 and 5.200 TND for categories 1 to 10. Each year adds a fixed amount per category, the same in 1997 and 1998: 8.000, 11.000, 12.500, 13.500, 14.500, 15.500, 17.000, 19.000, 20.000 and 21.000 TND. These are fixed-amount raises, not percentages. All 390 cells agree.
- **Degrees and categories:** in all three grids, no amount is lower than the one of the degree before it, or of the category before it.
- **2023 against 2022:** every cell of 2023 = the 2022 cell × 1.065 (rider 11, article 2), within one millime.
- **2022 against 2017:** every cell of 2022 = the 2017 cell × 1.219318 (all 130 cells agree to ±0.000002). That is 1.07 × 1.07 × 1.065, which points to two raises of 7 % between 2017 and 2022 (rider 10?) before rider 11's 6.5 %.
- **Read with care:**
  - table 1 of 2022, category 8, degree 7: the last glyph is damaged. It's read as 8 (1006.238), the same broken 8 as in 915.568 above it. This agrees with the row's progression and with 2023 (1071.644).
  - table 2 of 2023: the degree-13 column is clipped at the page edge; its leading digits are restored from the 6.5 % rule.

## Primes (not grids)

| Prime | From 1 May 2017 (rider 9) | From 1 May 2022 (rider 11) | From 1 May 2023 (rider 11) |
|---|---|---|---|
| Transport (منحة النقل), execution and supervision staff, per month | 61.583 | 75.089 | 79.970 |
| Transport (منحة النقل), cadres, per month | 66.583 | 81.186 | 86.463 |
| Presence (منحة الحضور), per day actually worked | 0.424 | 0.452 | 0.481 |
| Presence (منحة الحضور), plus per month | 3.180 | 3.387 | 3.607 |
| Drivers' flat overtime prime (السواق), October to May, per month | — | 42.600 | 45.369 |
| Drivers' flat overtime prime, June to September, per month | — | 106.500 | 113.423 |

The transport amounts include the transport prime of decree 503 of 16 March 1982.

**In the app:** seeded from `seed.json`, one prime per group (transport) or per season (drivers), the presence prime
as two primes (per day worked, and monthly). The 1997 amounts (articles 45, 46 and 48, below) are the first rate of
the same primes. The meal prime (art. 47), the production prime (art. 44) and the retirement prime (art. 43) are not
seeded: they are not a fixed monthly amount.

## Rules of rider 11 worth knowing

- **2022 raise:** +6.5 % of the base salaries, computed on the 2019 base salary. **2023 raise:** +6.5 %, on the 2022 base. Both apply from 1 May.
- **Backdating:** firms that had given less than the agreed raise owe the difference from 1 May 2022.

## Classifying an employee (agreement of 1997)

`agreement-1997_categories.json` is the classification table of JORT 41/1997 p. 933 (a scanned page). For each category
it gives its group, its definition and entry conditions, and examples of jobs.

- **Categories 5–10 are transcribed. Categories 1–4 are missing from this file:** the table starts at category 5. They
  have to come from another copy of the issue, or from a later rider.
- **«مسؤول الإعلامية» (IT lead) is category 8**: supervision staff (التسيير), someone who has completed the first stage of
  higher education. Golden case Y3 needed this, for the travel agencies sector.
- **Reclassification (art. 50):**
  - when the agreement came into force, every worker was placed according to the duties they actually performed;
  - after a reclassification, the worker is placed at the degree whose salary equals their former base salary
    (seniority prime included), or failing that, at the next higher one.

## Primes and other amounts of the 1997 agreement (JORT 41/1997 pp. 931–932)

| Art. | Prime | Amount | Who, and conditions |
|---|---|---|---|
| 45 | Transport (منحة النقل) | 14 TND/month (execution and supervision staff), 19 TND/month (cadres) | Includes the transport prime of decree 503 of 16 March 1982, amended by decree 691 of 19 July 1986 |
| 46 | Presence (منحة الحضور) | 0.103 TND per day actually worked | Paid monthly |
| 47 | Meal (منحة الأكل) | 2.500 TND | Drivers and reception staff who meet or see off customers and must stay on duty at mealtimes at airports or ports; not for staff who normally work there |
| 48 | Care and maintenance (منحة العناية والصيانة) | 15 TND/month | Warehouse technicians and drivers; can be withheld for the month of a serious second-degree fault |
| 44 | Production and end-of-year (منحة الإنتاج وآخر السنة) | up to 45 days' pay a year, paid every six months, in proportion to time actually worked (annual leave included) | Annual score out of 20: production 0–6, punctuality 0–5, conduct 0–5, care of property and dealings with customers 0–4. One month's pay whatever the score; plus half a month for 20/20, ten days above 15 and below 20, five days from 10 to 15. "Pay" here means base salary + transport + presence. Can be withheld after a sanction of the second degree. Under 15/20, the worker can ask the advisory committee for a review. |
| 43 | Retirement (منحة التقاعد) | 2 months' pay for 10–15 years with the last employer; 3 months for more than 15 and up to 20; 5 months for more than 20 and up to 25; 6 months for more than 25 | Beginning of the article to check on p. 931 |
| 52 | In force | from 1 May 1996 | Signed in Tunis on 22 April 1997 |
