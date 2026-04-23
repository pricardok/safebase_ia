from fastapi import APIRouter, Depends, HTTPException, status,Request
from typing import List
import logging

from app.models import PlanoChangeRequest, UserLoginData
# from app.database import get_planos_publicos, atribuir_plano_cliente, get_plano_by_id, get_user_details_by_id
from app.dependencies import get_current_user_dependency,check_module_permission, chamar_ia_otimizado

from app.models_planos import PlanoPublicoResponse, UpgradePlanoRequest, PlanoEfetivoResponse, SignupRequest
from app.database import (
    get_planos_publicos as db_get_planos_publicos, 
    get_plano_do_cliente, 
    create_plano as db_create_plano, 
    create_plano,
    delete_plano_by_id as db_delete_plano,
    get_planos_publicos, 
    atribuir_plano_cliente, 
    get_plano_by_id, 
    get_user_details_by_id
)

logger = logging.getLogger(__name__)
router = APIRouter()

@router.get("/planos", tags=["Planos"])
async def listar_planos_publicos():
    """
    Lista todos os planos comerciais disponíveis para assinatura.
    Este endpoint é público.
    """
    try:
        planos = get_planos_publicos()
        return planos
    except Exception as e:
        logger.error(f"Erro ao listar planos públicos: {e}")
        raise HTTPException(status_code=500, detail="Erro ao buscar planos.")

@router.post("/me/plano", tags=["Usuário"], response_model=UserLoginData)
async def mudar_plano_usuario(
    request: PlanoChangeRequest,
    current_user: dict = Depends(get_current_user_dependency)):
    """
    Permite que o usuário autenticado mude seu plano de assinatura.
    """
    cliente_id = current_user.get("cliente_id")
    if not cliente_id:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Usuário não está associado a um cliente.")

    plano_desejado = get_plano_by_id(request.plano_id)
    if not plano_desejado or not plano_desejado.get("ativo"):
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Plano solicitado não encontrado ou inativo.")

    try:
        atribuir_plano_cliente(cliente_id, request.plano_id)
        logger.info(f"Usuário {current_user['id']} (cliente {cliente_id}) mudou para o plano {request.plano_id}.")
        
        # Busca novamente todos os dados do usuário para garantir que a resposta esteja 100% atualizada
        user_details_atualizado = get_user_details_by_id(current_user["id"])
        return user_details_atualizado

    except Exception as e:
        logger.error(f"Erro ao mudar plano para cliente {cliente_id}: {e}")
        raise HTTPException(status_code=500, detail="Erro interno ao tentar mudar o plano.")

@router.post("/admin/planos", tags=["Administração"], status_code=201)
async def criar_plano_admin(plano_data: dict, request: Request, _ = Depends(check_module_permission("admin"))):
    """
    Cria um novo plano de assinatura. Acessível apenas por administradores.
    """
    try:
        novo_plano = db_create_plano(plano_data)
        if not novo_plano:
            raise HTTPException(status_code=500, detail="Não foi possível criar o plano.")
        return novo_plano
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao criar plano: {e}")

@router.delete("/admin/planos/{plano_id}", tags=["Administração"], status_code=204)
async def deletar_plano_admin(plano_id: int, request: Request, _ = Depends(check_module_permission("admin"))):
    """
    Exclui um plano de assinatura. Acessível apenas por administradores.
    """
    try:
        success = db_delete_plano_by_id(plano_id)
        if not success:
            raise HTTPException(status_code=404, detail="Plano não encontrado.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao excluir plano: {e}")
