import logging
import traceback
from .core import get_db_connection
from app.auth_jwt import get_password_hash, verify_password # type: ignore
from psycopg2.extras import DictCursor

logger = logging.getLogger(__name__)

def get_user_by_id(user_id: int):
    """Busca um usuário pelo seu ID."""
    with get_db_connection() as conn:
        with conn.cursor(cursor_factory=DictCursor) as cur:
            cur.execute("""
                SELECT id, username, email, full_name, hashed_password, is_active, cliente_id, session_salt, telefone
                FROM users WHERE id = %s
            """, (user_id,))
            user = cur.fetchone()
            if user:
                return dict(user)
    return None

def get_user_by_username(username: str):
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT id, username, email, full_name, hashed_password, is_active, cliente_id, session_salt, telefone
                FROM users WHERE username = %s
            """, (username,))
            user = cur.fetchone()
            if user:
                return {
                    "id": user[0],
                    "username": user[1],
                    "email": user[2],
                    "full_name": user[3],
                    "hashed_password": user[4],
                    "is_active": user[5],
                    "cliente_id": user[6],
                    "session_salt": user[7]
                    ,"telefone": user[8]
                }
    return None

def get_user_details_by_id(user_id: int) -> dict:
    """
    Busca os detalhes completos de um usuário, incluindo informações
    do cliente e do plano atual, se existirem.
    """
    # Importação local para quebrar a dependência circular
    from .planos import get_plano_do_cliente

    user = get_user_by_id(user_id)
    if not user:
        return None

    plano_atual = None
    cliente_info = None

    if user.get("cliente_id"):
        plano_data = get_plano_do_cliente(user["cliente_id"])
        if plano_data:
            # Importa o modelo aqui para evitar dependência circular
            from app.models_planos import PlanoEfetivoResponse
            plano_atual = PlanoEfetivoResponse(**plano_data).model_dump()
            cliente_info = {
                "id": user["cliente_id"],
                "status": plano_data.get("status_cliente"),
                "trial_expires_at": plano_data.get("trial_expires_at")
            }

    user["cliente"] = cliente_info
    user["plano_atual"] = plano_atual

    return user

def get_user_by_login_identifier(identifier: str):
    """
    Busca um usuário pelo username OU pelo email.
    """
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            # CORRIGIDO: Adicionado o campo 'must_change_password' na consulta
            cur.execute("""
                SELECT id, username, email, full_name, hashed_password, is_active, cliente_id, must_change_password, telefone
                FROM users WHERE username = %s OR email = %s
            """, (identifier, identifier))
            user = cur.fetchone()
            if user:
                return {
                    "id": user[0],
                    "username": user[1],
                    "email": user[2],
                    "full_name": user[3],
                    "hashed_password": user[4],
                    "is_active": user[5],
                    "cliente_id": user[6],
                    "must_change_password": user[7]
                    ,"telefone": user[8]
                }
    return None

def create_user(username: str, email: str, password: str, full_name: str = None, cliente_id: str = None, must_change_password: bool = False):
    from app.auth_jwt import get_password_hash
    
    hashed_password = get_password_hash(password)
    
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO users (username, email, full_name, hashed_password, cliente_id, must_change_password) 
                VALUES (%s, %s, %s, %s, %s, %s) RETURNING id
            """, (username, email, full_name, hashed_password, cliente_id, must_change_password))
            user_id = cur.fetchone()[0]
        conn.commit()
    
    return user_id


def get_or_create_system_user(username: str = 'webhook_bot') -> int:
    """
    Garante que exista um usuário de sistema para gravação de simulações automatizadas.
    Retorna o `id` do usuário.
    """
    # Tenta achar usuário existente
    user = get_user_by_username(username)
    if user:
        return user['id'] if isinstance(user, dict) else user[0]

    # Cria um usuário com senha aleatória
    import secrets
    random_password = secrets.token_urlsafe(16)
    email = f"{username}@local"
    full_name = "System Webhook User"
    try:
        user_id = create_user(username=username, email=email, password=random_password, full_name=full_name, cliente_id=None)
        logger.info(f"Criado usuário de sistema '{username}' com id {user_id}")
        return user_id
    except Exception as e:
        logger.error(f"Falha ao criar usuário de sistema '{username}': {e}")
        return None

