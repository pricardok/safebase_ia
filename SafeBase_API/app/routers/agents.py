import json
import logging

from fastapi import APIRouter, Depends
from app.core.dependencies import require_auth
from app.schemas.agent import AgentPayload, AgentStatusResponse
from app.services.ingestion_service import IngestionService

logger = logging.getLogger("safebase_api.agents")
router = APIRouter()

ingestion_service = IngestionService()

@router.post("/ingest/agent-data")
async def ingest_agent_data(payload: AgentPayload, auth=Depends(require_auth)):
    payload_dict = payload.dict()
    try:
        payload_info = {
            "agent_id": payload_dict.get("agent_id"),
            "payload_type": payload_dict.get("payload_type"),
            "payload_data_type": type(payload_dict.get("payload_data")).__name__,
            "payload_data_keys": list(payload_dict.get("payload_data", {}).keys()) if isinstance(payload_dict.get("payload_data"), dict) else None,
            "metadata": payload_dict.get("metadata"),
        }
    except Exception:
        payload_info = {"agent_id": payload_dict.get("agent_id"), "payload_type": payload_dict.get("payload_type")}

    logger.info("Received ingest payload: %s", json.dumps(payload_info, ensure_ascii=False, default=str))
