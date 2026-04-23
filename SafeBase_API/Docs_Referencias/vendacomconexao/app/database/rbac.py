import logging
from typing import Optional

from .core import get_db_connection

logger = logging.getLogger(__name__)

def get_user_profile(user_id: int):
    """Obtém o perfil de um usuário"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT p.id, p.nome, p.descricao
                FROM perfis p
                JOIN usuario_perfis up ON p.id = up.perfil_id
                WHERE up.usuario_id = %s
                LIMIT 1
            """, (user_id,))
            result = cur.fetchone()
            if result:
                return {"id": result[0], "nome": result[1], "descricao": result[2]}
    return None

def get_profile_by_name(profile_name: str):
    """Obtém um perfil pelo nome."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "SELECT id, nome, descricao FROM perfis WHERE nome = %s",
                (profile_name,)
            )
            profile = cur.fetchone()
            if profile:
                return {"id": profile[0], "nome": profile[1], "descricao": profile[2]}
    return None

def get_profile_by_id(profile_id: int):
    """Obtém um perfil pelo seu ID."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "SELECT id, nome, descricao FROM perfis WHERE id = %s",
                (profile_id,)
            )
            profile = cur.fetchone()
            if profile:
                return {"id": profile[0], "nome": profile[1], "descricao": profile[2]}
    return None


def get_profile_permissions(profile_id: int):
    """Obtém as permissões de um perfil"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT m.nome, m.endpoint_base, pm.pode_acessar
                FROM modulos m
                JOIN perfil_modulos pm ON m.id = pm.modulo_id
                WHERE pm.perfil_id = %s
            """, (profile_id,))
            permissions = {}
            for row in cur.fetchall():
                permissions[row[0]] = {
                    "endpoint_base": row[1],
                    "pode_acessar": row[2]
                }
            return permissions

def get_api_key_profile(api_key_hash: str):
    """Obtém o perfil associado a uma API Key.

    Aceita tanto o hash armazenado quanto a chave em texto puro (api_key).
    Quando recebe a chave em texto, tenta verificar contra as chaves ativas
    usando `verify_password` (que suporta bcrypt ou fallback SHA256).
    """
    from app.auth_jwt import verify_password

    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Primeiro tenta match direto (caso o caller já passe o hash)
            cur.execute("""
                SELECT p.id, p.nome, p.descricao, ak.ativa, ak.chave_hash
                FROM api_keys ak
                JOIN perfis p ON ak.perfil_id = p.id
                WHERE ak.chave_hash = %s 
                AND ak.ativa = true
                AND (ak.expira_em IS NULL OR ak.expira_em > CURRENT_TIMESTAMP)
            """, (api_key_hash,))
            result = cur.fetchone()
            if result:
                return {
                    "id": result[0],
                    "nome": result[1],
                    "descricao": result[2],
                    "ativa": result[3]
                }

            # Se não encontrou, varre as chaves ativas e verifica plaintext via verify_password
            cur.execute("""
                SELECT p.id, p.nome, p.descricao, ak.ativa, ak.chave_hash
                FROM api_keys ak
                JOIN perfis p ON ak.perfil_id = p.id
                WHERE ak.ativa = true
                AND (ak.expira_em IS NULL OR ak.expira_em > CURRENT_TIMESTAMP)
            """)
            for row in cur.fetchall():
                stored_hash = row[4]
                try:
                    if verify_password(api_key_hash, stored_hash):
                        return {
                            "id": row[0],
                            "nome": row[1],
                            "descricao": row[2],
                            "ativa": row[3]
                        }
                except Exception:
                    # continue checking other keys
                    continue

    return None

def create_api_key(profile_id: int, description: str = None, usage_limit: int = 1000):
    """Cria uma nova API Key"""
    from app.services.security import generate_api_key
    from app.auth_jwt import get_password_hash
    
    api_key = generate_api_key()
    api_key_hash = get_password_hash(api_key)
    
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO api_keys (chave_hash, perfil_id, descricao, limites_uso)
                VALUES (%s, %s, %s, %s)
                RETURNING id
            """, (api_key_hash, profile_id, description, usage_limit))
            key_id = cur.fetchone()[0]
        conn.commit()
    
    return {"id": key_id, "api_key": api_key}

def deactivate_api_key(api_key_id: str):
    """Desativa uma API Key"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                UPDATE api_keys 
                SET ativa = false 
                WHERE id = %s
            """, (api_key_id,))
        conn.commit()

def get_all_profiles():
    """Obtém todos os perfis disponíveis"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT id, nome, descricao FROM perfis ORDER BY nome")
            profiles = []
            for row in cur.fetchall():
                profiles.append({
                    "id": row[0],
                    "nome": row[1],
                    "descricao": row[2]
                })
            return profiles

def get_all_modules():
    """Obtém todos os módulos disponíveis"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT id, nome, endpoint_base, descricao FROM modulos ORDER BY nome")
            modules = []
            for row in cur.fetchall():
                modules.append({
                    "id": row[0],
                    "nome": row[1],
                    "endpoint_base": row[2],
                    "descricao": row[3]
                })
            return modules

def assign_user_profile(user_id: int, profile_id: int):
    """Atribui um perfil a um usuário"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # Remove perfis existentes (um usuário pode ter apenas um perfil)
            cur.execute("DELETE FROM usuario_perfis WHERE usuario_id = %s", (user_id,))
            
            # Atribui novo perfil
            cur.execute("""
                INSERT INTO usuario_perfis (usuario_id, perfil_id)
                VALUES (%s, %s)
            """, (user_id, profile_id))
        conn.commit()

def get_active_api_keys():
    """Obtém todas as API Keys ativas"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT ak.id, p.nome as perfil, ak.descricao, ak.criado_em, ak.expira_em, ak.limites_uso
                FROM api_keys ak
                JOIN perfis p ON ak.perfil_id = p.id
                WHERE ak.ativa = true
                ORDER BY ak.criado_em DESC
            """)
            keys = []
            for row in cur.fetchall():
                keys.append({
                    "id": row[0],
                    "perfil": row[1],
                    "descricao": row[2],
                    "criado_em": row[3],
                    "expira_em": row[4],
                    "limites_uso": row[5]
                })
            return keys

def get_module_by_name(module_name: str):
    """Obtém um módulo pelo nome."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "SELECT id, nome, endpoint_base, descricao FROM modulos WHERE nome = %s",
                (module_name,)
            )
            module = cur.fetchone()
            if module:
                return {"id": module[0], "nome": module[1], "endpoint_base": module[2], "descricao": module[3]}
    return None

def update_profile_name(profile_id: int, new_name: str):
    """Atualiza o nome de um perfil."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "UPDATE perfis SET nome = %s WHERE id = %s",
                (new_name, profile_id)
            )
        conn.commit()

def clear_profile_permissions(profile_id: int):
    """Remove todas as permissões de módulo de um perfil."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "DELETE FROM perfil_modulos WHERE perfil_id = %s",
                (profile_id,)
            )
        conn.commit()

def add_permission_to_profile(profile_id: int, module_id: int):
    """Adiciona uma permissão de módulo a um perfil."""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO perfil_modulos (perfil_id, modulo_id, pode_acessar)
                VALUES (%s, %s, true)
                ON CONFLICT (perfil_id, modulo_id) DO UPDATE SET pode_acessar = true
            """, (profile_id, module_id))
        conn.commit()