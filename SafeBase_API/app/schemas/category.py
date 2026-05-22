from datetime import datetime
from typing import List, Optional

from pydantic import BaseModel, Field


class CategoryCreate(BaseModel):
    codigo: str = Field(..., max_length=128)
    nome: str = Field(..., max_length=256)
    descricao: Optional[str] = Field(None, max_length=512)
    ativo: bool = True


class CategoryUpdate(BaseModel):
    nome: Optional[str] = Field(None, max_length=256)
    descricao: Optional[str] = Field(None, max_length=512)
    ativo: Optional[bool] = None


class CategoryResponse(BaseModel):
    id: int
    codigo: str
    nome: str
    descricao: Optional[str]
    ativo: bool
    criado_em: Optional[datetime] = None


class UserCategoryAssign(BaseModel):
    categoria_codigos: List[str]
