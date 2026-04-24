import logging

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from app.routers import auth, health, agents, ia
from app.middleware.auth_middleware import AuthMiddleware
from app.services.apikey_service import api_key_service
from app.db.session import engine
from app.db.base import Base

logger = logging.getLogger("safebase_api")
logging.basicConfig(level=logging.INFO)

app = FastAPI(
    title="SafeBase API",
    version="0.1.0",
    description="SafeBase central API with JWT and API Key authentication",
    openapi_url="/openapi.json",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.add_middleware(AuthMiddleware, api_key_service=api_key_service)

app.include_router(health.router)
app.include_router(auth.router)
app.include_router(agents.router)
app.include_router(ia.router)


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    body = await request.body()
    body_text = body.decode("utf-8", errors="replace")
    error_details = exc.errors()
    logger.error(
        "Validation error on %s from %s: %s",
        request.url.path,
        request.client.host if request.client else "unknown",
        error_details,
    )
    logger.error("Request body: %s", body_text)
    return JSONResponse(
        status_code=422,
        content={"detail": error_details, "body": body_text},
    )


@app.on_event("startup")
async def startup_event():
    Base.metadata.create_all(bind=engine)


@app.get("/")
async def root():
    return {"message": "SafeBase API - FastAPI running"}
