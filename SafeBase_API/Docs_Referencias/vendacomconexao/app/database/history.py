import logging
import json
from typing import Optional, List, Dict, Any
import psycopg2.extras

from .core import get_db_connection

logger = logging.getLogger(__name__)

def save_simulation(usuario_id: int, modulo: str, produto_descricao: str, perfil_cliente: str, 
                   conversa: dict, metricas: dict, feedback_ia: str, is_exemplo: bool = False, session_id: Optional[str] = None) -> Optional[int]:
    """Salva uma simulação no banco de dados"""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO simulacoes
                    (usuario_id, modulo, produto_descricao, perfil_cliente, conversa, metricas, feedback_ia, is_exemplo, session_id)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
                    RETURNING id
                """, (usuario_id, modulo, produto_descricao, perfil_cliente,
                      json.dumps(conversa) if conversa else None,
                      json.dumps(metricas) if metricas else None,
                      feedback_ia, is_exemplo, session_id))
                simulation_id = cur.fetchone()[0]
            conn.commit()
        return simulation_id
    except Exception as e:
        logger.error(f"Erro ao salvar simulação no banco de dados: {e}")
        conn.rollback()
        return None

def get_user_simulations(usuario_id: int, limit: int = 50):
    """Obtém as simulações de um usuário, mais recentes primeiro"""
    with get_db_connection() as conn:
        with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
            cur.execute("""
                SELECT id, modulo, produto_descricao, perfil_cliente, conversa, metricas, feedback_ia, data_criacao, is_exemplo, session_id
                FROM simulacoes
                WHERE usuario_id = %s
                ORDER BY data_criacao DESC
                LIMIT %s
            """, (usuario_id, limit))
            simulations = []
            for row in cur.fetchall():
                simulations.append(dict(row))
    return simulations

def get_user_sessions(usuario_id: int, limit: int = 50) -> List[Dict[str, Any]]:
    """
    Obtém as simulações de um usuário e as agrupa por session_id.
    Retorna uma lista de sessões, onde cada sessão contém suas interações.
    """
    # Busca um número maior de simulações para garantir que as sessões fiquem completas
    raw_simulations = get_user_simulations(usuario_id, limit=limit * 3) 

    sessions = {}
    for sim in raw_simulations:
        session_id = str(sim.get("session_id")) if sim.get("session_id") else f"no_session_{sim['id']}"

        if session_id not in sessions:
            sessions[session_id] = {
                "session_id": str(sim.get("session_id")) if sim.get("session_id") else None,
                "produto_descricao": sim["produto_descricao"],
                "data_inicio": sim["data_criacao"],
                "modulos_utilizados": set(),
                "interacoes": []
            }
        
        # Adiciona a simulação à lista de interações da sessão
        sessions[session_id]["interacoes"].append(sim)
        sessions[session_id]["modulos_utilizados"].add(sim["modulo"])

        # Atualiza a data de início para a mais antiga da sessão
        if sim["data_criacao"] < sessions[session_id]["data_inicio"]:
            sessions[session_id]["data_inicio"] = sim["data_criacao"]
            # Atualiza a descrição do produto para a da primeira interação
            sessions[session_id]["produto_descricao"] = sim["produto_descricao"]

    # Converte o set de módulos para lista e ordena as interações por data
    for session_id, session_data in sessions.items():
        session_data["modulos_utilizados"] = sorted(list(session_data["modulos_utilizados"]))
        session_data["interacoes"] = sorted(session_data["interacoes"], key=lambda x: x["data_criacao"])
        session_data["total_interacoes"] = len(session_data["interacoes"])

    # Converte o dicionário de sessões em uma lista e ordena as sessões pela data de início
    sorted_sessions = sorted(sessions.values(), key=lambda x: x["data_inicio"], reverse=True)

    # Aplica o limite final ao número de sessões
    return sorted_sessions[:limit]



def user_has_simulations(usuario_id: int) -> bool:
    """Verifica se um usuário tem simulações (excluindo exemplos)"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT COUNT(*) 
                FROM simulacoes 
                WHERE usuario_id = %s AND is_exemplo = FALSE
            """, (usuario_id,))
            count = cur.fetchone()[0]
    return count > 0

def get_example_scripts(modulo: str = None, canal: str = None):
    """Obtém scripts de exemplo para onboarding"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            query = "SELECT id, modulo, canal, script, data_criacao FROM scripts_exemplo"
            params = []
            if modulo or canal:
                conditions = []
                if modulo:
                    conditions.append("modulo = %s")
                    params.append(modulo)
                if canal:
                    conditions.append("canal = %s")
                    params.append(canal)
                query += " WHERE " + " AND ".join(conditions)
            query += " ORDER BY data_criacao DESC"
            
            cur.execute(query, params)
            scripts = []
            for row in cur.fetchall():
                scripts.append({
                    "id": row[0],
                    "modulo": row[1],
                    "canal": row[2],
                    "script": row[3],
                    "data_criacao": row[4]
                })
    return scripts

def create_example_simulation(usuario_id: int, exemplo_data: dict):
    """Cria uma simulação de exemplo para onboarding"""
    return save_simulation(
        usuario_id=usuario_id,
        modulo=exemplo_data["modulo"],
        produto_descricao=exemplo_data["produto_descricao"],
        perfil_cliente=exemplo_data["perfil_cliente"],
        conversa=exemplo_data["conversa"],
        metricas=exemplo_data["metricas"],
        feedback_ia=exemplo_data["feedback_ia"],
        is_exemplo=True
    )

def get_user_simulations_secure(usuario_id: int, limit: int = 50):
    """Obtém as simulações de um usuário com validação de segurança"""
    return get_user_simulations(usuario_id, limit)

def validate_simulation_ownership(simulation_id: int, usuario_id: int) -> bool:
    """Valida se uma simulação pertence ao usuário"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT COUNT(*) 
                FROM simulacoes 
                WHERE id = %s AND usuario_id = %s
            """, (simulation_id, usuario_id))
            count = cur.fetchone()[0]
    
    is_owner = count > 0
    
    if not is_owner:
        logger.warning(f"Tentativa de acesso a simulação não pertencente: User {usuario_id}, Simulation {simulation_id}")
    
    return is_owner