from fastapi import APIRouter, File, HTTPException, UploadFile

from ..schemas.ocr import OcrResponse
from ..services.ocr_service import ocr_service

router = APIRouter(prefix="/analyze", tags=["ocr"])


@router.post("/receipt", response_model=OcrResponse)
async def analyze_receipt(file: UploadFile = File(...)) -> OcrResponse:
    image_bytes = await file.read()
    try:
        return ocr_service.extract(image_bytes)
    except Exception as err:
        raise HTTPException(status_code=502, detail=f"OCR poziv nije uspio: {err}") from err
