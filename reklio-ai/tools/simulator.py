"""

Generiše simulirane naloge, kupovine i istoriju reklamacija za trening fraud modela.

Princip:
  - svaki nalog dobije SKRIVENI TIP (honest / abuser, p abuser ~= 0.08),
  - iz tipa se generiše ŠUMOVITO, PREKLAPAJUĆE ponašanje (i pošteni ponekad
    izgledaju sumnjivo i obrnuto — nema pravila koje čisto razdvaja grupe),
  - LABELA = skriveni tip naloga, nikad funkcija feature-a.

Etiketa se piše u CSV (tools/data/sim_accounts.csv), fizički odvojena od baze.
Idempotentno: pri svakom pokretanju prvo briše prethodne simulirane redove.

Detalji odluka: docs/product-catalog.md (katalog) i plan za EPIC 5.
"""

import csv
import os
import random
import uuid
from datetime import datetime, timedelta

import numpy as np
import pyodbc

# --- konfiguracija ---
SERVER = "localhost"
DATABASE = "ReklioDb"
N_ACCOUNTS = 500
ABUSER_PROB = 0.08
SEED = 42
# Broj reklamacija po nalogu — Poisson sa različitim prosjekom, ali dovoljnim
# preklapanjem (volume feature-i ostaju korisni ~0.55-0.65, ne dominantni ~0.80).
HONEST_CLAIMS_LAMBDA = 1.8
ABUSER_CLAIMS_LAMBDA = 3.2
SIM_DOMAIN = "sim.reklio"          # marker simuliranih naloga
DOC_PREFIX = "SIM-"               # marker simuliranih kupovina
CSV_PATH = os.path.join(os.path.dirname(__file__), "data", "sim_accounts.csv")
BRANCHES = [
    "Sarajevo Centar", "Sarajevo Alipasino", "Tuzla",
    "Mostar", "Banja Luka", "Zenica", "Online",
]
ISSUE_TYPES = [
    "Oštećenje pri dostavi", "Proizvod ne radi", "Pogrešan proizvod", "Ostalo",
]

random.seed(SEED)
np.random.seed(SEED)
NOW = datetime.now().replace(microsecond=0)


def connect():
    last_err = None
    for driver in ("ODBC Driver 18 for SQL Server", "ODBC Driver 17 for SQL Server"):
        try:
            return pyodbc.connect(
                f"DRIVER={{{driver}}};SERVER={SERVER};DATABASE={DATABASE};"
                f"Trusted_Connection=yes;TrustServerCertificate=yes;",
                autocommit=False,
            )
        except pyodbc.Error as err:
            last_err = err
    raise RuntimeError(f"Nije pronađen ODBC Driver 17/18 za SQL Server. {last_err}")


def load_products(cur):
    cur.execute("SELECT Id, Category, Price, WarrantyMonths FROM Products")
    products = [
        {"id": r[0], "category": r[1], "price": float(r[2]), "warranty_months": r[3]}
        for r in cur.fetchall()
    ]
    if not products:
        raise RuntimeError("Katalog je prazan — pokreni backend da seeda Products tabelu.")
    return products


def clean_previous(cur):
    """Briše sve iz prethodnog pokretanja (sprječava duplikate)."""
    cur.execute(
        "DELETE FROM Claims WHERE UserId IN "
        "(SELECT Id FROM AspNetUsers WHERE Email LIKE ?)",
        (f"%@{SIM_DOMAIN}",),
    )
    cur.execute("DELETE FROM Purchases WHERE DocumentNumber LIKE ?", (DOC_PREFIX + "%",))
    cur.execute("DELETE FROM AspNetUsers WHERE Email LIKE ?", (f"%@{SIM_DOMAIN}",))


def insert_user(cur, n, registered_at):
    uid = str(uuid.uuid4())
    email = f"sim+{n}@{SIM_DOMAIN}"
    cur.execute(
        """INSERT INTO AspNetUsers
               (Id, FullName, Role, RegisteredAt, UserName, NormalizedUserName,
                Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed,
                TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
           VALUES (?, ?, 'Customer', ?, ?, ?, ?, ?, 1, 0, 0, 1, 0)""",
        (uid, f"Sim Korisnik {n}", registered_at, email, email.upper(), email, email.upper()),
    )
    return uid


def insert_purchase(cur, product, branch, purchase_date, amount, doc_counter):
    ptype = "Online" if branch == "Online" else ("Online" if random.random() < 0.25 else "InStore")
    cur.execute(
        """INSERT INTO Purchases (ProductId, PurchaseType, DocumentNumber, Branch, PurchaseDate, Amount)
           OUTPUT INSERTED.Id
           VALUES (?, ?, ?, ?, ?, ?)""",
        (product["id"], ptype, f"{DOC_PREFIX}{doc_counter}", branch, purchase_date, round(amount, 2)),
    )
    return cur.fetchone()[0]


