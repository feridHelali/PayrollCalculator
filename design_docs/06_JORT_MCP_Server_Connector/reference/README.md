# Reference: Tunisian payroll rules proposed by Gemini

`tunisian_payroll_reference.gemini.json` is a structure the product owner obtained from Gemini. It's kept here as
**input for the MCP prompts** (sprint 06): it gives vocabulary, prime codes, and the shape of the court-related
compensations. It is **not** a source of values for the app. Every rule or amount used in a calculation must come from
a cited text (law, decree, agreement, rider), like the grids.

## Review

| Item | Status | Note |
|---|---|---|
| CNSS rates (employee 9.18 %, employer 16.57 %, work accidents ≈ 0.5 %) | Plausible for recent years, **to check** | The rates changed over 20 years. Each change is a rule event with its effective date (the golden case simulated 7 %). |
| Income tax scale labelled "2026" (0 / 26 / 28 / 32 / 35 %) | **Probably out of date** | This looks like the scale used up to 2024. The finance law for 2025 is reported to have replaced it with a scale of more brackets. To check against the finance law before any use. |
| CSS 1 %, professional expenses 10 % capped at 2 000, family deductions | **To check** per year | All of them changed over time. |
| "Standard" seniority prime scale (5 % after 3 years … 25 % after 20) | **No such general rule** | Each agreement sets its own brackets. Take them from the agreement, never from this list. |
| Meal allowance tax-exempt up to 5 TND/day | **To check** | It depends on the year's tax rules. |
| Notice compensation, legal severance (1 day per month of service, capped at 3 months), unfair dismissal (1 to 2 months per year, capped at 36) | Useful outline; **articles and ceilings to check** in the Labour Code | These are the court outputs that could follow arrears (a later sprint). |
| Arabic labels | One corrected | The JSON had «غramة طرد»; the copy here has «غرامة طرد». |

## How it is used

- **MCP prompts (sprint 06, 06.14):**
  - prime codes and their Arabic and French names, for mapping the labels found in texts;
  - the idea of a "to check" flag on each value.
- **Prime catalogue (sprint 07, 07.11):** the starting list of codes: transport, presence, meal, seniority.
- **Not used:** any rate or amount, until a cited text confirms it.
