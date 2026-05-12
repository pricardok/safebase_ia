# app/schemas/agent.py
from typing import Optional, Dict, Any
from pydantic import BaseModel
from datetime import datetime

class AgentPayload(BaseModel):
    agent_id: str
    timestamp: Optional[str] = None
    payload_type: str
    payload_data: Dict[str, Any]
    metadata: Optional[Dict[str, Any]] = {}

class AgentStatusResponse(BaseModel):
    status: str
    message: str
    agent_id: str
    details: Optional[Dict[str, Any]] = None