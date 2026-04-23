from pydantic import BaseModel
from typing import Any, Dict, Optional


class AgentPayload(BaseModel):
    agent_id: str
    timestamp: str
    payload_type: str
    payload_data: Dict[str, Any]
    metadata: Optional[Dict[str, Any]] = None


class AgentStatusResponse(BaseModel):
    agent_id: str
    status: str
    last_seen: str
