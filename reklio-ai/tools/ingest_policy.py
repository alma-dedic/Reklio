"""

Ingest pravilnika u ChromaDB. Chunk po članu (Markdown ##) + dalje po veličini.

VAŽNO: embedding je eksplicitno OpenAI text-embedding-3-small — NIKAD Chroma
default (all-MiniLM-L6-v2, engleski-only, tiho skida ~79MB).
"""

import glob
import os
import shutil
import sys

from langchain_chroma import Chroma
from langchain_openai import OpenAIEmbeddings
from langchain_text_splitters import (
    MarkdownHeaderTextSplitter,
    RecursiveCharacterTextSplitter,
)

HERE = os.path.dirname(os.path.abspath(__file__))
REKLIO_AI = os.path.dirname(HERE)
REPO_ROOT = os.path.dirname(REKLIO_AI)
sys.path.insert(0, REKLIO_AI)

from app.config import (  # noqa: E402
    CHROMA_COLLECTION, CHROMA_DIR, EMBEDDING_MODEL, OPENAI_API_KEY,
)

DOCS = os.path.join(REPO_ROOT, "docs")


def collect_files():
    files = [os.path.join(DOCS, "pravilnik_reklamacije_i_garancija.md")]
    files += sorted(glob.glob(os.path.join(DOCS, "policy", "*.md")))
    return files


def build_chunks(files):
    md_splitter = MarkdownHeaderTextSplitter(
        headers_to_split_on=[("#", "doc_title"), ("##", "article")],
        strip_headers=False,
    )
    char_splitter = RecursiveCharacterTextSplitter(chunk_size=320, chunk_overlap=70)

    chunks = []
    for path in files:
        text = open(path, encoding="utf-8").read()
        source = os.path.basename(path)
        sections = md_splitter.split_text(text)
        for section in sections:
            section.metadata["source"] = source
        chunks.extend(char_splitter.split_documents(sections))

    # Chroma dozvoljava samo skalarne metapodatke (str/int/float/bool).
    for chunk in chunks:
        chunk.metadata = {k: str(v) for k, v in chunk.metadata.items() if v is not None}
    return chunks


def main():
    files = collect_files()
    chunks = build_chunks(files)

    if os.path.exists(CHROMA_DIR):
        shutil.rmtree(CHROMA_DIR)

    embeddings = OpenAIEmbeddings(model=EMBEDDING_MODEL, api_key=OPENAI_API_KEY)
    Chroma.from_documents(
        chunks,
        embedding=embeddings,
        persist_directory=CHROMA_DIR,
        collection_name=CHROMA_COLLECTION,
    )
    print(f"Dokumenata: {len(files)}")
    print(f"Chunkova ingestovano: {len(chunks)}")
    print(f"Embedding: {EMBEDDING_MODEL}")
    print(f"Chroma: {CHROMA_DIR}")


if __name__ == "__main__":
    main()
