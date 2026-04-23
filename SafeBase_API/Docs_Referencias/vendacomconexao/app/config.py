import os
import logging
from app.services.ia_orchestrator import ia_orchestrator
from dotenv import load_dotenv

logger = logging.getLogger(__name__)
load_dotenv()

# Configuração de inicialização do ambiente
AMBIENTE_ENV = os.getenv("AMBIENTE_ENV", "development")

# Configuração do serviço de logging da API
API_LOGGING_ENABLED = os.getenv("API_LOGGING_ENABLED", "false").lower() == 'true'
# Caches globais da aplicação
response_cache = {}
conversation_analytics_cache = {}

try:
    ia_initial_status = ia_orchestrator.get_system_status()
    PROVIDER_NOME = "orchestrator"
    # Usar .get() para segurança caso a chave não exista
    USE_MOCK_FORCADO = ia_initial_status.get('mock_ativo', True)
    
    logger.info(f"Sistema IA configurado - Provedor: {PROVIDER_NOME}, Mock: {USE_MOCK_FORCADO}")
    stats = ia_initial_status.get('estatisticas', {})
    logger.info(f"Status IA - Provedores operacionais: {stats.get('provedores_operacionais', 0)}")
    
except Exception as e:
    logger.error(f"Falha crítica na configuração IA: {e}")
    PROVIDER_NOME = "error"
    USE_MOCK_FORCADO = True