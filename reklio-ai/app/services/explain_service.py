import json

from openai import OpenAI

from ..config import OPENAI_API_KEY, OPENAI_MODEL
from ..schemas.explain import ExplainRequest, ExplainResponse

_SCHEMA = {
    "type": "object",
    "properties": {
        "user_text": {"type": "string"},
        "operator_text": {"type": "string"},
    },
    "required": ["user_text", "operator_text"],
    "additionalProperties": False,
}

_SYSTEM = (
    "Ti objašnjavaš VEĆ DONESENU odluku o reklamaciji. Odluku su donijela "
    "deterministička pravila i ona je KONAČNA — ne preispituj je, ne predlaži drugačiju, "
    "samo je jasno objasni. Pišeš na bosanskom. Vrati dva teksta:\n"
    "user_text — za korisnika: kratko, jednostavno, ljudski; bez žargona, brojeva rizika "
    "i imena modela. Reci šta je odlučeno i šta to znači za njega, i šta je sljedeći korak.\n"
    "operator_text — za operatera: sažeto i tehnički. Navedi ključne signale (validacija "
    "računa, rizik naspram praga, vizuelni nalaz) i OBAVEZNO citiraj RAG nalaz (član "
    "pravilnika i eventualni izuzetak) ako je priložen."
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
