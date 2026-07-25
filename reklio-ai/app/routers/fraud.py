from fastapi import APIRouter, HTTPException

from ..schemas.fraud import FraudScoreRequest, FraudScoreResponse
from ..services.model_service import model_service

router = APIRouter(prefix="/fraud", tags=["fraud"])


@router.post("/score", response_model=FraudScoreResponse)
def score(request: FraudScoreRequest) -> FraudScoreResponse:
    try:
        risk, top_factors = model_service.score(request.model_dump())
    except ValueError as err:
        raise HTTPException(status_code=400, detail=str(err)) from err
    return FraudScoreResponse(risk_score=risk, top_factors=top_factors)
