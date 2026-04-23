from pydantic import BaseModel
from typing import List, Optional


class UserLogin(BaseModel):
    username: str
    password: str


class UserCreate(BaseModel):
    username: str
    email: str
    full_name: Optional[str] = None
    password: str


class ApiKeyCreate(BaseModel):
    name: str
    scopes: Optional[List[str]] = ["default"]


class ApiKeyResponse(BaseModel):
    name: str
    key: str
    scopes: Optional[List[str]]
