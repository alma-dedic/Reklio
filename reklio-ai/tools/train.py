"""tools/train.py — T5.5 / T5.6 / T5.8

Trenira XGBoost fraud model na simuliranom skupu.

  - feature-i: fn_features(claim_id) za svaku simuliranu reklamaciju (jedini izvor istine),
  - labela: sim_accounts.csv (po UserId, fizički odvojena od baze),
  - fit NA PANDAS DataFrame (ne numpy) → model.feature_names_in_ postoji → T5.7 safeguard radi,
  - AUC gate (T5.6): >0.95 znači leakage, ne uspjeh → STOP bez čuvanja,
  - prag (T5.8): iz precision/recall krive, cilj visoka preciznost (malo lažnih optužbi).

Izlaz: models/model.pkl (model) + models/model_meta.json (prag + metrika).
"""

import csv
import json
import os
import pickle
import sys

import numpy as np
import pandas as pd
import pyodbc
import xgboost as xgb
from sklearn.metrics import precision_recall_curve, roc_auc_score
from sklearn.model_selection import cross_val_predict, train_test_split

HERE = os.path.dirname(__file__)
CSV_PATH = os.path.join(HERE, "data", "sim_accounts.csv")
MODEL_DIR = os.path.join(HERE, "..", "models")
MODEL_PATH = os.path.join(MODEL_DIR, "model.pkl")
META_PATH = os.path.join(MODEL_DIR, "model_meta.json")

AUC_LEAKAGE_LIMIT = 0.95
TARGET_PRECISION = 0.90     # ≤10% lažnih optužbi među označenima
RANDOM_STATE = 42

# Kanonski redoslijed feature-a — MORA odgovarati kolonama iz fn_features.
FEATURE_NAMES = [
    "prior_claims_on_purchase",
    "purchase_claimed_by_other_account",
    "distinct_accounts_on_purchase",
    "days_purchase_to_claim",
    "warranty_period_used_pct",
    "claimed_within_first_n_days",
    "total_claims",
    "claims_last_30d",
    "claims_last_90d",
    "mean_days_between_claims",
    "account_age_days",
    "prior_rejection_rate",
    "claim_amount",
    "amount_vs_user_mean",
    "distinct_categories",
    "distinct_stores",
]


def connect():
    for driver in ("ODBC Driver 18 for SQL Server", "ODBC Driver 17 for SQL Server"):
        try:
            return pyodbc.connect(
                f"DRIVER={{{driver}}};SERVER=localhost;DATABASE=ReklioDb;"
                "Trusted_Connection=yes;TrustServerCertificate=yes;")
        except pyodbc.Error:
            continue
    raise RuntimeError("Nije pronađen ODBC Driver 17/18 za SQL Server.")


def load_labels():
    labels = {}
    with open(CSV_PATH, encoding="utf-8") as f:
        for row in csv.DictReader(f):
            labels[row["user_id"]] = int(row["is_abuser"])
    return labels


def load_dataset():
    labels = load_labels()
    cn = connect()
    cur = cn.cursor()
    cur.execute("""
        SELECT c.UserId, f.*
        FROM Claims c
        JOIN AspNetUsers u ON u.Id = c.UserId AND u.Email LIKE '%@sim.reklio'
        CROSS APPLY dbo.fn_features(c.Id) f
    """)
    cols = [d[0] for d in cur.description]
    df = pd.DataFrame.from_records(cur.fetchall(), columns=cols)
    cn.close()

    y = df["UserId"].map(labels).astype(int)
    x = df[FEATURE_NAMES].apply(pd.to_numeric, errors="coerce").astype(float)
    return x, y


def make_model():
    return xgb.XGBClassifier(
        n_estimators=250, max_depth=4, learning_rate=0.08,
        subsample=0.9, colsample_bytree=0.9,
        eval_metric="auc", random_state=RANDOM_STATE,
    )


def choose_threshold(y_true, proba):
    precision, recall, thresholds = precision_recall_curve(y_true, proba)
    # precision/recall imaju jedan element više od thresholds
    best = None
    for p, r, t in zip(precision[:-1], recall[:-1], thresholds):
        if p >= TARGET_PRECISION and (best is None or r > best[1]):
            best = (p, r, t)
    if best is None:  # nijedan prag ne dostiže cilj → uzmi najprecizniji
        idx = int(np.argmax(precision[:-1]))
        best = (precision[idx], recall[idx], thresholds[idx])
    return {"threshold": float(best[2]), "precision": float(best[0]), "recall": float(best[1])}


def main():
    x, y = load_dataset()
    print(f"Uzoraka: {len(x)}  |  pozitivnih (fraud): {int(y.sum())} ({y.mean():.1%})")
    print(f"Feature-a: {x.shape[1]}\n")

    # AUC gate (T5.6) na pravom held-out test skupu.
    x_train, x_test, y_train, y_test = train_test_split(
        x, y, test_size=0.25, stratify=y, random_state=RANDOM_STATE)
    gate_model = make_model()
    gate_model.fit(x_train, y_train)
    auc = roc_auc_score(y_test, gate_model.predict_proba(x_test)[:, 1])
    print(f"Test AUC: {auc:.4f}  (očekivano 0.85–0.92)")

    if auc > AUC_LEAKAGE_LIMIT:
        print(f"\n*** STOP: AUC {auc:.4f} > {AUC_LEAKAGE_LIMIT} ***")
        print("To nije uspjeh nego znak leakage-a / cirkularnih labela.")
        print("Model NIJE sačuvan. Provjeri fn_features i simulator prije nastavka.")
        sys.exit(1)

    # Prag (T5.8) iz cross-validated predviđanja na CIJELOM skupu — stabilnija PR
    # kriva (sve pozitivne) nego iz malog test skupa.
    oof = cross_val_predict(make_model(), x, y, cv=5, method="predict_proba")[:, 1]
    thr = choose_threshold(y, oof)
    flagged = float((oof >= thr["threshold"]).mean())
    print(f"\nPrag (iz PR krive na CV predviđanjima, cilj preciznost ≥ {TARGET_PRECISION:.0%}):")
    print(f"  threshold = {thr['threshold']:.4f}")
    print(f"  preciznost = {thr['precision']:.3f}  (udio tačnih među označenim rizičnim)")
    print(f"  recall     = {thr['recall']:.3f}  (udio uhvaćenih stvarnih abusera)")
    print(f"  označeno kao rizično: {flagged:.1%} skupa")

    # Finalni model na SVIM podacima (za deployment), fit na DataFrame.
    model = make_model()
    model.fit(x, y)
    assert list(model.feature_names_in_) == FEATURE_NAMES, \
        "feature_names_in_ se ne poklapa sa kanonskom listom!"

    os.makedirs(MODEL_DIR, exist_ok=True)
    with open(MODEL_PATH, "wb") as f:
        pickle.dump(model, f)
    meta = {
        "threshold": thr["threshold"],
        "target_precision": TARGET_PRECISION,
        "precision_at_threshold": thr["precision"],
        "recall_at_threshold": thr["recall"],
        "test_auc": float(auc),
        "feature_names": FEATURE_NAMES,
        "n_samples": int(len(x)),
        "positive_rate": float(y.mean()),
    }
    with open(META_PATH, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)

    print(f"\nSačuvano: {os.path.normpath(MODEL_PATH)}")
    print(f"          {os.path.normpath(META_PATH)}")


if __name__ == "__main__":
    main()
