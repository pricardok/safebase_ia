import json
from datetime import datetime, timedelta
from typing import Any, Dict, List

from app.services.ia_orchestrator_runtime import IAOrchestratorRuntime

from app.db.session import SessionLocal
from sqlalchemy.orm import Session
from app.core.config import settings
from app.db.models import (
    Agent,
    AgentPayload,
    AlertaSafe,
    DadosBackupSafe,
    DadosEsperasSafe,
    DadosJobsSafe,
    DadosLoginsSafe,
    KnowledgeMd,
)
from sqlalchemy import func


class IAService:
    def __init__(self) -> None:
        self._orchestrator = IAOrchestratorRuntime()

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
        context = payload.get("context", {}) or {}
        categoria_codigo = payload.get("categoria_codigo")

        if categoria_codigo:
            db_context = self._build_context_for_category(categoria_codigo)
            context = {**context, **db_context}

        return self._orchestrator.generate_insight(
            payload.get("query", ""),
            context,
        )

    def _build_context_for_category(self, categoria_codigo: str) -> Dict[str, Any]:
        categoria = (categoria_codigo or "").lower()
        if categoria == "dba":
            return self._build_dba_context()
        return {"categoria_codigo": categoria}

    def _build_dba_context(self) -> Dict[str, Any]:
        db = SessionLocal()
        try:
            now = datetime.utcnow()
            window_24h = now - timedelta(hours=24)
            window_7d = now - timedelta(days=7)

            total_agents = db.query(Agent).count()
            last_payload = (
                db.query(AgentPayload)
                .order_by(AgentPayload.recebido_em.desc())
                .first()
            )

            waits = (
                db.query(
                    DadosEsperasSafe.tipo_espera,
                    func.sum(DadosEsperasSafe.tempo_espera_ms).label("total_wait_ms"),
                    func.sum(DadosEsperasSafe.contagem_tarefas).label("total_tasks"),
                )
                .filter(DadosEsperasSafe.payload_agente_id.isnot(None))
                .group_by(DadosEsperasSafe.tipo_espera)
                .order_by(func.sum(DadosEsperasSafe.tempo_espera_ms).desc())
                .limit(10)
                .all()
            )

            alerts = (
                db.query(AlertaSafe)
                .filter(AlertaSafe.criado_em >= window_7d)
                .order_by(AlertaSafe.criado_em.desc())
                .limit(10)
                .all()
            )

            jobs_recent = (
                db.query(DadosJobsSafe)
                .filter(DadosJobsSafe.iniciado_em >= window_7d)
                .order_by(DadosJobsSafe.iniciado_em.desc())
                .limit(10)
                .all()
            )

            jobs_fail = (
                db.query(DadosJobsSafe)
                .filter(DadosJobsSafe.mensagem_erro.isnot(None))
                .filter(DadosJobsSafe.iniciado_em >= window_7d)
                .order_by(DadosJobsSafe.iniciado_em.desc())
                .limit(10)
                .all()
            )

            backups = (
                db.query(DadosBackupSafe)
                .filter(DadosBackupSafe.data_full >= window_7d)
                .order_by(DadosBackupSafe.data_full.desc())
                .limit(10)
                .all()
            )

            logins = (
                db.query(DadosLoginsSafe)
                .filter(DadosLoginsSafe.create_date >= window_7d)
                .order_by(DadosLoginsSafe.create_date.desc())
                .limit(10)
                .all()
            )

            knowledge = []
            if settings.knowledge_md_enabled:
                knowledge = self._load_knowledge_md(db)

            return {
                "categoria_codigo": "dba",
                "window_24h": window_24h.isoformat(),
                "window_7d": window_7d.isoformat(),
                "agents_total": total_agents,
                "ultimo_payload_em": last_payload.recebido_em.isoformat() if last_payload else None,
                "waits_top": [
                    {
                        "tipo": row.tipo_espera,
                        "total_wait_ms": int(row.total_wait_ms or 0),
                        "total_tasks": int(row.total_tasks or 0),
                    }
                    for row in waits
                ],
                "alertas_recentes": [
                    {
                        "tipo": alert.tipo_alerta,
                        "gravidade": alert.gravidade,
                        "mensagem": alert.mensagem,
                        "criado_em": alert.criado_em.isoformat() if alert.criado_em else None,
                    }
                    for alert in alerts
                ],
                "jobs_recentes": [
                    {
                        "nome": job.nome_job,
                        "status": job.status,
                        "iniciado_em": job.iniciado_em.isoformat() if job.iniciado_em else None,
                        "finalizado_em": job.finalizado_em.isoformat() if job.finalizado_em else None,
                        "duracao_ms": job.duracao_ms,
                    }
                    for job in jobs_recent
                ],
                "jobs_falha": [
                    {
                        "nome": job.nome_job,
                        "status": job.status,
                        "mensagem_erro": job.mensagem_erro,
                        "iniciado_em": job.iniciado_em.isoformat() if job.iniciado_em else None,
                    }
                    for job in jobs_fail
                ],
                "backups_recentes": [
                    {
                        "servidor": backup.servidor,
                        "banco": backup.banco,
                        "full_backup_status": backup.full_backup_status,
                        "diff_backup_status": backup.diff_backup_status,
                        "log_backup_status": backup.log_backup_status,
                        "data_full": backup.data_full.isoformat() if backup.data_full else None,
                    }
                    for backup in backups
                ],
                "logins_recentes": [
                    {
                        "login_name": login.login_name,
                        "login_type": login.login_type,
                        "is_disabled": login.is_disabled,
                        "create_date": login.create_date.isoformat() if login.create_date else None,
                    }
                    for login in logins
                ],
                "knowledge_md": knowledge,
            }
        finally:
            db.close()

    def _load_knowledge_md(self, db: "Session") -> List[Dict[str, Any]]:
        docs = (
            db.query(KnowledgeMd)
            .order_by(KnowledgeMd.atualizado_em.desc())
            .limit(settings.knowledge_md_max_items)
            .all()
        )
        items: List[Dict[str, Any]] = []
        for doc in docs:
            content_preview = None
            try:
                with open(doc.caminho_arquivo, "r", encoding="utf-8") as file:
                    content_preview = file.read(settings.knowledge_md_max_chars)
            except Exception:
                content_preview = None

            items.append(
                {
                    "titulo": doc.titulo,
                    "origem": doc.origem,
                    "caminho_arquivo": doc.caminho_arquivo,
                    "preview": content_preview,
                }
            )
        return items

    def generate_chat_reply(self, user_message: str, context: Dict[str, Any]) -> str:
        return self._orchestrator.generate_reply(user_message, context)
