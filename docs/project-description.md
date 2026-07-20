Razvoj inteligentnog sistema za automatizaciju obrade reklamacija kupljenih proizvoda korištenjem vještačke inteligencije”. 

Opis završnog rada 
Cilj ovog završnog rada je razvoj inteligentnog web sistema za automatizaciju, klasifikaciju i obradu korisničkih reklamacija kupljenih proizvoda u okviru e-commerce i fizičkog poslovanja (omnichannel model). Sistem je namijenjen unapređenju procesa obrade reklamacija smanjenjem potrebe za ručnim pregledom zahtjeva, skraćenjem vremena obrade i smanjenjem rizika od zloupotrebe reklamacijskog procesa primjenom tehnika vještačke inteligencije.
Proces započinje na korisničkom panelu, gdje kupac podnosi reklamaciju dostavljanjem dokaza o kupovini, fotografija proizvoda i opisa problema. U slučaju fizičke kupovine korisnik dostavlja fotografiju računa, dok se za online kupovinu koristi identifikator narudžbe ili elektronskog računa. Sistem primjenjuje multimodalni model vještačke inteligencije za optičko prepoznavanje teksta (OCR) i automatsku ekstrakciju podataka sa računa, nakon čega vrši njihovu validaciju poređenjem sa bazom podataka transakcija.
Nakon uspješne validacije aktivira se centralni AI agent koji koordinira obradu zahtjeva. Model analizira fotografije proizvoda radi identifikacije vidljivih oštećenja (računarski vid), te istovremeno obrađuje tekstualni opis problema i generiše objašnjenje rezultata analize. Istovremeno, RAG (Retrieval-Augmented Generation) mehanizam koristi internu bazu pravilnika i uslova garancije kako bi odluke bile usklađene sa poslovnim pravilima, dok model mašinskog učenja procjenjuje rizik od potencijalne zloupotrebe reklamacijskog procesa. Sistem uključuje i inteligentni chatbot koji korisnicima na prirodnom jeziku pruža informacije o uslovima garancije i pravilima reklamacije.
Na osnovu rezultata svih AI komponenti sistem vrši inteligentnu klasifikaciju zahtjeva. Jednostavni i niskorizični slučajevi mogu biti automatski obrađeni, dok se složeniji ili sumnjivi zahtjevi prosljeđuju operateru na konačnu odluku prema principu Human-in-the-loop. Nakon donošenja konačne odluke korisnik putem aplikacije dobija obavještenje o ishodu reklamacije, uz odgovarajuće obrazloženje i informacije o narednim koracima. Na taj način rad demonstrira integraciju velikih jezičkih modela, računarskog vida, mašinskog učenja i RAG pristupa u jedinstvenu inteligentnu web platformu za podršku procesu obrade reklamacija.




Popis funkcionalnosti
•	Podnošenje reklamacije
Korisnici mogu podnijeti reklamaciju unosom opisa problema, odabirom proizvoda i dostavljanjem potrebnih dokaza (račun, slike oštećenja). 
•	Verifikacija dokaza o kupovini (OCR obrada)
Sistem automatski ekstraktuje podatke iz dostavljenih računa (digitalnih ili fotografisanih) i vrši njihovu validaciju poređenjem sa internim zapisima o transakcijama.
•	Analiza oštećenja proizvoda
Na osnovu dostavljenih slika i opisa korisnika, sistem vrši analizu vidljivih oštećenja i klasifikaciju tipa kvara.
•	Kontekstualna analiza pravila reklamacije (RAG)
Sistem omogućava dohvat relevantnih pravila, garancijskih uslova i poslovnih politika na osnovu sadržaja reklamacije, kako bi se osigurala konzistentnost odluka.
•	Procjena rizika od zloupotrebe (fraud detection)
Sistem analizira obrasce ponašanja korisnika i karakteristike reklamacije radi procjene vjerovatnoće zloupotrebe reklamacijskog procesa.
•	Inteligentna obrada i donošenje odluke
Na osnovu rezultata svih analiza sistem automatski klasifikuje reklamacije, tj. vrši automatsko odobrenje/odbijanje ili prosljeđivanje operateru.
•	Operaterski panel (Human-in-the-loop)
Operateri imaju uvid u sve kompleksne i rizične slučajeve, zajedno sa AI generisanim objašnjenjima i preporukama sistema, te vrše finalnu odluku.
•	Inteligentni chatbot 
Sistem omogućava korisnicima komunikaciju sa AI asistentom koji pruža informacije o pravilima reklamacije.
•	Notifikacije i obavještavanje korisnika
Korisnici dobijaju obavještenja o statusu reklamacije i konačnoj odluci sistema ili operatera.




Alati za implementaciju
Backend
•	C# 
•	ASP.NET Core Web API 
•	Entity Framework Core
Baza podataka
•	SQL Server 
Frontend
•	Angular 
•	TypeScript 
•	Angular Material 
AI i obrada podataka
•	Python (FastAPI framework) 
•	OpenAI API (Multimodalni model za OCR i vizuelnu analizu)
•	RAG sistem (LangChain)
•	ChromaDB (Vektorska baza podataka) 
•	XGBoost (Model za detekciju prevara) 
Razvojna okruženja
•	Visual Studio 2022 
•	Visual Studio Code
