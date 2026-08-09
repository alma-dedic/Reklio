"""tools/e2e_seed.py — T9.5

Priprema prvi pravi end-to-end test decision gate-a. Ubacuje 3 SVJEŽA purchase-a
(bez istorije, da rizik ne bude vještački podignut), renderuje im račune, i kreira
3 Submitted reklamacije s dokazima. Worker ih pokupi crash-recovery-jem na startu
API-ja i provuče kroz pun pipeline.

Tri scenarija, tri grane gate-a:
  1. APPROVE  — napuhana baterija u garanciji, potvrđeno oštećenje, pokriveno
  2. ESCALATE — mehaničko oštećenje (RAG izuzetak → eskalacija, NE odbijanje)
  3. REJECT   — lažni račun (broj ne postoji u Purchases) → NotFound → odbij

Idempotentno: prvo obriše sve E2E- podatke od ranije.
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

# (marker doc, product_id, product_name, branch, dana_prije, iznos)
PURCHASES = {
    "approve":  ("E2E-APPROVE-1",  9, "Powerbank 10000mAh", "Sarajevo Centar", 15, 79.00),
    "escalate": ("E2E-ESCALATE-1", 2, "Laptop Pro 15",      "Tuzla",           30, 1890.00),
    "reject":   ("E2E-REJECT-1",   7, "USB-C kabl 1m",      "Mostar",          20, 19.90),
}

FAKE_DOC = "E2E-FAKE-NEPOSTOJI"  # namjerno NIJE u Purchases → validacija NotFound


def cleanup(cur):
    cur.execute("""
        DELETE FROM Notifications WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'E2E-%'));
        DELETE FROM ClaimEvidence WHERE ClaimId IN (
            SELECT Id FROM Claims WHERE PurchaseId IN (
                SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'E2E-%'));
        DELETE FROM Claims WHERE PurchaseId IN (
            SELECT Id FROM Purchases WHERE DocumentNumber LIKE 'E2E-%');
        DELETE FROM Purchases WHERE DocumentNumber LIKE 'E2E-%';
    """)


def user_id(cur, email):
    cur.execute("SELECT Id FROM AspNetUsers WHERE Email = ?", email)
    return cur.fetchone()[0]


def insert_purchase(cur, doc, product_id, branch, pdate, amount):
    cur.execute(
        "INSERT INTO Purchases (ProductId, PurchaseType, DocumentNumber, Branch, PurchaseDate, Amount) "
        "OUTPUT INSERTED.Id VALUES (?, 'InStore', ?, ?, ?, ?)",
        product_id, doc, branch, pdate, amount)
    return cur.fetchone()[0]


def insert_claim(cur, user, purchase_id, issue_type, issue_desc):
    cur.execute(
        "INSERT INTO Claims (UserId, PurchaseId, Status, IssueType, IssueDescription, SubmittedAt) "
        "OUTPUT INSERTED.Id VALUES (?, ?, 'Submitted', ?, ?, ?)",
        user, purchase_id, issue_type, issue_desc, NOW)
    return cur.fetchone()[0]


def insert_evidence(cur, claim_id, ev_type, path):
    cur.execute(
        "INSERT INTO ClaimEvidence (ClaimId, Type, FilePath) VALUES (?, ?, ?)",
        claim_id, ev_type, path)


def save_receipt(doc, branch, pdate, amount, product_name):
    os.makedirs(OUT_DIR, exist_ok=True)
    img = degrade(render_receipt(doc, branch, pdate, float(amount), product_name))
    path = os.path.join(OUT_DIR, f"{doc}.png")
    img.save(path)
    return os.path.abspath(path)


def main():
    cn = connect()
    cur = cn.cursor()
    cleanup(cur)

    k1 = user_id(cur, "kupac1@reklio.ba")
    k2 = user_id(cur, "kupac2@reklio.ba")
    k3 = user_id(cur, "kupac3@reklio.ba")

    # ---- Purchase-evi + računi ----
    pids, receipts = {}, {}
    for key, (doc, pid_product, name, branch, days_ago, amount) in PURCHASES.items():
        pdate = NOW - timedelta(days=days_ago)
        pid = insert_purchase(cur, doc, pid_product, branch, pdate, amount)
        pids[key] = pid
        receipts[key] = save_receipt(doc, branch, pdate, amount, name)

    # Lažni račun (za reject) — broj koji NE postoji u Purchases.
    fake_receipt = save_receipt(FAKE_DOC, "Mostar", NOW - timedelta(days=20), 19.90, "USB-C kabl 1m")

    dmg = lambda f: os.path.abspath(os.path.join(DAMAGE_DIR, f))

    # ---- Scenario 1: APPROVE (napuhana baterija u garanciji) ----
    c1 = insert_claim(cur, k1, pids["approve"], "Kvar baterije",
                      "Powerbank se sam napuhao tokom normalne upotrebe u garantnom roku; "
                      "nije padao, nije bio na vlazi niti fizicki ostecen.")
    insert_evidence(cur, c1, "Receipt", receipts["approve"])
    insert_evidence(cur, c1, "Photo", dmg("damage_11_Severe.png"))

    # ---- Scenario 2: ESCALATE (mehanicko ostecenje → RAG izuzetak) ----
    c2 = insert_claim(cur, k2, pids["escalate"], "Fizicko ostecenje",
                      "Na kucistu laptopa je udubljenje nastalo prilikom pada uredaja.")
    insert_evidence(cur, c2, "Receipt", receipts["escalate"])
    insert_evidence(cur, c2, "Photo", dmg("damage_03_Moderate.png"))

    # ---- Scenario 3: REJECT (lazni racun, broj ne postoji) ----
    c3 = insert_claim(cur, k3, pids["reject"], "Kvar",
                      "Kabl je prestao raditi nakon kratkog perioda koristenja.")
    insert_evidence(cur, c3, "Receipt", fake_receipt)

    cn.commit()
    cn.close()

    print("E2E podaci ubaceni:")
    print(f"  Scenario 1 (APPROVE)  ClaimId={c1}  PurchaseId={pids['approve']}")
    print(f"  Scenario 2 (ESCALATE) ClaimId={c2}  PurchaseId={pids['escalate']}")
    print(f"  Scenario 3 (REJECT)   ClaimId={c3}  PurchaseId={pids['reject']}")
    print(f"  Racuni: {OUT_DIR}")
    print(f"  ClaimId-evi za pracenje: {c1},{c2},{c3}")


if __name__ == "__main__":
    main()
