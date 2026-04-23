import logging
import json
from typing import Optional

from .core import get_db_connection

logger = logging.getLogger(__name__)

def get_prompt_versoes(nome: str = None, ativa: bool = True):
    """ Obtém versões de prompts para A/B testing"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            query = """
                SELECT id, nome, versao, template, ativa, peso_teste, modulo, descricao, parametros, criado_em, atualizado_em
                FROM prompt_versoes
            """
            params = []
            
            conditions = []
            if nome:
                conditions.append("nome = %s")
                params.append(nome)
            if ativa is not None:
                conditions.append("ativa = %s")
                params.append(ativa)
            
            if conditions:
                query += " WHERE " + " AND ".join(conditions)
            
            query += " ORDER BY nome, criado_em DESC"
            
            cur.execute(query, params)
            versoes = []
            for row in cur.fetchall():
                versoes.append({
                    "id": row[0],
                    "nome": row[1],
                    "versao": row[2],
                    "template": row[3],
                    "ativa": row[4],
                    "peso_teste": float(row[5]) if row[5] else 1.0,
                    "modulo": row[6],
                    "descricao": row[7],
                    "parametros": row[8],
                    "criado_em": row[9],
                    "atualizado_em": row[10]
                })
            return versoes

def create_prompt_versao(nome: str, versao: str, template: str, modulo: str = None, 
                        descricao: str = None, parametros: dict = None, peso_teste: float = 1.0):
    """Cria nova versão de prompt para A/B testing"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                INSERT INTO prompt_versoes 
                (nome, versao, template, modulo, descricao, parametros, peso_teste)
                VALUES (%s, %s, %s, %s, %s, %s, %s)
                RETURNING id
            """, (nome, versao, template, modulo, descricao, 
                  json.dumps(parametros) if parametros else None, peso_teste))
            
            versao_id = cur.fetchone()[0]
            conn.commit()
            
            logger.info(f"✅ Versão de prompt criada: {nome} v{versao}")
            return versao_id

def update_prompt_versao_status(versao_id: int, ativa: bool):
    """Ativa/desativa versão de prompt"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                UPDATE prompt_versoes 
                SET ativa = %s, atualizado_em = CURRENT_TIMESTAMP
                WHERE id = %s
            """, (ativa, versao_id))
            conn.commit()
            
            status = "ativada" if ativa else "desativada"
            logger.info(f"Versão de prompt {versao_id} {status}")

def get_active_prompt_versoes_for_testing():
    """Obtém versões ativas para A/B testing com pesos"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT nome, versao, template, peso_teste, modulo
                FROM prompt_versoes 
                WHERE ativa = true
                ORDER BY nome, peso_teste DESC
            """)
            
            versoes_por_nome = {}
            for row in cur.fetchall():
                nome = row[0]
                if nome not in versoes_por_nome:
                    versoes_por_nome[nome] = []
                
                versoes_por_nome[nome].append({
                    "versao": row[1],
                    "template": row[2],
                    "peso_teste": float(row[3]) if row[3] else 1.0,
                    "modulo": row[4]
                })
            
            return versoes_por_nome

def update_prompt_versao(versao_id: int, ativa: bool = None, peso_teste: float = None, descricao: str = None):
    """Atualiza versão de prompt completamente"""
    with get_db_connection() as conn:
        with conn.cursor() as cur:
            query = "UPDATE prompt_versoes SET atualizado_em = CURRENT_TIMESTAMP"
            params = []
            
            updates = []
            if ativa is not None:
                updates.append("ativa = %s")
                params.append(ativa)
            if peso_teste is not None:
                updates.append("peso_teste = %s")
                params.append(peso_teste)
            if descricao is not None:
                updates.append("descricao = %s")
                params.append(descricao)
            
            if updates:
                query += ", " + ", ".join(updates)
            
            query += " WHERE id = %s"
            params.append(versao_id)
            
            cur.execute(query, params)
            conn.commit()
            
            logger.info(f"Versão de prompt {versao_id} atualizada")