from datetime import timedelta
from fastapi import APIRouter, Depends, HTTPException, status
from app.core.dependencies import require_jwt
from app.schemas.token import TokenResponse
from app.schemas.user import UserLogin, ApiKeyCreate, ApiKeyResponse
from app.services.auth_service import AuthService
from app.services.apikey_service import api_key_service

router = APIRouter()

auth_service = AuthService()

@router.post("/auth/login", response_model=TokenResponse)
async def login(form_data: UserLogin):
    user = auth_service.authenticate(form_data.username, form_data.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid credentials",
            headers={"WWW-Authenticate": "Bearer"},
        )

    access_token = auth_service.create_access_token(subject=user["username"])
    return {"access_token": access_token, "token_type": "bearer"}

@router.post("/auth/api-keys", response_model=ApiKeyResponse, dependencies=[Depends(require_jwt)])
async def create_api_key(data: ApiKeyCreate):
    api_key = api_key_service.create_api_key(name=data.name, scopes=data.scopes)
    return api_key
