
# app/database/webhooks_db.py
import logging
import json
import uuid
import psycopg2
import psycopg2.extras
from typing import Dict, Any, Optional

from .core import get_db_connection

logger = logging.getLogger(__name__)
 
def log_webhook_event(
    gateway: str = "kiwify",
    event_type: Optional[str] = None,
    payload: Optional[Dict[str, Any]] = None,
    status: str = "received",
    processing_log: Optional[str] = None,
    event_id: Optional[uuid.UUID] = None,
    external_message_id: Optional[str] = None
) -> uuid.UUID:
    """
    Loga um evento de webhook recebido ou atualiza um log existente.
    """
    try:
        # Compatibilidade: algumas chamadas antigas passavam `event_id` como
        # primeiro positional argument. Detectamos e corrigimos isso aqui.
        if event_id is None:
            try:
                # se 'gateway' for um UUID válido e event_type/payload não enviados,
                # interpretamos o primeiro arg como event_id (chamada de atualização antiga)
                maybe_uuid = uuid.UUID(str(gateway))
                if event_type is None and payload is None:
                    event_id = maybe_uuid
                    gateway = "kiwify"
            except Exception:
                # não é um UUID — ignora
                pass

        # Normalize/validate status to avoid DB check constraint violations
        allowed_statuses = {'received', 'processing', 'processed', 'failed', 'webhook_disabled', 'processing_error'}
        if not status or status not in allowed_statuses:
            logger.warning('log_webhook_event: received unknown status=%s; coercing to "failed"', status)
            status = 'failed'

        # If attempting to INSERT a new event, do not use 'processing' as initial status
        # (some DB schemas don't accept it for new rows). Coerce to 'received'.
        if event_id is None and status == 'processing':
            logger.debug('log_webhook_event: coercing initial status "processing" -> "received" to avoid DB constraint')
            status = 'received'
 
        # Ensure processing_log is a string (JSON if passed as dict)
        if processing_log is not None and not isinstance(processing_log, str):
            try:
                processing_log = json.dumps(processing_log, ensure_ascii=False)
            except Exception:
                processing_log = str(processing_log)

        with get_db_connection() as conn:
            with conn.cursor() as cur:
                if event_id:
                    # Atualiza um evento existente. Também atualiza external_message_id
                    # se for fornecido.
                    if external_message_id is not None:
                        cur.execute("""
                            UPDATE webhook_events
                            SET status = %s, processing_log = %s, external_message_id = %s
                            WHERE id = %s
                        """, (status, processing_log, external_message_id, event_id))
                    else:
                        cur.execute("""
                            UPDATE webhook_events
                            SET status = %s, processing_log = %s
                            WHERE id = %s
                        """, (status, processing_log, event_id))
                    logger.debug('webhook_events.UPDATE id=%s status=%s processing_log=%s external_message_id=%s', event_id, status, processing_log, external_message_id)
                else:
                    # Insere um novo evento; inclui external_message_id quando presente
                    if external_message_id is not None:
                        cur.execute("""
                            INSERT INTO webhook_events (gateway, event_type, payload, status, external_message_id)
                            VALUES (%s, %s, %s, %s, %s)
                            RETURNING id
                        """, (gateway, event_type, json.dumps(payload), status, external_message_id))
                    else:
                        cur.execute("""
                            INSERT INTO webhook_events (gateway, event_type, payload, status)
                            VALUES (%s, %s, %s, %s)
                            RETURNING id
                        """, (gateway, event_type, json.dumps(payload), status))
                    event_id = cur.fetchone()[0]
                    logger.debug('webhook_events.INSERT id=%s gateway=%s event_type=%s status=%s external_message_id=%s', event_id, gateway, event_type, status, external_message_id)

                conn.commit()
                return event_id
    except Exception as e:
        logger.error(f"Erro ao logar evento de webhook: {e}")
        # Não relança a exceção para não quebrar o fluxo principal
        return None

