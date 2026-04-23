# backend/app/services/__init__.py
from .rbac_service import rbac_service
from .history_service import history_service
from .database_context import db_context
from .audit_service import audit_service
from .security import generate_api_key, validate_api_key_format, mask_api_key, generate_temporary_password
from .crypto_manager import crypto_manager
from .prompt_service import prompt_service
from .ia_orchestrator import ia_orchestrator
from .key_manager import key_manager
from .signup_service import signup_service
from .utils_service import ( 
    parsear_resposta_contexto,
    parsear_resposta_predicao_objecoes,
    parsear_resposta_mudanca_emocional,
    parsear_analise_resposta_conexao,
    parsear_analise_textual,
    analisar_probabilidade_conversao,
    parsear_respostas_ia,
    gerar_resposta_mock_simulador,
    gerar_feedback_mock,
    quebrar_objecao_mock,
    detector_analisar_mock,
    gerar_mock_contexto,
    gerar_mock_predicao_objecoes,
    gerar_mock_mudanca_emocional
)

__all__ = [
    'rbac_service',
    'history_service', 
    'db_context',
    'audit_service',
    'signup_service',
    'generate_api_key',
    'validate_api_key_format',
    'mask_api_key',
    'crypto_manager',
    'prompt_service',
    'ia_orchestrator',
    'key_manager',
    'generate_temporary_password',
    # 
    'parsear_resposta_contexto',
    'parsear_resposta_predicao_objecoes',
    'parsear_resposta_mudanca_emocional',
    'parsear_analise_resposta_conexao',
    'parsear_analise_textual',
    'analisar_probabilidade_conversao',
    'parsear_respostas_ia',
    'gerar_resposta_mock_simulador',
    'gerar_feedback_mock',
    'quebrar_objecao_mock',
    'detector_analisar_mock',
    'gerar_mock_contexto',
    'gerar_mock_predicao_objecoes',
    'gerar_mock_mudanca_emocional'
]