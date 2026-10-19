# Analiza povjerenja ulaza (trust boundary analiza)

## Svrha i metod

Funkcionalno testiranje provjerava **radi li funkcija**. Ne otkriva slučajeve u kojima
sistem radi ispravno, ali se **ishod može isforsirati** podatkom koji dolazi od onoga
o kome se odlučuje.

Metod: za svaki ulaz u automatsku odluku utvrditi **odakle dolazi** i **šta smije
promijeniti**.

## Princip

> Nepouzdan ulaz smije ishod samo **pooštriti**, nikada **ublažiti**.

Decision gate ovo već primjenjuje na AI signale: vizija, RAG i fraud mogu odvesti u
eskalaciju, ali nikada u automatsko odbijanje. Isto pravilo treba važiti i za podatke
koje unosi kupac.

## Mapa ulaza

| Ulaz | Izvor | Povjerljivost | Utiče na |
|---|---|---|---|
| Istek garancije | proračun u kodu nad datumima iz baze | pouzdan | auto-odbijanje |
| Fraud feature-i | `fn_features` (SQL nad bazom) | pouzdan | prag rizika |
| Izlaz vizije | model nad priloženom fotografijom | srednji | potvrda oštećenja, tip proizvoda |
| RAG nalaz | model nad korpusom pravilnika | srednji | pokrivenost |
| **PurchaseId** | **klijent** | **nepouzdan** | proizvod, garancija, iznos, fraud |
| **IssueType** | **klijent** | **nepouzdan** | grananje u gate-u |
| **IssueDescription** | **klijent** | **nepouzdan** | RAG pokrivenost |
| **Fotografije** | **klijent** | **nepouzdan** | vizuelna potvrda |
| Slika računa | klijent | nepouzdan | dokaz operateru |

## Nalazi

### N1 — PurchaseId nije vezan za priloženi dokaz

`Submit` provjerava samo **da kupovina postoji**, ne i da je ona ta koju je korisnik
dokazao pri razrješavanju računa. Razrješavanje i podnošenje su dva nezavisna zahtjeva
bez veze između sebe.

Posljedica: direktnim pozivom API-ja može se podnijeti reklamacija na bilo koju
kupovinu iz baze, uz proizvoljnu sliku računa.

Ublažavanje: fraud model ima feature-e `purchase_claimed_by_other_account` i
`distinct_accounts_on_purchase`, pa se pojava **detektuje** (eskalacija), ali se ne
**sprječava**.

Prijedlog: pri razrješavanju izdati kratkoročni potpisani token koji sadrži validirani
broj računa i dozvoljene `PurchaseId`; `Submit` ga zahtijeva i provjerava. Time se čuva
princip „OCR samo jednom".

### N2 — „Dokaz kupovine: Validno" je tvrdnja, ne provjera

Validacija računa (broj, iznos, datum) izvršena je pri razrješavanju, nad slikom
poslanom u **tom** zahtjevu. Pipeline zatim bezuslovno postavlja `ReceiptValidationResult.Valid`.

Operater vidi „Validno — poklapa se s kupovinom" i za reklamaciju čija priložena slika
nikada nije upoređena s izabranom kupovinom.

Prijedlog: rješava se zajedno s N1. Dok se ne riješi, formulacija prema operateru treba
biti preciznija.

### N3 — IssueType grana odluku, a dolazi od klijenta

Tip problema određuje da li se traži vizuelna potvrda. Vrijednost se ne validira na
backendu; nepoznata vrijednost pada u granu fizičkog oštećenja (što je sigurna grana).

Izbor `Proizvod ne radi / kvar` **ublažava** uslove (preskače se vizija). Prihvatljivo
je jer odobrenje vodi na servis, gdje serviser pregleda fizički uređaj.

Prijedlog: validirati vrijednost prema `IssueTypes` i odbiti nepoznatu.

### N4 — Opis kupca određuje pokrivenost

Opis je samoprijavljen i neprovjerljiv. Kada se koristi kao ulaz u RAG provjeru
pokrivenosti, ishod zavisi od formulacije: isto oštećenje opisano različitim riječima
daje različitu odluku.

Prijedlog: kada postoji fotografija, RAG upit graditi iz **izlaza vizije** (tip i
ozbiljnost oštećenja, kategorija proizvoda). Opis ostaje operateru na uvid i u AI
preporuci. Kod kvara, gdje fotografije nema, opis ostaje jedini ulaz.

### N5 — Verifikacija je na nivou modela, ne primjerka

Vizija potvrđuje da fotografija prikazuje oštećen proizvod odgovarajućeg tipa, ali ne i
da je riječ o **tom** primjerku koji je kupljen.

Prijedlog (ranije zabilježen): provjera serijskog broja ili dodatna provjera kategorije.

### N6 — Razrješavanje računa nije ograničeno na vlasnika

`resolve-purchase` vraća stavke i iznose za bilo koji broj računa. Kupovine u modelu
nemaju vlasnika, pa provjera vlasništva nije ni moguća bez izmjene modela.

Posljedica: moguće je pogađanjem brojeva saznati sadržaj tuđih računa.

Prijedlog: ograničiti učestalost poziva; dugoročno vezati kupovinu za kupca.

## Šta je uredno

- Jedino automatsko odbijanje oslanja se na proračun u kodu (istek garancije), nikada na AI
- Fraud feature-i se računaju u bazi, klijent na njih ne utiče
- AI signali mogu samo eskalirati
- Svi endpointi zahtijevaju prijavu
- Operaterova odluka je uvijek konačna, a prijedlog AI-a je označen kao prijedlog

## Metamorfni testovi

Testovi koji provjeravaju **odnose**, a ne pojedinačne ishode. Ovakvi testovi hvataju
klasu problema iz N3 i N4:

1. Ista fotografija, dvije formulacije opisa → **ista** odluka o pokrivenosti
2. Isti dokazi, promijenjen samo tip problema → odluka se ne smije **ublažiti**
3. Ista reklamacija podnesena dva puta → isti ishod (determinizam gate-a)
4. Povećan rizik uz nepromijenjene ostale signale → ishod se ne smije **poboljšati**
