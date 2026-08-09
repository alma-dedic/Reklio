# Postupak kod sumnjivih slučajeva – Reklio

> Test/demo dokument za RAG bazu. Uređuje postupanje kada AI komponenta označi reklamaciju kao rizičnu.

## Član 1 – Procjena rizika zloupotrebe

Sistem koristi posebnu AI komponentu za procjenu rizika zloupotrebe reklamacije. Procjena se zasniva na ponašanju naloga, a ne na sadržaju pojedinačne reklamacije, jer je vlasništvo nad dokazom o kupovini strukturno neprovjerljivo.

## Član 2 – Indikatori sumnje

Kao sumnjivi mogu biti označeni slučajevi sa neuobičajeno visokom učestalošću reklamacija sa istog naloga u kratkom periodu, sa istom kupovinom prijavljenom sa više naloga, ili sa drugim obrascima koji odstupaju od uobičajenog ponašanja poštenih kupaca.

## Član 3 – Automatsko prosljeđivanje operateru

Kada procjena rizika pređe definisani prag, zahtjev se automatski prosljeđuje operateru na ručni pregled prije donošenja konačne odluke. Sistem u tom slučaju ne donosi automatsku odluku o odbijanju.

## Član 4 – Zaštita poštenog kupca

Prag za označavanje rizika postavljen je tako da favorizuje visoku preciznost — pogrešna optužba poštenog kupca smatra se težom greškom od propuštanja pojedinačne zloupotrebe manje vrijednosti. Zbog toga sumnja vodi ka pregledu, a ne ka automatskom odbijanju.

## Član 5 – Objašnjivost odluke

Uz svaku rizičnu procjenu sistem bilježi glavne faktore koji su doprinijeli sumnji, kako bi operater imao uvid u razloge i mogao donijeti informisanu odluku. Ovo je dio principa transparentnosti i Human-in-the-loop pristupa.
