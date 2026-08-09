from pydantic import BaseModel


class PolicyRequest(BaseModel):
    question: str
    product_category: str | None = None
    issue_type: str | None = None


class PolicyResponse(BaseModel):
    covered: bool
    applicable_exclusion: str | None
    cited_article: str | None
    answer: str
