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

# (doc, product_id, product_name, branch, is_online, dana_prije, iznos, demo_namjena)
HEROES = [
    ("R-2026-001", 7, "USB-C kabl 1m",       "Tuzla",           False, 20, 19.90,
     "IN-STORE → priloži hero/R-2026-001.png + connector foto → očekivano ODOBRENO"),
    ("R-2026-002", 1, "Pametni telefon X20", "Sarajevo Centar", False, 40, 899.00,
     "IN-STORE → priloži hero/R-2026-002.png + screencrack foto → očekivano ESKALACIJA"),
    ("R-2026-003", 4, "Kamera GX10",         "Online",          True,  15, 459.00,
     "ONLINE → ukucaj broj R-2026-003 + bilo koja foto"),
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
    for doc, pid_product, name, branch, is_online, days_ago, amount, note in HEROES:
        pdate = NOW - timedelta(days=days_ago)
        ptype = "Online" if is_online else "InStore"
        cur.execute(
            "INSERT INTO Purchases (ProductId, PurchaseType, DocumentNumber, Branch, PurchaseDate, Amount) "
            "OUTPUT INSERTED.Id VALUES (?, ?, ?, ?, ?, ?)",
            pid_product, ptype, doc, branch, pdate, amount)
        pid = cur.fetchone()[0]

        img_note = ""
        if not is_online:
            img = degrade(render_receipt(doc, branch, pdate, float(amount), name))
            path = os.path.join(OUT_DIR, f"{doc}.png")
            img.save(path)
            img_note = f"  slika: {path}"

        print(f"  {doc}  ({ptype}, {name}, {amount} KM)  PurchaseId={pid}")
        print(f"     {note}{img_note}\n")

    cn.commit()
    cn.close()
    print(f"Slike računa: {OUT_DIR}")


if __name__ == "__main__":
    main()
