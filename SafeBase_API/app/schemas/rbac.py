from datetime import datetime
from typing import List, Optional

from pydantic import BaseModel, Field


class RoleCreate(BaseModel):
    name: str = Field(..., max_length=128)
    description: Optional[str] = Field(None, max_length=512)
    is_active: bool = True


class RoleUpdate(BaseModel):
    description: Optional[str] = Field(None, max_length=512)
    is_active: Optional[bool] = None


class RoleResponse(BaseModel):
    id: int
    name: str
    description: Optional[str] = None
    is_active: bool
    created_at: datetime
    permissions: List[str] = []


class PermissionCreate(BaseModel):
    code: str = Field(..., max_length=128)
    description: Optional[str] = Field(None, max_length=512)


class PermissionResponse(BaseModel):
    id: int
    code: str
    description: Optional[str] = None
    created_at: datetime


class RolePermissionsAssign(BaseModel):
    permission_codes: List[str]


class UserRolesAssign(BaseModel):
    role_names: List[str]
