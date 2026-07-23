# Reklio — Epics, User Stories, Tasks (v2 — jedan projekat)

Redoslijed epika = redoslijed implementacije. Ovo je uređena verzija nakon restarta
projekta — ugrađuje sve odluke iz `DECISIONS.md` (grill-me sesija), uz **dva svjesna
odstupanja** koja si eksplicitno tražila:

## Odstupanja od DECISIONS.md (namjerna, tvoja odluka)

| DECISIONS.md kaže | Sad radimo | Zašto je promjena OK |
|---|---|---|
| §11 — tri projekta (`Api`/`Core`/`Infrastructure`), granica na nivou kompajlera | **Jedan projekat**, slojevit po folderima | Tri projekta su postojala **isključivo** da spriječe da se stub "zavari" za pravu implementaciju. Pošto gradiš prave implementacije odmah (ne stub-pa-zamjena), ta granica ti ne treba — nema šta da štiti |
| §9 — vertical slice, sve AI komponente prvo kao stub, pa zamjena jedna po jedna | **EPIC 4** sad gradi interfejse + osnovni Angular tok bez stub odgovora; svaka AI komponenta (EPIC 5-8) se gradi **stvarno**, kad na nju dođe red | Direktna posljedica gornje odluke. **Cijena:** gubiš "uvijek imam demo od sedmice 2" sigurnosnu mrežu — sistem nije end-to-end demo-sposoban dok EPIC 9 (decision gate) ne poveže sve. Ako ponestane vremena, ono što nedostaje je **jasna, imenovana kutija** (npr. "chatbot nije stigao"), ne polovično povezan lanac — to je i dalje prihvatljivo pod breadth ocjenjivanjem, samo drugačiji oblik rizika |

Interfejsi (`IOcrService` i sl.) **ostaju** — to je dobra DI praksa nezavisno od stub odluke, samo više ne postoje da bi omogućili zamjenu bez refaktora, nego čisto radi testabilnosti i čitljivosti.

**Jezik ostaje kako je zaključano:** kod/entiteti/kolone engleski, UI/korpus/LLM izlaz bosanski.

---

## EPIC 0 — Temelj projekta

**User Story:** Kao developer, želim postavljen skeleton sva tri sloja (backend,
frontend, AI servis), da bih imao čistu, jednostavnu osnovu bez nepotrebne
strukture koju ne razumijem.

**Taskovi:**
- [x] T0.1 — Novi Git repo, `.gitignore` po stacku (`node_modules/`, `.venv/`, `bin/`, `obj/`)
- [x] T0.2 — **Jedan projekat** `Reklio.Api`, slojevit po folderima:
  ```
  Reklio.Api/
  ├── Controllers/
  ├── Services/
  │   ├── Interfaces/   (IOcrService, IVisionService, IFraudService, IRagService, ...)
  │   └── ...
  ├── BackgroundJobs/
  ├── Data/ (+ Migrations/)
  ├── Models/            (EF Core entiteti)
  ├── DTOs/ (Requests/, Responses/)
  ├── Exceptions/
  └── Middleware/
  ```
  Bez repository sloja preko EF Core-a (`DbContext` je već unit of work — vidi DECISIONS §11 "Rejected")
- [x] T0.3 — Angular scaffold: standalone, SCSS, lazy routing, folderi `core/ features/ shared/`
- [x] T0.4 — FastAPI skeleton: `app/` (main, config, routers, schemas, services) odvojen od `tools/` (offline: simulator, trener — vidi §13)
- [x] T0.5 — SQL Server konekcija testirana
- [x] T0.6 — Provjera OpenAI plaćenog naloga (billing enabled) — blokira sedmicu 4 (OCR), 5 (embeddings), 6 (vid)
- [x] T0.7 — ChromaDB "hello world" spike (~2h) — samo provjeri da se instalira i radi

> **Napomena:** ako je prethodni pokušaj već potvrdio da alati rade (.NET/Node/Python
> verzije, SQL Server konekcija, Python AI paketi instalirani bez kompajliranja iz
> izvora, ChromaDB spike prošao) — te provjere **ne moraš ponavljati**, to su činjenice
> o mašini, ne o projektnom kodu. Samo T0.1-T0.4 (stvarni fajlovi/struktura) kreću
> ispočetka.

---

## EPIC 1 — Autentikacija i uloge

**User Story:** Kao korisnik, želim da se registrujem i ulogujem, da bih mogao
podnositi reklamacije; kao operater, želim posebnu ulogu, da bih pristupio
operaterskom panelu.

