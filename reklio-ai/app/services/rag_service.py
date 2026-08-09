import json

from langchain_chroma import Chroma
from langchain_openai import OpenAIEmbeddings
from openai import OpenAI

from ..config import (
    CHROMA_COLLECTION, CHROMA_DIR, EMBEDDING_MODEL, OPENAI_API_KEY, OPENAI_MODEL,
)
from ..schemas.policy import PolicyResponse

_SCHEMA = {
    "type": "object",
    "properties": {
        "covered": {"type": "boolean"},
        "applicable_exclusion": {"type": ["string", "null"]},
        "cited_article": {"type": ["string", "null"]},
        "answer": {"type": "string"},
    },
    "required": ["covered", "applicable_exclusion", "cited_article", "answer"],
    "additionalProperties": False,
}

_SYSTEM = (
    "Ti si asistent za pravilnik o reklamacijama i garanciji. Odgovaraš ISKLJUČIVO na "
    "osnovu priloženih članova. "
    "covered=true SAMO ako je konkretan slučaj stvarno pokriven (garancija važi ili je "
    "povrat dozvoljen). Ako se primjenjuje bilo koji izuzetak (mehaničko oštećenje, "
    "istekao rok, nepravilna upotreba...), covered=false i popuni applicable_exclusion; "
    "ako nema izuzetka, applicable_exclusion je null. "
    "Ako odgovor nije u kontekstu, covered=false i objasni. "
    "Pazi na razliku garancija/reklamacija naspram povrata/odustanka — dijele riječi ali "
    "su različite. Kod kolizije opšteg pravila i izuzetka, primijeni izuzetak. "
    "cited_article citiraj iz oznaka [izvor | član], u formatu 'izvor — Član X'."
)


class RagService:
    def __init__(self):
        self._retriever = None
        self._client = None

    def load(self):
        embeddings = OpenAIEmbeddings(model=EMBEDDING_MODEL, api_key=OPENAI_API_KEY)
        store = Chroma(
            persist_directory=CHROMA_DIR,
            collection_name=CHROMA_COLLECTION,
            embedding_function=embeddings,
        )
        self._retriever = store.as_retriever(search_kwargs={"k": 5})
        self._client = OpenAI(api_key=OPENAI_API_KEY)

    def answer(self, question: str, category: str | None, issue_type: str | None) -> PolicyResponse:
        query = question
        if category:
            query += f" (kategorija: {category})"
        if issue_type:
            query += f" (tip problema: {issue_type})"

        chunks = self._retriever.invoke(query)
        context = "\n\n".join(
            f"[{c.metadata.get('source', '?')} | {c.metadata.get('article', '')}]\n{c.page_content}"
            for c in chunks
        )

        response = self._client.chat.completions.create(
            model=OPENAI_MODEL,
            response_format={
                "type": "json_schema",
                "json_schema": {"name": "policy", "strict": True, "schema": _SCHEMA},
            },
            messages=[
                {"role": "system", "content": _SYSTEM},
                {"role": "user", "content":
                    f"PITANJE:\n{question}\n\nČLANOVI PRAVILNIKA:\n{context}"},
            ],
        )
        return PolicyResponse(**json.loads(response.choices[0].message.content))


rag_service = RagService()
