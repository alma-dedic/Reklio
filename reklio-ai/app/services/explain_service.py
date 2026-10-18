import json

from openai import OpenAI

from ..config import OPENAI_API_KEY, OPENAI_MODEL
from ..schemas.explain import ExplainRequest, ExplainResponse

_SCHEMA = {
    "type": "object",
    "properties": {
        "recommendation": {"type": "string", "enum": ["approve", "reject"]},
        "operator_text": {"type": "string"},
        "customer_reason": {"type": "string"},
    },
    "required": ["recommendation", "operator_text", "customer_reason"],
    "additionalProperties": False,
}

_SYSTEM = (
    "Reklamacija je EskalIRANA — deterministička pravila nisu mogla sama presuditi, pa je "
    "pregleda operater. Ti si mu asistent: NE odlučuješ, nego PREPORUČUJEŠ i pripremaš "
    "tekst. Odluku donosi operater. Pišeš na bosanskom. Vrati tri polja:\n"
    "recommendation — 'approve' ili 'reject', preporuka smjera na osnovu signala:\n"
    "  • potvrđeno oštećenje + pokriveno pravilnikom + bez izuzetka (eventualna briga samo "
    "rizik) → 'approve'\n"
    "  • mogući izuzetak / oštećenje nije potvrđeno / slika ne odgovara proizvodu → 'reject'\n"
    "VAŽNO za tip problema 'Proizvod ne radi / kvar': tu NEMA fotografija ni vizuelnog "
    "nalaza jer se kvar ne vidi na slici — odsustvo vizuelnog nalaza NIJE razlog za "
    "odbijanje. Preporuku donosi na osnovu pokrivenosti pravilnikom i rizika, a ispravnost "
    "se utvrđuje provjerom u ovlaštenom servisu.\n"
    "Za tip 'Ostalo' (npr. nedostaje dio, pogrešan proizvod): slučaj nije modeliran — osloni "
    "se na opis kupca i pravilnik, ne na vizuelni nalaz.\n"
    "operator_text — za operatera, jedna do dvije rečenice u formatu: 'S obzirom na "
    "[ključni signali ljudski] i prema AI analizi, preporučuje se [ODOBRITI/ODBITI] — "
    "ukoliko vašim pregledom ne utvrdite drugačije.' Sažeto. NE navodi nazive datoteka "
    "pravilnika ni brojeve članova (to je već prikazano u analizi) — pozovi se na signale "
    "ljudski (npr. 'potvrđeno oštećenje', 'mogući izuzetak iz pravilnika', 'visok rizik').\n"
    "customer_reason — za kupca, kratko i ljudski: obrazloženje za PREPORUČENI ishod "
    "(zašto prihvaćeno ako je 'approve', zašto odbijeno ako je 'reject'). Bez žargona, "
    "brojeva rizika i imena modela. STROGO ZABRANJENO tražiti dodatne fotografije, "
    "dokumente ili datume, i navoditi rokove (48 sati, 14 dana)."
)


def _build_context(req: ExplainRequest) -> str:
    lines = [
        f"ODLUKA: {req.decision} (razlog: {req.reason_code})",
        f"Faktori: {'; '.join(req.factors) if req.factors else '—'}",
        f"Problem: {req.issue_type} — {req.issue_description}",
        f"Proizvod: {req.product_name or '?'} ({req.product_category or '?'})",
        f"Validacija računa: {req.validation_status or 'nije rađena'}",
        f"Garancija istekla: {'da' if req.warranty_expired else 'ne'}",
        f"Rizik: {req.risk_score:.4f} (prag {req.risk_threshold:.4f})",
    ]
    if req.damage_confirmed is not None:
        lines.append(
            f"Vizuelni nalaz: oštećenje={'da' if req.damage_confirmed else 'ne'}, "
            f"tip={req.damage_type}, ozbiljnost={req.severity}"
        )
    if req.policy_covered is not None:
        lines.append(f"RAG pokrivenost: {'pokriveno' if req.policy_covered else 'nije pokriveno'}")
    if req.applicable_exclusion:
        lines.append(f"RAG izuzetak: {req.applicable_exclusion}")
    if req.cited_article:
        lines.append(f"RAG citat: {req.cited_article}")
    if req.policy_answer:
        lines.append(f"RAG obrazloženje: {req.policy_answer}")
    return "\n".join(lines)


class ExplanationService:
    def __init__(self):
        self._client = OpenAI(api_key=OPENAI_API_KEY)

    def explain(self, req: ExplainRequest) -> ExplainResponse:
        # gpt-5.6-luna podržava samo temperature=1 (default), pa ga ne postavljamo.
        response = self._client.chat.completions.create(
            model=OPENAI_MODEL,
            response_format={
                "type": "json_schema",
                "json_schema": {"name": "explanation", "strict": True, "schema": _SCHEMA},
            },
            messages=[
                {"role": "system", "content": _SYSTEM},
                {"role": "user", "content": _build_context(req)},
            ],
        )
        return ExplainResponse(**json.loads(response.choices[0].message.content))


explanation_service = ExplanationService()
