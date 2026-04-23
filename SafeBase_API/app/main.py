from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routers import auth, health, agents, ia
from app.middleware.auth_middleware import AuthMiddleware
from app.services.apikey_service import api_key_service
from app.db.session import engine
from app.db.base import Base

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


@app.on_event("startup")
async def startup_event():
    Base.metadata.create_all(bind=engine)


@app.get("/")
async def root():
    return {"message": "SafeBase API - FastAPI running"}
