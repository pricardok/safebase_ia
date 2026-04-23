# app/routes/admin_routes.py
from fastapi import APIRouter, Request, HTTPException, Depends
from typing import List, Dict, Any, Optional, Union, Annotated
from pydantic import BaseModel 
import logging, json
from datetime import datetime
import uuid 

from app.database import (
    get_all_profiles, get_all_modules, create_api_key, deactivate_api_key,
    get_active_api_keys, assign_user_profile, get_user_by_username, get_profile_permissions, get_api_logs,
    get_prompt_versoes, create_prompt_versao, update_prompt_versao, get_active_prompt_versoes_for_testing, get_user_by_id, update_user, update_plano, get_all_system_configs, update_system_config, update_cliente_status, atribuir_plano_cliente,
    get_all_email_templates, get_email_template_by_id, create_email_template, update_email_template, delete_email_template, # type: ignore
    get_webhook_logs, get_user_sessions, get_webhook_event_by_id, get_clients_summary_metrics, get_all_clientes, get_all_users, get_cliente_details_for_admin, criar_desconto_cliente, get_all_planos_for_admin,

    get_planos_publicos, get_plano_by_id,
    get_total_users_count, get_pending_webhooks_count, get_db_connection, get_planos_publicos as db_get_planos_publicos)
from app.database.planos import update_cliente_whatsapp_webhooks_enabled
from app.models import ( 
    KeyCreate, KeyResponse, GlobalKeyStatusResponse, ProfileUpdateRequest, # type: ignore
    PromptVersaoCreate, PromptVersaoResponse, PromptVersaoUpdate,
    EmailTemplateCreate, EmailTemplateUpdate, EmailTemplateResponse, AdminEmailStatusResponse, TestEmailRequest
)
from app.models_planos import PlanoUpdateRequest,SignupRequest
from app.auth_jwt import get_password_hash 
from app.services import ia_orchestrator, audit_service, webhook_service, rbac_service, email_service, signup_service
from app.utils.parsing_utils import parsing_utils
from app.config import (
    response_cache, conversation_analytics_cache, API_LOGGING_ENABLED

)
from app.dependencies import check_module_permission
from app.services.email_service import email_service

logger = logging.getLogger(__name__)

# importações diretas do diretorio app/database/
from app.database import admin_core  
router = APIRouter(
    prefix="/admin",
    tags=["Administração"])

# Modelo específico para a criação de cliente pelo admin
class AdminClientCreateRequest(BaseModel):
    razao_social: str
    email_contato: str
    plano_id: int
    # Dados do primeiro usuário
    full_name: str
    username: str
    password: str


@router.get("/summary",  dependencies=[Depends(check_module_permission("admin"))])
async def get_admin_summary():
    """
    Retorna um resumo completo do status do sistema para o dashboard administrativo.
    """

    try:
        # Métricas de Negócio
        client_metrics = get_clients_summary_metrics()
        total_users = get_total_users_count()
        pending_webhooks = get_pending_webhooks_count()

        # Métricas da IA
        ia_status = ia_orchestrator.get_system_status()
        ia_performance = ia_status.get('performance', {}) # As métricas de performance já vêm no status do sistema

        # Status do Banco
        db_status = "Conectado"
        try:
            with get_db_connection() as conn:
                with conn.cursor() as cur:
                    cur.execute("SELECT 1")
        except Exception:
            db_status = "Erro"

        summary = {
            "api_status": "Online",
            "database_status": db_status,
            "ia_system_status": "Operacional" if not ia_status.get('mock_ativo') else "Degradado",
            "ia_metrics": {
                "mode": "production", # Pode ser ajustado com base em uma variável de ambiente
                "operational_providers": ia_status.get('estatisticas', {}).get('provedores_operacionais', 0),
                "active_keys": ia_status.get('estatisticas', {}).get('chaves_ativas', 0),
                "cache_hit_rate": ia_performance.get('cache_hit_ratio', 0) * 100,
                "average_latency_p95_ms": int(ia_performance.get('p95_latency', 0) * 1000)
            },
            "business_metrics": {
                "total_clients": client_metrics.get('total_clients', 0),
                "active_clients": client_metrics.get('active_clients', 0),
                "total_users": total_users,
                "pending_webhooks": pending_webhooks
            }
        }
        return summary

    except Exception as e:
        logger.error(f"Erro ao gerar o resumo administrativo: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao gerar o resumo do sistema.")

