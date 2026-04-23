import logging
import json
from typing import List, Dict, Any
from .core import get_db_connection

logger = logging.getLogger(__name__)

def get_all_system_configs() -> List[Dict[str, Any]]:
    """
    Busca todas as configurações do sistema no banco de dados para o painel de admin.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT chave, valor, descricao, tipo_valor, grupo
                    FROM configuracoes_sistema
                    ORDER BY grupo, chave;
                """)
                configs = []
                for row in cur.fetchall():
                    configs.append({
                        "chave": row[0],
                        "valor": row[1],
                        "descricao": row[2],
                        "tipo_valor": row[3],
                        "grupo": row[4]
                    })
                return configs
    except Exception as e:
        logger.error(f"Erro ao buscar todas as configurações do sistema: {e}")
        return []

def update_system_config(chave: str, valor: Any) -> bool:
    """
    Atualiza uma configuração específica do sistema no banco de dados.
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # O valor é armazenado como JSONB, então o convertemos para uma string JSON.
                valor_json = json.dumps(valor)
                cur.execute("""
                    UPDATE configuracoes_sistema
                    SET valor = %s, updated_at = NOW()
                    WHERE chave = %s;
                """, (valor_json, chave))
                
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao atualizar a configuração '{chave}': {e}")
        return False
