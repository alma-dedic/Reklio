from contextlib import asynccontextmanager

from fastapi import FastAPI

from .routers import fraud
from .services.model_service import model_service


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup asertacija (T5.7): puknuće ako model nema imena ili se ne poklapaju.
    model_service.load()
    yield


app = FastAPI(title="Reklio AI", lifespan=lifespan)
app.include_router(fraud.router)


@app.get("/health")
def health():
    return {
        "status": "ok",
        "features": len(model_service.feature_names),
        "threshold": model_service.threshold,
    }
