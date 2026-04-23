import json
from datetime import datetime
from typing import Any, Dict

from app.db.session import SessionLocal
from app.db.models import Agent, AgentPayload


class IAService:
    def process_agent_payload(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        db = SessionLocal()
        try:
            agent_id = payload.get("agent_id")
            agent = db.query(Agent).filter(Agent.agent_id == agent_id).first()
            if not agent:
                agent = Agent(agent_id=agent_id, status="active")
                db.add(agent)
                db.flush()

            coletado_em = None
            if payload.get("timestamp"):
                try:
                    coletado_em = datetime.fromisoformat(payload.get("timestamp"))
                except ValueError:
                    coletado_em = None

            payload_record = AgentPayload(
                agent_id=agent.agent_id,
                tipo_payload=payload.get("payload_type", "unknown"),
                dados_payload=json.dumps(payload.get("payload_data", {}), ensure_ascii=False),
                metadata_json=json.dumps(payload.get("metadata", {}), ensure_ascii=False),
                coletado_em=coletado_em,
            )
            db.add(payload_record)
            db.commit()

            return {
                "agent_id": agent.agent_id,
                "processed_at": payload_record.recebido_em.isoformat(),
                "payload_type": payload_record.tipo_payload,
                "summary": "Payload received and saved to database",
            }
        finally:
            db.close()

    def query_insights(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "insight": "Esta é uma resposta de exemplo.",
            "metadata": {
                "agent_id": payload.get("agent_id"),
                "query": payload.get("query"),
            },
        }
