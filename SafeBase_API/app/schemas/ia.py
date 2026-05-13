from pydantic import BaseModel
from typing import Dict, Any, Optional


class IAQueryRequest(BaseModel):
    query: str
    context: Dict[str, Any]
    agent_id: str


class IAQueryResponse(BaseModel):
    query: str
    result: Dict[str, Any]


class IAKeyCreateRequest(BaseModel):
    provider_name: Optional[str] = None
    provider_id: Optional[int] = None
    api_key: str
    descricao: Optional[str] = None
    ativa: Optional[bool] = True
    metadados: Optional[Dict[str, Any]] = None


class IAKeyCreateResponse(BaseModel):
    id: int
    provider_id: int
    provider_name: str
    hash_chave: str
    descricao: Optional[str]
    ativa: bool
