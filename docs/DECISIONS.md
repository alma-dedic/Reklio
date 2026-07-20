# Reklio — Locked Decisions

Output of the grilling session. Every decision here is settled; the reasoning is recorded so it can be defended at the odbrana, not just followed.

## Language policy (locked)

| Layer | Language |
|---|---|
| **Code** — identifiers, entities, DB tables & columns, enums, API contracts, commit messages | **English** |
| **UI** — all user-facing strings in Angular | **Bosnian** |
| **Content** — RAG corpus, receipts, damage-photo context | **Bosnian** |
| **LLM output** — decision explanations, chatbot replies | **Bosnian** |
| **Thesis document** | **Bosnian** |

**Flip it all the way, including the database.** English entities mapped to Bosnian tables via EF Core configuration is the worst available outcome — a permanent mental translation layer, and grep finds half of anything. The schema is part of the codebase.

**The language boundary is inside the prompt, not at the API.** Prompts live in English source but must instruct Bosnian output, and the retrieved chunks feeding them are Bosnian. Applies to the decision explanation (§4) and the chatbot (§7).

**No Angular i18n infrastructure.** `@angular/localize` for a single-language UI is pure ceremony — hardcode Bosnian strings. Status enums are stored in English and mapped to Bosnian labels for display via a single lookup.

Prose in this document uses Bosnian domain terms freely (*reklamacija*, *garancija*) — that's vocabulary, not identifiers.

---

## 0. Constraints (the inputs everything else was derived from)

| Constraint | Value |
|---|---|
| Time budget | ~20h/week × ~7 weeks ≈ **140h** |
| Scope of that budget | **Code only** — thesis document has a separate later deadline |
| Grading reality | **Breadth** — the funkcionalnosti list is read as a contract; every promised component must exist and demo |
| Tech stack | **Binding** — mentor-approved, not negotiable |
| Estimated build cost | **~125h** → **~15h buffer (11%)** |

**Implication that drove everything:** under breadth grading, a missing component costs more than a shallow one. Each component is therefore built as thin as possible while remaining genuinely functional and defensible. Depth is added only where it is free or near-free.

### Stack (fixed)

- **Backend:** C#, ASP.NET Core Web API, EF Core
- **Database:** SQL Server
- **Frontend:** Angular, TypeScript, Angular Material
- **AI service:** Python, FastAPI
- **AI/data:** OpenAI API (multimodal — OCR + vision), LangChain (RAG), ChromaDB (vectors), XGBoost (fraud)

---

## 1. What Reklio actually is

**A claims system for a single omnichannel business** — one company, physical stores plus its own webshop. **Not** a multi-retailer B2B platform. There is no `Trgovac` entity.

**Reklio sits downstream of every purchase it processes.** It never participates in the transaction. It receives *evidence* of one after the fact, when a claim is filed. This is true of **both** channels — the online channel is not "a purchase made inside Reklio."

### The spine of the thesis

> A receipt photo and an order code are both **bearer credentials**. Neither proves ownership. Anyone holding the evidence can file a claim, and Reklio cannot tell otherwise.
>
> Therefore **ownership is structurally unverifiable**, uniformly across both channels — and **fraud detection is the compensating control for a gap the architecture cannot close.**

This is the central argument. It is what makes the ML component load-bearing rather than decorative, and it gives one consistent answer to the obvious examiner question ("how do you know the claimant actually bought it?"): *you don't, it's structural, and that's why fraud scoring exists.*

The document should be built around this.

---

## 2. Schema

### Core shape

- **`Purchase`** *(Kupovina)* — a **pure mirror of an external transaction**. Fields: channel, date, amount, receipt/order number, product, store/location.
  - **No `user_id`. Neither channel.** The link is removed from the schema entirely, not made nullable.
  - **No `buyer_email`.** Explicitly rejected (see §8).
  - Populated from the company's own **POS and webshop feeds**.
  - **Deduplicated on a natural key:** store/channel + receipt-or-order number + date. One real-world transaction = one row, no matter how many people submit evidence of it.
- **`Claim`** *(Reklamacija)* — carries its **own direct `user_id`** (the filer, from their Reklio account) and a `purchase_id`.
  - **`status`** — load-bearing (see §11). State machine:
    `Submitted → Processing → {AutoApproved, AutoRejected, Escalated} → {OperatorApproved, OperatorRejected}`
    Read by the queue, the UI poller, the operator panel, and the notification trigger.
    Stored in English; displayed in Bosnian via a single label lookup.