**Taskovi:**
- [x] T1.1 — ASP.NET Core Identity + JWT bearer, token servis iza `IJwtTokenService`
- [x] T1.2 — `POST /api/auth/register` — bez email potvrde, nalog odmah aktivan. Duplikat emaila → 409
- [x] T1.3 — `RegisterRequest` **nema** polje `Role`; kontroler fiksno dodjeljuje `Customer` — uloga se nikad ne prima od klijenta
- [x] T1.4 — Seeder: uloge + test nalozi (`operater@reklio.ba` / `kupac@reklio.ba`)
- [x] T1.5 — Angular: login/registracija (navy mockup sa scan animacijom u lijevom panelu), `AuthService`, JWT interceptor, `authGuard` + `roleGuard`

---

## EPIC 2 — Podatkovni model

**User Story:** Kao sistem, trebam strukturiran podatkovni model koji odražava
stvarne odnose (kupovina bez vlasništva, reklamacija sa podnosiocem), da bi
validacija i fraud detekcija bile moguće.

**Finalna šema** (usklađena sa ER dijagramom):

| Entitet | Ključna polja |
|---|---|
| `User` | id, full_name, email, password_hash, role, registered_at |
| `Product` | id, name, category, price |
| `Purchase` | id, product_id (FK), purchase_type, document_number, branch, purchase_date, amount — **bez `user_id`, ni za jedan kanal** |
| `Claim` | id, user_id (FK, podnosilac), purchase_id (FK), operator_id (FK, nullable), status, issue_type, issue_description, risk_score, ai_explanation, submitted_at |
| `ClaimEvidence` | id, claim_id (FK), type, file_path |
| `Notification` | id, user_id (FK), claim_id (FK), message, is_read |

**Taskovi:**
- [x] T2.1 — EF Core entiteti prema tabeli gore
- [x] T2.2 — `Purchase` deduplicirana po prirodnom ključu: `branch/channel + document_number + purchase_date` — jedna stvarna transakcija = jedan red, koliko god ljudi dostavilo dokaz o njoj
- [x] T2.3 — `Claim.status` state machine: `Submitted → Processing → {AutoApproved, AutoRejected, Escalated} → {OperatorApproved, OperatorRejected}`
- [x] T2.4 — **Katalog proizvoda — fiksirati ovdje, prije svega ostalog.** ~10-12 trajnih dobara, 3-4 kategorije, garantni rok po kategoriji uključujući override (npr. baterije 6mj naspram opštih 24mj). Ovo je jedini ulaz od kojeg zavise simulator (E5), RAG korpus (E7) i hero računi (E13) — pravilnik se piše **prema katalogu**, ne obrnuto
- [x] T2.5 — Migracije + seed podaci (test korisnici, katalog iz T2.4)
- [x] T2.6 — Servisni sloj po entitetu. Operaterska lista (T11.1) ide direktno na `DbContext`, ne kroz generički CRUD — filter po statusu + sort po riziku + paginacija traže `IQueryable` kompoziciju koju generički `GetAll()` gubi

---

## EPIC 3 — Asinhrona obrada

**User Story:** Kao korisnik, ne želim da čekam 10-30 sekundi na ekranu dok se
AI analiza završi — želim da odmah dobijem potvrdu i obavještenje kad je gotovo.

**Taskovi:**
- [ ] T3.1 — `BackgroundService` + in-memory `Channel<T>` red čekanja u `BackgroundJobs/`
- [ ] T3.2 — Submit endpoint vraća `202 Accepted` odmah, stavlja posao u red
- [ ] T3.3 — Crash-recovery pass pri startu — traži zaglavljene `Processing` zapise, vraća ih u red (~1h, restarti su česti tokom razvoja)
- [ ] T3.4 — Angular: polling na ekranu detalja (`timer(0,3000)...takeWhile status==='Processing'`), obavezno zaustaviti na `ngOnDestroy`
- [ ] T3.5 — Status label lookup (engleski enum → bosanski prikaz), jedno mjesto u `shared/`

---

## EPIC 4 — Interfejsi AI servisa i osnovni Angular tok

**User Story:** Kao developer, želim jasan ugovor (interfejs) prema svakoj AI
komponenti i osnovni korisnički tok kroz UI, da bih znao tačno šta svaka
komponenta treba da vrati prije nego je gradim.

