# Pravilnik o reklamacijama i garanciji – Reklio

> Napomena: ovo je interni pravilnik izmišljene kompanije, napravljen kao test/demo podatak za RAG bazu (ChromaDB) u okviru završnog rada. Nije pravno obavezujući dokument i ne treba ga tretirati kao stvarni zakonski tekst — brojevi i rokovi su odabrani da budu realistični i pogodni za testiranje AI komponenti (RAG, klasifikacija, chatbot).

## Član 1 – Opšte odredbe

Ovaj pravilnik uređuje postupak podnošenja i obrade reklamacija za proizvode kupljene putem fizičkih prodajnih mjesta i putem online prodavnice (omnichannel model). Pravilnik se primjenjuje na sve registrovane korisnike sistema, bez obzira na kanal kupovine.

## Član 2 – Rok za podnošenje reklamacije

Reklamacija se može podnijeti u roku od **14 dana** od dana prijema proizvoda, ukoliko se radi o nesaobraznosti proizvoda (proizvod ne radi ispravno ili ne odgovara opisu). Za oštećenja nastala tokom transporta i dostave primjenjuje se poseban, kraći rok definisan Članom 5.

## Član 3 – Garantni rok po kategorijama proizvoda

Opšti garantni rok za trajna dobra iznosi **24 mjeseca** od dana kupovine. Za pojedine kategorije važe posebni, kraći rokovi:
- Elektronika (pametni telefoni, laptopi, tableti, kamere): **24 mjeseca**
- Audio i dodaci (bežične slušalice, zvučnici, USB-C kablovi, punjači): **12 mjeseci**
- Potrošni dijelovi (baterije, powerbank): **6 mjeseci** — poseban izuzetak od opšteg roka, detaljno uređen posebnim dokumentom o izuzecima po kategoriji

Garancija pokriva fabričke nedostatke i kvarove nastale redovnom upotrebom, a ne pokriva oštećenja nastala nepravilnom upotrebom (vidi Član 6).

## Član 4 – Dokazi potrebni za prijavu reklamacije

Za validnu prijavu reklamacije korisnik je dužan dostaviti:
- Dokaz o kupovini: fotografiju fiskalnog računa (fizička kupovina) ili identifikator narudžbe/broj e-računa (online kupovina)
- Najmanje jednu fotografiju proizvoda koja jasno prikazuje problem
- Kratak opis problema

Reklamacije bez validnog dokaza o kupovini se ne mogu obraditi i automatski se odbijaju uz obavještenje korisniku o razlogu.

## Član 5 – Oštećenja nastala pri dostavi

Oštećenja koja su vidljiva odmah po prijemu pošiljke (fizička oštećenja ambalaže ili proizvoda nastala tokom transporta) moraju biti prijavljena u roku od **48 sati** od trenutka prijema. Ovakve reklamacije imaju prioritet u obradi i po pravilu rezultiraju besplatnom zamjenom proizvoda, bez dodatne naknade.

## Član 6 – Isključenja iz garancije

Garancija se ne primjenjuje u sljedećim slučajevima:
- Oštećenje nastalo usljed nepravilne upotrebe, nemara ili namjernog oštećenja od strane korisnika
- Oštećenje nastalo usljed neovlaštenog popravka od strane trećih lica
- Normalno habanje proizvoda tokom uobičajene upotrebe
- Nedostatak validnog dokaza o kupovini u propisanom roku

## Član 7 – Mogući ishodi reklamacije

Nakon obrade, reklamacija može rezultirati jednim od sljedećih ishoda, prema sljedećem redoslijedu prioriteta:
1. **Zamjena** proizvoda istim ili ekvivalentnim modelom (ukoliko je dostupan na stanju)
2. **Popravka** proizvoda o trošku prodavca (ukoliko zamjena nije moguća)
3. **Povrat novca** u punom iznosu (ukoliko ni zamjena ni popravka nisu izvodljivi u razumnom roku)

## Član 8 – Rokovi za obradu reklamacije

Sistem je dužan obraditi reklamaciju u sljedećim rokovima:
- Automatski obrađeni, niskorizični slučajevi: odgovor u roku od nekoliko minuta do 24 sata
- Slučajevi koji zahtijevaju pregled operatera: odgovor u roku od **15 radnih dana** od dana podnošenja

## Član 9 – Postupak kod sumnjivih ili spornih slučajeva

Ukoliko sistem za procjenu rizika (AI komponenta za detekciju zloupotrebe) označi zahtjev kao sumnjiv — na primjer zbog neuobičajeno visoke učestalosti reklamacija sa istog korisničkog naloga u kratkom vremenskom periodu — zahtjev se automatski prosljeđuje operateru na ručni pregled prije donošenja konačne odluke (princip Human-in-the-loop).

## Član 10 – Pravo na prigovor

Korisnik koji nije zadovoljan ishodom reklamacije ima pravo uložiti prigovor u roku od **8 dana** od prijema obavještenja o odluci, uz mogućnost dostavljanja dodatnih dokaza. Prigovor se automatski prosljeđuje operateru na ponovni pregled i ne obrađuje se ponovo isključivo automatskim putem.
