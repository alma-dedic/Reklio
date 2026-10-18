# Van obima aplikacije

Dokumenti u ovom folderu **nisu dio RAG korpusa** (ingest čita samo `docs/policy/*.md`).

Aplikacija pokriva **garancijske reklamacije**: provjerava razriješenu kupovinu,
važenje garancije, pokrivenost pravilnikom, vizuelni nalaz i rizik od zloupotrebe.

Izuzeti su procesi koje sistem ne sprovodi:

- `01_povrat_i_refundacija.md` — povrat/odustanak od **ispravnog** proizvoda u roku
  od 14 dana. Drugi poslovni proces (nije reklamacija), vidi `04_definicije_pojmova.md`, Član 6.
- `06_ostecenja_pri_dostavi.md` — transportno oštećenje s rokom od 48 sati i
  besplatnom zamjenom. Sistem nema podatak o datumu isporuke, pa se takvi slučajevi
  prijavljuju kao fizičko oštećenje i rješava ih operater.

Razlog izdvajanja: pravilnik u korpusu smije sadržavati **samo pravila koja sistem
zaista provjerava**. Inače RAG (i chatbot) kupcu obećavaju rokove i ishode koje
decision gate nikad ne primjenjuje.
