import logging
import json
from app.database.core import get_db_connection
from typing import Dict, Any, Optional

logger = logging.getLogger(__name__)

def log_integration_event(
    gateway: str,
    event_type: str,
    payload: Dict[str, Any],
    status: str = "received",
    processing_log: str = None
) -> int:
    """
    Registra um evento de webhook recebido na tabela de logs de integrações.
    Retorna o ID do log criado.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO logs_integracoes (gateway, event_type, payload, status, processing_log)
                    VALUES (%s, %s, %s, %s, %s)
                    RETURNING id
                """, (gateway, event_type, json.dumps(payload), status, processing_log))
                log_id = cur.fetchone()[0]
                conn.commit()
                return log_id
    except Exception as e:
        logger.error(f"Falha ao registrar evento de integração no banco de dados: {e}")
        # Não lança exceção para não quebrar o fluxo principal se o log falhar
        return -1