from fastapi import APIRouter, HTTPException

from ..schemas.explain import ExplainRequest, ExplainResponse
from ..services.explain_service import explanation_service

router = APIRouter(prefix="/explain", tags=["explain"])


@router.post("/decision", response_model=ExplainResponse)
def explain_decision(request: ExplainRequest) -> ExplainResponse:
    try:
        return explanation_service.explain(request)
    except Exception as err:
        raise HTTPException(status_code=502, detail=f"Explanation poziv nije uspio: {err}") from err