def get_user_by_email(email: str):
    """Buscar usuário por email"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "SELECT id, username, email, full_name, hashed_password, is_active, telefone FROM users WHERE email = %s",
                (email,)
            )
            user = cur.fetchone()
            if user:
                return {
                    "id": user[0],
                    "username": user[1],
                    "email": user[2],
                    "full_name": user[3],
                    "hashed_password": user[4],
                    "is_active": user[5],
                    "telefone": user[6]
                }
    return None

def update_user_password(user_id: int, hashed_password: str):
    """Atualizar senha do usuário"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "UPDATE users SET hashed_password = %s WHERE id = %s",
                (hashed_password, user_id)
            )
        conn.commit()

def update_user(user_id: int, **kwargs) -> bool:
    """
    Atualiza dados do usuário
    """
    try:
        logger.info(f"Tentando atualizar usuário {user_id} com campos: {list(kwargs.keys())}")
        
        with get_db_connection() as conn:
            cursor = conn.cursor()
            
            if not kwargs:
                logger.warning("Nenhum campo para atualizar")
                return False
            
            set_clause = ", ".join([f"{key} = %s" for key in kwargs.keys()])
            values = list(kwargs.values())
            values.append(user_id)
            
            query = f"UPDATE users SET {set_clause} WHERE id = %s"
            logger.info(f"Query: {query}")
            logger.info(f"Valores: {values}")
            
            cursor.execute(query, values)
            conn.commit()
            
            rows_affected = cursor.rowcount
            logger.info(f"Linhas afetadas: {rows_affected}")
            
            return rows_affected > 0
            
    except Exception as e:
        logger.error(f"Erro ao atualizar usuário {user_id}: {e}")
        logger.error(f"Stack trace: {traceback.format_exc()}")
        return False

def get_all_users() -> list:
    """Busca todos os usuários do sistema para o painel de admin."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("""
                    SELECT 
                        u.id, 
                        u.username, 
                        u.email, 
                        u.full_name, 
                        u.is_active,
                        p.nome as perfil_nome
                    FROM users u
                    LEFT JOIN usuario_perfis pu ON u.id = pu.usuario_id
                    LEFT JOIN perfis p ON pu.perfil_id = p.id
                    ORDER BY u.created_at DESC;
                """)
                
                # Formata a saída para corresponder à interface do frontend
                users = []
                for row in cur.fetchall():
                    users.append({
                        "id": row["id"],
                        "nome": row["full_name"] or row["username"], # Usa full_name, com fallback para username
                        "email": row["email"],
                        "perfil": row["perfil_nome"] or "N/A", # Renomeia 'perfil_nome' para 'perfil'
                        "status": "ativo" if row["is_active"] else "inativo" # Converte o booleano para string
                    })
                return users
    except Exception as e:
        logger.error(f"Erro ao buscar todos os usuários: {e}")
        return []

def get_users_by_cliente_id(cliente_id: str) -> list:
    """
    Obtém todos os usuários associados a um cliente (tenant).
    """
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("""
                    SELECT u.id, u.username, u.email, u.full_name, u.is_active, p.nome as perfil_nome
                    FROM users u
                    LEFT JOIN perfis_usuarios pu ON u.id = pu.usuario_id
                    LEFT JOIN perfis p ON pu.perfil_id = p.id
                    WHERE u.cliente_id = %s
                    ORDER BY u.created_at DESC;
                """, (cliente_id,))
                return [dict(row) for row in cur.fetchall()]
    except Exception as e:
        logger.error(f"Erro ao buscar usuários para o cliente {cliente_id}: {e}")
        return []