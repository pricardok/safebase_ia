# backend/app/services/logging_service.py
import logging
from logging.handlers import QueueHandler, QueueListener
from queue import Queue
import json
from app.database import get_db_connection

class DatabaseLogHandler(logging.Handler):
    """
    Um handler de log que escreve registros em uma tabela do banco de dados.
    """
    def emit(self, record):
        try:
            # Correção Definitiva: Construir o dicionário 'log_entry' explicitamente
            # para garantir que ele contenha apenas as chaves esperadas pela query.
            details = getattr(record, 'details', None)
            
            log_entry = {
                "level": record.levelname,
                "message": record.getMessage(),
                "path": getattr(record, 'path', None),
                "method": getattr(record, 'method', None),
                "status_code": getattr(record, 'status_code', None),
                "user_id": getattr(record, 'user_id', None),
                "username": getattr(record, 'username', None),
                "cliente_id": getattr(record, 'cliente_id', None),
                "razao_social": getattr(record, 'razao_social', None),
                "auth_type": getattr(record, 'auth_type', None),
                "plano": getattr(record, 'plano', None),
                "ip_address": getattr(record, 'ip_address', None),
                "details": json.dumps(details) if details is not None else None
            }

            # Correção: Usar 'get_db_connection' como um gerenciador de contexto.
            # O 'with' garante que a conexão seja aberta e fechada corretamente.
            with get_db_connection() as conn:
                with conn.cursor() as cur:
                    cur.execute("""
                        INSERT INTO api_logs (level, message, path, method, status_code, user_id, username, cliente_id, razao_social, auth_type, plano, ip_address, details)
                        VALUES (%(level)s, %(message)s, %(path)s, %(method)s, %(status_code)s, %(user_id)s, %(username)s, %(cliente_id)s, %(razao_social)s, %(auth_type)s, %(plano)s, %(ip_address)s, %(details)s)
                    """, log_entry)
                conn.commit() # O commit deve ocorrer dentro do escopo da conexão, mas fora do escopo do cursor.
        except Exception as e:
            # Em caso de falha ao logar no banco, não quebramos a aplicação.
            # O erro será logado no handler padrão (console/arquivo).
            self.handleError(record)

def setup_database_logging():
    """
    Configura o logging assíncrono para o banco de dados para não bloquear as requisições.
    """
    db_handler = DatabaseLogHandler()
    
    # Usa uma fila para tornar a escrita no banco assíncrona
    log_queue = Queue(-1)  # Fila infinita
    queue_handler = QueueHandler(log_queue)
    
    # O listener pega os logs da fila e os envia para o handler do banco em uma thread separada
    listener = QueueListener(log_queue, db_handler, respect_handler_level=True)
    
    # Adiciona o handler de fila ao logger raiz
    root_logger = logging.getLogger()
    root_logger.addHandler(queue_handler)
    
    # Inicia o listener
    listener.start()
    
    logging.info("Logging para o banco de dados configurado e iniciado de forma assíncrona.")
    return listener
