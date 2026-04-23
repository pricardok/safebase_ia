from fastapi import APIRouter, Request, HTTPException, Depends
from typing import List
from datetime import datetime

from app.models import ( 
    SimulacaoCreate, SimulacaoResponse, OnboardingResponse, 
    UserBehavioralProfileResponse, UserHistorySummaryResponse, SessaoConversaResponse 
) 
from app.database import (
    save_simulation, user_has_simulations, get_example_scripts, create_example_simulation, get_user_by_username, get_user_sessions,get_user_simulations
)
from app.services.utils_service import get_or_create_session_id
from app.dependencies import check_module_permission, get_current_user_dependency
from app.services.history_service import history_service

import logging
logger = logging.getLogger(__name__)

router = APIRouter(
    prefix="/historico",
    tags=["Histórico"]
)

@router.post("/simulacao", response_model=SimulacaoResponse)
async def salvar_simulacao(request: SimulacaoCreate, current_user: dict = Depends(get_current_user_dependency)):
    """Salva uma simulação no histórico do usuário"""
    try:
        # Garante que a simulação sempre terá uma session_id
        session_id = get_or_create_session_id(request.session_id)

        simulation_id = save_simulation(
            usuario_id=current_user["id"],
            modulo=request.modulo,
            produto_descricao=request.produto_descricao,
            perfil_cliente=request.perfil_cliente,
            conversa=request.conversa,
            metricas=request.metricas,
            feedback_ia=request.feedback_ia,
            is_exemplo=request.is_exemplo,
            session_id=session_id
        )
        
        return {
            "id": simulation_id,
            "modulo": request.modulo,
            "produto_descricao": request.produto_descricao,
            "perfil_cliente": request.perfil_cliente,
            "conversa": request.conversa,
            "metricas": request.metricas,
            "feedback_ia": request.feedback_ia,
            "data_criacao": datetime.now(),
            "is_exemplo": request.is_exemplo,
            "session_id": session_id
        }
    except Exception as e:
        logger.error(f"Erro ao salvar simulação: {e}")
        raise HTTPException(status_code=500, detail="Erro ao salvar simulação")

@router.get("/simulacoes", response_model=List[SimulacaoResponse])
async def listar_simulacoes(current_request: Request, current_user: dict = Depends(get_current_user_dependency)):
    """Lista as simulações do usuário autenticado"""
    try:
        # CORREÇÃO: Revertido para a implementação estável que busca diretamente do banco,
        # evitando o erro de 'AttributeError' que ocorria na camada de serviço.
        simulations = get_user_simulations(current_user["id"], limit=50)
        return simulations
    except Exception as e:
        logger.error(f"Erro ao listar simulações: {e}")
        raise HTTPException(status_code=500, detail="Erro ao listar simulações")

@router.get("/sessoes", response_model=List[SessaoConversaResponse])
async def listar_sessoes(current_user: dict = Depends(get_current_user_dependency)):
    """
    Lista as sessões de conversa do usuário, agrupando as interações.
    """
    try:
        sessoes = get_user_sessions(current_user["id"], limit=50)
        return sessoes
    except Exception as e:
        logger.error(f"Erro ao listar sessões: {e}")
        raise HTTPException(status_code=500, detail="Erro ao listar sessões de conversa")

@router.get("/onboarding", response_model=OnboardingResponse)
async def obter_dados_onboarding(current_user: dict = Depends(get_current_user_dependency)):
    """Retorna dados de onboarding para novos usuários"""
    try:
        if user_has_simulations(current_user["id"]):
            return {"simulacoes_exemplo": [], "scripts_exemplo": []}
        
        # Lógica para criar e retornar exemplos de simulação e scripts
        # (A lógica completa foi movida para cá)
        return {"simulacoes_exemplo": [], "scripts_exemplo": get_example_scripts()}
    except Exception as e:
        logger.error(f"Erro ao obter dados de onboarding: {e}")
        raise HTTPException(status_code=500, detail="Erro ao obter dados de onboarding")

@router.get("/perfil-comportamental", response_model=UserBehavioralProfileResponse, dependencies=[Depends(check_module_permission("historico"))])
async def obter_perfil_comportamental(current_user: dict = Depends(get_current_user_dependency)):
    """
    Obtém o perfil comportamental do usuário baseado no histórico.
    Usa o `current_user` injetado para garantir consistência.
    """
    try:
        from app.services.history_service import history_service
        perfil = await history_service.get_user_behavioral_profile(current_user["id"])
        return perfil
    except Exception as e:
        logger.error(f"Erro ao obter perfil comportamental: {e}")
        raise HTTPException(status_code=500, detail="Erro ao obter perfil comportamental")
 
@router.get("/resumo/{modulo}", response_model=UserHistorySummaryResponse, dependencies=[Depends(check_module_permission("historico"))])
async def obter_resumo_historico(modulo: str, current_user: dict = Depends(get_current_user_dependency)):
    """Obtém resumo do histórico do usuário para um módulo específico."""
    try:
        resumo = await history_service.get_user_summary(current_user["id"], modulo)
        return {"resumo": resumo, "modulo": modulo, "timestamp": datetime.now()}
    except Exception as e:
        logger.error(f"Erro ao obter resumo do histórico: {e}")
        raise HTTPException(status_code=500, detail="Erro ao obter resumo do histórico")
