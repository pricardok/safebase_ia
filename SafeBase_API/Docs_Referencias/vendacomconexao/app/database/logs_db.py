# backend/app/database/logs_db.py
import logging
from typing import List, Dict, Any, Optional
from .core import get_db_connection
from psycopg2.extras import DictCursor
 
logger = logging.getLogger(__name__)

def get_api_logs(limit: int = 100, offset: int = 0, exclude_admin: bool = False, search: Optional[str] = None) -> List[Dict[str, Any]]:
    """
    Busca os últimos logs da API do banco de dados, com filtros opcionais.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query_parts = [
                    "SELECT al.id, al.timestamp, al.level, al.message, al.path, al.method, al.status_code, al.user_id, al.ip_address, al.username, al.cliente_id, al.razao_social, al.auth_type, al.plano",
                    "FROM api_logs al"
                ]
                params = []
                where_clauses = []

                if exclude_admin:
                    query_parts.append("LEFT JOIN users u ON al.user_id = u.id")
                    query_parts.append("LEFT JOIN usuario_perfis up ON u.id = up.usuario_id")
                    query_parts.append("LEFT JOIN perfis p ON up.perfil_id = p.id")
                    where_clauses.append("p.nome NOT IN ('admin', 'coach')")
                
                if search:
                    search_term = f"%{search}%"
                    where_clauses.append(
                        "(al.message ILIKE %s OR al.path ILIKE %s OR al.razao_social ILIKE %s OR al.username ILIKE %s)"
                    )
                    params.extend([search_term, search_term, search_term, search_term])

                if where_clauses:
                    query_parts.append("WHERE " + " AND ".join(where_clauses))
                
                query_parts.append("ORDER BY al.timestamp DESC")
                query_parts.append("LIMIT %s OFFSET %s")
                params.extend([limit, offset])

                full_query = " ".join(query_parts)
                cur.execute(full_query, params)
                
                logs = [dict(row) for row in cur.fetchall()]
                return logs
    except Exception as e:
        logger.error(f"Erro ao buscar logs da API do banco de dados: {e}")
        return []
