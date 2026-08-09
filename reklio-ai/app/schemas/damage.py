from pydantic import BaseModel

DAMAGE_TYPES = [
    "ScreenCrack", "Dent", "Scratch", "ConnectorDamage",
    "PhysicalBreak", "WaterDamage", "Swelling", "None", "Other",
]
SEVERITIES = ["None", "Mild", "Moderate", "Severe"]


class DamageResponse(BaseModel):
    damage_confirmed: bool
    damage_type: str
    severity: str
    confidence: float
    description: str
