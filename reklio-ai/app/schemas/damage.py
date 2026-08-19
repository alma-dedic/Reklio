from pydantic import BaseModel

DAMAGE_TYPES = [
    "ScreenCrack", "Dent", "Scratch", "ConnectorDamage",
    "PhysicalBreak", "WaterDamage", "Swelling", "None", "Other",
]
SEVERITIES = ["None", "Mild", "Moderate", "Severe"]

# Tipovi proizvoda iz kataloga (1:1 sa proizvodima) — za provjeru da slika
# odgovara izabranom proizvodu. "Nepoznato" kad se ne može pouzdano odrediti.
PRODUCT_TYPES = [
    "Telefon", "Laptop", "Tablet", "Kamera", "Slušalice",
    "Zvučnik", "Kabl", "Punjač", "Powerbank", "Baterija", "Nepoznato",
]


class DamageResponse(BaseModel):
    damage_confirmed: bool
    damage_type: str
    severity: str
    product_type: str
    confidence: float
    description: str
