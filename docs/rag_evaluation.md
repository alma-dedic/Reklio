# Evaluacija pretrage: BM25 naspram embeddings (EPIC 7, T7.4)

Poređenje leksičke pretrage (BM25) i semantičke pretrage (OpenAI
`text-embedding-3-small` + ChromaDB) na upitima sa namjernim zamkama iz
proširenog pravilnika. Referenca za Poglavlje 6 rada.

## Postavka

- Korpus: 15 dokumenata, 135 chunkova (chunk_size 320, overlap 70).
- Zamke: leksička (povrat/refundacija dijeli vokabular sa garancijom),
  overlap-authority (izuzetak Potrošni dijelovi 6mj naspram opštih 24mj),
  kontradikcija (akcijski proizvodi 12mj naspram opštih 24mj).
- Metrika: koji je dokument dohvaćen kao top-1, i da li je tačan u top-3.

## Rezultati (top-1 dohvaćeni dokument)

| Upit | BM25 | Embeddings |
|---|---|---|
| garancija na powerbank | 12_akcijski ✗ | 03_izuzetak ✓ |
| garancija na akcijski telefon (24mj?) | 03_izuzetak ✗ | 12_akcijski ✓ |
| zamjenska baterija | 03_izuzetak ✓ | 03_izuzetak ✓ |
| rok za prigovor | 10_prigovor ✓ | 10_prigovor ✓ |
| vratiti neoštećen (predomislio se) | 01_povrat ✓ | 04_definicije ✗ |
| povrat novca za ispravan proizvod | 01_povrat ✓ | 04_definicije ✗ |
| vratiti zbog boje | 01_povrat ✓ | 01_povrat ✓ |
| polomljen u dostavi | 05_iskljucenja ✗ | pravilnik (Član 5) ✗* |

*Embeddings su za dostavu dohvatili glavni pravilnik, koji takođe sadrži rok od
48h (Član 5) — sadržajno tačno, samo drugi fajl.

**Zbir:** top-1 → BM25 5/8, Embeddings 5/8. Top-3 recall → 7/8 oba.

## Tumačenje

Rezultat je nijansiran i pokazuje TAČNO gdje semantika pomaže:

- **Overlap-authority zamke (powerbank, akcijski):** embeddings jasno pobjeđuju.
  BM25 ključne riječi ("garancija", "mjeseca", "akcijski") odvuku na pogrešan
  dokument, dok semantika nađe tačan izuzetak. Ovo je najrelevantniji scenario —
  kolizija autoriteta gdje opšte pravilo lažno privuče keyword pretragu.
- **Leksička zamka povrata:** BM25 pobjeđuje jer tačan dokument (`01_povrat`) ima
  najjaču gustoću ključnih riječi; embeddings odlutaju na semantički blizak
  `04_definicije`.
- **Ukupno izjednačeno** — nijedan metod ne dominira na cijelom skupu.

## Praktična težina top-1 razlike

Endpoint `/analyze/policy` u LLM kontekst prosljeđuje **top-5** chunkova, ne samo
top-1. Pošto je top-3 recall 7/8 za oba metoda, tačan dokument je u kontekstu
generatora u velikoj većini slučajeva. Top-1 promašaj stoga ne vodi nužno ka
pogrešnom odgovoru — ključno je da je tačan chunk unutar top-5.

## Buduća unapređenja

- **Hibrid retriever (BM25 + embeddings):** kombinovanje leksičke i semantičke
  pretrage (npr. reciprocal rank fusion) uhvatilo bi obje snage — semantiku na
  authority-kolizijama i keyword preciznost na direktnim upitima. Nije
  implementirano; trenutni nalaz je dovoljan za tezu.
- Reranking dohvaćenih chunkova prije generisanja odgovora.