def get_gateway_product_mapping(gateway: str, gateway_product_id: str) -> Optional[Dict[str, Any]]:
    """
    Busca o mapeamento de um produto do gateway para um plano interno.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT plano_id, descricao FROM gateway_products
                    WHERE gateway = %s AND gateway_product_id = %s AND ativo = true
                """, (gateway, gateway_product_id))
                result = cur.fetchone()
                return {"plano_id": result[0], "descricao": result[1]} if result else None
    except Exception as e:
        logger.error(f"Erro ao buscar mapeamento de produto do gateway: {e}")
        return None

def update_cliente_status(cliente_id: str, status: str, expiry_date: str = None) -> bool:
    """
    Atualiza o status e data de expiração de um cliente.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                if expiry_date:
                    cur.execute("""
                        UPDATE clientes 
                        SET status = %s, expiry_date = %s, updated_at = NOW()
                        WHERE id = %s
                    """, (status, expiry_date, cliente_id))
                else:
                    cur.execute("""
                        UPDATE clientes 
                        SET status = %s, updated_at = NOW()
                        WHERE id = %s
                    """, (status, cliente_id))
                
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar status do cliente {cliente_id}: {e}")
        return False

def get_user_by_cliente_id(cliente_id: str) -> list:
    """
    Obtém todos os usuários de um cliente.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, username, email, is_active
                    FROM users 
                    WHERE cliente_id = %s
                """, (cliente_id,))
                
                users = []
                for row in cur.fetchall():
                    users.append({
                        "id": row[0],
                        "username": row[1],
                        "email": row[2],
                        "is_active": row[3]
                    })
                return users
    except Exception as e:
        logger.error(f"Erro ao buscar usuários do cliente {cliente_id}: {e}")
        return []

def update_user_status(user_id: int, is_active: bool) -> bool:
    """
    Atualiza o status de ativação de um usuário.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE users 
                    SET is_active = %s
                    WHERE id = %s
                """, (is_active, user_id))
                
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar status do usuário {user_id}: {e}")
        return False

def get_webhook_logs(limit: int = 100, offset: int = 0) -> list:
    """
    Busca os logs de eventos de webhook do banco de dados com paginação.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, gateway, event_type, status, received_at created_at, processing_log, payload, external_message_id
                    FROM webhook_events
                    ORDER BY received_at desc
                    LIMIT %s OFFSET %s
                """, (limit, offset))
                
                logs = []
                for row in cur.fetchall():
                    logs.append({
                        "id": row[0], "gateway": row[1], "event_type": row[2],
                        "status": row[3], "created_at": row[4],
                        "processing_log": row[5], "payload": row[6],
                        "external_message_id": row[7]
                    })
                return logs
    except Exception as e:
        logger.error(f"Erro ao buscar logs de webhook: {e}")
        return []

def get_webhook_event_by_id(event_id: uuid.UUID) -> Optional[Dict[str, Any]]:
    """
    Busca um único evento de webhook pelo seu ID.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute("""
                    SELECT id, gateway, event_type, status, created_at, processing_log, payload, external_message_id
                    FROM webhook_events
                    WHERE id = %s
                """, (event_id,))
                event = cur.fetchone()
                return dict(event) if event else None
    except Exception as e:
        logger.error(f"Erro ao buscar evento de webhook por ID {event_id}: {e}")
        return None


