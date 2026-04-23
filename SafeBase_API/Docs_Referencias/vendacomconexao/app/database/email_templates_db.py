# app/database/email_templates_db.py
import logging
import json
from typing import List, Dict, Any, Optional
from psycopg2.extras import DictCursor
from app.database.core import get_db_connection
from app.models import EmailTemplateCreate, EmailTemplateUpdate


logger = logging.getLogger(__name__)

def get_email_template_by_chave(chave: str) -> Optional[Dict[str, Any]]:
    """Busca um template de e-mail pela sua chave única."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute(
                    "SELECT * FROM email_templates WHERE chave = %s AND ativo = TRUE",
                    (chave,)
                )
                template = cur.fetchone()
                return dict(template) if template else None
    except Exception as e:
        logger.error(f"Erro ao buscar template de e-mail pela chave '{chave}': {e}")
        return None

def get_all_email_templates() -> List[Dict[str, Any]]:
    """Lista todos os templates de e-mail do banco de dados."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("SELECT * FROM email_templates ORDER BY tipo, nome")
                templates = [dict(row) for row in cur.fetchall()]
                return templates
    except Exception as e:
        logger.error(f"Erro ao listar todos os templates de e-mail: {e}")
        return []

def get_email_template_by_id(template_id: int) -> Optional[Dict[str, Any]]:
    """Busca um template de e-mail pelo seu ID."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                cur.execute("SELECT * FROM email_templates WHERE id = %s", (template_id,))
                template = cur.fetchone()
                return dict(template) if template else None
    except Exception as e:
        logger.error(f"Erro ao buscar template de e-mail por ID {template_id}: {e}")
        return None

def create_email_template(template_data: EmailTemplateCreate) -> Optional[Dict[str, Any]]:
    """Cria um novo template de e-mail no banco de dados."""
    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                # CORREÇÃO: A lógica de extrair variáveis foi removida.
                # Agora, o valor de 'variaveis_disponiveis' vem diretamente do payload (template_data).
                cur.execute("""
                    INSERT INTO email_templates (chave, nome, assunto, html_content, text_content, variaveis_disponiveis, tipo, ativo)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT (chave) DO NOTHING
                    RETURNING *;
                """, (
                    template_data.chave, template_data.nome, template_data.assunto,
                    template_data.html_content, template_data.text_content,
                    json.dumps(template_data.variaveis_disponiveis),
                    template_data.tipo, template_data.ativo
                ))
                new_template = cur.fetchone()
                conn.commit()
                return dict(new_template) if new_template else None
    except Exception as e:
        logger.error(f"Erro ao criar novo template de e-mail: {e}")
        conn.rollback()
        return None

def update_email_template(template_id: int, update_data: EmailTemplateUpdate) -> Optional[Dict[str, Any]]:
    """Atualiza um template de e-mail existente."""
    update_dict = update_data.model_dump(exclude_unset=True)
    if not update_dict:
        return get_email_template_by_id(template_id)

    # Converte 'variaveis_disponiveis' para JSON string se presente
    if 'variaveis_disponiveis' in update_dict:
        update_dict['variaveis_disponiveis'] = json.dumps(update_dict['variaveis_disponiveis'])

    set_clause = ", ".join([f"{key} = %s" for key in update_dict.keys()])
    values = list(update_dict.values())
    values.append(template_id)

    try:
        with get_db_connection() as conn:
            with conn.cursor(cursor_factory=DictCursor) as cur:
                query = f"UPDATE email_templates SET {set_clause}, updated_at = NOW() WHERE id = %s RETURNING *;"
                cur.execute(query, values)
                updated_template = cur.fetchone()
                conn.commit()
                return dict(updated_template) if updated_template else None
    except Exception as e:
        logger.error(f"Erro ao atualizar template de e-mail {template_id}: {e}")
        conn.rollback()
        return None

def delete_email_template(template_id: int) -> bool:
    """Exclui um template de e-mail do banco de dados."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                # Primeiro, verifica se o template não é do tipo SYSTEM
                cur.execute("SELECT tipo FROM email_templates WHERE id = %s", (template_id,))
                template = cur.fetchone()
                if not template:
                    return False # Template não existe
                if template[0] == 'SYSTEM':
                    logger.warning(f"Tentativa de exclusão do template de sistema ID {template_id} bloqueada.")
                    return False

                cur.execute("DELETE FROM email_templates WHERE id = %s", (template_id,))
                conn.commit()
                return cur.rowcount > 0
    except Exception as e:
        logger.error(f"Erro ao excluir template de e-mail {template_id}: {e}")
        conn.rollback()
        return False

