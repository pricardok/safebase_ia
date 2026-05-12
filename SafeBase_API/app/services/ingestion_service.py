import json
import logging
from datetime import datetime
from typing import Any, Dict, Optional, List

from fastapi import HTTPException, status

from app.core.config import settings
from app.db.session import SessionLocal
from app.db.models import Agent, AgentPayload

logger = logging.getLogger("safebase_api.ingestion")


class IngestionService:
    def persist_agent_payload(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        batch_size_limit = settings.max_ingestion_batch_size or 100
        payload_data = payload.get("payload_data", {})
        payload_items = self._extract_payload_items(payload_data)
        payload_count = len(payload_items) if payload_items is not None else 1

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
                logger.info(f"Creating new agent with ID: {agent_id}")
                agent = Agent(agent_id=agent_id, status="active")
                db.add(agent)
                db.flush()

            coletado_em = self._parse_timestamp(payload.get("timestamp"))

            logger.info(
                "Persisting payload for agent_id=%s payload_type=%s payload_count=%s",
                agent_id,
                payload.get("payload_type", "unknown"),
                payload_count,
            )

            records = []
            if payload_items is not None:
                for item in payload_items:
                    payload_record = AgentPayload(
                        agent_id=agent.agent_id,
                        tipo_payload=payload.get("payload_type", "unknown"),
                        dados_payload=json.dumps(item, ensure_ascii=False),
                        metadata_json=json.dumps(payload.get("metadata", {}), ensure_ascii=False),
                        coletado_em=coletado_em,
                    )
                    db.add(payload_record)
                    records.append(payload_record)
            else:
                payload_record = AgentPayload(
                    agent_id=agent.agent_id,
                    tipo_payload=payload.get("payload_type", "unknown"),
                    dados_payload=json.dumps(payload_data, ensure_ascii=False),
                    metadata_json=json.dumps(payload.get("metadata", {}), ensure_ascii=False),
                    coletado_em=coletado_em,
                )
                db.add(payload_record)
                records.append(payload_record)

            db.commit()
            
            # Refresh para obter os IDs gerados
            for record in records:
                db.refresh(record)
            
            logger.info(
                "Persisted %s records for agent_id=%s",
                len(records),
                agent_id,
            )

            return {
                "agent_id": agent.agent_id,
                "payload_ids": [r.id for r in records],
                "processed_at": records[-1].recebido_em.isoformat() if records[-1].recebido_em else datetime.now().isoformat(),
                "payload_type": payload.get("payload_type", "unknown"),
                "record_count": payload_count,
                "summary": "Payload received and saved to database",
            }
        except Exception as exc:
            logger.exception(
                "Failed to persist payload for agent_id=%s payload_type=%s",
                payload.get("agent_id"),
                payload.get("payload_type", "unknown"),
            )
            db.rollback()
            raise
        finally:
            db.close()

    def _parse_timestamp(self, timestamp_value: Any) -> Optional[datetime]:
        """Parse timestamp in various formats to datetime object."""
        if not timestamp_value:
            return None
        
        try:
            if isinstance(timestamp_value, datetime):
                return timestamp_value
            
            if isinstance(timestamp_value, str):
                # Remove 'Z' and replace with '+00:00' for UTC timezone
                timestamp_value = timestamp_value.replace('Z', '+00:00')
                
                # Try standard ISO format first
                try:
                    return datetime.fromisoformat(timestamp_value)
                except ValueError:
                    pass
                
                # Try format without timezone (YYYY-MM-DDTHH:MM:SS.mmmmmm)
                try:
                    # Handle microseconds
                    if '.' in timestamp_value and '+' not in timestamp_value:
                        # Split into date and time parts
                        parts = timestamp_value.split('T')
                        if len(parts) == 2:
                            date_part = parts[0]
                            time_part = parts[1]
                            
                            # Handle microseconds
                            if '.' in time_part:
                                time_part = time_part.split('.')[0]
                            
                            timestamp_value = f"{date_part}T{time_part}"
                            return datetime.fromisoformat(timestamp_value)
                    else:
                        # Try without microseconds
                        timestamp_value = timestamp_value.split('.')[0]
                        return datetime.fromisoformat(timestamp_value)
                except ValueError:
                    pass
                
                # Try format: YYYY-MM-DD HH:MM:SS
                try:
                    return datetime.strptime(timestamp_value, "%Y-%m-%d %H:%M:%S")
                except ValueError:
                    pass
                
                # Try format: YYYY-MM-DD
                try:
                    return datetime.strptime(timestamp_value, "%Y-%m-%d")
                except ValueError:
                    pass
                
                # If all parsing attempts fail, log warning and return None
                logger.warning(f"Could not parse timestamp format: {timestamp_value}")
                return None
                
        except Exception as e:
            logger.warning(f"Error parsing timestamp '{timestamp_value}': {str(e)}")
            return None

    def _extract_payload_items(self, payload_data: Any):
        """Extract items from payload data for batch processing."""
        if isinstance(payload_data, list):
            return payload_data
        if isinstance(payload_data, dict):
            if "logins" in payload_data and isinstance(payload_data["logins"], list):
                return payload_data["logins"]
            if "items" in payload_data and isinstance(payload_data["items"], list):
                return payload_data["items"]
            list_values = [v for v in payload_data.values() if isinstance(v, list)]
            if list_values:
                return list_values[0]
        return None

    def _count_payload_items(self, payload_data: Any) -> int:
        """Count number of items in payload for batch validation."""
        if isinstance(payload_data, list):
            return len(payload_data)
        if isinstance(payload_data, dict):
            if "logins" in payload_data and isinstance(payload_data["logins"], list):
                return len(payload_data["logins"])
            if "items" in payload_data and isinstance(payload_data["items"], list):
                return len(payload_data["items"])
            list_values = [v for v in payload_data.values() if isinstance(v, list)]
            if list_values:
                return len(list_values[0])
        return 1