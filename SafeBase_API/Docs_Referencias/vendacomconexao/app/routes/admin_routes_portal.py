# app/routes/admin_routes_portal.py
from fastapi import APIRouter, Request, HTTPException,Depends
from typing import List, Dict, Any, Optional, Union, Annotated
import logging
import uuid 

from app.dependencies import check_module_permission

logger = logging.getLogger(__name__)

# importações diretas do diretorio app/database/
from app.database import admin_core
from app.database.webhooks_db import get_webhook_logs
from app.database.webhooks_db import get_contatos_whatsapp_desconhecidos
from app.database.webhooks_db import get_webhook_event_by_id, log_webhook_event
from app.services.waha_service import waha_service
from ..models_admin import (
    GlobalDiscountResponse, GlobalDiscountCreate, GlobalDiscountUpdate,
    ApplyDiscountToClientRequest
)

router = APIRouter(
    prefix="/admin",
    tags=["Administração"])
@router.get("/perfis", tags=["Administração"], name="listar_perfis_legado")
async def listar_perfis(current_request: Request):
    """
    Lista todos os perfis disponíveis (apenas admin).
    """
    try:
        profiles = get_all_profiles()
        from app.database import get_all_modules, get_profile_permissions
        modules = get_all_modules()
        
        for profile in profiles:
            permissions = get_profile_permissions(profile["id"])
            accessible_modules = [mod for mod, perm in permissions.items() if perm["pode_acessar"]]
            profile["modulos_acessiveis"] = accessible_modules
        
        return profiles
    except Exception as e:
        logger.error(f"Erro ao listar perfis: {e}")
        raise HTTPException(status_code=500, detail="Erro interno")


@router.get("/perfis/detalhados", summary="Visão Detalhada de Perfis e Módulos")
async def get_perfis_detalhados(search: Optional[str] = None):
    """
    Retorna uma lista completa de todos os perfis, os módulos associados,
    a permissão de acesso para cada um e a contagem de usuários vinculados.
    """
    try:
        perfis = admin_core.get_perfis_detalhados_admin(search=search)
        return perfis
    except Exception as e:
        logger.error(f"Erro na rota /perfis/detalhados: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar detalhes dos perfis.")

@router.get("/perfis/usuarios", summary="Visão de Usuários com Perfis e Módulos")
async def get_usuarios_com_perfis(
    limit: int = 50, 
    offset: int = 0, 
    search: Optional[str] = None, 
    perfil_id: Optional[int] = None):
    """
    Lista todos os usuários, seus respectivos clientes, perfis e uma string
    agregada dos módulos que eles podem acessar.
    """
    try:
        usuarios = admin_core.get_usuarios_com_perfis_admin(limit=limit, offset=offset, search=search, perfil_id=perfil_id)
        return usuarios
    except Exception as e:
        logger.error(f"Erro na rota /perfis/usuarios: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar usuários e seus perfis.")

@router.get("/perfis/modulos", summary="Visão de Módulos e Perfis com Acesso")
async def get_modulos_e_acessos():
    """
    Lista todos os módulos disponíveis no sistema, quantos perfis têm acesso
    a cada um e quais são esses perfis.
    """
    try:
        modulos = admin_core.get_modulos_disponiveis_admin()
        return modulos
    except Exception as e:
        logger.error(f"Erro na rota /perfis/modulos: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar módulos do sistema.")

@router.get("/perfis/permissoes-por-cliente", summary="Visão Detalhada de Permissões por Cliente")
async def get_permissoes_por_cliente(
    limit: int = 20, 
    offset: int = 0, 
    cliente_id: Optional[str] = None, 
    search_user: Optional[str] = None):
    """
    Retorna uma visão detalhada das permissões de cada usuário, agrupada por cliente.
    Ideal para auditoria de acesso de um cliente específico.
    """
    try:
        dados = admin_core.get_permissoes_detalhadas_por_cliente_admin(limit=limit, offset=offset, cliente_id=cliente_id, search_user=search_user)
        return dados
    except Exception as e:
        logger.error(f"Erro na rota /perfis/permissoes-por-cliente: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao buscar permissões por cliente.")

@router.get("/perfis/auditoria", summary="Visão de Auditoria de Acessos por Perfil")
async def get_auditoria_de_perfis():
    """
    Fornece um resumo de auditoria para cada perfil, mostrando o total de usuários,
    o número de módulos com acesso e uma lista desses módulos.
    """
    try:
        auditoria = admin_core.get_auditoria_acessos_perfis_admin()
        return auditoria
    except Exception as e:
        logger.error(f"Erro na rota /perfis/auditoria: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro interno ao gerar auditoria de perfis.")