> Ovo VIŠE NIJE vertical-slice-sa-stub-ovima (vidi odstupanja na vrhu). Interfejsi
> se definišu ovdje, ali implementacije dolaze kasnije u EPIC 5-8, redom. Sistem
> nije end-to-end demo-sposoban dok EPIC 9 ne postoji — to je prihvaćen kompromis.

**Taskovi:**
- [ ] T4.1 — Interfejsi u `Services/Interfaces/`: `IOcrService`, `IVisionService`, `IFraudService`, `IRagService`, sa finalnim DTO oblicima (bez implementacije još)
- [ ] T4.2 — Orkestracija — fixed pipeline (ne agent loop), poziva servise redom, prosljeđuje rezultate decision gate-u. Poziva interfejse; implementacije dolaze u EPIC 5-9
- [ ] T4.3 — Angular: login → dashboard → wizard (4 koraka: tip kupovine, dokaz, opis, pregled) → ekran potvrde → ekran detalja
- [ ] T4.4 — Angular ekrani se mogu testirati sa mock JSON odgovorima u dev modu (privremeno, ne kao arhitektonska odluka — samo da frontend ne čeka backend)

---

## EPIC 5 — Simulator podataka i XGBoost fraud model

**User Story:** Kao sistem, trebam procijeniti rizik zloupotrebe reklamacije na
osnovu ponašanja naloga, jer je vlasništvo nad dokazom kupovine strukturalno
neprovjerljivo (bearer credential problem — vidi DECISIONS §1, kičma cijelog rada).

**Taskovi:**
- [ ] T5.1 — `fn_features(@claim_id)` — parametrizovana SQL funkcija, jedini izvor istine za sve feature-e (poziva je i trening i serving strana). Isporučiti kroz EF Core migraciju kao raw SQL
- [ ] T5.2 — Simulator (`tools/simulator.py`) — skriveni tip (honest/abuser, p≈0.08) → šumovito, **preklapajuće** ponašanje → generiše redove u `Purchase` + istoriju `Claim`. Labela = skriveni tip, nikad funkcija feature-a
- [ ] T5.3 — **Provjeriti point-in-time leakage** — svaki agregat računat kao stanje u trenutku te reklamacije, isključujući nju samu (`c2.submitted_at < c.submitted_at`, nikad `GETDATE()`)
- [ ] T5.4 — Feature-i iz četiri porodice:
  - veza sa kupovinom — `prior_claims_on_purchase`, `purchase_claimed_by_other_account` (najjači pojedinačni feature), `distinct_accounts_on_purchase`
  - vremenski — `days_purchase_to_claim`, `warranty_period_used_pct`, `claimed_within_first_n_days`
  - ponašanje naloga — `total_claims`, `claims_last_30d`, `claims_last_90d`, `mean_days_between_claims`, `account_age_days`, `prior_rejection_rate`
  - vrijednost/raspršenost — `claim_amount`, `amount_vs_user_mean`, `distinct_categories`, `distinct_stores`
  - **Namjerno izostavljeno:** photo-reuse hash, vid/opis slaganje, OCR pouzdanost — drži simulator na čistim redovima (bez fajlova slika, bez LLM ocjena koje bi morale biti simulirane bez kružnosti)
- [ ] T5.5 — Trening XGBoost (`tools/train.py`) na simuliranom skupu → `model.pkl`. **Mora fitovati na pandas DataFrame, ne numpy array** — inače `feature_names_in_` ne postoji i safeguard iz T5.7 tiho ne postoji umjesto da pukne
- [ ] T5.6 — **AUC gate.** Očekivano 0.85-0.92. Ako prvi AUC dođe iznad ~0.95, to nije uspjeh nego bug (leakage ili cirkularne labele) — stati i tražiti uzrok prije nastavka
- [ ] T5.7 — **Feature-order safeguard** (~30 min): C# šalje imenovani dict, nikad niz; FastAPI reordera po imenu prema `model.feature_names_in_`; startup asertacija sa glasnim padom ako se imena ne poklapaju
- [ ] T5.8 — Prag odluke izveden iz precision/recall krive (ciljana niska stopa lažnih optužbi), ne proizvoljan broj — pogrešna optužba pravog kupca košta mnogo više od odobrene lažne reklamacije od 50 KM
- [ ] T5.9 — `IFraudService` implementacija — učitava `model.pkl` pri startu, poziva `fn_features`

---

## EPIC 6 — OCR i validacija kupovine

