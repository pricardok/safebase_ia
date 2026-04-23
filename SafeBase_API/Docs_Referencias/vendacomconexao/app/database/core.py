# app/database/core.py
import psycopg2
from psycopg2 import pool
import os
from dotenv import load_dotenv
from contextlib import contextmanager
import hashlib
import json
import traceback
from datetime import datetime
from typing import Optional

import logging

logger = logging.getLogger(__name__)

load_dotenv()

# Configuração do PostgreSQL
DATABASE_URL = os.getenv("DATABASE_URL")

# --- IMPLEMENTAÇÃO DO CONNECTION POOL ---
# Cria um pool de conexões na inicialização do módulo.
try:
    connection_pool = psycopg2.pool.SimpleConnectionPool(
        minconn=5, # Aumentando o número mínimo de conexões
        maxconn=25,  # Ajuste conforme a necessidade e capacidade do seu banco
        dsn=DATABASE_URL
    )
    logger.info("✅ Pool de conexões com o banco de dados criado com sucesso.")
except Exception as e:
    logger.critical(f"❌ Falha crítica ao criar o pool de conexões: {e}")
    connection_pool = None

@contextmanager
def get_db_connection():
    """Obtém uma conexão do pool e a devolve ao final."""
    if not connection_pool:
        raise Exception("Pool de conexões não está disponível.")
    conn = connection_pool.getconn()

    try:
        yield conn
    except Exception as e:
        logger.error(f"Erro ao obter conexão do pool: {e}")
        conn.rollback()
        raise # Re-lança a exceção
    finally:
        connection_pool.putconn(conn)

def get_total_users_count() -> int:
    """Retorna o número total de usuários no sistema."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT COUNT(*) FROM users;")
                result = cur.fetchone()
                return result[0] if result else 0
    except Exception as e:
        logger.error(f"Erro ao contar usuários: {e}")
        return 0

def get_clients_summary_metrics() -> dict:
    """Retorna o número total de clientes e o número de clientes ativos."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT
                        COUNT(*) AS total_clients,
                        COUNT(*) FILTER (WHERE status = 'ativo') AS active_clients
                    FROM clientes;
                """)
                result = cur.fetchone()
                return {"total_clients": result[0], "active_clients": result[1]} if result else {"total_clients": 0, "active_clients": 0}
    except Exception as e:
        logger.error(f"Erro ao buscar métricas de clientes: {e}")
        return {"total_clients": 0, "active_clients": 0}

def get_pending_webhooks_count() -> int:
    """Retorna o número de webhooks que falharam e podem precisar de re-processamento."""
    try:
        with get_db_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT COUNT(*) FROM webhook_events WHERE status = 'failed';")
                result = cur.fetchone()
                return result[0] if result else 0
    except Exception as e:
        logger.error(f"Erro ao contar webhooks pendentes: {e}")
        return 0