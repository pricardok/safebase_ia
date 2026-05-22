from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field


class ExternalSourceCreate(BaseModel):
    nome: str = Field(..., max_length=128)
    tipo: str = Field(..., max_length=64)
    configuracao: Optional[str] = None
    ativo: bool = True


class ExternalSourceUpdate(BaseModel):
    nome: Optional[str] = Field(None, max_length=128)
    tipo: Optional[str] = Field(None, max_length=64)
    configuracao: Optional[str] = None
    ativo: Optional[bool] = None


class ExternalSourceResponse(BaseModel):
    id: int
    nome: str
    tipo: str
    configuracao: Optional[str]
    ativo: bool
    criado_em: datetime
    atualizado_em: datetime
