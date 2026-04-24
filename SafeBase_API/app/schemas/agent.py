from pydantic import BaseModel
from typing import Any, Optional


class AgentPayload(BaseModel):
    agent_id: str
    timestamp: str
    payload_type: str
    payload_data: Any
    metadata: Optional[dict] = None


class AgentStatusResponse(BaseModel):
    agent_id: str
    status: str
    last_seen: str
