import json
import logging

from fastapi import APIRouter, Depends, HTTPException, status
from app.core.dependencies import require_api_key, require_auth
from app.schemas.agent import AgentPayload, AgentStatusResponse
from app.services.ingestion_service import IngestionService
from app.services.normalization_service import NormalizationService

logger = logging.getLogger("safebase_api.agents")
router = APIRouter(tags=["Agents"])

ingestion_service = IngestionService()
normalization_service = NormalizationService()

@router.post("/ingest/agent-data", response_model=AgentStatusResponse)
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
    
    # ✅ CHAMADA AO SERVIÇO DE INGESTÃO
    try:
        result = ingestion_service.persist_agent_payload(payload_dict)
        return AgentStatusResponse(
            status="success",
            message="Data ingested successfully",
            agent_id=payload.agent_id,
            details=result
        )
    except HTTPException as e:
        logger.error(f"HTTP error during ingestion: {e.detail}")
        raise e
    except Exception as e:
        logger.error(f"Unexpected error during ingestion: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to ingest data: {str(e)}"
        )


@router.post("/normalize/run")
async def run_normalization(auth=Depends(require_api_key)):
    normalization_service.run_once()
    return {"status": "ok", "message": "Normalization cycle executed"}