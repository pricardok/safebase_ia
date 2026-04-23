import logging
import time
from fastapi import Request, HTTPException

from app.services.rbac_service import rbac_service
from app.services.audit_service import audit_service
from app.services.ia_orchestrator import ia_orchestrator
from app.database import get_user_by_username

logger = logging.getLogger(__name__)

def check_module_permission(module_name: str):
    """
    Fábrica de dependências que retorna uma função de verificação de permissão para um módulo específico.
    """
    def dependency(request: Request):
        """Verifica se o usuário atual tem permissão para acessar um módulo com auditoria"""
        try:
            # A lógica de verificação de permissão foi movida para o rbac_service
            # para centralizar as regras de negócio de acesso.
            granted = rbac_service.check_module_access(request, module_name)
            audit_service.log_module_access(request, module_name, granted)
            return granted
        except HTTPException as e:
            # Loga a falha de acesso e relança a exceção.
            audit_service.log_module_access(request, module_name, False)
            raise e
    return dependency

def get_current_user_dependency(request: Request) -> dict:
    """
    Dependência para obter o objeto completo do usuário a partir do estado da requisição.
    Esta é a forma padronizada e segura de obter o usuário autenticado.
    """
    if not hasattr(request.state, 'user'):
        raise HTTPException(status_code=401, detail="Não autenticado")
    # CORREÇÃO: O estado 'user' contém o payload do token (dicionário). Acessamos o 'sub' (username).
    username = request.state.user.get("sub")
    if not username:
        raise HTTPException(status_code=401, detail="Token inválido, 'sub' não encontrado.")
    user = get_user_by_username(username)
    if not user:
        raise HTTPException(status_code=404, detail="Usuário não encontrado")
    return user

async def chamar_ia_otimizado(prompt: str, use_cache: bool = True):
    """
    Chama o provedor de IA usando o orquestrador, com lógica de cache e métricas.
    Esta função agora é o ponto central para todas as chamadas de IA.
    """
    start_time = time.time()

    # Verifica se o modo mock está forçado pelo orquestrador
    if ia_orchestrator.should_use_mock():
        raise Exception("Modo mock ativo via orquestrador")

    cache_key = hash(prompt)
    # A lógica de cache foi movida para dentro do loop para evitar chamadas desnecessárias

    max_retries = 2
    # Nova lógica: Obtém uma lista global de chaves já ordenada pela saúde
    globally_sorted_keys = ia_orchestrator.get_globally_sorted_keys()

    if not globally_sorted_keys:
        logger.error("Nenhuma chave operacional encontrada em nenhum provedor.")
        raise Exception("Nenhuma chave de IA operacional disponível.")

    # Itera sobre a lista global das melhores chaves disponíveis
    for key_info in globally_sorted_keys:
        chave_id = key_info['id']
        provider_name = key_info['provider_name']
        config_modelo = key_info['provider_config']

        logger.info(f"🔧 Tentando a melhor chave disponível: ID {chave_id} do provedor {provider_name}")

        try:
            chave_real = ia_orchestrator.get_real_key_for_use(chave_id)
            if not chave_real:
                logger.warning(f"Não foi possível obter a chave real para o ID {chave_id}. Pulando.")
                continue

            resultado = await ia_orchestrator.call_provider(provider_name, chave_real, prompt, config_modelo)

            response_time = time.time() - start_time
            ia_orchestrator.mark_key_success(chave_id)
            ia_orchestrator.record_request_metrics(response_time, cache_hit=False)

            return resultado

        except Exception as provider_error:
            logger.warning(f"❌ Falha na chave ID {chave_id} ({provider_name}): {provider_error}")
            ia_orchestrator.mark_key_failure(key_info['id'], str(provider_error))
            # Continua para a próxima melhor chave na lista global

    # Se todas as tentativas falharem, lança uma exceção para que a rota possa usar o mock.
    logger.error("Todos os provedores de IA falharam.")
    
    # Verifica se o fallback para mock está habilitado
    fallback_enabled_str = ia_orchestrator._get_cached_config('MOCK_FALLBACK_ENABLED')
    if fallback_enabled_str and fallback_enabled_str.lower() == 'true':
        ia_orchestrator.record_request_metrics(time.time() - start_time, cache_hit=False)
        raise Exception("Todos os provedores de IA falharam. Ativando modo de contingência (mock).")
    else:
        logger.critical("MOCK_FALLBACK_ENABLED está desativado. Retornando erro final.")
        raise HTTPException(status_code=503, detail="Serviço de IA indisponível no momento.")
