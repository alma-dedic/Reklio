from fastapi import APIRouter, HTTPException

from ..schemas.chat import ChatRequest, ChatResponse
from ..services.rag_service import rag_service

router = APIRouter(prefix="/chat", tags=["chat"])


@router.post("", response_model=ChatResponse)
def chat(request: ChatRequest) -> ChatResponse:
    try:
        return rag_service.chat(request.message, request.history)
    except Exception as err:
        raise HTTPException(status_code=502, detail=f"Chat poziv nije uspio: {err}") from err
