from pydantic import BaseModel


class ExplainRequest(BaseModel):
    decision: str
    reason_code: str
    factors: list[str]
    validation_status: str | None = None
    warranty_expired: bool = False
    risk_score: float
    risk_threshold: float
    damage_confirmed: bool | None = None
    damage_type: str | None = None
    severity: str | None = None
    policy_covered: bool | None = None
    applicable_exclusion: str | None = None
    cited_article: str | None = None
    policy_answer: str | None = None
    issue_type: str
    issue_description: str
    product_name: str | None = None
    product_category: str | None = None


class ExplainResponse(BaseModel):
    recommendation: str  # "approve" | "reject" — savjetodavni lean za operatera
    operator_text: str  # obrazloženje + caveat za operatera
    customer_reason: str  # AI draft razloga za kupca (za preporučeni smjer)
