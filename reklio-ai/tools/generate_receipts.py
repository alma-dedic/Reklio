"""

Bulk skup test računa za OCR (samo fizičke kupovine — online se validira po
document_number, bez slike). Tekst se RENDERUJE iz Purchase reda (tačan po
konstrukciji), pa se programski degradira (rotacija, tekstura, sjena, blur)
da liči na fotografisan račun.

Izlaz: tools/data/receipts/*.png + tools/data/receipts_manifest.csv (ground truth).
"""

import csv
import os
import random

import numpy as np
import pyodbc
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

N_RECEIPTS = 60
SEED = 42
HERE = os.path.dirname(__file__)
OUT_DIR = os.path.join(HERE, "data", "receipts")
MANIFEST = os.path.join(HERE, "data", "receipts_manifest.csv")

random.seed(SEED)
np.random.seed(SEED)

_FONT_CANDIDATES = [
    r"C:\Windows\Fonts\consola.ttf",
    r"C:\Windows\Fonts\cour.ttf",
    r"C:\Windows\Fonts\arial.ttf",
]


def load_font(size):
    for path in _FONT_CANDIDATES:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def connect():
    for driver in ("ODBC Driver 18 for SQL Server", "ODBC Driver 17 for SQL Server"):
        try:
            return pyodbc.connect(
                f"DRIVER={{{driver}}};SERVER=localhost;DATABASE=ReklioDb;"
                "Trusted_Connection=yes;TrustServerCertificate=yes;")
        except pyodbc.Error:
            continue
    raise RuntimeError("Nije pronađen ODBC Driver 17/18 za SQL Server.")


def load_purchases(cur):
    # samo fizičke kupovine (Online ide na lookup po broju, bez računa-slike)
    cur.execute(f"""
        SELECT TOP {N_RECEIPTS} p.Id, p.DocumentNumber, p.Branch, p.PurchaseDate, p.Amount, pr.Name
        FROM Purchases p
        JOIN Products pr ON pr.Id = p.ProductId
        WHERE p.DocumentNumber LIKE 'SIM-%' AND p.Branch <> 'Online'
        ORDER BY p.Id
    """)
    return cur.fetchall()


def money(amount):
    return f"{amount:.2f}".replace(".", ",") + " KM"


def render_receipt(doc_number, branch, purchase_date, amount, product_name):
    width = 440
    title_font = load_font(30)
    head_font = load_font(18)
    body_font = load_font(17)
    small_font = load_font(14)

    img = Image.new("RGB", (width, 360), (252, 252, 250))
    draw = ImageDraw.Draw(img)
    cx = width // 2

    def center(text, y, font, fill=(20, 20, 20)):
        w = draw.textlength(text, font=font)
        draw.text((cx - w / 2, y), text, font=font, fill=fill)

    def row(left, right, y, font):
        draw.text((24, y), left, font=font, fill=(20, 20, 20))
        w = draw.textlength(right, font=font)
        draw.text((width - 24 - w, y), right, font=font, fill=(20, 20, 20))

    def sep(y):
        draw.line((24, y, width - 24, y), fill=(120, 120, 120), width=1)

    center("TehnoDom", 20, title_font)
    center(branch, 58, head_font, fill=(80, 80, 80))
    sep(90)
    row("Racun br:", doc_number, 104, body_font)
    row("Datum:", purchase_date.strftime("%d.%m.%Y"), 132, body_font)
    sep(162)
    draw.text((24, 178), product_name[:34], font=body_font, fill=(20, 20, 20))
    row("", money(amount), 206, body_font)
    sep(236)
    row("UKUPNO:", money(amount), 252, head_font)
    sep(286)
    center("Hvala na kupovini!", 306, small_font, fill=(90, 90, 90))
    return img


def add_noise(img, sigma):
    arr = np.asarray(img).astype(np.int16)
    noise = np.random.normal(0, sigma, arr.shape)
    arr = np.clip(arr + noise, 0, 255).astype(np.uint8)
    return Image.fromarray(arr)


def degrade(receipt):
    w, h = receipt.size
    pad = 44
    bg = (223, 221, 214)
    canvas = add_noise(Image.new("RGB", (w + 2 * pad, h + 2 * pad), bg), 7)

    shadow = Image.new("RGB", (w, h), (150, 148, 143))
    canvas.paste(shadow, (pad + 7, pad + 9))
    canvas.paste(receipt, (pad, pad))

    canvas = canvas.rotate(random.uniform(-4, 4), expand=True,
                           fillcolor=bg, resample=Image.BICUBIC)
    canvas = ImageEnhance.Brightness(canvas).enhance(random.uniform(0.88, 1.06))
    canvas = ImageEnhance.Contrast(canvas).enhance(random.uniform(0.9, 1.05))
    canvas = canvas.filter(ImageFilter.GaussianBlur(random.uniform(0.4, 0.9)))
    return add_noise(canvas, 5)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for old in os.listdir(OUT_DIR):
        os.remove(os.path.join(OUT_DIR, old))

    cn = connect()
    rows = load_purchases(cn.cursor())
    cn.close()
    if not rows:
        raise RuntimeError("Nema fizičkih SIM- kupovina — pokreni simulator prije ovoga.")

    with open(MANIFEST, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["file", "purchase_id", "document_number", "branch",
                         "purchase_date", "amount", "product_name"])
        for pid, doc, branch, pdate, amount, name in rows:
            image = degrade(render_receipt(doc, branch, pdate, float(amount), name))
            filename = f"{doc}.png"
            image.save(os.path.join(OUT_DIR, filename))
            writer.writerow([filename, pid, doc, branch,
                             pdate.strftime("%Y-%m-%d"), f"{float(amount):.2f}", name])

    print(f"Generisano {len(rows)} računa u {OUT_DIR}")
    print(f"Ground truth: {MANIFEST}")


if __name__ == "__main__":
    main()
