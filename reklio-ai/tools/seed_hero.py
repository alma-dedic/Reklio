"""tools/seed_hero.py — T13.5

Hero kupovine za UI demo: pamtljivi brojevi računa + račun-slike za in-store.
Kupac u demou: in-store → priloži hero račun-sliku; online → ukuca broj.

Izlaz: hero kupovine u bazi + tools/data/hero/*.png (slike za upload).
Idempotentno: prvo obriše ranije R-2026- podatke.
"""

import os
import sys
from datetime import datetime, timedelta

HERE = os.path.dirname(__file__)
sys.path.insert(0, HERE)

from generate_receipts import connect, degrade, render_receipt  # noqa: E402

OUT_DIR = os.path.join(HERE, "data", "hero")
NOW = datetime.utcnow()

# (doc, branch, is_online, dana_prije, [(product_id, name, amount), ...], demo_namjena)
# Više stavki na istom računu = više Purchase redova sa istim DocumentNumber.
HEROES = [
    ("R-2026-001", "Tuzla",           False, 20,
     [(7, "USB-C kabl 1m", 19.90)],
     "IN-STORE → priloži hero/R-2026-001.png + connector foto → očekivano ODOBRENO"),
    ("R-2026-002", "Sarajevo Centar", False, 40,
     [(1, "Pametni telefon X20", 899.00)],
     "IN-STORE → priloži hero/R-2026-002.png + screencrack foto → očekivano ESKALACIJA"),
    ("R-2026-003", "Online",          True,  15,
     [(4, "Kamera GX10", 459.00)],
     "ONLINE → ukucaj broj R-2026-003 + bilo koja foto"),
    ("R-2026-004", "Sarajevo Centar", False, 25,
     [(2, "Laptop Pro 15", 1890.00), (7, "USB-C kabl 1m", 19.90), (5, "Bežične slušalice Q3", 149.00)],
     "IN-STORE MULTI → priloži hero/R-2026-004.png → dropdown 3 proizvoda, izaberi Laptop"),
]


def main():
    cn = connect()
    cur = cn.cursor()

    cur.execute("""
        DELETE FROM Notifications WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'R-2026-%'));
        DELETE FROM ClaimEvidence WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'R-2026-%'));
        DELETE FROM Claims WHERE PurchaseId IN (
            SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'R-2026-%');
        DELETE FROM Purchases WHERE DocumentNumber LIKE 'R-2026-%';
    """)

    os.makedirs(OUT_DIR, exist_ok=True)
    for old in os.listdir(OUT_DIR):
        os.remove(os.path.join(OUT_DIR, old))

    print("Hero kupovine ubačene:\n")
    for doc, branch, is_online, days_ago, lines, note in HEROES:
        pdate = NOW - timedelta(days=days_ago)
        ptype = "Online" if is_online else "InStore"

        pids = []
        for product_id, name, amount in lines:
            cur.execute(
                "INSERT INTO Purchases (ProductId, PurchaseType, DocumentNumber, Branch, PurchaseDate, Amount) "
                "OUTPUT INSERTED.Id VALUES (?, ?, ?, ?, ?, ?)",
                product_id, ptype, doc, branch, pdate, amount)
            pids.append(cur.fetchone()[0])

        img_note = ""
        if not is_online:
            items = [(name, float(amount)) for _, name, amount in lines]
            img = degrade(render_receipt(doc, branch, pdate, items))
            path = os.path.join(OUT_DIR, f"{doc}.png")
            img.save(path)
            img_note = f"  slika: {path}"

        total = sum(float(a) for _, _, a in lines)
        print(f"  {doc}  ({ptype}, {len(lines)} stavki, {total:.2f} KM)  PurchaseIds={pids}")
        print(f"     {note}{img_note}\n")

    cn.commit()
    cn.close()
    print(f"Slike računa: {OUT_DIR}")


if __name__ == "__main__":
    main()
