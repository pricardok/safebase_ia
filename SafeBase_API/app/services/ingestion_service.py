import json
from datetime import datetime
from typing import Any, Dict, Optional

from fastapi import HTTPException, status

from app.core.config import settings
from app.db.session import SessionLocal
from app.db.models import Agent, AgentPayload


class IngestionService:
    def persist_agent_payload(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        batch_size_limit = settings.max_ingestion_batch_size or 100
        payload_data = payload.get("payload_data", {})
        payload_count = self._count_payload_items(payload_data)

        if payload_count > batch_size_limit:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail=(
                    f"Payload contains {payload_count} records, which exceeds the "
                    f"MAX_INGESTION_BATCH_SIZE limit of {batch_size_limit}. "
                    "Please split the payload into smaller batches."
                ),
            )

        db = SessionLocal()
        try:
            agent_id = payload.get("agent_id")
            agent = db.query(Agent).filter(Agent.agent_id == agent_id).first()
            if not agent:
                agent = Agent(agent_id=agent_id, status="active")
                db.add(agent)
                db.flush()

            coletado_em = None
            timestamp_value = payload.get("timestamp")
            if timestamp_value:
                try:
                    coletado_em = datetime.fromisoformat(timestamp_value)
                except ValueError:
                    coletado_em = None

            payload_record = AgentPayload(
                agent_id=agent.agent_id,
                tipo_payload=payload.get("payload_type", "unknown"),
                dados_payload=json.dumps(payload_data, ensure_ascii=False),
                metadata_json=json.dumps(payload.get("metadata", {}), ensure_ascii=False),
                coletado_em=coletado_em,
            )
            db.add(payload_record)
            db.commit()

            return {
                "agent_id": agent.agent_id,
                "payload_id": payload_record.id,
                "processed_at": payload_record.recebido_em.isoformat(),
                "payload_type": payload_record.tipo_payload,
                "record_count": payload_count,
                "summary": "Payload received and saved to database",
            }
        finally:
            db.close()

    def _count_payload_items(self, payload_data: Any) -> int:
        if isinstance(payload_data, list):
            return len(payload_data)
        if isinstance(payload_data, dict):
            if "logins" in payload_data and isinstance(payload_data["logins"], list):
                return len(payload_data["logins"])
            if "items" in payload_data and isinstance(payload_data["items"], list):
                return len(payload_data["items"])
            list_values = [v for v in payload_data.values() if isinstance(v, list)]
            if list_values:
                return max(len(v) for v in list_values)
        return 1
