from fastapi import APIRouter, File, HTTPException, UploadFile

from ..schemas.damage import DamageResponse
from ..services.vision_service import vision_service

router = APIRouter(prefix="/analyze", tags=["vision"])


@router.post("/damage", response_model=DamageResponse)
async def analyze_damage(file: UploadFile = File(...)) -> DamageResponse:
    image_bytes = await file.read()
    try:
        return vision_service.analyze(image_bytes)
    except Exception as err:
        raise HTTPException(status_code=502, detail=f"Vision poziv nije uspio: {err}") from err
