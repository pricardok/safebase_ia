from datetime import timedelta
from fastapi import APIRouter, Depends, HTTPException, Request, status
from app.core.dependencies import require_jwt
from app.schemas.token import LogoutRequest, RefreshTokenRequest, RefreshTokenResponse, TokenResponse
from app.schemas.user import UserLogin, ApiKeyCreate, ApiKeyResponse, UserMeResponse
from app.db import models
from app.services.auth_service import AuthService
from app.services.apikey_service import api_key_service
from app.db.session import get_db

router = APIRouter(tags=["Auth"])

auth_service = AuthService()

@router.post("/auth/login", response_model=TokenResponse)
async def login(form_data: UserLogin, db=Depends(get_db)):
    user = auth_service.authenticate(db, form_data.username, form_data.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid credentials",
            headers={"WWW-Authenticate": "Bearer"},
        )

    access_token = auth_service.create_access_token(
        subject=user["username"],
        roles=user.get("roles", []),
        permissions=user.get("permissions", []),
    )
    refresh_token = auth_service.issue_refresh_token(db, user["id"])
    return {
        "access_token": access_token,
        "token_type": "bearer",
        "roles": user.get("roles", []),
        "permissions": user.get("permissions", []),
        "refresh_token": refresh_token,
    }

@router.post("/auth/api-keys", response_model=ApiKeyResponse, dependencies=[Depends(require_jwt)])
async def create_api_key(data: ApiKeyCreate):
    api_key = api_key_service.create_api_key(name=data.name, scopes=data.scopes)
    return api_key


@router.post("/auth/refresh", response_model=RefreshTokenResponse)
async def refresh_token(payload: RefreshTokenRequest, db=Depends(get_db)):
    new_access_token = auth_service.refresh_access_token(db, payload.refresh_token)
    if not new_access_token:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid refresh token")
    return RefreshTokenResponse(access_token=new_access_token)


@router.post("/auth/logout")
async def logout(payload: LogoutRequest, request: Request, db=Depends(get_db)):
    auth_state = request.state.auth
    if auth_state.get("type") != "jwt":
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="JWT token required")

    username = auth_state.get("payload", {}).get("sub")
    if not username:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid token")

    user = db.query(models.User).filter(models.User.username == username).first()
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    if payload.refresh_token:
        revoked = auth_service.revoke_refresh_token(db, payload.refresh_token)
        if not revoked:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Refresh token not found")
    else:
        auth_service.revoke_all_refresh_tokens(db, user.id)

    return {"message": "Logout successful"}


@router.get("/auth/me", response_model=UserMeResponse, dependencies=[Depends(require_jwt)])
async def auth_me(request: Request, db=Depends(get_db)):
    auth_state = request.state.auth
    payload = auth_state.get("payload", {})
    username = payload.get("sub")
    if not username:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid token")

    user = auth_service.get_user_with_permissions(db, username)
    if not user:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="User not found")

    return UserMeResponse(
        id=user["id"],
        username=user["username"],
        email=user["email"],
        full_name=user["full_name"],
        is_active=user["is_active"],
        roles=user.get("roles", []),
        permissions=user.get("permissions", []),
        categorias=user.get("categorias", []),
    )
