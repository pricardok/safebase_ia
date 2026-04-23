from pydantic import BaseModel
from typing import Dict, Any


class IAQueryRequest(BaseModel):
    query: str
    context: Dict[str, Any]
    agent_id: str


class IAQueryResponse(BaseModel):
    query: str
    result: Dict[str, Any]
