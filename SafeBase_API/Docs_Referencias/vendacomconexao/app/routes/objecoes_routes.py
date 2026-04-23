from fastapi import APIRouter, Request, Depends
import logging
from datetime import datetime

from app.models import ObjecaoRequest
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME
from app.services.utils_service import parsear_respostas_ia, quebrar_objecao_mock, get_or_create_session_id

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/objecoes",
    tags=["Objecoes"]
)

@router.post("/quebrar", dependencies=[Depends(check_module_permission("objecoes"))])
async def quebrar_objecao(request: ObjecaoRequest, current_request: Request):
    try:
        # Usa prompt com contexto personalizado
        prompt = await prompt_service.obter_prompt_quebrar_objecao(
            request=current_request,
            produto_descricao=request.produto_descricao,
            objecao=request.objecao
        )
        logger.info(f"Prompt gerado para /objecoes/quebrar (len={len(prompt)}). Preview:\n{prompt[:1000]}")

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)

        if not USE_MOCK_FORCADO:
            try:
                respostas_texto = await chamar_ia_otimizado(prompt)

                # Log mais detalhado para diagnosticar truncamentos
                try:
                    texto_preview = respostas_texto if len(respostas_texto) <= 2000 else respostas_texto[:2000] + '...'
                except Exception:
                    texto_preview = str(respostas_texto)
                logger.debug(f"Resposta crua da IA para /objecoes/quebrar (len={len(respostas_texto) if respostas_texto else 0}): {repr(texto_preview)}")

                if not respostas_texto:
                    logger.warning("Resposta crua da IA vazia ou None.")
                    respostas_parseadas = quebrar_objecao_mock(request.produto_descricao, request.objecao)
                else:
                    # Heurística simples: se não terminar com ponto, exclamação ou interrogação, pode estar truncada
                    tail = respostas_texto.strip()[-1] if respostas_texto.strip() else ''
                    if tail and tail.isalnum():
                        logger.warning("Resposta crua da IA termina sem pontuação final — possível truncamento.")

                    respostas_parseadas = parsear_respostas_ia(respostas_texto)

            except Exception as ia_err:
                logger.error(f"Erro ao chamar IA otimizado para /objecoes/quebrar: {ia_err}")
                # Fallback para mock quando a IA falhar
                respostas_parseadas = quebrar_objecao_mock(request.produto_descricao, request.objecao)
        else:
            respostas_parseadas = quebrar_objecao_mock(request.produto_descricao, request.objecao)
        
        try:
            conversa_data = {
                "objecao_original": request.objecao,
                "respostas_geradas": respostas_parseadas
            }
            
            metricas_data = {
                "numero_abordagens": 5,
                "timestamp": datetime.now().isoformat()
            }
            
            user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="quebra_objecoes",
                produto_descricao=request.produto_descricao,
                perfil_cliente=None,
                conversa=conversa_data,
                metricas=metricas_data,
                feedback_ia="Objeção processada com 5 abordagens",
                session_id=session_id
            )
            
        except Exception as save_error:
            logger.warning(f"Erro ao salvar objeção: {save_error}")
        
        return {**respostas_parseadas, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
        
    except Exception as e:
        logger.error(f"Erro quebra objeção: {e}")
        respostas_parseadas = quebrar_objecao_mock(request.produto_descricao, request.objecao)
        return {**respostas_parseadas, "modo": "mock-fallback", "session_id": request.session_id or "error"}