# Dokazi o kupovini – Reklio

> Test/demo dokument za RAG bazu. Uređuje koji dokazi su potrebni i kako se validiraju.

## Član 1 – Obavezni dokaz o kupovini

Svaka reklamacija mora sadržavati validan dokaz o kupovini koji nedvosmisleno povezuje proizvod sa stvarnom transakcijom. Bez ovog dokaza reklamacija se ne može obraditi.

## Član 2 – Fizička kupovina: fiskalni račun

Za proizvode kupljene u fizičkoj prodavnici, dokaz je fotografija fiskalnog računa. Sistem iz fotografije računa čita prodavnicu, datum, iznos i broj računa, te ih poredi sa evidencijom transakcija. Broj računa mora se tačno poklopiti sa zabilježenom transakcijom.

## Član 3 – Online kupovina: broj narudžbe

Za proizvode kupljene putem online prodavnice, dokaz je identifikator narudžbe ili broj e-računa. Ovaj kod se direktno provjerava u evidenciji narudžbi, bez potrebe za fotografijom, jer je već u digitalnom obliku.

## Član 4 – Tolerancija pri čitanju računa

Pošto automatsko čitanje fotografije računa nije savršeno, na iznos i datum dozvoljava se mala tolerancija (fuzzy poklapanje). Broj računa, međutim, mora biti tačan. Ako se pročitani podaci ne poklapaju sa evidencijom u okviru tolerancije, slučaj se prosljeđuje operateru.

## Član 5 – Vlasništvo dokaza

Dokaz o kupovini je bearer dokument — sistem ne može strukturno dokazati da je podnosilac reklamacije stvarni kupac. Zbog toga se rizik zloupotrebe procjenjuje posebnom AI komponentom, a sumnjivi slučajevi idu na ručni pregled.
