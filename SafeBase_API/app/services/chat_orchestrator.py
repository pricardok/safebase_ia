from datetime import datetime, timedelta
from typing import Dict, List

from sqlalchemy import func
from sqlalchemy.orm import Session

from app.db.models import Agent, AgentPayload, AlertaSafe, DadosEsperasSafe, DadosJobsSafe
from app.services.chat_service import ChatService
from app.services.ia_service import IAService


class ChatOrchestrator:
    def __init__(self) -> None:
        self.chat_service = ChatService()
        self.ia_service = IAService()

    def handle_user_message(self, db: Session, conversation_id: int, user_id: str, content: str):
        conversation = self.chat_service.get_conversation(db, conversation_id, user_id)
        if not conversation:
            return None, None

        user_message = self.chat_service.add_message(db, conversation, "user", content)

        context = self._build_context(db, conversation_id, content)
        assistant_text = self.ia_service.generate_chat_reply(content, context)

        assistant_message = self.chat_service.add_message(db, conversation, "assistant", assistant_text)
        return user_message, assistant_message

    def _build_context(self, db: Session, conversation_id: int, content: str) -> Dict:
        recent_messages = self.chat_service.get_recent_messages(db, conversation_id, limit=10)
        environment_snapshot = self._load_environment_context(db)
        knowledge_notes = self._load_knowledge_notes(db, content)

        return {
            "conversation": [
                {"role": msg.papel, "content": msg.conteudo, "created_at": msg.criado_em.isoformat()}
                for msg in recent_messages
            ],
            "environment": environment_snapshot,
            "knowledge_notes": knowledge_notes,
        }

    def _load_environment_context(self, db: Session) -> Dict:
        since = datetime.utcnow() - timedelta(hours=24)

        total_agents = db.query(func.count(Agent.id)).scalar() or 0
        last_payload = db.query(func.max(AgentPayload.recebido_em)).scalar()

        alerts_recent = (
            db.query(AlertaSafe)
            .filter(AlertaSafe.criado_em >= since)
            .order_by(AlertaSafe.criado_em.desc())
            .limit(10)
            .all()
        )

        waits_top = (
            db.query(
                DadosEsperasSafe.tipo_espera,
                func.coalesce(func.sum(DadosEsperasSafe.tempo_espera_ms), 0).label("tempo_espera_ms"),
                func.coalesce(func.sum(DadosEsperasSafe.tempo_recurso_ms), 0).label("tempo_recurso_ms"),
                func.coalesce(func.sum(DadosEsperasSafe.tempo_sinal_ms), 0).label("tempo_sinal_ms"),
                func.coalesce(func.sum(DadosEsperasSafe.contagem_tarefas), 0).label("contagem_tarefas"),
            )
            .filter(DadosEsperasSafe.id.isnot(None))
            .group_by(DadosEsperasSafe.tipo_espera)
            .order_by(func.coalesce(func.sum(DadosEsperasSafe.tempo_espera_ms), 0).desc())
            .limit(10)
            .all()
        )

        jobs_recent = (
            db.query(DadosJobsSafe)
            .filter(DadosJobsSafe.iniciado_em >= since)
            .order_by(DadosJobsSafe.iniciado_em.desc())
            .limit(10)
            .all()
        )

        failed_jobs = [
            job for job in jobs_recent
            if job.mensagem_erro or (job.status and job.status.lower() not in {"success", "sucesso"})
        ]

        return {
            "periodo_horas": 24,
            "agentes_total": total_agents,
            "ultimo_payload_em": last_payload.isoformat() if last_payload else None,
            "alertas_recentes": [
                {
                    "tipo": alert.tipo_alerta,
                    "gravidade": alert.gravidade,
                    "mensagem": alert.mensagem,
                    "criado_em": alert.criado_em.isoformat() if alert.criado_em else None,
                }
                for alert in alerts_recent
            ],
            "waits_top": [
                {
                    "tipo": row.tipo_espera,
                    "tempo_espera_ms": int(row.tempo_espera_ms or 0),
                    "tempo_recurso_ms": int(row.tempo_recurso_ms or 0),
                    "tempo_sinal_ms": int(row.tempo_sinal_ms or 0),
                    "contagem_tarefas": int(row.contagem_tarefas or 0),
                }
                for row in waits_top
            ],
            "jobs_recentes": [
                {
                    "nome": job.nome_job,
                    "status": job.status,
                    "iniciado_em": job.iniciado_em.isoformat() if job.iniciado_em else None,
                    "finalizado_em": job.finalizado_em.isoformat() if job.finalizado_em else None,
                    "duracao_ms": job.duracao_ms,
                    "mensagem_erro": job.mensagem_erro,
                }
                for job in jobs_recent
            ],
            "jobs_falha": [
                {
                    "nome": job.nome_job,
                    "status": job.status,
                    "iniciado_em": job.iniciado_em.isoformat() if job.iniciado_em else None,
                    "mensagem_erro": job.mensagem_erro,
                }
                for job in failed_jobs
            ],
        }

    def _load_knowledge_notes(self, db: Session, content: str) -> List[Dict]:
        # TODO: indexar e buscar notas MD relevantes
        return []
