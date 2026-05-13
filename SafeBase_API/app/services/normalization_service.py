# app/services/normalization_service.py
import json
import logging
import threading
from datetime import datetime
from typing import Any, Dict, Iterable, Optional

from sqlalchemy.orm import Session

from app.core.config import settings
from app.db.models import (
    AgentPayload,
    AlertaSafe,
    DadosBackupSafe,
    DadosEsperasSafe,
    DadosJobsSafe,
    DadosLoginsSafe,
    PayloadCategoria,
)
from app.db.session import SessionLocal

logger = logging.getLogger("safebase_api.normalization")


class NormalizationService:
    def __init__(self) -> None:
        self._stop_event = threading.Event()
        self._thread: Optional[threading.Thread] = None

    def start(self) -> None:
        if self._thread and self._thread.is_alive():
            return
        self._thread = threading.Thread(target=self._loop, daemon=True)
        self._thread.start()
        logger.info("Normalization service started with interval=%s", settings.normalization_interval_seconds)

    def stop(self) -> None:
        self._stop_event.set()

    def _loop(self) -> None:
        interval = max(5, int(settings.normalization_interval_seconds or 60))
        while not self._stop_event.is_set():
            try:
                self.run_once()
            except Exception:
                logger.exception("Normalization cycle failed")
            self._stop_event.wait(interval)

    def run_once(self) -> None:
        supported_types = {
            "jobs",
            "job",
            "waits",
            "wait",
            "alerts",
            "alert",
            "alertas",
            "instancelogins",
            "backupstatus",
        }
        db = SessionLocal()
        try:
            categories = self._load_categories(db)
            pending = (
                db.query(AgentPayload)
                .filter(AgentPayload.payload_normalizado == False)
                .filter(AgentPayload.tipo_payload.in_(supported_types))
                .order_by(AgentPayload.recebido_em.asc())
                .limit(settings.normalization_batch_size)
                .all()
            )

            for payload in pending:
                data = self._load_payload(payload.dados_payload)
                normalized = self._normalize_payload(db, payload, data, categories)
                if normalized:
                    payload.payload_normalizado = True
                    payload.normalizado_em = datetime.utcnow()

            if pending:
                db.commit()
        except Exception:
            db.rollback()
            raise
        finally:
            db.close()

    def _load_payload(self, raw_payload: Optional[str]) -> Dict[str, Any]:
        if not raw_payload:
            return {}
        try:
            return json.loads(raw_payload)
        except json.JSONDecodeError:
            logger.warning("Invalid JSON payload")
            return {}

    def _normalize_payload(
        self,
        db: Session,
        payload: AgentPayload,
        data: Dict[str, Any],
        categories: Dict[str, int],
    ) -> bool:
        payload_type = (payload.tipo_payload or "").lower()
        category_name = self._map_category(payload_type)
        if category_name and category_name in categories:
            payload.categoria_id = categories[category_name]

        if payload_type in {"jobs", "job"}:
            self._normalize_jobs(db, payload, data)
            return True
        if payload_type in {"waits", "wait"}:
            self._normalize_waits(db, payload, data)
            return True
        if payload_type in {"alerts", "alert", "alertas"}:
            self._normalize_alerts(db, payload, data)
            return True
        if payload_type == "instancelogins":
            self._normalize_logins(db, payload, data)
            return True
        if payload_type == "backupstatus":
            self._normalize_backup(db, payload, data)
            return True
        return False

    def _load_categories(self, db: Session) -> Dict[str, int]:
        categories = db.query(PayloadCategoria).filter(PayloadCategoria.ativo == True).all()
        return {c.nome.lower(): c.id for c in categories}

    def _map_category(self, payload_type: str) -> Optional[str]:
        mapping = {
            "jobs": "jobs",
            "job": "jobs",
            "waits": "waits",
            "wait": "waits",
            "alerts": "alerts",
            "alert": "alerts",
            "alertas": "alerts",
            "instancelogins": "instance_logins",
            "backupstatus": "backup_status",
        }
        return mapping.get(payload_type)

    def _normalize_jobs(self, db: Session, payload: AgentPayload, data: Dict[str, Any]) -> None:
        job = DadosJobsSafe(
            payload_agente_id=payload.id,
            agent_id=payload.agent_id,
            nome_job=self._get_first(data, ["nome_job", "job_name", "name"]),
            status=self._get_first(data, ["status", "resultado"]),
            iniciado_em=self._parse_datetime(self._get_first(data, ["iniciado_em", "started_at", "start_time"])),
            finalizado_em=self._parse_datetime(self._get_first(data, ["finalizado_em", "finished_at", "end_time"])),
            duracao_ms=self._to_int(self._get_first(data, ["duracao_ms", "duration_ms"])),
            mensagem_erro=self._get_first(data, ["mensagem_erro", "error_message", "erro"]),
        )
        db.add(job)

    def _normalize_waits(self, db: Session, payload: AgentPayload, data: Dict[str, Any]) -> None:
        wait = DadosEsperasSafe(
            payload_agente_id=payload.id,
            agent_id=payload.agent_id,
            tipo_espera=self._get_first(data, ["tipo_espera", "wait_type"]),
            tempo_espera_ms=self._to_int(self._get_first(data, ["tempo_espera_ms", "wait_time_ms"])),
            tempo_recurso_ms=self._to_int(self._get_first(data, ["tempo_recurso_ms", "resource_wait_ms"])),
            tempo_sinal_ms=self._to_int(self._get_first(data, ["tempo_sinal_ms", "signal_wait_ms"])),
            contagem_tarefas=self._to_int(self._get_first(data, ["contagem_tarefas", "task_count"])),
        )
        db.add(wait)

    def _normalize_alerts(self, db: Session, payload: AgentPayload, data: Dict[str, Any]) -> None:
        created_at = self._parse_datetime(self._get_first(data, ["criado_em", "created_at", "timestamp"]))
        alert = AlertaSafe(
            payload_agente_id=payload.id,
            agent_id=payload.agent_id,
            tipo_alerta=self._get_first(data, ["tipo_alerta", "alert_type"]),
            gravidade=self._get_first(data, ["gravidade", "severity"]),
            mensagem=self._get_first(data, ["mensagem", "message"]),
            criado_em=created_at or datetime.utcnow(),
        )
        db.add(alert)

    def _normalize_logins(self, db: Session, payload: AgentPayload, data: Dict[str, Any]) -> None:
        login = DadosLoginsSafe(
            payload_agente_id=payload.id,
            agent_id=payload.agent_id,
            nome_servidor=self._get_first(data, ["nome_servidor", "NomeServidor"]),
            nome_instancia=self._get_first(data, ["nome_instancia", "NomeInstancia"]),
            servidor_completo=self._get_first(data, ["servidor_completo", "ServidorCompleto"]),
            login_name=self._get_first(data, ["login_name", "LoginName"]),
            login_type=self._get_first(data, ["login_type", "LoginType"]),
            is_disabled=self._to_bool(self._get_first(data, ["is_disabled", "IsDisabled"])),
            create_date=self._parse_datetime(self._get_first(data, ["create_date", "CreateDate"])),
            modify_date=self._parse_datetime(self._get_first(data, ["modify_date", "ModifyDate"])),
            server_roles=self._get_first(data, ["server_roles", "ServerRoles"]),
            server_permissions=self._get_first(data, ["server_permissions", "ServerPermissions"]),
        )
        db.add(login)

    def _normalize_backup(self, db: Session, payload: AgentPayload, data: Dict[str, Any]) -> None:
        backup = DadosBackupSafe(
            payload_agente_id=payload.id,
            agent_id=payload.agent_id,
            nome_servidor=self._get_first(data, ["NomeServidor", "nome_servidor"]),
            nome_instancia=self._get_first(data, ["NomeInstancia", "nome_instancia"]),
            servidor_completo=self._get_first(data, ["ServidorCompleto", "servidor_completo"]),
            servidor=self._get_first(data, ["Servidor", "servidor"]),
            banco=self._get_first(data, ["Banco", "banco"]),
            full_backup_status=self._get_first(data, ["FullBackupStatus", "full_backup_status"]),
            diff_backup_status=self._get_first(data, ["DiffBackupStatus", "diff_backup_status"]),
            log_backup_status=self._get_first(data, ["LogBackupStatus", "log_backup_status"]),
            tipo_recuperacao=self._get_first(data, ["TipoRecuperacao", "tipo_recuperacao"]),
            ultimo_full=self._to_int(self._get_first(data, ["UltimoFull", "ultimo_full"])),
            data_full=self._parse_datetime(self._get_first(data, ["DataFull", "data_full"])),
            tamanho_full_mb=self._get_first(data, ["TamanhoFull_MB", "tamanho_full_mb"]),
            ultimo_diff=self._to_int(self._get_first(data, ["UltimoDiff", "ultimo_diff"])),
            data_diff=self._parse_datetime(self._get_first(data, ["DataDiff", "data_diff"])),
            ultimo_full_diff=self._to_int(self._get_first(data, ["UltimoFullDiff", "ultimo_full_diff"])),
            tamanho_diff_mb=self._get_first(data, ["TamanhoDiff_MB", "tamanho_diff_mb"]),
            ultimo_log_min=self._to_int(self._get_first(data, ["UltimoLog_Min", "ultimo_log_min"])),
            data_log=self._parse_datetime(self._get_first(data, ["DataLog", "data_log"])),
            tamanho_log_mb=self._get_first(data, ["TamanhoLog_MB", "tamanho_log_mb"]),
            full_backup_alarm=self._to_int(self._get_first(data, ["FullBackupAlarm", "full_backup_alarm"])),
            diff_backup_alarm=self._to_int(self._get_first(data, ["DiffBackupAlarm", "diff_backup_alarm"])),
            log_backup_alarm=self._to_int(self._get_first(data, ["LogBackupAlarm", "log_backup_alarm"])),
        )
        db.add(backup)

    def _get_first(self, data: Dict[str, Any], keys: Iterable[str]) -> Optional[Any]:
        for key in keys:
            if key in data and data[key] is not None:
                return data[key]
        return None

    def _to_int(self, value: Optional[Any]) -> Optional[int]:
        if value is None:
            return None
        try:
            return int(value)
        except (TypeError, ValueError):
            return None

    def _to_bool(self, value: Optional[Any]) -> Optional[bool]:
        if value is None:
            return None
        if isinstance(value, bool):
            return value
        if isinstance(value, str):
            return value.strip().lower() in {"1", "true", "yes", "y"}
        if isinstance(value, (int, float)):
            return value != 0
        return None

    def _parse_datetime(self, value: Optional[Any]) -> Optional[datetime]:
        if not value:
            return None
        if isinstance(value, datetime):
            return value
        if isinstance(value, str):
            try:
                value = value.replace("Z", "+00:00")
                return datetime.fromisoformat(value)
            except ValueError:
                return None
        return None
