from fastapi import APIRouter, Request, HTTPException, Depends
from typing import Dict, Any
import logging
from datetime import datetime

from app.models import SimuladorRequest, FeedbackRequest
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.services.utils_service import (
    gerar_resposta_mock_simulador,
    gerar_feedback_mock,
    get_or_create_session_id,
    executar_copiloto_cognitivo
)
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME


logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/simulador",
    tags=["Simulador"]
)

@router.post("/responder", dependencies=[Depends(check_module_permission("simulador"))])
async def simulador_responder(request: SimuladorRequest, current_request: Request):
    try:
        # Usa prompt com contexto personalizado
        prompt = await prompt_service.obter_prompt_simulador_cliente(
            request=current_request,
            produto_descricao=request.produto_descricao,
            perfil_cliente=request.perfil_cliente,
            mensagem_usuario=request.mensagem_usuario,
            historico=request.historico
        )

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)
        
        if not USE_MOCK_FORCADO:
            resposta = await chamar_ia_otimizado(prompt)
        else:
            resposta = gerar_resposta_mock_simulador(
                request.produto_descricao, 
                request.perfil_cliente, 
                request.mensagem_usuario
            )

        # Prepara a resposta base
        response_data = {
            "resposta": resposta,
            "perfil": request.perfil_cliente,
            "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME,
            "session_id": session_id,
            "copiloto": None
        }

        # Se o frontend solicitou, executa o Co-piloto Cognitivo
        if request.incluir_copiloto:
            conversa_atual_para_analise = [{"tipo": "vendedor", "texto": request.mensagem_usuario}, {"tipo": "cliente", "texto": resposta}]
            analises_copiloto = await executar_copiloto_cognitivo(current_request, request.produto_descricao, conversa_atual_para_analise, session_id, current_request.state.current_user_id)
            response_data["copiloto"] = analises_copiloto
        
        try:
            # Padroniza o acesso ao ID do usuário a partir do dicionário injetado
            user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

            conversa_data = {
                "mensagens": [
                    {"tipo": "vendedor", "texto": request.mensagem_usuario},
                    {"tipo": "cliente", "texto": resposta}
                ],
                "historico": request.historico
            }
            
            metricas_data = {
                "perfil_cliente": request.perfil_cliente,
                "modo_operacao": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME,
                "timestamp": datetime.now().isoformat()
            }
            
            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="simulador",
                produto_descricao=request.produto_descricao,
                perfil_cliente=request.perfil_cliente,
                conversa=conversa_data,
                metricas=metricas_data,
                feedback_ia="",
                session_id=session_id
            )
            
        except Exception as save_error:
            logger.warning(f"Erro ao salvar simulação: {save_error}")
        
        return response_data
        
    except Exception as e:
        logger.error(f"Erro no simulador: {e}")
        resposta = gerar_resposta_mock_simulador(
            request.produto_descricao, 
            request.perfil_cliente, 
            request.mensagem_usuario
        )
        return {
            "resposta": resposta,
            "perfil": request.perfil_cliente,
            "modo": "mock-fallback",
            "session_id": request.session_id or "error",
            "copiloto": None
        }

@router.post("/feedback", dependencies=[Depends(check_module_permission("simulador"))])
async def simulador_feedback(request: FeedbackRequest, current_request: Request):
    try:
        prompt = await prompt_service.obter_prompt_simulador_feedback(
            request=current_request,
            produto_descricao=request.produto_descricao,
            mensagem_usuario=request.mensagem_usuario
        )

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)

        if not USE_MOCK_FORCADO:
            feedback = await chamar_ia_otimizado(prompt)
        else:
            feedback = gerar_feedback_mock(request.produto_descricao, request.mensagem_usuario)

        try:
            # Padroniza o acesso ao ID do usuário
            user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

            # Salva a interação de feedback no histórico
            await history_service.save_simulation_secure(
                usuario_id=user_id, # Já estava correto, mas a obtenção do user_id acima foi corrigida
                modulo="simulador_feedback",
                produto_descricao=request.produto_descricao,
                perfil_cliente=None, # Feedback não tem perfil de cliente
                conversa={
                    "mensagem_analisada": request.mensagem_usuario,
                    "feedback_gerado": feedback
                },
                metricas={"modo_operacao": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME},
                feedback_ia=feedback,
                session_id=session_id
            )
        except Exception as save_error:
            logger.warning(f"Erro ao salvar feedback no histórico: {save_error}")

        return {
            "feedback": feedback,
            "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME,
            "session_id": session_id
        }

    except Exception as e:
        logger.error(f"Erro ao gerar feedback: {e}")
        feedback = gerar_feedback_mock(request.produto_descricao, request.mensagem_usuario)
        return {
            "feedback": feedback,
            "modo": "mock-fallback",
            "session_id": request.session_id or "error"
        }
