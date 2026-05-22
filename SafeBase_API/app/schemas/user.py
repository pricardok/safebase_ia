from pydantic import BaseModel, EmailStr, Field
from typing import List, Optional


class UserLogin(BaseModel):
    username: str
    password: str


class UserCreate(BaseModel):
    username: str = Field(..., max_length=128)
    email: EmailStr
    full_name: Optional[str] = Field(None, max_length=256)
    password: str = Field(..., min_length=8, max_length=128)
    role_names: Optional[List[str]] = None


class UserResponse(BaseModel):
    id: int
    username: str
    email: EmailStr
    full_name: Optional[str] = None
    is_active: bool
    roles: List[str] = []


class UserMeResponse(UserResponse):
    permissions: List[str] = []
    categorias: List[str] = []


class UserAdminResponse(UserResponse):
    categorias: List[str] = []


class ApiKeyCreate(BaseModel):
    name: str
    scopes: Optional[List[str]] = ["default"]


class ApiKeyResponse(BaseModel):
    name: str
    key: str
    scopes: Optional[List[str]]
