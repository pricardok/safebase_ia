import time
import asyncio
import json
import logging
from typing import Dict, Any, Optional
from app.database.core import get_db_connection

logger = logging.getLogger(__name__)

# Session store mode: 'db' (shared across processes) or 'memory' (fast local)
_STORE = __import__('os').getenv('WAHA_SESSION_STORE', 'db')  # default to DB in production
# TTL em segundos
_TTL = int(__import__('os').getenv('WAHA_SESSION_TTL', 600))  # 10 minutos por padrão

# In-memory fallback store (used for tests or when configured)
_sessions: Dict[str, Dict[str, Any]] = {}
_lock = asyncio.Lock()


async def _cleanup_memory():
    now = time.time()
    to_delete = [k for k, v in _sessions.items() if v.get('_ts', 0) + _TTL < now]
    for k in to_delete:
        _sessions.pop(k, None)


def _ensure_db_table():
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                CREATE TABLE IF NOT EXISTS waha_sessions (
                    session_id text PRIMARY KEY,
                    data jsonb,
                    updated_at timestamptz DEFAULT now()
                )
                """)
                try:
                    conn.commit()
                except Exception:
                    pass
    except Exception:
        # table creation is best-effort
        pass


async def get_session(session_id: str) -> Dict[str, Any]:
    if _STORE == 'memory':
        async with _lock:
            await _cleanup_memory()
            val = _sessions.get(session_id, {}).copy()
            logger.debug('waha_session.get_session memory session_id=%s val=%s', session_id, val)
            return val

    # DB-backed
    _ensure_db_table()
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT data, updated_at FROM waha_sessions WHERE session_id = %s LIMIT 1", (session_id,))
                row = cur.fetchone()
                if not row:
                    return {}
                data, updated = row[0], row[1]
                # TTL check
                if updated and (time.time() - updated.timestamp()) > _TTL:
                    # expired
                    try:
                        with conn.cursor() as c2:
                            c2.execute("DELETE FROM waha_sessions WHERE session_id = %s", (session_id,))
                    except Exception:
                        pass
                    return {}
                return dict(data or {})
    except Exception:
        # fallback to empty session
        return {}


async def set_session(session_id: str, data: Dict[str, Any]):
    if _STORE == 'memory':
        async with _lock:
            await _cleanup_memory()
            entry = _sessions.get(session_id, {})
            entry.update(data)
            entry['_ts'] = time.time()
            _sessions[session_id] = entry
            logger.info('waha_session.set_session memory session_id=%s entry=%s', session_id, entry)
            return

    _ensure_db_table()
    try:
        # Upsert JSONB
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT data FROM waha_sessions WHERE session_id = %s LIMIT 1", (session_id,))
                row = cur.fetchone()
                if row and row[0]:
                    # merge existing
                    merged = dict(row[0] or {})
                    merged.update(data or {})
                    cur.execute("UPDATE waha_sessions SET data = %s, updated_at = now() WHERE session_id = %s", (json.dumps(merged), session_id))
                    try:
                        conn.commit()
                    except Exception:
                        pass
                else:
                    cur.execute("INSERT INTO waha_sessions(session_id, data) VALUES (%s, %s)", (session_id, json.dumps(data or {})))
                    try:
                        conn.commit()
                    except Exception:
                        pass
    except Exception:
        # non-fatal
        pass


async def clear_session(session_id: str):
    if _STORE == 'memory':
        async with _lock:
            _sessions.pop(session_id, None)
            return
    try:
        _ensure_db_table()
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("DELETE FROM waha_sessions WHERE session_id = %s", (session_id,))
            try:
                conn.commit()
            except Exception:
                pass
    except Exception:
        pass


async def update_session_key(session_id: str, key: str, value: Any):
    if _STORE == 'memory':
        async with _lock:
            await _cleanup_memory()
            entry = _sessions.get(session_id, {})
            entry[key] = value
            entry['_ts'] = time.time()
            _sessions[session_id] = entry
            return
    try:
        _ensure_db_table()
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT data FROM waha_sessions WHERE session_id = %s LIMIT 1", (session_id,))
                row = cur.fetchone()
                merged = dict(row[0] or {}) if row and row[0] else {}
                merged[key] = value
                cur.execute("INSERT INTO waha_sessions(session_id, data) VALUES (%s, %s) ON CONFLICT (session_id) DO UPDATE SET data = %s, updated_at = now()", (session_id, json.dumps(merged), json.dumps(merged)))
                try:
                    conn.commit()
                except Exception:
                    pass
    except Exception:
        pass


async def get_session_key(session_id: str, key: str) -> Optional[Any]:
    if _STORE == 'memory':
        async with _lock:
            await _cleanup_memory()
            entry = _sessions.get(session_id, {})
            return entry.get(key)

    try:
        _ensure_db_table()
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT data FROM waha_sessions WHERE session_id = %s LIMIT 1", (session_id,))
                row = cur.fetchone()
                if not row or not row[0]:
                    return None
                return row[0].get(key)
    except Exception:
        return None
