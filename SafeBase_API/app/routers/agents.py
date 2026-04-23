from fastapi import APIRouter, Depends
from app.core.dependencies import require_auth
from app.schemas.agent import AgentPayload, AgentStatusResponse
from app.services.ingestion_service import IngestionService

router = APIRouter()

ingestion_service = IngestionService()

@router.post("/ingest/agent-data")
async def ingest_agent_data(payload: AgentPayload, auth=Depends(require_auth)):
    result = ingestion_service.persist_agent_payload(payload.dict())
    return {"status": "processed", "result": result}

@router.get("/agents/{agent_id}/status", response_model=AgentStatusResponse)
async def get_agent_status(agent_id: str, auth=Depends(require_auth)):
    return {"agent_id": agent_id, "status": "active", "last_seen": "2026-04-23T00:00:00Z"}
