import base64
import json

from openai import OpenAI

from ..config import OPENAI_API_KEY, OPENAI_MODEL
from ..schemas.ocr import OcrResponse

_SCHEMA = {
    "type": "object",
    "properties": {
        "document_number": {"type": ["string", "null"]},
        "branch": {"type": ["string", "null"]},
        "purchase_date": {"type": ["string", "null"]},
        "amount": {"type": ["number", "null"]},
        "product_name": {"type": ["string", "null"]},
        "confidence": {"type": "number"},
        "raw_text": {"type": ["string", "null"]},
    },
    "required": [
        "document_number", "branch", "purchase_date", "amount",
        "product_name", "confidence", "raw_text",
    ],
    "additionalProperties": False,
}

_PROMPT = (
    "Ovo je fotografija računa iz prodavnice. Izvuci polja sa slike. "
    "Datum vrati kao ISO YYYY-MM-DD. Iznos vrati kao broj (npr. '138,67 KM' -> 138.67). "
    "Broj računa prepiši tačno kako piše. confidence je tvoja procjena pouzdanosti 0.0-1.0. "
    "raw_text je sav vidljivi tekst. Ako polje nije čitljivo, stavi null."
)


class OcrService:
    def __init__(self):
        self._client = OpenAI(api_key=OPENAI_API_KEY)

    def extract(self, image_bytes: bytes) -> OcrResponse:
        b64 = base64.b64encode(image_bytes).decode("ascii")
        response = self._client.chat.completions.create(
            model=OPENAI_MODEL,
            response_format={
                "type": "json_schema",
                "json_schema": {"name": "receipt", "strict": True, "schema": _SCHEMA},
            },
            messages=[
                {"role": "system", "content": "Ti si precizan OCR za račune."},
                {"role": "user", "content": [
                    {"type": "text", "text": _PROMPT},
                    {"type": "image_url",
                     "image_url": {"url": f"data:image/png;base64,{b64}"}},
                ]},
            ],
        )
        return OcrResponse(**json.loads(response.choices[0].message.content))


ocr_service = OcrService()
