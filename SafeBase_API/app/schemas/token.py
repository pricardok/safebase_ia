from pydantic import BaseModel
from typing import List, Optional


class TokenResponse(BaseModel):
    access_token: str
    token_type: str
    roles: Optional[List[str]] = None
    permissions: Optional[List[str]] = None
    refresh_token: Optional[str] = None


class TokenPayload(BaseModel):
    sub: str
    exp: int
    scope: str
    roles: Optional[List[str]] = None
    permissions: Optional[List[str]] = None


class RefreshTokenRequest(BaseModel):
    refresh_token: str


class RefreshTokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"


class LogoutRequest(BaseModel):
    refresh_token: Optional[str] = None
