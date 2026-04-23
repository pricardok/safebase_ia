# app/routes/detector_routes.py
from fastapi import APIRouter, Request,Depends
import logging
from datetime import datetime

from app.models import DetectorRequest
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME
from app.services.utils_service import parsear_analise_textual, detector_analisar_mock, get_or_create_session_id, executar_copiloto_cognitivo

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/detector",
    tags=["Detector"]
)

@router.post("/analisar", dependencies=[Depends(check_module_permission("detector"))])
async def detector_analisar(request: DetectorRequest, current_request: Request):
    try:
        # Usa prompt com contexto personalizado
        prompt = await prompt_service.obter_prompt_detector_analisar(
            request=current_request,
            produto_descricao=request.produto_descricao,
            mensagem=request.mensagem
        )

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)
        
        if not USE_MOCK_FORCADO:
            analise_texto = await chamar_ia_otimizado(prompt)
            analise_parseada = parsear_analise_textual(analise_texto)
        else:
            analise_parseada = detector_analisar_mock(request.produto_descricao, request.mensagem)

        # Prepara a resposta base
        response_data = {
            **analise_parseada,
            "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME,
            "session_id": session_id,
            "copiloto": None
        }

        # Se o frontend solicitou, executa o Co-piloto Cognitivo
        if request.incluir_copiloto:
            analises_copiloto = await executar_copiloto_cognitivo(current_request, request.produto_descricao, [{"tipo": "vendedor", "texto": request.mensagem}], session_id, current_request.state.current_user_id)
            response_data["copiloto"] = analises_copiloto
        
        try:
            conversa_data = {
                "mensagem_analisada": request.mensagem,
                "analise_detalhada": analise_parseada
            }

            # Adiciona a análise do copiloto aos dados da conversa, se ela existir
            if response_data.get("copiloto"):
                conversa_data["analise_copiloto"] = response_data["copiloto"]
            
            metricas_data = {
                "classificacao": analise_parseada.get("classificacao", "EQUILIBRADO"),
                "pontuacao_empatia": analise_parseada.get("pontuacao_empatia", 50),
                "nivel_pressao": analise_parseada.get("nivel_pressao", "MÉDIO"),
                "timestamp": datetime.now().isoformat()
            }
            
            user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="detector_vendedor",
                produto_descricao=request.produto_descricao,
                perfil_cliente=None,
                conversa=conversa_data,
                metricas=metricas_data,
                feedback_ia=analise_parseada.get("sugestao", ""),
                session_id=session_id
            )
            
        except Exception as save_error:
            logger.warning(f"Erro ao salvar análise do detector: {save_error}")
        
        return response_data
        
    except Exception as e:
        logger.error(f"Erro detector: {e}")
        analise_parseada = detector_analisar_mock(request.produto_descricao, request.mensagem)
        return {**analise_parseada, "modo": "mock-fallback", "session_id": request.session_id or "error", "copiloto": None}