"""tools/generate_damage.py — T8.1

Bazen AI-generisanih fotografija oštećenja proizvoda iz kataloga TehnoDom,
sa različitim stepenima ozbiljnosti. Slike se generišu OpenAI image modelom.

Izlaz: tools/data/damage/*.png + tools/data/damage_manifest.csv (ground truth:
proizvod, damage_type, očekivani severity).
"""

import base64
import csv
import os
import sys

from openai import OpenAI

HERE = os.path.dirname(os.path.abspath(__file__))
REKLIO_AI = os.path.dirname(HERE)
sys.path.insert(0, REKLIO_AI)

from app.config import OPENAI_API_KEY, OPENAI_IMAGE_MODEL  # noqa: E402

OUT_DIR = os.path.join(HERE, "data", "damage")
MANIFEST = os.path.join(HERE, "data", "damage_manifest.csv")

# (id, proizvod, damage_type, severity, engleski opis oštećenja za prompt)
SCENARIOS = [
    ("01", "Pametni telefon X20", "ScreenCrack", "Severe",
     "a badly shattered front screen with spiderweb cracks covering most of the display and glass fragments"),
    ("02", "Pametni telefon X20", "ScreenCrack", "Mild",
     "a single thin hairline crack in one corner of the otherwise intact screen"),
    ("03", "Laptop Pro 15", "Dent", "Moderate",
     "a visible dent and slight deformation on the corner of the aluminium laptop lid"),
    ("04", "Laptop Pro 15", "Scratch", "Mild",
     "a few light shallow surface scratches on the laptop lid"),
    ("05", "Tableta 10.1 inch", "ScreenCrack", "Moderate",
     "a crack running across part of the tablet screen"),
    ("06", "Kamera GX10", "Dent", "Severe",
     "a heavily dented and deformed camera body, crushed on one side"),
    ("07", "Bezicne slusalice Q3", "PhysicalBreak", "Severe",
     "wireless earbuds with a cracked and broken plastic casing, one earbud split open"),
    ("08", "Zvucnik prijenosni", "PhysicalBreak", "Moderate",
     "a portable bluetooth speaker with a torn speaker mesh grille and a cracked corner"),
    ("09", "USB-C kabl", "ConnectorDamage", "Severe",
     "a USB-C cable with a bent and broken connector and frayed insulation near the plug with exposed wires"),
    ("10", "Punjac brzi 30W", "ConnectorDamage", "Mild",
     "a small white wall charger with one slightly bent metal prong"),
    ("11", "Powerbank 10000mAh", "Swelling", "Severe",
     "a power bank with a badly swollen bulging casing, clearly deformed by internal battery swelling"),
    ("12", "Baterija za laptop", "Swelling", "Moderate",
     "a laptop replacement battery pack that is visibly swollen and puffed up in the middle"),
]


def build_prompt(product, damage_desc):
    return (
        f"A realistic customer complaint photograph of {product}, a consumer electronics "
        f"product, lying on a plain neutral surface. The product clearly shows {damage_desc}. "
        "Photorealistic, natural indoor lighting, taken with a smartphone camera, slightly "
        "imperfect. Single product centered. No text, no watermark, no overlay."
    )


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for old in os.listdir(OUT_DIR):
        os.remove(os.path.join(OUT_DIR, old))

    client = OpenAI(api_key=OPENAI_API_KEY)

    with open(MANIFEST, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["file", "product", "damage_type", "severity"])
        for sid, product, dtype, severity, desc in SCENARIOS:
            result = client.images.generate(
                model=OPENAI_IMAGE_MODEL,
                prompt=build_prompt(product, desc),
                size="1024x1024",
                n=1,
            )
            image = base64.b64decode(result.data[0].b64_json)
            filename = f"damage_{sid}_{severity}.png"
            with open(os.path.join(OUT_DIR, filename), "wb") as img:
                img.write(image)
            writer.writerow([filename, product, dtype, severity])
            print(f"  {filename}  ({product}, {dtype}, {severity})")

    print(f"\nGenerisano {len(SCENARIOS)} slika u {OUT_DIR}")
    print(f"Ground truth: {MANIFEST}")


if __name__ == "__main__":
    main()