- **`User`** *(Korisnik)* — a Reklio account. Only ever linked to claims, never to purchases.
- **`Notification`** *(Obavjestenje)* — in-app notifications (see §7).

### Why dedupe matters

Because `Purchase` is deduplicated, two accounts claiming the same physical transaction both point at **the same row**. The strongest fraud signal in the system therefore reduces to a `COUNT` query. Without dedupe it would require fuzzy matching — invented work.

### Channel handling

The claim process is **identical for both channels after validation** — same `Claim` flow, same AI analysis, same fraud scoring, **no channel-based risk asymmetry**. The only difference is how the purchase is validated:

| Channel | Evidence | Validation |
|---|---|---|
| Physical | Receipt photo | OCR extracts fields → compare to `Purchase` |
| Online | Order code / ID | **Direct lookup** against `Purchase` — already text, **no OCR at all** (~1h) |

---

## 3. Fraud detection

### Training data: simulate the *population*, not the *labels*

No real labelled claims fraud exists. Data is generated by simulating a hidden cause, not by writing labels from features.

1. Draw a **latent type** per account the model never sees: `abuser` with p ≈ 0.08, else `honest`.
2. Generate behaviour **from that type**, with **overlapping, noisy distributions** — honest accounts file rarely, abusers more often; abusers cluster near warranty expiry; but an honest customer *can* file four claims in a month and an abuser *can* file one.
3. **Label = the latent type**, never a function of the features.
4. XGBoost must infer the hidden type from observable behaviour alone.

**The test for correctness:** try to write an if-statement that recovers the labels perfectly. If you can, it's still circular. If the noise and overlap destroyed that information, it's a real learning problem.

**Expected result:** AUC ≈ 0.85–0.92, not ~1.0. The model *cannot* be perfect — some abusers are genuinely indistinguishable. That ceiling is the point.

> **If your first AUC comes back above ~0.95, that is not success — that is the bug.**
>
> Demonstrated in the week-1 spike: a 30-line toy simulator using Poisson(0.4) vs Poisson(3.5) scored **AUC 0.994**. Those populations barely overlap, so they are nearly linearly separable and the model just recovers the split — the exact failure this section warns about, hit *while trying to demonstrate the fix*. **Real overlap needs to be substantially wider and noisier than feels intuitive.**

**What this earns:** real train/test metrics; feature importance as a genuine finding (the ranking was never encoded); interactions the model discovers that were never written as rules; a comparison against a rule baseline that XGBoost beats. The generative assumptions become a documented methodology subsection, and the limitation is a normal thesis sentence: *results hold conditional on the assumptions in §X; deployment requires retraining on observed claims.*

### Feature set — ~12 features, pure SQL over Reklio's own tables

Every feature must be computable **from Reklio's own data at the moment the claim is filed**. No purchase history per person, no basket data — those do not exist by §1.

**Purchase-linkage** (the bearer-credential vector — strongest family):
- `prior_claims_on_purchase` — same transaction claimed twice
- `purchase_claimed_by_other_account` — **single best feature**; someone else already claimed it (found/stolen receipt, collusion)
- `distinct_accounts_on_purchase` — ring behaviour around one transaction

**Temporal:**
- `days_purchase_to_claim` — the receipt carries the date
- `warranty_period_used_pct` — days elapsed ÷ warranty length; normalizes across products with different warranty periods, catches expiry-clustering where raw day count would not
- `claimed_within_first_n_days` — suspiciously immediate claims