**User Story:** Kao kupac, želim da priložim dokaz kupovine (sliku računa ili
kod narudžbe), da bi sistem potvrdio da je kupovina stvarna prije obrade
reklamacije.

> Hero računi se NE rade ovdje — samo za demo, zamrzavaju se u sedmici 7 (T13.5).

**Taskovi:**
- [ ] T6.1 — Bulk skup računa: HTML→PNG render (tekst tačan po konstrukciji) pa programska degradacija (perspektiva, tekstura, sjena, zamućenje — Augraphy)
- [ ] T6.2 — Opcionalno: odštampati i fotografisati ~5 komada za poštenu tvrdnju da je OCR testiran na stvarnim fotografijama
- [ ] T6.3 — Fizička kupovina: `POST /analyze/receipt` — OpenAI multimodalni poziv, `temperature=0`, structured output
- [ ] T6.4 — Online kupovina: direktan lookup po kodu narudžbe protiv `Purchase` — bez OCR-a
- [ ] T6.5 — Validacija — poređenje ekstrahovanih/unesenih podataka sa `Purchase` (fuzzy match za OCR polja)
- [ ] T6.6 — `IOcrService` implementacija

---

## EPIC 7 — RAG (pravilnik i garancija)

**User Story:** Kao sistem, trebam provjeriti da li reklamacija odgovara
uslovima garancije, citirajući tačan član pravilnika.

**Taskovi:**
- [ ] T7.1 — Proširiti pravilnik sa namjernim "zamkama": leksički gotovo-promašaji (poseban dokument o povratu/refundaciji), preklapajući autoritet (izuzetak za kategoriju), kontradiktorni izuzeci
- [ ] T7.2 — ~15 dokumenata / 100-150 chunk-ova ukupno
- [ ] T7.3 — ChromaDB ingest + LangChain retrieval chain. **Eksplicitno postaviti embedding funkciju na OpenAI `text-embedding-3-small`** — Chroma default (`all-MiniLM-L6-v2`) tiho skida 79MB, engleski-only, past bi na demo mašini
- [ ] T7.4 — Evaluacija: BM25 naspram embeddings na upitima sa leksičkim zamkama (~2h, tabela za Poglavlje 6)
- [ ] T7.5 — `POST /analyze/policy` — vraća `{covered, applicable_exclusion, cited_article}`
- [ ] T7.6 — `IRagService` implementacija

---

## EPIC 8 — Vizuelna analiza oštećenja

**User Story:** Kao sistem, trebam procijeniti da li je oštećenje na proizvodu
stvarno i koliko ozbiljno.

**Taskovi:**
- [ ] T8.1 — Bazen ~10-15 AI-generisanih fotografija oštećenja
- [ ] T8.2 — `POST /analyze/damage` — GPT vision poziv, `temperature=0`, structured output: `{damage_confirmed, damage_type, severity, confidence}`
- [ ] T8.3 — `IVisionService` implementacija

---

## EPIC 9 — Decision gate i orkestracija (tačka gdje sistem postaje demo-sposoban)

**User Story:** Kao sistem, trebam donijeti konzistentnu, objašnjivu odluku na
osnovu svih AI signala, i tu odluku jasno objasniti korisniku i operateru.

**Taskovi:**
- [ ] T9.1 — Decision gate — deterministička funkcija (ne LLM): kombinuje validaciju + rizik skor + vid + RAG rezultat
- [ ] T9.2 — Konzervativna automatizacija: auto-odobri samo jasan nizak rizik; auto-odbij samo na tvrdim greškama (nema poklapanja u `Purchase`, van garancije); sve ostalo → eskaliraj operateru. **Nikad auto-odbij na osnovu mekog signala**
- [ ] T9.3 — LLM objašnjenje — poziva se TEK nakon što je odluka fiksirana; dva teksta (korisnik — jednostavno, operater — tehnički), citira RAG nalaz
- [ ] T9.4 — Napomena za rad: kombinacija je deterministička, ulazi (vid, RAG verdikt) nisu — "near-reproducible", ne "reproducible"
- [ ] T9.5 — **Prvi pravi end-to-end test** — od ovog trenutka sistem ima puni demo-sposoban tok

---

## EPIC 10 — Chatbot

**User Story:** Kao korisnik, želim da pitam o pravilima garancije i uslovima
reklamacije, na prirodnom jeziku.

