from contextlib import asynccontextmanager

from fastapi import FastAPI

from .routers import chat, explain, fraud, ocr, policy, vision
from .services.model_service import model_service
from .services.rag_service import rag_service


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup asertacija (T5.7): puknuće ako model nema imena ili se ne poklapaju.
    model_service.load()
    rag_service.load()
    yield


app = FastAPI(title="Reklio AI", lifespan=lifespan)
app.include_router(fraud.router)
app.include_router(ocr.router)
app.include_router(policy.router)
app.include_router(vision.router)
app.include_router(explain.router)
app.include_router(chat.router)


@app.get("/health")
def health():
    return {
        "status": "ok",
        "features": len(model_service.feature_names),
        "threshold": model_service.threshold,
    }