**Account behaviour** (Reklio's own records):
- `total_claims`
- `claims_last_30d` / `claims_last_90d`
- `mean_days_between_claims`
- `account_age_days` at claim time — new account filing immediately is a classic
- `prior_rejection_rate`

**Value / spread:**
- `claim_amount`, `amount_vs_user_mean`, `distinct_categories`, `distinct_stores`

**Explicitly excluded:** photo-reuse perceptual hashing, vision/description agreement, OCR confidence, OCR-vs-`Purchase` partial mismatch. Dropped to keep the simulator generating **claim streams as pure rows** — no image files to attach, no LLM-derived scores to simulate without circularity.

### Threshold

**Derived from the precision/recall curve**, not picked. Target a false-accusation rate justified by the business asymmetry: **wrongly accusing a real customer costs far more than approving a fraudulent 50 KM claim.** ~2h, since model and labels already exist. Turns a magic number into a defended decision.

### Known traps

- **Point-in-time correctness — the one most likely to bite.** Every aggregate must be computed *as of the claim's timestamp*, **excluding the claim itself**, and must not count claims that happened after it. Leaking the future makes AUC look wonderful and mean nothing. This bug is silent and easy to write.
- **Circularity, round two.** Any feature derived from another component must fall out of the latent type *noisily*, never be dialed in.
- **Feedback loop.** `prior_rejection_rate` makes the model's own past decisions its future inputs — real self-reinforcing bias. One free paragraph in limitations.
- **Channel-mix feature.** Tempting (an abuser who understands the system prefers the unverifiable physical channel) but only include if the preference **emerges** from simulation rather than being hard-coded. Otherwise the model just recovers the assumption.

---

## 4. Orchestration and decision logic

### Fixed pipeline, not an agent (~6h)

Deterministic: `validate → vision + RAG + fraud → decide`.

**Defense for the naming:** the workflow is fixed by business rules and consumer law, so autonomy adds risk without benefit in a compliance context. This is a real argument, not a dodge. A real agent loop (~15–20h, non-deterministic on stage, occasionally skips fraud scoring) was rejected — there is no slack for it.

### Deterministic gate, LLM-written explanation (~6h + ~2h)

Hardcoded thresholds decide. The LLM then explains the decision in natural language using the retrieved rules — satisfying the spec's *"generiše objašnjenje"* genuinely.

**Precision required in the write-up:** the inputs are still non-deterministic. The rule can only fire on structured values, so:
- Vision emits `{damage_confirmed, damage_type, severity}` — **structured, not prose**
- RAG needs an LLM step turning retrieved chunks into `{covered, applicable_exclusion}`

Both are LLM calls. **What is deterministic is the *combination*, not the pipeline.** Set `temperature=0` everywhere, use OpenAI structured-output/JSON-schema mode, and write **"near-reproducible"** — not "reproducible." The combination is auditable and traceable to exactly which condition failed; that is the real benefit.

### Conservative automation

- **Auto-approve:** clear low-risk only (~40–50%)
- **Auto-reject:** **hard failures only** — no matching `Purchase`, outside warranty window
- **Escalate:** everything else → Human-in-the-loop

**Never auto-reject on a soft signal.** A false fraud flag must never deny a real customer.

This preserves the spec's *"automatsko odobrenje/odbijanje"* honestly — hard failures *are* auto-rejections. Gives the operator panel plenty to demo. Supports an honest headline claim: *manual review roughly halved.*

---

## 5. RAG

### Corpus: fully invented, built adversarially (~10h incl. eval)

An invented company pravilnik is **not** a compromise — every company's production RAG runs over documents that company wrote. This is exactly what the real thing looks like.

**The real risk is triviality, not circularity.** If each query matches exactly one tidy document, retrieval is a dictionary lookup wearing a vector database, and there is no answer to *"why not just grep?"*

So the corpus is deliberately adversarial:

- **Lexical near-misses (most important).** A return/refund policy (*rok za povrat, 14 dana, neoštećena ambalaža*) sharing heavy vocabulary with the warranty policy but answering a different question. Keyword search returns it confidently; semantic retrieval should not.
- **Overlapping authority.** General warranty policy (24 months) plus a category-specific document overriding it (6 months for batteries). The answer depends on which document *wins*.
- **Contradicting exceptions.** User-caused damage excluded — *except* manufacturing defects presenting as physical damage. Forces reasoning over chunks rather than parroting one.
- **Volume:** ~15 documents → **100–150 chunks**. Below that, everything lands in top-k and retrieval never discriminates.

### Evaluation (~2h, take it)

**BM25 vs embeddings on decoy-present queries**, reported as a table. This is the ~2h that converts *"I used a vector DB"* into *"I showed why."*

### Embedding model — OpenAI `text-embedding-3-small`, NOT Chroma's default

**Verified in the week-1 spike.** ChromaDB's default embedding function silently downloads **`all-MiniLM-L6-v2` (79MB ONNX)** on first use — a hidden dependency that took ~14 minutes on a normal connection and would hang or fail on a fresh demo machine.

It is also **English-only**, and the corpus is Bosnian.

**Honest spike result:** it did *not* fail outright. Query `"koliko traje garancija na bateriju"` ranked correctly — `baterije` (0.71) → `garancija` (0.92) → `povrat` (1.09), decoy last. But the distances are weak (correct hit at 0.71, everything else near-orthogonal), and it almost certainly won on literal token overlap `bateriju`/`baterije` rather than meaning. Adequate on 3 toy documents; not on 100–150 chunks with deliberate lexical decoys (§5).

**Decision:** override with OpenAI `text-embedding-3-small` — genuinely multilingual, already in the stack, no local model, no download, no demo-day network dependency.

The multilingual-embedding question remains a legitimate thesis subsection; it just has a decided answer rather than an open risk.

---

## 6. Evidence corpora

The governing principle is **seeding direction**. Both directions are internally consistent; the fatal move is mixing them — seeding a row first and *hoping* a generated image agrees with it.

### Hero receipts — 5–8, image is upstream

AI-generated photorealistic receipts for maximum visual realism in the demo. Then: **generate → manually verify → seed the DB *from* the verified values.**

Verification is O(n) with n fixed and small — ~20–30 min total. **Not a scaling problem.**

**The real cost is re-verification churn, not n.** Change the schema, catalog, or currency format and the hero set is stale fixtures needing manual re-verification. **Freeze the hero receipts late and once** (week 7), after schema and catalog are settled. Never generate them in week 1.

**Verification checklist — beyond line-items-sum-to-total and legibility:**
- The product must **exist in the catalog**, be a **claimable durable good**, and have **warranty terms in the RAG corpus**. (A grocery receipt cannot support a reklamacija.) Escape hatch: generate freely, read what the model invented, then insert *that* product into the catalog with matching warranty rules — image drives catalog. Fine at n=5–8, but do it deliberately.
- Every hero transaction needs an **owner** — graft it onto an existing simulated user with a plausible history, or the fraud component has nothing to score. Demo one honest and one suspicious.
- **Verify blind.** Have a separate model transcribe cold, then diff against your reading. Two minutes per receipt; removes confirmation bias from the step all ground truth rests on.

### Bulk OCR test set — template is upstream

**Render HTML→PNG** (text pixel-exact by construction, correctness free, no per-image checking) then **degrade programmatically** — perspective warp, paper texture, lighting gradient, shadow, blur, JPEG artifacts. Use [Augraphy](https://github.com/sparkfish/augraphy).

**Citable:** synthetic document augmentation is standard practice in document-AI research (SROIE, DocVQA pipelines). *"I followed the standard augmentation approach from the document-understanding literature"* is a strong defense sentence.

Optionally print and photograph ~5 real ones to honestly claim OCR was tested on genuine photographs.

**Note:** bulk receipts serve OCR *development* in week 4; the hero set exists purely for the *demo*. This is why freezing hero in week 7 costs nothing.

### Damage photos — AI-generated, ~10–15

No text fidelity requirement, so the failure mode disappears. The prompt *is* the ground truth — ask for a cracked screen and the image genuinely contains a crack, so vision still does real work. Variety on demand; sidesteps the licensing mess of scraping real photos into a published thesis.

**Limitation to write down:** generated damage is unrealistically clean and well-lit; real claim photos are blurry, badly framed, ambiguous. Vision will look better in the thesis than in production. Saying so converts a gap into rigor.

Pool shrank from ~40–50 to ~10–15 once photo-reuse hashing was dropped (§3).

---

## 7. Remaining components

- **Chatbot (~6h)** — **RAG over the corpus only, no user context.** Answers policy questions (warranty length, exclusions, how to file). Reuses the RAG work entirely, matches the spec's wording exactly, **no auth surface to get wrong.** Claim-status lookup was rejected: scoping every lookup to the authenticated `user_id` is easy to get wrong, and a chatbot leaking other people's claims is far worse for an examiner to find than a missing feature.
- **Notifications (~4h)** — **in-app only.** `Notification` table, bell icon with unread count, status on claim detail. Zero external dependencies, nothing to configure on demo day. Email/SMTP rejected: works locally, dies on the demo machine's network.
- **Frontend (~28h)** — full Angular + Material. The minimal 4-screen option was **rejected**; more will be implemented if needed.

---

## 8. Explicitly rejected (recorded so they don't get relitigated)

| Rejected | Why |
|---|---|
| `user_id` on `Purchase` | Reklio is downstream of the purchase in **both** channels. A nullable FK would be a lie that half the code then dereferences. |
| `Trgovac` / multi-retailer B2B | Single omnichannel business. One company. |
| `kupac_email` + ownership-match feature | Over-engineering. A receipt photo and an order code are **both bearer credentials** — neither proves ownership, so the asymmetry it created was an artifact of the invented feature, not the domain. |
| Channel-based risk asymmetry | Same reason. The claim process is identical after validation. A clean invariant beats a special case, and it makes the §1 argument *stronger*. |
| Real agent loop | ~15–20h, non-deterministic on stage, hard to reproduce in writing, will silently skip fraud scoring. No slack. |
| LLM as decision-maker | Same claim → different outcomes across runs. Impossible to put a confusion matrix on. |
| Photo-reuse hashing / cross-modal features | Keeps the simulator generating pure rows — no images to attach, no LLM-derived scores to justify. |
| Minimal 4-screen frontend | User's call; the demo is the artifact under breadth grading. |
| Real BiH consumer law in the corpus | Fully invented chosen instead. Invented ≠ circular for retrieval. |
| Email notifications | External dependency on demo day. |

---

## 9. Build order

**Vertical slice with stubs, then deepen.**

The failure mode this avoids: building components sequentially, doing each well, and arriving at week 6 with a beautiful OCR pipeline, a solid fraud model, nothing connected, and no chatbot. Every hour well spent, no demo. Under breadth grading that is the worst outcome — and it's the *default* one.

**Define stub interfaces as real contracts from day one** — `IOcrService`, `IVisionService`, `IFraudService`, `IRagService`, each returning the actual DTO shape. Swapping a stub for a real implementation is then a one-line DI change and never a refactor. That is what makes the ~4h of throwaway code cheap rather than wasted.

From week 2 onward there is always a demoable system with every box nominally ticked. **Every overrun then costs depth, not a checkbox** — and depth is not what is graded.

### Schedule

| Week | Work |
|---|---|
| **1–2** | Schema, auth, C# API, FastAPI service, HTTP seam, basic Angular, **all AI components stubbed behind real interfaces**. End-to-end path works, ugly, demoable. |
| **3** | **Simulator + XGBoost.** Watch point-in-time leakage. |
| **4** | OCR against the rendered/degraded bulk corpus + online order-code lookup. |
| **5** | RAG: adversarial corpus, ChromaDB, BM25 comparison. |
| **6** | Vision (structured output), chatbot, decision gate + LLM explanation. |
| **7** | Notifications, operator panel, **hero receipts frozen here**, remaining buffer. |

### Why the simulator is deepened first

**It is a dependency, not a peer.** Every other component reads data it produces — without seeded `Purchase` rows OCR has nothing to validate against; without seeded claim history there are no features to score. A flawed simulator silently corrupts OCR validation and every demo case. It is also the highest-thesis-value component and holds the subtlest bug (point-in-time leakage), so it deserves the freshest hours.

---

## 10. Risk register

| Risk | Mitigation |
|---|---|
| **Point-in-time leakage** in fraud features. Silent; makes AUC look wonderful. | Compute every aggregate as of claim time, excluding the current claim. Test explicitly. |
| **ChromaDB / LangChain integration** — least-familiar dependency, sits at week 5 with ~15h buffer left. | **Spike a hello-world ChromaDB query in week 1** (~2h). Not building RAG — just proving the versions install and talk. |
| **11% buffer** on a polyglot stack with three first-time integrations. Thin; standard estimation wants 50%+. | Vertical-slice order means overruns cost depth, not checkboxes. |
| Frontend polish is infinitely expandable and buys zero marks. | Timebox it. |
| **OpenAI free tier will not work.** | ~$20–50 total across development (vision + OCR calls; embeddings are pennies). Cost is a non-issue — but **confirm a paid account in week 1**, not week 4. |

---

## 11. Backend architecture

### Pragmatic three-project layering (~2h setup)

```
Reklio.Api/             controllers, DI wiring, DTOs
Reklio.Core/            entities, service interfaces, pipeline orchestration, decision gate
Reklio.Infrastructure/  EF Core DbContext + migrations, HTTP clients to FastAPI
```

- `IOcrService`, `IVisionService`, `IFraudService`, `IRagService` live in **Core**.
- Their HTTP implementations live in **Infrastructure**, alongside the DbContext — correctly, because a FastAPI call and a database call are the same kind of thing: an external dependency.
- The pipeline (§4) and decision gate sit in **Core** as a domain service, depending only on interfaces.

**Why projects, not folders.** The whole build plan (§9) rests on one property: stubs swap for real implementations via a one-line DI change, never a refactor. A *project* boundary enforces it **at compile time** — Core cannot reference Infrastructure, so nothing can quietly `using` a concrete `OcrHttpClient` and weld the seam shut. With folders, nothing stops you, and the breakage surfaces in week 4 when the swap becomes a refactor there are no hours for.

**Defense line:** *dependency inversion at the infrastructure boundary, because the system's central technical requirement is swappable AI components — the layering exists to enforce exactly that, and nothing more.* An architecture chosen for a reason.

### Rejected

| Rejected | Why |
|---|---|
| Full Clean Architecture + MediatR + CQRS | 4 projects, a handler per operation, ~10–15h of ceremony amortized over ~15 endpoints and one developer. CQRS pays when reads/writes scale differently or teams work in parallel — neither is true here. Spends buffer on structure the project cannot cash in. |
| Repository layer over EF Core | `DbContext` is already a unit of work; `DbSet<T>` is already a repository. Wrapping them costs EF's query composition for nothing. **Caveat:** if the mentor expects to *see* a repository and would read its absence as ignorance rather than judgment, add it (~3h) and don't fight about it. |

### Asynchronous claim processing — `BackgroundService` + in-memory `Channel<T>` (~4h + ~1h recovery)

Submit returns **202 Accepted** immediately; a hosted `BackgroundService` drains a `Channel<T>` and runs the pipeline; the notification fires on completion.

**Why async is required, not preferred:** the pipeline is ~10–30s (OCR + vision + RAG + LLM explanation). Synchronous submission means a 30-second hold on stage with a live OpenAI dependency. More importantly — **the notification feature only has a purpose if processing is async.** If the result came back in the HTTP response there would be nothing to notify anyone about. The spec's own feature list implies this. `status: u obradi → obrađeno` is also a better demo beat than a spinner.

**Consequences:**
- `Claim.status` state machine (§2) becomes load-bearing.
- **Crash recovery (~1h, do it):** on startup, scan for claims stuck in `Processing` and requeue. Closes most of the in-memory queue's durability gap without the clunkiness of DB polling. Restarts are constant during a 7-week build; losing seed claims to a rebuild eats an afternoon before you understand why.
- **Angular polls** the claim detail endpoint every few seconds while status is `Processing`. Trivial, but real work that did not exist under the sync model.

**Limitation to state in writing:** an in-memory queue is single-instance and not durable across hard failures. Startup recovery mitigates but does not eliminate this. A real broker is future work.

**Rejected:** RabbitMQ / Azure Service Bus — a fourth integration on a stack with three first-time ones, sitting directly on the 15h buffer. SQL-backed queue with status polling — durable, but `Channel<T>` + startup recovery gets ~the same guarantee more cleanly.

---

## 12. Frontend architecture (Angular)

**Standalone components, signals for state, feature folders, no NgRx.**

```
src/app/
  core/          auth service, JWT interceptor, role guards
  features/
    claims/      submit, list, detail (polls while Processing)
    operator/    queue, decision view
    chat/        chatbot widget
  shared/        Material imports, DTO types
```

Lazy-loaded routes per feature. One typed HTTP service per backend resource. Role guards split customer from operator.

### Rejected

| Rejected | Why |
|---|---|
| **NgRx** | The canonical Angular overengineering trap — 10h+ of actions/reducers/effects/selectors for state that six screens and a few signals handle natively. Pays for large teams with complex cross-cutting state; neither applies. **Defense line:** *"signals plus services; application state is small and local, and NgRx would add ceremony without addressing any problem this system has."* |
| **NgModules** | Standalone is Angular's default now; NgModules read as legacy in a 2026 thesis. |

### Status polling (the only non-obvious piece)

```ts
timer(0, 3000).pipe(
  switchMap(() => api.get(id)),
  takeWhile(c => c.status === 'Processing', true)
)
```
**Stop polling on destroy** or intervals leak across navigations.

---

## 13. AI service architecture (Python)

```
app/
  main.py           FastAPI app; lifespan loads model.pkl + Chroma ONCE (not per-request)
  routers/          ocr.py, vision.py, rag.py, fraud.py
  services/         openai_client.py, rag_engine.py, fraud_model.py
  schemas/          pydantic models mirroring the C# DTOs
  config.py         pydantic-settings
tools/              OFFLINE ONLY: simulator.py, train.py
models/             model.pkl, chroma/
```

**The split that matters:** `tools/` is **offline and may touch the database**; `app/` is **online and must not**. The simulator needs numpy distributions and writes raw entities to SQL Server — a one-off seeding job, not a request path. The trainer reads data and writes `model.pkl`. Neither is part of the service. The service loads artifacts at startup and **holds no state**.

The simulator writes **raw entities only** — never features. Features are always derived (below).

### Feature computation — parameterized SQL, called by both paths

`fn_features(@reklamacija_id)` lives in **SQL Server**.

- **Training:** Python (`tools/train.py`) calls it per historical claim to build the CSV.
- **Serving:** C# calls it for the incoming claim and posts the vector to FastAPI.

**One implementation, zero training/serving skew by construction.**

**Why this matters:** the 12 features must be computed twice — once over historical claims for the training set, once at serving. If those live in different code they *will* drift: pandas and LINQ disagree on rounding, date boundaries, and null handling. The model then sees a different distribution than it trained on. **Training/serving skew is silent** — your AUC never shows it, because the AUC was measured on the training-side computation. Same class of bug as point-in-time leakage, and without this decision you'd have had two chances to hit it.

**Bonus:** SQL window functions handle point-in-time naturally, so **both traps die in one place**:
```sql
WHERE c2.user_id = c.user_id AND c2.submitted_at < c.submitted_at
```
Strictly less-than, self-excluded, relative to **the claim's own timestamp** — never `GETDATE()`.

**Ship it in an EF Core migration as raw SQL** — versioned, not hand-applied to a dev DB and forgotten on the demo machine.

### Feature-order safeguard (~30 min — cheapest insurance in the project)

One SQL implementation kills skew but **not order drift**. XGBoost consumes a *positional* vector; if SQL column order and training CSV order diverge, the model scores `claim_amount` as if it were `account_age_days`. No error, plausible scores, completely wrong.

- C# posts a **named dict**, never an array.
- FastAPI reorders by name against `model.feature_names_in_`.
- **Startup assertion** that expected names match the trained model. Fail loudly.
- **`train.py` MUST fit on a pandas DataFrame, not a numpy array.** Verified in the week-1 spike: fitting on numpy leaves `feature_names_in_` *absent*, so the assertion above silently does not exist rather than failing loudly. One line; the whole safeguard depends on it.

### Rejected

| Rejected | Why |
|---|---|
| Python computes features both times (FastAPI gets DB access) | Nicest code, no skew — but the service stops being stateless, needs a connection string and SQLAlchemy, and the schema gains a second owner in a second language. A real coupling to maintain for 7 weeks. |
| C# at serving, Python/pandas at training | **The naive default you'd drift into without deciding.** Silent skew bug that no test you'd think to write would catch. Explicitly rejected. |
| C# computes both + exports CSV | No skew, but point-in-time window logic is painful in LINQ and you'd drop to raw SQL anyway — at which point it's the chosen option, arrived at accidentally. |

### Contract drift (C# DTO ↔ pydantic)

Hand-maintained across ~5 endpoints; accepted. Optional (~2h): generate C# clients from FastAPI's OpenAPI schema via NSwag to remove drift entirely. Marginal at this endpoint count.

---

## 14. Still open (resolve on contact; none change the above)

Product catalog contents · auth depth (real vs stubbed) · demo machine / deployment target · evaluation metrics for OCR and vision (thesis deadline is separate).