> **Bez pristupa korisničkim podacima — samo korpusu.** Status reklamacije se ne
> dohvata kroz chat (korisnik ga vidi na ekranu detalja i kroz notifikacije).
> Svaki upit o statusu bi morao biti scoped na `user_id` iz JWT-a — jedan propust
> znači curenje tuđih reklamacija. Dobitak mali, gubitak (sigurnosna greška na
> odbrani) veliki.

**Taskovi:**
- [ ] T10.1 — RAG upiti nad istim korpusom kao EPIC 7, bez korisničkog konteksta
- [ ] T10.2 — Pitanja koja pokriva: trajanje garancije, izuzeci, kako se podnosi reklamacija, rokovi
- [ ] T10.3 — Minimalan Angular chat widget (floating bubble + prozor)

---

## EPIC 11 — Operaterski panel

**User Story:** Kao operater, želim pregledati eskalirane reklamacije sa svim
AI nalazima na jednom mjestu.

**Taskovi:**
- [ ] T11.1 — Lista reklamacija na čekanju, sortirano po riziku (direktno na `DbContext`, vidi T2.6)
- [ ] T11.2 — Detaljan prikaz: OCR podaci, foto sa naznakom oštećenja, RAG citat, risk skor + razlog, LLM preporuka za operatera
- [ ] T11.3 — Akcije: odobri / odbij / zatraži dopunu

---

## EPIC 12 — Notifikacije

**User Story:** Kao korisnik, želim biti obaviješten kad je moja reklamacija
obrađena, pošto ne čekam na ekranu.

**Taskovi:**
- [ ] T12.1 — In-app notifikacija — tabela, zvonce sa brojem nepročitanih
- [ ] T12.2 — Okidač: promjena statusa `Claim` generiše notifikaciju
- [ ] T12.3 — "Označi pročitano" akcija

---

## EPIC 13 — Testiranje, evaluacija i demo fixture-i

**User Story:** Kao autor rada, trebam mjerljive dokaze da svaka AI komponenta
radi, i skup računa koji dobro izgleda na odbrani.

**Taskovi:**
- [ ] T13.1 — OCR preciznost na bulk skupu računa
- [ ] T13.2 — XGBoost metrike zapisane za Poglavlje 6 (AUC gate je već prošao u T5.6)
- [ ] T13.3 — RAG: BM25 vs. embeddings na upitima sa zamkama (iz T7.4)
- [ ] T13.4 — End-to-end smoke test — puna reklamacija kroz sve prave komponente
- [ ] T13.5 — **Hero računi — zamrznuti ovdje, jednom.** 5-8 AI-generisanih fotorealističnih primjera:
  - proizvod postoji u katalogu, trajno je dobro, ima garantne uslove u korpusu
  - svaka hero transakcija ima vlasnika (postojeći simulirani korisnik) — jedan pošten, jedan sumnjiv
  - verifikovati naslijepo (drugi model transkribuje hladno, diff protiv tvog čitanja)

---

## Redoslijed po sedmicama (referenca, orijentaciono)

| Sedmica | Epici |
|---|---|
| 1-2 | EPIC 0, 1, 2, 3, 4 — temelj, auth, šema, red čekanja, interfejsi + Angular tok. **Katalog fiksiran u T2.4** |
| 3 | EPIC 5 — simulator + XGBoost + `fn_features`. AUC gate T5.6 mora proći prije sedmice 4 |
| 4 | EPIC 6 — OCR (bulk skup, bez hero računa) |
| 5 | EPIC 7 — RAG (korpus prema katalogu iz T2.4) |
| 6 | EPIC 8, 9, 10 — vid, decision gate (**sistem postaje end-to-end demo-sposoban ovdje**), chatbot |
| 7 | EPIC 11, 12, 13 — operater, notifikacije, testiranje, hero računi zamrznuti |

### Zavisnosti koje presijecaju sedmice

Katalog (T2.4) → simulator (T5.2), RAG korpus (T7.1), hero računi (T13.5). Fiksira se
u sedmici 2, poslije se ne dira.

`fn_features` (T5.1) je jedina definicija feature-a — zove je i `tools/train.py` i C#
pri serviranju. Dvije implementacije = tih training/serving skew.

**Najveća promjena naspram v1:** sistem nije demo-sposoban od sedmice 2 nadalje
(kako bi bio uz stub pristup), nego tek od EPIC 9 (sedmica 6). Ako ponestane vremena
prije toga, budi svjesna da nemaš "ružan ali kompletan" fallback — planiraj bafer
u skladu s tim.
