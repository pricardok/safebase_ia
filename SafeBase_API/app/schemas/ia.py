from pydantic import BaseModel
from typing import Any, Dict, Optional

from app.schemas.charts import ChartPayload


class IAQueryRequest(BaseModel):
    query: str
    context: Dict[str, Any]
    agent_id: str
    categoria_codigo: Optional[str] = None
    mode: Optional[str] = None


class IAQueryResponse(BaseModel):
    query: str
    response: Optional[str] = None
    result: Optional[Dict[str, Any]] = None
    chart: Optional[ChartPayload] = None
    insights: Optional[str] = None
    message_id: Optional[int] = None


class IAKeyCreateRequest(BaseModel):
    provider_name: Optional[str] = None
    provider_id: Optional[int] = None
    api_key: str
    descricao: Optional[str] = None
    ativa: Optional[bool] = True
    prioridade: Optional[int] = 1
    metadados: Optional[Dict[str, Any]] = None


class IAKeyCreateResponse(BaseModel):
    id: int
    provider_id: int
    provider_name: str
    hash_chave: str
    descricao: Optional[str]
    ativa: bool
    prioridade: int
