from pydantic import BaseModel


# Oblik koji /analyze/receipt vraća (mapira se 1:1 na C# OcrResult).
class OcrResponse(BaseModel):
    document_number: str | None
    branch: str | None
    purchase_date: str | None   # ISO YYYY-MM-DD
    amount: float | None
    product_name: str | None
    confidence: float
    raw_text: str | None
