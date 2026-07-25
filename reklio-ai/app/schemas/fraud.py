from pydantic import BaseModel


# Svih 16 feature-a je IMENOVANO i OBAVEZNO — C# ne može poslati goli niz.
# Vrijednost može biti null (cold-start feature-i) → NaN u modelu.
class FraudScoreRequest(BaseModel):
    prior_claims_on_purchase: float | None
    purchase_claimed_by_other_account: float | None
    distinct_accounts_on_purchase: float | None
    days_purchase_to_claim: float | None
    warranty_period_used_pct: float | None
    claimed_within_first_n_days: float | None
    total_claims: float | None
    claims_last_30d: float | None
    claims_last_90d: float | None
    mean_days_between_claims: float | None
    account_age_days: float | None
    prior_rejection_rate: float | None
    claim_amount: float | None
    amount_vs_user_mean: float | None
    distinct_categories: float | None
    distinct_stores: float | None


class FraudScoreResponse(BaseModel):
    risk_score: float
    top_factors: list[str]
