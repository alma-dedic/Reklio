import base64
import json

from openai import OpenAI

from ..config import OPENAI_API_KEY, OPENAI_MODEL
from ..schemas.damage import DAMAGE_TYPES, PRODUCT_TYPES, SEVERITIES, DamageResponse

# Structured output sa enum ograničenjima — model NE može vratiti vrijednost
# van fiksne liste (bitno za decision gate koji grana po tim vrijednostima).
_SCHEMA = {
    "type": "object",
    "properties": {
        "damage_confirmed": {"type": "boolean"},
        "damage_type": {"type": "string", "enum": DAMAGE_TYPES},
        "severity": {"type": "string", "enum": SEVERITIES},
        "product_type": {"type": "string", "enum": PRODUCT_TYPES},
        "confidence": {"type": "number"},
        "description": {"type": "string"},
    },
    "required": [
        "damage_confirmed", "damage_type", "severity", "product_type",
        "confidence", "description",
    ],
    "additionalProperties": False,
}

_PROMPT = (
    "Ovo je fotografija proizvoda priložena uz reklamaciju. Procijeni vidljivo oštećenje. "
    "damage_confirmed=true ako postoji jasno oštećenje, inače false. "
    "damage_type odaberi iz dozvoljene liste; ako nema oštećenja koristi 'None'. "
    "severity kalibriši ovako: "
    "Mild = kozmetičko, ne utiče na funkciju (sitna ogrebotina, jedna tanka pukotina u uglu); "
    "Moderate = jasno vidljivo, proizvod djelimično upotrebljiv; "
    "Severe = teško, proizvod neupotrebljiv ili velika deformacija/lom; "
    "None = nema oštećenja. "
    "product_type = koji je proizvod na slici, odaberi iz dozvoljene liste; "
    "'Nepoznato' ako se ne vidi jasno ili nisi siguran. "
    "confidence je tvoja pouzdanost 0.0-1.0. description je kratak opis na bosanskom."
)


class VisionService:
    def __init__(self):
        self._client = OpenAI(api_key=OPENAI_API_KEY)

    def analyze(self, image_bytes: bytes) -> DamageResponse:
        b64 = base64.b64encode(image_bytes).decode("ascii")
        # gpt-5.6-luna podržava samo temperature=1; structured output drži izlaz stabilnim.
        response = self._client.chat.completions.create(
            model=OPENAI_MODEL,
            response_format={
                "type": "json_schema",
                "json_schema": {"name": "damage", "strict": True, "schema": _SCHEMA},
            },
            messages=[
                {"role": "system", "content": "Ti si procjenitelj oštećenja proizvoda."},
                {"role": "user", "content": [
                    {"type": "text", "text": _PROMPT},
                    {"type": "image_url",
                     "image_url": {"url": f"data:image/png;base64,{b64}"}},
                ]},
            ],
        )
        return DamageResponse(**json.loads(response.choices[0].message.content))


vision_service = VisionService()
