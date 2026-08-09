# Rokovi obrade i eskalacija – Reklio

> Test/demo dokument za RAG bazu. Uređuje rokove obrade reklamacije i pravila eskalacije.

## Član 1 – Automatska obrada niskorizičnih slučajeva

Reklamacije koje sistem procijeni kao niskorizične i saobrazne pravilniku obrađuju se automatski, sa odgovorom u rasponu od nekoliko minuta do najviše 24 sata. Ovo su slučajevi sa jasnim dokazom o kupovini i unutar garantnog roka, bez indikatora rizika.

## Član 2 – Slučajevi za operatera

Slučajevi koji zahtijevaju ljudski pregled obrađuju se u roku od **15 radnih dana** od dana podnošenja. U ovu grupu spadaju granični slučajevi, slučajevi sa nepodudarnim dokazom o kupovini, te slučajevi koje je AI komponenta označila kao rizične.

## Član 3 – Kriteriji eskalacije

Zahtjev se eskalira operateru kada nastupi bilo koji od sljedećih uslova:
- procjena rizika prelazi definisani prag
- dokaz o kupovini se ne poklapa sa evidencijom u okviru tolerancije
- reklamacija se odnosi na granični slučaj garantnog roka ili isključenja

## Član 4 – Princip Human-in-the-loop

Nijedan rizičan ili sporan slučaj ne odbija se niti odobrava isključivo automatski. Konačnu odluku u takvim slučajevima uvijek donosi operater, čime se štiti kupac od pogrešne automatske optužbe, a prodavac od zloupotrebe.

## Član 5 – Obavještavanje kupca

O svakom ishodu, bilo automatskom bilo operaterskom, kupac se obavještava putem notifikacije u aplikaciji, sa navođenjem razloga odluke i, gdje je primjenjivo, citiranog člana pravilnika.