def insert_claim(cur, user_id, purchase_id, submitted_at, status):
    cur.execute(
        """INSERT INTO Claims (UserId, PurchaseId, Status, IssueType, IssueDescription, SubmittedAt)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (user_id, purchase_id, status, random.choice(ISSUE_TYPES),
         "Simulirana reklamacija.", submitted_at),
    )


def pick_status(is_abuser):
    """Ishod ranije reklamacije (odlučen u trenutku slanja). Rizik odbijanja veći
    kod abusera, ali šumovit — status je feature (prior_rejection_rate), ne labela."""
    reject_p = 0.35 if is_abuser else 0.05
    if random.random() < reject_p:
        return random.choice(["AutoRejected", "OperatorRejected"])
    return random.choice(["AutoApproved", "OperatorApproved"])


def claim_offset_days(is_abuser, warranty_days):
    """Razmak kupovina→reklamacija. Abuseri nešto skloniji ranom/kasnom, ali sa
    jakim preklapanjem: i pošteni ponekad reklamiraju rano (legitiman kvar),
    i abuseri često u sredini garancije. Cilj: timing feature-i ~0.60-0.65 AUC."""
    r = random.random()
    if is_abuser:
        if r < 0.40:
            return random.randint(0, 18)                                # rano
        if r < 0.62:
            return int(warranty_days * random.uniform(0.82, 1.15))      # pred istek
        return int(warranty_days * random.uniform(0.05, 0.85))          # sredina
    if r < 0.08:
        return random.randint(0, 18)                                    # rijetko i pošteni rano
    return int(warranty_days * random.uniform(0.03, 0.95))


def write_csv(accounts):
    os.makedirs(os.path.dirname(CSV_PATH), exist_ok=True)
    with open(CSV_PATH, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["user_id", "hidden_type", "is_abuser"])
        for uid, is_abuser in accounts:
            writer.writerow([uid, "abuser" if is_abuser else "honest", int(is_abuser)])


def main():
    cnxn = connect()
    cur = cnxn.cursor()
    try:
        products = load_products(cur)
        clean_previous(cur)

        doc_counter = 1
        shared_pool = []   # kupovine dostupne za dijeljenje među nalozima (bearer abuse)
        accounts = []
        n_purchases = 0
        n_claims = 0

        for n in range(1, N_ACCOUNTS + 1):
            is_abuser = random.random() < ABUSER_PROB
            # Abuseri skloni novijim nalozima (svježe napravljeni), ali sa preklapanjem
            # — nezavisan umjeren signal (account_age_days), realističan za fraud ringove.
            if is_abuser:
                registered_at = NOW - timedelta(days=random.randint(30, 650))
            else:
                registered_at = NOW - timedelta(days=random.randint(150, 1050))
            uid = insert_user(cur, n, registered_at)
            accounts.append((uid, is_abuser))

            lam = ABUSER_CLAIMS_LAMBDA if is_abuser else HONEST_CLAIMS_LAMBDA
            claims_for_account = max(1, int(np.random.poisson(lam)))

            for _ in range(claims_for_account):
                # Dijeljenje iste kupovine među nalozima (bearer abuse) — glavni
                # strukturni signal; abuseri ga rade znatno češće od poštenih.
                reuse_p = 0.62 if is_abuser else 0.02
                if shared_pool and random.random() < reuse_p:
                    pid, product, branch, purchase_date = random.choice(shared_pool)
                else:
                    product = random.choice(products)
                    branch = random.choice(BRANCHES)
                    max_back = max((NOW - registered_at).days - 1, 1)
                    purchase_date = registered_at + timedelta(days=random.randint(0, max_back))
                    amount = product["price"] * random.uniform(0.9, 1.12)
                    pid = insert_purchase(cur, product, branch, purchase_date, amount, doc_counter)
                    doc_counter += 1
                    n_purchases += 1
                    # neke kupovine postaju "dijeljive" (abuseri mnogo češće)
                    if random.random() < (0.65 if is_abuser else 0.05):
                        shared_pool.append((pid, product, branch, purchase_date))

                warranty_days = product["warranty_months"] * 30
                offset = claim_offset_days(is_abuser, warranty_days)
                submitted = purchase_date + timedelta(days=max(offset, 0))
                submitted = min(submitted, NOW)
                if submitted <= registered_at:
                    submitted = registered_at + timedelta(days=1)
                insert_claim(cur, uid, pid, submitted, pick_status(is_abuser))
                n_claims += 1

        cnxn.commit()
        write_csv(accounts)

        abusers = sum(1 for _, a in accounts if a)
        print(f"Naloga:     {len(accounts)} (abusera {abusers}, {abusers / len(accounts):.1%})")
        print(f"Kupovina:   {n_purchases}")
        print(f"Reklamacija:{n_claims}")
        print(f"Dijeljivih kupovina u poolu: {len(shared_pool)}")
        print(f"Labele upisane: {CSV_PATH}")
    except Exception:
        cnxn.rollback()
        raise
    finally:
        cnxn.close()


if __name__ == "__main__":
    main()