@router.get("/integracoes/whatsapp/waha/logs", summary="Logs Waha (WhatsApp) - Portal Admin", dependencies=[Depends(check_module_permission("admin"))])
async def portal_get_waha_logs(limit: int = 50, offset: int = 0, gateway: Optional[str] = None):
    """
    Retorna logs do gateway Waha (WhatsApp) para o painel de administração/backoffice.
    - `limit`/`offset` paginam os resultados.
    - `gateway` (opcional) filtra por provedor (mantido para compatibilidade).
    """
    try:
        logs = get_webhook_logs(limit=limit, offset=offset)
        if gateway:
            logs = [l for l in logs if l.get('gateway') == gateway]
        return logs
    except Exception as e:
        logger.error(f"Erro ao buscar logs de webhooks: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro ao buscar logs de webhooks")


@router.get("/integracoes/whatsapp/waha/unmapped", summary="Waha (WhatsApp) - eventos não mapeados", dependencies=[Depends(check_module_permission("admin"))])
async def portal_get_unmapped_waha_logs(limit: int = 50, offset: int = 0):
    """
    Retorna eventos do gateway 'waha' que não foram mapeados para nenhum usuário/cliente.
    Útil para o backoffice identificar números que precisam ser associados.
    """
    try:
        logs = get_webhook_logs(limit=limit, offset=offset)
        # Filtra por gateway 'waha' e processing_log indicando 'matched:none' ou ausente
        unmapped = []
        for l in logs:
            if l.get('gateway') != 'waha':
                continue
            pl = l.get('processing_log')
            if pl is None or ('matched:none' in str(pl)):
                unmapped.append(l)
        return unmapped
    except Exception as e:
        logger.error(f"Erro ao buscar webhooks não mapeados Waha: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro ao buscar webhooks não mapeados")