@router.get("/clientes",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_clientes_admin():
    """
    Lista todos os clientes (tenants) do sistema. Acessível apenas por administradores.
    """
    try:
        return get_all_clientes()
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao listar clientes: {e}")

@router.get("/clientes/{cliente_id}", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def get_cliente_details_admin(cliente_id: str):
    """
    Retorna os detalhes completos de um cliente específico para o painel de administração.
    """
    try:
        cliente = get_cliente_details_for_admin(cliente_id)
        if not cliente:
            raise HTTPException(status_code=404, detail="Cliente não encontrado.")
        return cliente
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao buscar detalhes do cliente {cliente_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao buscar detalhes do cliente.")

@router.post("/clientes/{cliente_id}/desconto", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def aplicar_desconto_cliente_admin(cliente_id: str, request_data: Dict[str, Any]):
    """
    Aplica um desconto a um cliente específico.
    """
    try:
        # A função criar_desconto_cliente já lida com ON CONFLICT para atualizar ou inserir
        desconto_id = criar_desconto_cliente(cliente_id, **request_data)
        return {"message": "Desconto aplicado/atualizado com sucesso.", "desconto_id": desconto_id}
    except Exception as e:
        logger.error(f"Erro ao aplicar desconto ao cliente {cliente_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao aplicar desconto.")

@router.put("/clientes/{cliente_id}", tags=["Administração"], name="atualizar_cliente_admin",  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_cliente_pelo_admin(cliente_id: str, update_data: dict):
    """
    Atualiza o plano ou o status de um cliente.
    Exemplo de corpo: {"plano_id": 5} ou {"status": "inativo"}
    """
    try:
        if "plano_id" in update_data:
            atribuir_plano_cliente(cliente_id, update_data["plano_id"])
        
        if "status" in update_data:
            update_cliente_status(cliente_id, update_data["status"])
        
        # Permite ao admin habilitar/desabilitar webhooks WhatsApp via API
        if "whatsapp_webhooks_enabled" in update_data:
            enabled = bool(update_data.get("whatsapp_webhooks_enabled"))
            update_cliente_whatsapp_webhooks_enabled(cliente_id, enabled)

        return {"message": f"Cliente {cliente_id} atualizado com sucesso."}
    except Exception as e:
        logger.error(f"Erro ao atualizar cliente {cliente_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar cliente.")

@router.post("/clientes", tags=["Administração"], name="criar_cliente_admin",  dependencies=[Depends(check_module_permission("admin"))])
async def criar_cliente_pelo_admin(client_data: AdminClientCreateRequest):
    """
    Cria um novo cliente (tenant) e seu primeiro usuário administrador.
    Acessível apenas por administradores do sistema.
    """
    try:
        # Valida se o plano existe
        plano = get_plano_by_id(client_data.plano_id)
        if not plano:
            raise HTTPException(status_code=404, detail=f"Plano com ID {client_data.plano_id} não encontrado.")

        # Adapta os dados para o signup_service, preenchendo os campos obrigatórios que não vêm do front
        signup_data = SignupRequest(
            razao_social=client_data.razao_social,
            email_contato=client_data.email_contato,
            full_name=client_data.full_name,
            username=client_data.username,
            password=client_data.password,
            # Preenche campos obrigatórios com valores padrão
            documento="00000000000", # CPF genérico
            tipo_pessoa="F",
            telefone="00000000000"
        )

        # Força o plano selecionado pelo admin, sobrescrevendo o padrão do service
        result = await signup_service.register_new_cliente_and_user(
            signup_data, 
            plano_id_override=client_data.plano_id
        )

        return {
            "message": "Cliente e usuário principal criados com sucesso.",
            **result
        }
    except HTTPException as e:
        raise e # Re-lança exceções de negócio (ex: email já existe)
    except Exception as e:
        logger.error(f"Erro ao criar cliente pelo admin: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Erro interno ao criar cliente: {str(e)}")

@router.get("/planos", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def listar_planos_admin():
    """
    Lista todos os planos de assinatura disponíveis no sistema.
    """
    try:
        # CORREÇÃO: Usa a função específica para admin que retorna todos os planos, incluindo o status 'ativo'.
        return get_all_planos_for_admin()
    except Exception as e:
        logger.error(f"Erro ao listar planos para admin: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar planos.")

@router.put("/planos/{plano_id}", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_plano_admin(plano_id: int, update_data: PlanoUpdateRequest):
    """
    Atualiza os detalhes de um plano de assinatura existente.
    """
    
    update_dict = update_data.model_dump(exclude_unset=True)
    if not update_dict:
        raise HTTPException(status_code=400, detail="Nenhum campo fornecido para atualização.")

    try:
        success = update_plano(plano_id, **update_dict)
        if not success:
            raise HTTPException(status_code=404, detail=f"Plano com ID {plano_id} não encontrado ou nenhuma alteração foi feita.")
        return {"message": f"Plano {plano_id} atualizado com sucesso."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao atualizar o plano: {e}")

@router.get("/configuracoes", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def listar_configuracoes_sistema():
    """
    Lista todas as configurações dinâmicas do sistema.
    """
    try:
        return get_all_system_configs()
    except Exception as e:
        logger.error(f"Erro ao listar configurações do sistema: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar configurações.")

@router.put("/configuracoes", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_configuracoes_sistema(updates: Dict[str, Any]):
    """
    Atualiza uma ou mais configurações do sistema.
    O corpo da requisição deve ser um dicionário: {"chave": "novo_valor"}.
    """
    try:
        for chave, valor in updates.items():
            update_system_config(chave, valor)
        # TODO: Adicionar lógica para limpar caches relevantes após a atualização.
        return {"message": "Configurações atualizadas com sucesso."}
    except Exception as e:
        logger.error(f"Erro ao atualizar configurações do sistema: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar configurações.")

@router.get("/permissoes", tags=["Administração"], name="listar_permissoes_disponiveis",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_permissoes_disponiveis():
    """
    Retorna uma lista de todas as strings de permissão válidas no sistema.
    """
    try:
        return rbac_service.get_all_possible_permissions()
    except Exception as e:
        logger.error(f"Erro ao listar permissões disponíveis: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar permissões.")

@router.put("/perfis/{perfil_id}", tags=["Administração"], name="atualizar_perfil",  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_perfil(perfil_id: int, update_data: ProfileUpdateRequest):
    """
    Atualiza o nome e/ou a lista de permissões de um perfil.
    """
    
    try:
        # A lógica de negócio para traduzir permissões em módulos e atualizar o perfil
        # foi encapsulada no RBAC Service para manter o controller limpo.
        updated_profile = rbac_service.update_profile_with_permissions(
            perfil_id=perfil_id,
            nome=update_data.nome,
            permissoes=update_data.permissoes
        )
        if not updated_profile:
            raise HTTPException(status_code=404, detail=f"Perfil com ID {perfil_id} não encontrado.")
        
        return updated_profile
    except Exception as e:
        logger.error(f"Erro ao atualizar o perfil {perfil_id}: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Erro interno ao atualizar o perfil: {str(e)}")

@router.get("/modulos", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def listar_modulos():
    """
    Lista todos os módulos disponíveis (apenas admin)
    """
    
    try:
        modules = get_all_modules()
        return modules
    except Exception as e:
        logger.error(f"Erro ao listar módulos: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")

@router.post("/api-keys", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def criar_api_key(request: dict):
    """
    Cria uma nova API Key (apenas admin)
    """
    
    try:
        profile_id = request.get("perfil_id")
        description = request.get("descricao", "")
        usage_limit = request.get("limites_uso", 1000)
        
        if not profile_id:
            raise HTTPException(status_code=400, detail="perfil_id é obrigatório")
        
        result = create_api_key(profile_id, description, usage_limit)
        
        logger.info(f"✅ Nova API Key criada: {result['id']} para perfil {profile_id}")
        
        return {
            "id": result["id"],
            "api_key": result["api_key"], 
            "descricao": description,
            "perfil_id": profile_id,
            "limites_uso": usage_limit,
            "aviso": "GUARDE ESTA API KEY - ELA NÃO PODERÁ SER RECUPERADA NOVAMENTE"
        }
        
    except Exception as e:
        logger.error(f"Erro ao criar API Key: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")

@router.delete("/api-keys/{api_key_id}", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def revogar_api_key(api_key_id: str):
    """
    Revoga uma API Key (apenas admin)
    """
    
    try:
        deactivate_api_key(api_key_id)
        logger.info(f"✅ API Key revogada: {api_key_id}")
        
        return {"message": "API Key revogada com sucesso"}
        
    except Exception as e:
        logger.error(f"Erro ao revogar API Key: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")

@router.get("/api-keys", tags=["Administração"], name="listar_api_keys",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_api_keys():
    try:
        keys = get_active_api_keys()
        return keys
    except Exception as e:
        logger.error(f"Erro ao listar API Keys: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")

# Alias para compatibilidade com o frontend
router.add_api_route("/apikeys", router.routes[-1].endpoint, methods=["GET"], include_in_schema=False)

@router.get("/email/status", tags=["Administração"], response_model=AdminEmailStatusResponse,  dependencies=[Depends(check_module_permission("admin"))])
async def get_email_service_status_admin():
    """
    Retorna o status atual do serviço de e-mail para o painel de administração.
    """
    try:
        status_data = email_service.get_admin_status()
        return AdminEmailStatusResponse(**status_data)
    except Exception as e:
        logger.error(f"Erro ao obter status do serviço de e-mail: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao obter status do serviço de e-mail.")

@router.post("/email/test", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def send_test_email_admin(request_data: TestEmailRequest):
    """
    Envia um e-mail de teste para um destinatário especificado.
    """
    try:
        response = await email_service.send_test_email(request_data.recipient_email)
        if not response.success:
            raise HTTPException(status_code=500, detail=f"Falha ao enviar e-mail de teste: {response.message}")
        return {"message": "E-mail de teste enviado com sucesso.", "details": response.message}
    except Exception as e:
        logger.error(f"Erro ao enviar e-mail de teste: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Erro interno ao enviar e-mail de teste: {str(e)}")

@router.get("/usuarios", tags=["Administração"], name="listar_usuarios",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_usuarios_admin():
    """
    Lista todos os usuários do sistema.
    """
    return get_all_users()

# Alias para compatibilidade com o frontend
router.add_api_route("/users", router.routes[-1].endpoint, methods=["GET"], include_in_schema=False)

@router.put("/usuarios/{username}/perfil", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def alterar_perfil_usuario(username: str, request: dict):
    try:
        profile_id = request.get("perfil_id")
        if not profile_id:
            raise HTTPException(status_code=400, detail="perfil_id é obrigatório")
        user = get_user_by_username(username)
        if not user:
            raise HTTPException(status_code=404, detail="Usuário não encontrado")
        assign_user_profile(user["id"], profile_id)
        return {"message": f"Perfil do usuário {username} alterado com sucesso"}
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao alterar perfil: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")

@router.put("/usuarios/{user_id}", tags=["Administração"], name="atualizar_usuario",  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_usuario_admin(user_id: int, update_data: dict):
    """
    Atualiza dados de um usuário, como status de ativação ou perfil.
    Para desativar um usuário (soft delete), envie: {"is_active": false}
    """
    
    # Evita que campos não permitidos sejam atualizados
    allowed_fields = {"is_active", "full_name", "email"}
    update_payload = {k: v for k, v in update_data.items() if k in allowed_fields}

    if not update_payload:
        raise HTTPException(status_code=400, detail="Nenhum campo válido para atualização fornecido.")

    try:
        success = update_user(user_id, **update_payload)
        if not success:
            raise HTTPException(status_code=404, detail=f"Usuário com ID {user_id} não encontrado.")
        return {"message": f"Usuário {user_id} atualizado com sucesso."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao atualizar usuário: {e}")

@router.get("/usuarios/{user_id}/historico", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def admin_get_user_history(user_id: int):
    """
    Permite que um administrador visualize o histórico de sessões de um usuário específico.
    """
    user = get_user_by_id(user_id)
    if not user:
        raise HTTPException(status_code=404, detail="Usuário não encontrado.")
    return get_user_sessions(user_id)

@router.post("/usuarios/{user_id}/force-reset-password", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def admin_force_reset_password(user_id: int):
    """
    Força a redefinição de senha para um usuário.
    Um administrador pode usar este endpoint para gerar uma nova senha temporária
    e obrigar o usuário a trocá-la no próximo login.
    """
    user = get_user_by_id(user_id)
    if not user:
        raise HTTPException(status_code=404, detail="Usuário não encontrado.")

    try:
        from app.services.security import generate_temporary_password
        nova_senha_temporaria = generate_temporary_password()
        update_user(
            user_id=user_id,
            hashed_password=get_password_hash(nova_senha_temporaria),
            must_change_password=True
        )
        # TODO: Idealmente, enviar um e-mail para o usuário informando sobre o reset.
        return {
            "message": "Senha do usuário redefinida com sucesso. O usuário precisará alterá-la no próximo login.",
            "nova_senha_temporaria": nova_senha_temporaria # Retornar a senha para o admin poder informar ao usuário
        }
    except Exception as e:
        logger.error(f"Erro ao redefinir a senha do usuário {user_id}: {e}")
        raise HTTPException(status_code=500, detail=f"Erro ao redefinir a senha: {e}")

@router.get("/webhooks/logs", tags=["Administração"], name="listar_logs_webhook",  dependencies=[Depends(check_module_permission("admin"))])
async def admin_get_webhook_logs(limit: int = 100, offset: int = 0):
    """
    Lista os logs de eventos de webhooks recebidos pelo sistema, com paginação.
    """
    try:
        logs = get_webhook_logs(limit=limit, offset=offset)
        return logs
    except Exception as e:
        logger.error(f"Erro ao buscar logs de webhook: {e}")
        raise HTTPException(status_code=500, detail=f"Erro ao buscar logs de webhook: {e}")

# Alias para compatibilidade com o frontend
router.add_api_route("/webhooks", router.routes[-1].endpoint, methods=["GET"], include_in_schema=False)

@router.post("/webhooks/logs/{event_id}/reprocess", tags=["Administração"],  dependencies=[Depends(check_module_permission("admin"))])
async def admin_reprocess_webhook(event_id: uuid.UUID):
    """
    Permite que um administrador re-processe um evento de webhook que falhou.
    """
    
    event = get_webhook_event_by_id(event_id)
    if not event:
        raise HTTPException(status_code=404, detail="Evento de webhook não encontrado.")

    if event['status'] != 'failed':
        raise HTTPException(status_code=400, detail=f"O evento {event_id} não está com status 'failed'. Status atual: {event['status']}.")

    try:
        payload = event['payload']
        logger.info(f"ADMIN: Re-processando evento de webhook {event_id}...")
        # Chama diretamente o serviço de webhook com o payload original
        processing_result = await webhook_service.process_kiwify_event(payload)
        return {"message": "Evento de webhook re-processado com sucesso.", "details": processing_result}
    except Exception as e:
        logger.error(f"ADMIN: Falha ao re-processar evento {event_id}: {e}")
        raise HTTPException(status_code=500, detail=f"Falha ao re-processar o evento: {e}")

@router.get("/audit/logs", tags=["Administração"], name="listar_logs_auditoria",  dependencies=[Depends(check_module_permission("admin"))])
async def admin_get_audit_logs():
    """
    Retorna os logs de auditoria de operações sensíveis do sistema.
    """
    logs = audit_service.get_audit_logs()
    return logs

@router.get("/logs/api", tags=["Administração"], name="listar_logs_api",  dependencies=[Depends(check_module_permission("admin"))])
async def admin_get_api_logs(limit: int = 100, offset: int = 0, exclude_admin: Optional[bool] = False, search: Optional[str] = None): # Adicionando valores padrão
    """
    Retorna os últimos logs da API, se o serviço de logging estiver habilitado.
    """

    if not API_LOGGING_ENABLED:
        # Retornando um JSON padronizado em caso de erro, como o frontend espera.
        raise HTTPException(status_code=404, detail="O serviço de logging da API não está disponível ou habilitado.")

    logs = get_api_logs(limit=limit, offset=offset, exclude_admin=exclude_admin, search=search)
    return logs


@router.get("/webhooks", tags=["Administração"], name="listar_logs_webhook_duplicado", include_in_schema=False) # Marcado como duplicado
async def admin_get_webhook_logs_duplicado(limit: int = 100, offset: int = 0):
    """
    Lista os logs de eventos de webhooks recebidos pelo sistema, com paginação.
    """
    try:
        logs = get_webhook_logs(limit=limit, offset=offset)
        # Formata a saída para corresponder à interface do frontend
        return [
            {**log, "status": log.get("status", "pendente").lower()} 
            for log in logs
        ]
    except Exception as e:
        logger.error(f"Erro ao buscar logs de webhook: {e}")
        raise HTTPException(status_code=500, detail=f"Erro ao buscar logs de webhook: {e}")

@router.post("/webhooks/{event_id}/reprocessar", tags=["Administração"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_reprocess_webhook_reprocessar(event_id: uuid.UUID):
    """
    Permite que um administrador re-processe um evento de webhook que falhou.
    """
    
    event = get_webhook_event_by_id(event_id)
    if not event:
        raise HTTPException(status_code=404, detail="Evento de webhook não encontrado.")

    if event['status'] != 'falha':
        raise HTTPException(status_code=400, detail=f"O evento {event_id} não está com status 'falha'. Status atual: {event['status']}.")

    try:
        payload = event['payload']
        logger.info(f"ADMIN: Re-processando evento de webhook {event_id}...")
        # Chama diretamente o serviço de webhook com o payload original
        processing_result = await webhook_service.process_kiwify_event(payload)
        return {"message": "Evento de webhook re-processado com sucesso.", "details": processing_result}
    except Exception as e:
        logger.error(f"ADMIN: Falha ao re-processar evento {event_id}: {e}")
        raise HTTPException(status_code=500, detail=f"Falha ao re-processar o evento: {e}")

@router.get("/clientes_list", tags=["Administração"], dependencies=[Depends(check_module_permission("admin"))])
async def listar_clientes_admin_novo(
    limit: int = 100,
    offset: int = 0,
    search: Optional[str] = None,
    status: Optional[str] = None):
    """
    Lista todos os clientes (tenants) do sistema com suporte a paginação,
    busca por nome ou e-mail, e filtro por status. Acessível apenas por administradores.
    """
    try:
        clientes = admin_core.get_all_clientes_admin(limit=limit, offset=offset, search=search, status=status)
        return clientes
    except Exception as e:
        logger.error(f"Erro ao listar clientes para admin: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar clientes.")

# ========== ENDPOINTS ADMIN - EMAIL TEMPLATES ==========

@router.get("/email/templates", response_model=List[EmailTemplateResponse], tags=["Admin - Email Templates"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_list_email_templates():
    """Lista todos os templates de e-mail."""
    try:
        return get_all_email_templates()
    except Exception as e:
        logger.error(f"Erro ao listar templates de e-mail: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar templates.")

@router.get("/email/templates/{template_id}", response_model=EmailTemplateResponse, tags=["Admin - Email Templates"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_get_email_template(template_id: int):
    """Busca um template de e-mail específico pelo seu ID."""
    try:
        template = get_email_template_by_id(template_id)
        if not template:
            raise HTTPException(status_code=404, detail="Template de e-mail não encontrado.")
        return template
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao buscar template de e-mail por ID {template_id}: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar o template.")

@router.post("/email/templates", response_model=EmailTemplateResponse, tags=["Admin - Email Templates"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_create_email_template(template_data: EmailTemplateCreate):
    """Cria um novo template de e-mail."""
    try:
        # Garante que apenas templates 'CAMPAIGN' possam ser criados pela API
        if template_data.tipo != "CAMPAIGN":
            raise HTTPException(status_code=403, detail="Apenas templates do tipo 'CAMPAIGN' podem ser criados.")
        
        new_template = create_email_template(template_data)
        if not new_template:
            raise HTTPException(status_code=400, detail="Não foi possível criar o template. Verifique se a 'chave' já existe.")
        return new_template
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Erro ao criar template de e-mail: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao criar o template.")

@router.put("/email/templates/{template_id}", response_model=EmailTemplateResponse, tags=["Admin - Email Templates"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_update_email_template(template_id: int, update_data: EmailTemplateUpdate):
    """Atualiza um template de e-mail existente."""
    
    template = get_email_template_by_id(template_id)
    if not template:
        raise HTTPException(status_code=404, detail="Template não encontrado.")

    # Regra de negócio: Não permitir alteração de 'chave' e 'tipo' para templates SYSTEM.
    # O modelo EmailTemplateUpdate já previne isso, mas é uma boa prática verificar.
    if template['tipo'] == 'SYSTEM':
        logger.info(f"Atualização em template SYSTEM (ID: {template_id}). Campos restritos não serão alterados.")

    try:
        updated_template = update_email_template(template_id, update_data)
        if not updated_template:
            raise HTTPException(status_code=404, detail="Template não encontrado durante a atualização.")
        return updated_template
    except Exception as e:
        logger.error(f"Erro ao atualizar template de e-mail {template_id}: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar o template.")

@router.delete("/email/templates/{template_id}", status_code=204, tags=["Admin - Email Templates"], dependencies=[Depends(check_module_permission("admin"))])
async def admin_delete_email_template(template_id: int):
    """Exclui um template de e-mail (apenas do tipo CAMPAIGN)."""
    
    success = delete_email_template(template_id)
    if not success:
        raise HTTPException(status_code=403, detail="Não foi possível excluir o template. Verifique se ele existe e não é do tipo 'SYSTEM'.")
    return None

# ========== ENDPOINTS ADMIN - IA ==========
@router.get("/ia/provedores", tags=["Admin - IA"], dependencies=[Depends(check_module_permission("admin"))])
async def listar_provedores_ia():
    try:
        status = ia_orchestrator.get_system_status()
        return status
    except Exception as e:
        logger.error(f"Erro ao listar provedores IA: {e}")
        raise HTTPException(status_code=500, detail="Erro ao obter provedores")

@router.put("/ia/provedores/{provedor_id}", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_provedor_ia(provedor_id: int, request: dict):
    try:
        success = ia_orchestrator.update_provider(provedor_id, request)
        if not success:
            raise HTTPException(status_code=400, detail="Falha ao atualizar provedor")
        return {"message": "Provedor atualizado com sucesso"}
    except Exception as e:
        logger.error(f"Erro ao atualizar provedor {provedor_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar provedor")

@router.get("/ia/chaves", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def listar_chaves_ia():
    try:
        return ia_orchestrator.get_detailed_keys()
    except Exception as e:
        logger.error(f"Erro ao listar chaves IA: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar chaves")

@router.get("/ia/chaves-globais-ordenadas", response_model=List[GlobalKeyStatusResponse], tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def listar_chaves_globais_ordenadas():
    """
    Retorna a lista global de chaves de IA, ordenadas pela saúde e prioridade
    que o orquestrador usará para a próxima chamada.
    """
    try:
        return ia_orchestrator.get_globally_sorted_keys()
    except Exception as e:
        logger.error(f"Erro ao listar chaves globais ordenadas: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao listar chaves globais")

@router.post("/ia/chaves", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def adicionar_chave_ia(request: dict):
    try:
        provedor_id = request.get("provedor_id")
        chave_real = request.get("chave_real")
        descricao = request.get("descricao", "")
        ordem_prioridade = request.get("ordem_prioridade", 1)
        if not provedor_id or not chave_real:
            raise HTTPException(status_code=400, detail="provedor_id e chave_real são obrigatórios")
        chave_id = ia_orchestrator.add_key(provedor_id, chave_real, descricao, ordem_prioridade)
        if not chave_id:
            raise HTTPException(status_code=400, detail="Falha ao adicionar chave")
        return {"message": "Chave adicionada com sucesso", "chave_id": chave_id}
    except Exception as e:
        logger.error(f"Erro ao adicionar chave IA: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao adicionar chave")

@router.put("/ia/chaves/{chave_id}/toggle", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def toggle_chave_ia(chave_id: int):
    try:
        success = ia_orchestrator.toggle_key(chave_id)
        if not success:
            raise HTTPException(status_code=404, detail="Chave não encontrada")
        return {"message": "Status da chave alterado com sucesso"}
    except Exception as e:
        logger.error(f"Erro ao alternar chave {chave_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao alternar chave")

@router.delete("/ia/chaves/{chave_id}", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def deletar_chave_ia(chave_id: int):
    """
    Exclui permanentemente uma chave de IA do sistema.
    """
    try:
        success = ia_orchestrator.delete_key(chave_id)
        if not success:
            raise HTTPException(status_code=404, detail="Chave não encontrada.")
        return {"message": "Chave excluída com sucesso"}
    except Exception as e:
        logger.error(f"Erro ao excluir chave {chave_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao excluir chave")

@router.put("/ia/chaves/{chave_id}", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_chave_ia(chave_id: int, request: dict):
    """
    Atualiza os detalhes de uma chave de IA existente (descrição, prioridade).
    """
    try:
        success = ia_orchestrator.update_key_details(chave_id, request)
        if not success:
            raise HTTPException(status_code=404, detail="Chave não encontrada ou nenhum dado válido para atualizar.")
        return {"message": "Chave atualizada com sucesso"}
    except Exception as e:
        logger.error(f"Erro ao atualizar chave {chave_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao atualizar chave")

@router.get("/ia/status", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def status_sistema_ia():
    return ia_orchestrator.get_system_status()

@router.get("/ia/performance", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def get_ia_performance_metrics():
    try:
        return ia_orchestrator.get_performance_summary()
    except Exception as e:
        logger.error(f"Erro ao obter métricas de performance da IA: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar métricas de performance.")

@router.post("/ia/provedores", tags=["Admin - IA"], status_code=201,  dependencies=[Depends(check_module_permission("admin"))])
async def criar_provedor_ia(request: dict):
    """
    Cria um novo provedor de IA no sistema.
    """
    nome = request.get("nome")
    ordem_prioridade = request.get("prioridade") # Mantendo 'prioridade' como o frontend pediu
    ativo = request.get("operacional", True) # Mantendo 'operacional'
    config_modelo = request.get("config_modelo", {"modelos": ["default-model"]})

    if not nome or ordem_prioridade is None:
        raise HTTPException(status_code=400, detail="Os campos 'nome' e 'prioridade' são obrigatórios.")

    provedor_id = ia_orchestrator.create_provider(nome, ordem_prioridade, ativo, config_modelo)
    if not provedor_id:
        raise HTTPException(status_code=409, detail=f"Provedor com nome '{nome}' já existe ou falha ao criar.")
    
    return {"message": "Provedor criado com sucesso", "provedor_id": provedor_id}

@router.delete("/ia/provedores/{provedor_id}", tags=["Admin - IA"], status_code=204,  dependencies=[Depends(check_module_permission("admin"))])
async def deletar_provedor_ia(provedor_id: int):
    """
    Exclui um provedor de IA e todas as suas chaves associadas.
    """
    success = ia_orchestrator.delete_provider(provedor_id)
    if not success:
        raise HTTPException(status_code=404, detail=f"Provedor com ID {provedor_id} não encontrado.")
    return None # Retorna 204 No Content em caso de sucesso

@router.post("/ia/cache/clear", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def clear_ia_cache():
    response_cache.clear()
    conversation_analytics_cache.clear()
    parsing_utils.clear_cache()
    logger.info("ADMIN: Todos os caches da IA foram limpos.")
    return {"message": "Caches de resposta, análise e parsing foram limpos com sucesso."}

@router.post("/ia/metrics/reset", tags=["Admin - IA"],  dependencies=[Depends(check_module_permission("admin"))])
async def reset_ia_metrics():
    """
    Reseta todas as métricas de performance da IA (em memória e no banco de dados).
    """
    success = ia_orchestrator.reset_performance_metrics()
    if not success:
        raise HTTPException(status_code=500, detail="Falha ao resetar as métricas de performance.")
    return {"message": "Métricas de performance da IA foram resetadas com sucesso."}

# ========== ENDPOINTS ADMIN - PROMPTS ==========
@router.get("/prompt-versoes", response_model=List[PromptVersaoResponse], tags=["Admin - Prompt"], name="listar_versoes_prompt",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_prompt_versoes(nome: Optional[str] = None, ativa: Optional[bool] = True):
    return get_prompt_versoes(nome, ativa)

# Alias para compatibilidade com o frontend
router.add_api_route("/testes-ab", router.routes[-1].endpoint, methods=["GET"], include_in_schema=False)

@router.post("/prompt-versoes", response_model=PromptVersaoResponse, tags=["Admin - Prompt"], name="criar_versao_prompt",  dependencies=[Depends(check_module_permission("admin"))])
async def criar_prompt_versao_admin(request: PromptVersaoCreate):
    # ... (lógica movida para cá)
    versao_id = create_prompt_versao(
        nome=request.nome, versao=request.versao, template=request.template,
        modulo=request.modulo, descricao=request.descricao, parametros=request.parametros,
        peso_teste=request.peso_teste
    )
    versoes = get_prompt_versoes(nome=request.nome)
    return next((v for v in versoes if v["id"] == versao_id), None)

@router.put("/prompt-versoes/{versao_id}", response_model=PromptVersaoResponse, tags=["Admin - Prompt"], name="atualizar_versao_prompt",  dependencies=[Depends(check_module_permission("admin"))])
async def atualizar_prompt_versao_admin(versao_id: int, request: PromptVersaoUpdate):
    # ... (lógica movida para cá)
    update_prompt_versao(
        versao_id=versao_id, ativa=request.ativa,
        peso_teste=request.peso_teste, descricao=request.descricao
    )
    versoes = get_prompt_versoes()
    return next((v for v in versoes if v["id"] == versao_id), None)

@router.get("/prompt-versoes/ativas", tags=["Admin - Prompt"], name="listar_versoes_ativas",  dependencies=[Depends(check_module_permission("admin"))])
async def listar_prompt_versoes_ativas_admin():
    return get_active_prompt_versoes_for_testing()
