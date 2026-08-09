"""tools/e2e_approve_retry.py — T9.5 (čist AutoApproved scenario)

Napuhavanje baterije korpus tretira konzervativno (doc 03 zamka), pa eskalira.
Ovdje pravimo slučaj koji korpus JASNO pokriva: kabl koji je prestao raditi u
garantnom roku zbog fabričkog kvara (ne habanje) — RAG to vraća covered=true.
Uz potvrđeno vizuelno oštećenje konektora → sva 4 uslova → AutoApproved.
"""

import os
import sys
from datetime import datetime, timedelta

HERE = os.path.dirname(__file__)
sys.path.insert(0, HERE)

from generate_receipts import connect, degrade, render_receipt  # noqa: E402

OUT_DIR = os.path.join(HERE, "data", "e2e")
DAMAGE_DIR = os.path.join(HERE, "data", "damage")
NOW = datetime.utcnow()

DOC = "E2E-APPROVE-2"


def main():
    cn = connect()
    cur = cn.cursor()

    # čisto: obriši eventualni raniji E2E-APPROVE-2
    cur.execute("""
        DELETE FROM Notifications WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber = ?));
        DELETE FROM ClaimEvidence WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber = ?));
        DELETE FROM Claims WHERE PurchaseId IN (
            SELECT Id FROM Purchases WHERE DocumentNumber = ?);
        DELETE FROM Purchases WHERE DocumentNumber = ?;
    """, DOC, DOC, DOC, DOC)

    cur.execute("SELECT Id FROM AspNetUsers WHERE Email = ?", "kupac1@reklio.ba")
    k1 = cur.fetchone()[0]

    pdate = NOW - timedelta(days=25)
    amount = 19.90
    cur.execute(
        "INSERT INTO Purchases (ProductId, PurchaseType, DocumentNumber, Branch, PurchaseDate, Amount) "
        "OUTPUT INSERTED.Id VALUES (7, 'InStore', ?, 'Tuzla', ?, ?)",
        DOC, pdate, amount)
    pid = cur.fetchone()[0]

    os.makedirs(OUT_DIR, exist_ok=True)
    img = degrade(render_receipt(DOC, "Tuzla", pdate, float(amount), "USB-C kabl 1m"))
    receipt_path = os.path.abspath(os.path.join(OUT_DIR, f"{DOC}.png"))
    img.save(receipt_path)

    cur.execute(
        "INSERT INTO Claims (UserId, PurchaseId, Status, IssueType, IssueDescription, SubmittedAt) "
        "OUTPUT INSERTED.Id VALUES (?, ?, 'Submitted', ?, ?, ?)",
        k1, pid, "Kvar",
        "USB-C kabl je prestao raditi u garantnom roku zbog fabrickog kvara konektora - "
        "nije rijec o normalnom habanju, trosenju niti mehanickom ostecenju od pada.",
        NOW)
    cid = cur.fetchone()[0]

    photo = os.path.abspath(os.path.join(DAMAGE_DIR, "damage_09_Severe.png"))
    cur.execute("INSERT INTO ClaimEvidence (ClaimId, Type, FilePath) VALUES (?, 'Receipt', ?)", cid, receipt_path)
    cur.execute("INSERT INTO ClaimEvidence (ClaimId, Type, FilePath) VALUES (?, 'Photo', ?)", cid, photo)

    cn.commit()
    cn.close()
    print(f"APPROVE-retry ubacen: ClaimId={cid}  PurchaseId={pid}")


if __name__ == "__main__":
    main()
