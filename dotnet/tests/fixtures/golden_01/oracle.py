"""Golden case 01 (SIMULATED): the expected ledger, computed independently of the C# engine.

The rules below are SYNTHETIC (see design_docs/05_Regulatory_Events_Pay_Ledger/golden_case_01.md). They exist only to
check the engine end to end against a second, deliberately simple implementation. They are not legal data and must
never be copied into data/seed.

Run: python -I oracle.py  (writes expected.csv next to this file)
"""
import csv
from datetime import date
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

MILLIME = Decimal("0.001")


def r(x: Decimal) -> Decimal:
    return x.quantize(MILLIME, rounding=ROUND_HALF_UP)


# ---- synthetic rules -------------------------------------------------------------------------------------------
# Grid line SIM-ELEC-RETAIL, monthly, enacted 1998-01-01. Category C (cadres), degrees 1..6.
GRID_C = {1: Decimal("480"), 2: Decimal("495"), 3: Decimal("510"), 4: Decimal("525"), 5: Decimal("540"), 6: Decimal("555")}
MONTHS_IN_DEGREE = 24  # degrees 1..5; degree 6 is the ceiling
RAISES = [(date(1999, 5, 1), Decimal("4.5")), (date(2002, 5, 1), Decimal("5")), (date(2005, 5, 1), Decimal("5")),
          (date(2008, 5, 1), Decimal("4.5"))]  # % for all, compounding on the amount in force, half up
ALLOWANCES = {"TRANSPORT": Decimal("8.000"), "PRESENCE": Decimal("2.080"), "ASSIDUITY": Decimal("12.000")}  # fixed monthly

# ---- career (simulation choices) -------------------------------------------------------------------------------
HIRED, SEPARATED = date(1998, 9, 1), date(2008, 8, 31)
START_DEGREE = 1

# ---- paid (simulation): net amounts, and the simulated deductions to get back to gross -----------------------
NET_PAID = [(date(1998, 9, 1), Decimal("400")), (date(2001, 1, 1), Decimal("450")), (date(2003, 7, 1), Decimal("500")),
            (date(2005, 7, 1), Decimal("550")), (date(2007, 7, 1), Decimal("600"))]
CNSS, IRPP = Decimal("0.07"), Decimal("0.21")  # simplified: flat rates, net = gross x (1 - CNSS) x (1 - IRPP)


def months():
    y, m = HIRED.year, HIRED.month
    while (y, m) <= (SEPARATED.year, SEPARATED.month):
        yield y, m
        y, m = (y + 1, 1) if m == 12 else (y, m + 1)


def add_months(d: date, n: int) -> date:
    total = d.month - 1 + n
    return date(d.year + total // 12, total % 12 + 1, d.day)


def grid_on(day: date) -> dict:
    grid = dict(GRID_C)
    for effective, pct in RAISES:
        if effective <= day:
            grid = {k: r(v * (1 + pct / 100)) for k, v in grid.items()}
    return grid


def degree_on(day: date) -> int:
    degree, start = START_DEGREE, HIRED
    while degree < max(GRID_C) and add_months(start, MONTHS_IN_DEGREE) <= day:
        start = add_months(start, MONTHS_IN_DEGREE)
        degree += 1
    return degree


def net_on(day: date) -> Decimal:
    return [amount for since, amount in NET_PAID if since <= day][-1]


def main():
    out = Path(__file__).with_name("expected.csv")
    with out.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["month", "category", "degree", "BASE", *ALLOWANCES, "owed", "net_paid", "gross_paid", "difference"])
        totals = [Decimal(0)] * 3
        for y, m in months():
            day = max(date(y, m, 1), HIRED)  # every month is full in this case (hired on the 1st, left on the last day)
            degree = degree_on(day)
            base = grid_on(day)[degree]
            owed = base + sum(ALLOWANCES.values())
            net = net_on(day)
            gross = r(net / ((1 - CNSS) * (1 - IRPP)))
            diff = owed - gross
            totals = [totals[0] + owed, totals[1] + gross, totals[2] + diff]
            w.writerow([f"{y:04d}-{m:02d}", "C", degree, base, *ALLOWANCES.values(), owed, net, gross, diff])
    print(f"owed={totals[0]} gross_paid={totals[1]} difference={totals[2]}")


if __name__ == "__main__":
    main()