def log_unknown_whatsapp_contact(phone_normalized: str, raw_from: Optional[str] = None, event_id: Optional[uuid.UUID] = None, remetente: Optional[str] = None) -> bool:
    """Registra um número desconhecido que enviou mensagem ao gateway Waha.
    Cria a tabela `contatos_whatsapp_desconhecidos` se não existir e faz upsert
    incrementando `ocorrencias` e atualizando `ultima_visita`.
    Campos em português seguem o padrão do sistema.

    Args:
        phone_normalized: telefone normalizado (apenas dígitos, sem +)
        raw_from: o identificador bruto recebido no webhook (ex.: 555181364422@c.us)
        event_id: id do evento webhook, se disponível
        remetente: nome visível do remetente (notifyName) quando fornecido
    """
    try:
        uid = str(uuid.uuid4())
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # Create table if not exists (simple migration-on-demand)
                cur.execute("""
                    CREATE TABLE IF NOT EXISTS contatos_whatsapp_desconhecidos (
                        id uuid PRIMARY KEY,
                        telefone_normalizado text UNIQUE NOT NULL,
                        telefone_raw text,
                        remetente text,
                        primeira_visita timestamptz NOT NULL DEFAULT now(),
                        ultima_visita timestamptz NOT NULL DEFAULT now(),
                        ocorrencias integer NOT NULL DEFAULT 1,
                        ultimo_evento_id uuid
                    )
                """)
                cur.execute("""
                    INSERT INTO contatos_whatsapp_desconhecidos (id, telefone_normalizado, telefone_raw, remetente, ultimo_evento_id)
                    VALUES (%s, %s, %s, %s, %s)
                    ON CONFLICT (telefone_normalizado) DO UPDATE
                    SET ultima_visita = now(), ocorrencias = contatos_whatsapp_desconhecidos.ocorrencias + 1, telefone_raw = EXCLUDED.telefone_raw, remetente = COALESCE(EXCLUDED.remetente, contatos_whatsapp_desconhecidos.remetente), ultimo_evento_id = EXCLUDED.ultimo_evento_id
                """, (uid, phone_normalized, raw_from, remetente, event_id))
                conn.commit()
        return True
    except Exception as e:
        logger.exception('Erro ao registrar contatos_whatsapp_desconhecidos: %s', e)
        return False


def get_contatos_whatsapp_desconhecidos(limit: int = 50, offset: int = 0, search: Optional[str] = None) -> dict:
    """Retorna registros de `contatos_whatsapp_desconhecidos` para o painel admin.
    Retorna um dicionário com chaves `items` e `total` para suporte a paginação.
    Permite busca por `telefone_normalizado` ou `remetente` usando `search`.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                if search:
                    q = "%" + search + "%"
                    # total count
                    cur.execute("""
                        SELECT COUNT(*) FROM contatos_whatsapp_desconhecidos
                        WHERE telefone_normalizado ILIKE %s OR remetente ILIKE %s
                    """, (q, q))
                    total = cur.fetchone()[0]
                    cur.execute("""
                        SELECT id, telefone_normalizado, telefone_raw, remetente, primeira_visita, ultima_visita, ocorrencias, ultimo_evento_id
                        FROM contatos_whatsapp_desconhecidos
                        WHERE telefone_normalizado ILIKE %s OR remetente ILIKE %s
                        ORDER BY ultima_visita DESC
                        LIMIT %s OFFSET %s
                    """, (q, q, limit, offset))
                else:
                    cur.execute("""
                        SELECT COUNT(*) FROM contatos_whatsapp_desconhecidos
                    """)
                    total = cur.fetchone()[0]
                    cur.execute("""
                        SELECT id, telefone_normalizado, telefone_raw, remetente, primeira_visita, ultima_visita, ocorrencias, ultimo_evento_id
                        FROM contatos_whatsapp_desconhecidos
                        ORDER BY ultima_visita DESC
                        LIMIT %s OFFSET %s
                    """, (limit, offset))
                rows = cur.fetchall()
                results = []
                for r in rows:
                    results.append({
                        'id': r[0],
                        'telefone_normalizado': r[1],
                        'telefone_raw': r[2],
                        'remetente': r[3],
                        'primeira_visita': r[4],
                        'ultima_visita': r[5],
                        'ocorrencias': r[6],
                        'ultimo_evento_id': r[7]
                    })
                return {'items': results, 'total': total}
    except Exception as e:
        logger.exception('Erro ao buscar contatos_whatsapp_desconhecidos: %s', e)
        return {'items': [], 'total': 0}
