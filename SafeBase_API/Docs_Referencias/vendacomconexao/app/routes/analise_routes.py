from fastapi import APIRouter, Request,Depends
import logging
import json
from datetime import datetime

from app.models import (
    AnaliseConversaRequest, ProbabilidadeConversaoRequest,
    ContextoAlinhamentoRequest, ContextoAlinhamentoResponse,
    PredicaoObjecoesRequest, PredicaoObjecoesResponse,
    MudancaEmocionalRequest, MudancaEmocionalResponse
)
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME
from app.services.utils_service import (
    analisar_probabilidade_conversao,
    parsear_resposta_contexto,
    gerar_mock_contexto,
    parsear_resposta_predicao_objecoes,
    gerar_mock_predicao_objecoes,
    parsear_resposta_mudanca_emocional,
    gerar_mock_mudanca_emocional, 
    get_or_create_session_id,
    should_use_personalized_context
)

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/analise",
    tags=["Analise"]
)

@router.post("/conversa", dependencies=[Depends(check_module_permission("analise"))])
async def analisar_conversa_completa(request: AnaliseConversaRequest,  current_request: Request):
    """Analisa conversa completa e retorna métricas preditivas"""
    try:
        analise = await analisar_probabilidade_conversao(
            request.produto_descricao,
            request.conversa,
            request.perfil_cliente,
            USE_MOCK_FORCADO,
            PROVIDER_NOME,                
            chamar_ia_otimizado                     
        )
       
        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)
 
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

        try:
            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="analise_conversa",
                produto_descricao=request.produto_descricao,
                perfil_cliente=request.perfil_cliente,
                conversa={"conversa_analisada": request.conversa, "analise_gerada": analise},
                metricas={"probabilidade_conversao": analise.get("probabilidade", 50), "nivel": analise.get("nivel", "MEDIO")},
                feedback_ia=json.dumps(analise.get("sugestoes", [])),
                session_id=session_id
            )
        except Exception as save_error:
            logger.warning(f"Erro ao salvar análise: {save_error}")
        
        return {"analise": analise, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
    except Exception as e:
        logger.error(f"Erro análise conversa: {e}")
        return {"analise": {"probabilidade": 50, "nivel": "MEDIO", "sugestoes": ["Sistema temporariamente indisponível"]}, "modo": "mock-fallback", "session_id": request.session_id or "error"}

@router.post("/probabilidade-conversao",  dependencies=[Depends(check_module_permission("analise"))])
async def calcular_probabilidade_conversao(request: ProbabilidadeConversaoRequest, current_request: Request):
    """Calcula probabilidade de conversão após cada interação"""
    try:
        conversa_simulada = request.historico + [
            {"tipo": "vendedor", "texto": request.mensagem_vendedor},
            {"tipo": "cliente", "texto": request.resposta_cliente}
        ]
        
        analise = await analisar_probabilidade_conversao(
            request.produto_descricao,
            conversa_simulada,
            request.perfil_cliente,
            USE_MOCK_FORCADO,        
            PROVIDER_NOME,
            chamar_ia_otimizado             
        )
        
        return {
            "probabilidade": analise["probabilidade"],
            "nivel": analise["nivel"],
            "metricas": analise["metricas"],
            "sugestoes_imediata": analise["sugestoes"][:2] if analise["sugestoes"] else [],
            "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        }
    except Exception as e:
        logger.error(f"Erro probabilidade conversão: {e}")
        return {"probabilidade": 50, "nivel": "MEDIO", "metricas": {}, "sugestoes_imediata": ["Continue a conversa para análise"], "modo": "mock-fallback"}

@router.post("/contexto-alinhamento", response_model=ContextoAlinhamentoResponse)
async def analisar_contexto_alinhamento(request: ContextoAlinhamentoRequest, current_request: Request):
    """Analisa o alinhamento contextual da conversa e detecta rupturas"""
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_contexto_alinhamento(
                request=current_request,
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                historico_contexto=request.historico_contexto
            )
        else:
            prompt = prompt_service.obter_prompt_contexto_alinhamento_legacy(
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                historico_contexto=request.historico_contexto
            )

        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            data = parsear_resposta_contexto(resultado)
        else:
            data = gerar_mock_contexto(request.produto_descricao, request.conversa, request.historico_contexto)

        return {**data, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME}
    except Exception as e:
        logger.error(f"Erro na análise de contexto: {e}")
        return {"alinhamento_contextual": 0.5, "ruptura_detectada": False, "sugestao_transicao": "Sistema temporariamente indisponível.", "topicos_nao_abordados": [], "nivel_urgencia": "MEDIO", "modo": "mock-fallback"}

@router.post("/predicao-objecoes", response_model=PredicaoObjecoesResponse)
async def predizer_objecoes(request: PredicaoObjecoesRequest, current_request: Request):
    """Prevê objeções com base em padrões comportamentais"""
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_predicao_objecoes(
                request=current_request,
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                nicho=request.nicho,
                perfil_cliente=request.perfil_cliente
            )
        else:
            prompt = prompt_service.obter_prompt_predicao_objecoes_legacy(
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                nicho=request.nicho,
                perfil_cliente=request.perfil_cliente
            )

        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            data = parsear_resposta_predicao_objecoes(resultado)
        else:
            data = gerar_mock_predicao_objecoes(request.produto_descricao, request.conversa, request.nicho, request.perfil_cliente)

        return {**data, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME}
    except Exception as e:
        logger.error(f"Erro na predição de objeções: {e}")
        return {"objecoes_provaveis": [], "sinais_detectados": [], "abordagem_preventiva": "Sistema temporariamente indisponível.", "nivel_risco": "MEDIO", "modo": "mock-fallback"}

@router.post("/mudanca-emocional", response_model=MudancaEmocionalResponse)
async def analisar_mudanca_emocional(request: MudancaEmocionalRequest, current_request: Request):
    """Detecta mudanças emocionais e sugere ajustes táticos"""
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_mudanca_emocional(
                request=current_request,
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                metricas_base=request.metricas_base
            )
        else:
            prompt = prompt_service.obter_prompt_mudanca_emocional_legacy(
                produto_descricao=request.produto_descricao,
                conversa=request.conversa,
                metricas_base=request.metricas_base
            )

        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            data = parsear_resposta_mudanca_emocional(resultado)
        else:
            data = gerar_mock_mudanca_emocional(request.produto_descricao, request.conversa, request.metricas_base)
        
        return {**data, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME}
    except Exception as e:
        logger.error(f"Erro na análise de mudança emocional: {e}")
        return {"mudanca_detectada": False, "ponto_virada": "Erro no sistema", "direcao_mudanca": "ESTAVEL", "emocao_antes": "NEUTRO", "emocao_depois": "NEUTRO", "fator_critico": "Sistema indisponível", "sugestao_ajuste_imediato": "Continue a conversa normalmente.", "estrategia_recuperacao": "", "alerta_risco": "BAIXO", "probabilidade_recuperacao": 0.5, "modo": "mock-fallback"}