@router.get("/integracoes/whatsapp/contatos-desconhecidos", summary="Contatos WhatsApp Desconhecidos - Portal Admin", dependencies=[Depends(check_module_permission("admin"))])
async def portal_get_contatos_whatsapp_desconhecidos(limit: int = 50, offset: int = 0, search: Optional[str] = None):
    """Retorna registros de números que tentaram contato e não estão mapeados a clientes/usuários.
    Usado pelo backoffice para auditoria e associação manual.
    """
    try:
        data = get_contatos_whatsapp_desconhecidos(limit=limit, offset=offset, search=search)
        return {"items": data.get('items', []), "total": data.get('total', 0), "limit": limit, "offset": offset}
    except Exception as e:
        logger.error(f"Erro ao buscar contatos_whatsapp_desconhecidos: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail="Erro ao buscar contatos desconhecidos")


@router.post("/reprocess-webhook/{event_id}", summary="Reprocessar evento de webhook", dependencies=[Depends(check_module_permission("admin"))])
async def portal_reprocess_webhook(event_id: str, force: bool = False):
    """Permite reprocessar um evento de webhook manualmente a partir do painel admin.
    - Marca o evento como `received` para disparar novo processamento.
    - Agenda execução de `_background_process` do `waha_service` com os dados salvos.
    - `force=true` pode ser usado para reprocessar mesmo quando o status atual é `webhook_disabled`.
    """
    try:
        # Valida evento
        try:
            import uuid as _uuid
            eid = _uuid.UUID(event_id)
        except Exception:
            raise HTTPException(status_code=400, detail="event_id inválido")

        evt = get_webhook_event_by_id(eid)
        if not evt:
            raise HTTPException(status_code=404, detail="Evento não encontrado")

        # If webhook is disabled and not forced, don't reprocess
        if evt.get('status') == 'webhook_disabled' and not force:
            raise HTTPException(status_code=400, detail="Evento marcado como webhook_disabled; use force=true para reprocessar")

        # Update status to 'received' and add a trace to processing_log
        from datetime import datetime
        new_log = {"reprocess_by":"admin","reprocess_at": datetime.utcnow().isoformat(), "previous_log": evt.get('processing_log')}
        log_webhook_event(gateway=evt.get('gateway'), event_type=evt.get('event_type'), payload=evt.get('payload'), status='received', event_id=eid, external_message_id=evt.get('external_message_id'), processing_log=new_log)

        # Rebuild minimal conversation details for background processing
        payload = evt.get('payload') or {}
        # try to extract fields similar to the original handler
        from_number = (
            payload.get('from')
            or payload.get('sender')
            or payload.get('telefone')
            or payload.get('msisdn')
            or (payload.get('payload') and (payload.get('payload').get('from') or payload.get('payload').get('msisdn') or payload.get('payload').get('sender')))
            or (payload.get('message') and (payload.get('message').get('from') or payload.get('message').get('sender') or payload.get('message').get('msisdn')))
            or (payload.get('_data') and payload.get('_data').get('from'))
        )
        text = ''
        if isinstance(payload.get('message'), dict):
            text = payload['message'].get('text') or payload['message'].get('body') or ''
        if not text and isinstance(payload.get('payload'), dict):
            pl = payload.get('payload')
            text = pl.get('text') or pl.get('body') or ((pl.get('_data') and pl.get('_data').get('body')) if isinstance(pl.get('_data'), dict) else '') or ''
        if not text and isinstance(payload.get('data'), list) and len(payload.get('data')) > 0:
            first = payload.get('data')[0]
            if isinstance(first, dict):
                text = (first.get('_data') and (first.get('_data').get('body'))) or first.get('body') or ''
        text = (text or (payload.get('_data') and payload.get('_data').get('body')) or payload.get('text') or payload.get('body') or '')

        conversa = {"canal":"whatsapp","mensagens":[{"tipo":"cliente","texto":text,"from":from_number,"message_id":evt.get('external_message_id'),"timestamp":None}]}

        # Schedule background processing (do not await to keep endpoint responsive)
        try:
            import asyncio
            asyncio.create_task(waha_service._background_process(str(eid), evt.get('event_type'), payload, conversa, {"reprocessed": True}, from_number, text, evt.get('external_message_id')))
        except Exception as e:
            logger.exception('Falha ao agendar reprocessamento em background: %s', e)
            raise HTTPException(status_code=500, detail='Falha ao agendar reprocessamento')

        return {"message": "Reprocessamento agendado", "event_id": str(eid)}
    except HTTPException:
        raise
    except Exception as e:
        logger.exception('Erro ao reprocessar evento de webhook: %s', e)
        raise HTTPException(status_code=500, detail='Erro interno ao reprocessar evento')

@router.get("/descontos", response_model=List[GlobalDiscountResponse], dependencies=[Depends(check_module_permission("admin"))])
def listar_descontos_globais():
    """Lista todos os descontos globais (cupons) configurados no sistema."""
    return admin_core.get_all_global_discounts_admin()

@router.post("/descontos", response_model=GlobalDiscountResponse, status_code=201, dependencies=[Depends(check_module_permission("admin"))])
def criar_desconto_global(discount_data: GlobalDiscountCreate):
    """Cria um novo desconto global (cupom)."""
    new_discount = admin_core.create_global_discount_admin(discount_data)
    if not new_discount:
        raise HTTPException(status_code=400, detail=f"Não foi possível criar o desconto. O código '{discount_data.codigo}' já pode existir.")
    return new_discount

@router.put("/descontos/{discount_id}", response_model=GlobalDiscountResponse, dependencies=[Depends(check_module_permission("admin"))])
def editar_desconto_global(discount_id: int, discount_data: GlobalDiscountUpdate):
    """Atualiza um desconto global (cupom) existente."""
    updated_discount = admin_core.update_global_discount_admin(discount_id, discount_data)
    if not updated_discount:
        raise HTTPException(status_code=404, detail=f"Desconto com ID {discount_id} não encontrado.")
    return updated_discount

@router.delete("/descontos/{discount_id}", status_code=204, dependencies=[Depends(check_module_permission("admin"))])
def excluir_desconto_global(discount_id: int):
    """Exclui um desconto global (cupom)."""
    success = admin_core.delete_global_discount_admin(discount_id)
    if not success:
        raise HTTPException(status_code=404, detail=f"Desconto com ID {discount_id} não encontrado.")
    return

@router.post("/clientes/{cliente_id}/descontos", status_code=201, dependencies=[Depends(check_module_permission("admin"))])
def aplicar_desconto_a_cliente(cliente_id: str, request: ApplyDiscountToClientRequest):
    """Aplica um cupom de desconto global a um cliente específico."""
    desconto_aplicado = admin_core.apply_discount_to_client_admin(cliente_id, request.codigo_cupom, request.expira_em)
    
    if not desconto_aplicado:
        raise HTTPException(status_code=400, detail="Não foi possível aplicar o desconto. Verifique se o cliente e o cupom são válidos e se o cupom não atingiu o limite de uso.")

    return {
        "message": "Desconto aplicado com sucesso!",
        "desconto_id": desconto_aplicado['id']
    }
