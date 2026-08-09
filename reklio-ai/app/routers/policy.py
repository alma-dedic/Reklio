from fastapi import APIRouter, HTTPException

from ..schemas.policy import PolicyRequest, PolicyResponse
from ..services.rag_service import rag_service

router = APIRouter(prefix="/analyze", tags=["policy"])


@router.post("/policy", response_model=PolicyResponse)
def analyze_policy(request: PolicyRequest) -> PolicyResponse:
    try:
        return rag_service.answer(request.question, request.product_category, request.issue_type)
    except Exception as err:
        raise HTTPException(status_code=502, detail=f"RAG poziv nije uspio: {err}") from err
