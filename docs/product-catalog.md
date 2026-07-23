# Katalog proizvoda — TehnoDom

Radnja: **TehnoDom**, omnichannel prodavnica elektronike (online + fizičke poslovnice).

Ovo je fiksni katalog iz T2.4. Referenca je za sve kasnije korake koji zavise od
proizvoda i garancije: simulator podataka (EPIC 5), RAG korpus / pravilnik (EPIC 7),
te hero računi i fotografije oštećenja (EPIC 13).

`WarrantyMonths` u bazi (`Product.WarrantyMonths`) i garantni rokovi u RAG pravilniku
**moraju imati iste brojeve**, ali služe različitim svrhama:
- kolona = mašinski proračun (decision gate "van garancije", `fn_features` →
  `warranty_period_used_pct`),
- pravilnik = tekst za LLM objašnjenje.

## Elektronika — garancija 24 mjeseca

| Id | Proizvod | Cijena |
|---|---|---|
| 1 | Pametni telefon X20 | 899 KM |
| 2 | Laptop Pro 15 | 1890 KM |
| 3 | Tableta 10.1" | 549 KM |
| 4 | Kamera GX10 | 459 KM |

## Audio i dodaci — garancija 12 mjeseci

| Id | Proizvod | Cijena |
|---|---|---|
| 5 | Bežične slušalice Q3 | 149 KM |
| 6 | Zvučnik prijenosni | 129 KM |
| 7 | USB-C kabl 1m | 19,90 KM |
| 8 | Punjač brzi 30W | 39 KM |

## Potrošni dijelovi — garancija 6 mjeseci (izuzetak od opšteg pravila)

| Id | Proizvod | Cijena |
|---|---|---|
| 9 | Powerbank 10000mAh | 79 KM |
| 10 | Baterija za laptop (zamjenska) | 99 KM |

## Napomena o garanciji

Opšte pravilo je 24 mjeseca za trajna dobra. **Potrošni dijelovi (baterije,
powerbank) imaju skraćeni rok od 6 mjeseci** — ovo je namjerni override koji
decision gate i pravilnik moraju tretirati konzistentno.