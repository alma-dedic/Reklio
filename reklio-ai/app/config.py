from pathlib import Path

# Kanonska imena i redoslijed feature-a. MORA se poklapati sa fn_features
# kolonama i sa model.feature_names_in_ (provjerava se pri startu — T5.7).
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

_ROOT = Path(__file__).resolve().parent.parent
MODEL_PATH = _ROOT / "models" / "model.pkl"
META_PATH = _ROOT / "models" / "model_meta.json"
