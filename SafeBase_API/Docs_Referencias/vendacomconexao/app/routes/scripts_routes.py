# /app/routes/script_routes.py
from fastapi import APIRouter, Depends, HTTPException, Request
import logging
import json
  
from app.models import (
    ScriptsOtimizarRequest, ScriptsAnaliseRequest, ScriptsVariacoesRequest, 
    ScriptsOtimizarResponse, ScriptsAnaliseResponse, ScriptsVariacoesResponse # Assumindo ScriptsOtimizarResponse e ScriptsVariacoesResponse existem ou serão criados
)
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.services.database_context import db_context # CORREÇÃO: Importar o db_context
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME
from app.services.utils_service import should_use_personalized_context, detector_analisar_mock, get_or_create_session_id,parsear_analise_textual
from app.utils.parsing_utils import parsing_utils # Importa o módulo de parsing robusto

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/scripts",
    tags=["Scripts"]
)

@router.post("/otimizar", response_model=ScriptsOtimizarResponse, dependencies=[Depends(check_module_permission("scripts"))])
async def scripts_otimizar_completo(request: ScriptsOtimizarRequest, current_request: Request): # Mantém current_request para obter o user_id
    """ 
    Otimiza um script de vendas, retornando uma versão melhorada e uma lista de melhorias aplicadas.
    """
    try:
        # Obter prompt com contexto
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_otimizar_script(
                request=current_request,
                produto_descricao=request.produto_descricao,
                script_original=request.script_original,
                canal=request.canal,
                objetivo=request.objetivo
            )
        else:
            prompt = prompt_service.obter_prompt_otimizar_script_legacy(
                produto_descricao=request.produto_descricao,
                script_original=request.script_original,
                canal=request.canal,
                objetivo=request.objetivo
            )

        if not USE_MOCK_FORCADO:
            # CORREÇÃO: A função chamar_ia_otimizado é síncrona e não deve ser aguardada com 'await'.
            resposta_ia = await chamar_ia_otimizado(prompt, use_cache=False)
            dados_otimizacao = parsing_utils.parse_script_otimizado(resposta_ia)
        else:
            dados_otimizacao = {"script_otimizado": f"VERSÃO OTIMIZADA (MOCK): {request.script_original}", "melhorias": ["Tom otimizado"]}

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(getattr(request, "session_id", None))
        
        # Obtém o user_id do request.state (definido pelo middleware)
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        if not user_id:
            raise HTTPException(status_code=401, detail="Usuário não autenticado.")

        # Salva a interação de otimização de script no histórico
        try:
            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="script_otimizado",
                produto_descricao=request.produto_descricao,
                conversa={
                    "produto_descricao": request.produto_descricao,
                    "script_original": request.script_original,
                    "canal": request.canal,
                    "objetivo": request.objetivo
                },
                metricas=dados_otimizacao,
                feedback_ia=json.dumps(dados_otimizacao.get("melhorias", [])),
                session_id=session_id
            )
        except Exception as save_error:
            logger.warning(f"Erro ao salvar otimização de script no histórico: {save_error}")

        return ScriptsOtimizarResponse(
            script_otimizado=dados_otimizacao.get("script_otimizado", ""),
            melhorias=dados_otimizacao.get("melhorias", []),
            session_id=session_id,
            modo="mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        )

    except Exception as e:
        logger.error(f"Erro otimizar script: {e}")
        # Retorna uma estrutura compatível com o modelo em caso de erro
        raise HTTPException(status_code=500, detail={
            "detail": "Erro ao processar a otimização do script."
        })

@router.post("/analisar-eficacia", response_model=ScriptsAnaliseResponse, dependencies=[Depends(check_module_permission("scripts"))])
async def scripts_analisar_eficacia(request: ScriptsAnaliseRequest, current_request: Request): # Mantém current_request
    """
    Analisa a eficácia de um script de vendas, retornando uma análise estruturada
    com pontos fortes, fracos e sugestões.
    """
    
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_analisar_eficacia(
                request=current_request,
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        else:
            prompt = prompt_service.obter_prompt_analisar_eficacia_legacy(
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        
        if not USE_MOCK_FORCADO:
            resposta_texto_ia = await chamar_ia_otimizado(prompt, use_cache=False)
            analise = parsear_analise_textual(resposta_texto_ia)
        else:
            analise = detector_analisar_mock(request.produto_descricao, request.script)
        
        # LÓGICA MAIS REALISTA E VALORIZADORA
        pontos_fortes = []
        pontos_fracos = []

        classificacao = analise.get("classificacao", "EQUILIBRADO")
        motivo = analise.get("motivo", "")
        pontuacao = analise.get("pontuacao_empatia", 50)
        script_original = request.script

        # 1. SEMPRE tenta encontrar pontos fortes reais primeiro
        pontos_fortes_detectados = analise.get("pontos_fortes_detectados", [])
        if pontos_fortes_detectados:
            pontos_fortes.extend(pontos_fortes_detectados)

        # 2. Análise OTIMISTA do script original
        script_lower = script_original.lower()
        palavras_script = script_original.split()

        # Pontos fortes BASEADOS EM EVIDÊNCIAS do script
        pontos_fortes_evidencias = []

        # Análise de elementos positivos comprovados
        if "olá" in script_lower or "oi" in script_lower or "bom dia" in script_lower or "boa tarde" in script_lower:
            pontos_fortes_evidencias.append("Saudação educada e apropriada")
            
        if "?" in script_original:
            pontos_fortes_evidencias.append("Estímulo à conversa com perguntas")
            # Análise do tipo de pergunta
            if any(palavra in script_lower for palavra in ["gostaria", "poderia", "você", "seus", "suas"]):
                pontos_fortes_evidencias.append("Foco nas necessidades do cliente")
                
        # Comprimento adequado é sempre positivo
        if len(palavras_script) >= 5:
            pontos_fortes_evidencias.append("Script com conteúdo substancial")
            
        # Elementos de valor
        if any(palavra in script_lower for palavra in ["ajud", "convers", "entend", "ouvir", "escutar"]):
            pontos_fortes_evidencias.append("Disposição para ajudar")
            
        if any(palavra in script_lower for palavra in ["obrigado", "por favor", "agradeço", "com licença"]):
            pontos_fortes_evidencias.append("Cortesia e educação")
            
        if any(palavra in script_lower for palavra in ["objetivo", "meta", "necessidade", "desafio", "dificuldade"]):
            pontos_fortes_evidencias.append("Foco nos objetivos do cliente")

        # 3. Lógica BASEADA EM PONTUAÇÃO REALISTA
        if pontuacao >= 40:  # Scripts com pontuação razoável têm pontos fortes reais
            if not pontos_fortes:
                pontos_fortes.extend(pontos_fortes_evidencias[:3])  # Pega os 3 mais fortes
                
            # Adiciona pontos baseados na classificação
            if "CONSULTIVO" in classificacao.upper() or "EMPÁTICO" in classificacao.upper():
                pontos_fortes.append("Abordagem centrada no cliente")
            elif "EQUILIBRADO" in classificacao.upper():
                pontos_fortes.append("Tom equilibrado e profissional")
                
        elif pontuacao >= 20:  # Scripts com pontuação baixa mas não crítica
            pontos_fortes.extend([
                "Base sólida para desenvolvimento",
                f"Adequação ao canal {request.canal}",
                "Estrutura comunicativa funcional"
            ])

        # 4. Pontos fracos - apenas se realmente problemáticos
        if pontuacao <= 30:
            pontos_fracos.extend([
                f"Oportunidade para aumentar empatia ({pontuacao}%)",
                classificacao
            ])
            if motivo and "genérico" in motivo.lower():
                pontos_fracos.append("Personalização pode ser melhorada")
                
        elif "genérico" in str(motivo).lower():
            pontos_fracos.append("Oportunidade para personalização")

        # 5. Indicadores de problema específicos
        indicadores = analise.get("indicadores_problema", [])
        if indicadores and indicadores != ["Sistema em ajuste"]:
            # Filtra apenas indicadores realmente problemáticos
            problemas_graves = [ind for ind in indicadores if any(termo in ind.lower() for termo in 
                           ['agressivo', 'pressão', 'urgente', 'superlativo', 'impessoal'])]
            pontos_fracos.extend(problemas_graves[:2])  # Máximo 2 indicadores graves

        # 6. FALLBACKS INTELIGENTES E REALISTAS
        if not pontos_fortes:
            if pontuacao >= 50:
                pontos_fortes = ["Abordagem eficaz", "Comunicação clara", "Foco no cliente"]
            elif pontuacao >= 30:
                pontos_fortes = ["Estrutura funcional", f"Adequação ao {request.canal}", "Base para refinamento"]
            else:
                pontos_fortes = ["Oportunidade de aprendizado", "Insights para melhoria", "Desenvolvimento de habilidades"]

        if not pontos_fracos:
            if pontuacao < 60:
                pontos_fracos = ["Oportunidades de refinamento identificadas"]
            else:
                pontos_fracos = ["Nenhum ponto crítico identificado"]

        # Garante qualidade e limite
        pontos_fortes = [pf for pf in pontos_fortes if pf and len(pf) > 5][:3]
        pontos_fracos = [pf for pf in pontos_fracos if pf and len(pf) > 5][:3]

        # Session ID
        session_id = get_or_create_session_id(getattr(request, "session_id", None))
        
        # Salva no histórico
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        if user_id:
            try:
                await history_service.save_simulation_secure(
                    usuario_id=user_id,
                    modulo="script_analise",
                    produto_descricao=request.produto_descricao,
                    conversa={
                        "produto_descricao": request.produto_descricao,
                        "script": request.script,
                        "canal": request.canal
                    },
                    metricas=analise,
                    session_id=session_id
                )
            except Exception as save_error:
                logger.warning(f"Erro ao salvar análise no histórico: {save_error}")

        return ScriptsAnaliseResponse(
            pontuacao=pontuacao,
            pontos_fortes=pontos_fortes,
            pontos_fracos=pontos_fracos,
            sugestoes=[analise.get("sugestao", "Continue praticando."), f"Exemplo: {analise.get('exemplo_corrigido', 'N/A')}"],
            session_id=session_id,
            modo="mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        )
        
    except Exception as e:
        logger.error(f"Erro analisar script: {e}")
        raise HTTPException(status_code=500, detail="Erro ao analisar a eficácia do script.")

@router.post("/analisar-eficacia_OLD2", response_model=ScriptsAnaliseResponse, include_in_schema=False, dependencies=[Depends(check_module_permission("scripts"))])
async def scripts_analisar_eficacia_old2(request: ScriptsAnaliseRequest, current_request: Request):
    """
    Analisa a eficácia de um script de vendas, retornando uma análise estruturada
    com pontos fortes, fracos e sugestões.
    """
    
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_analisar_eficacia(
                request=current_request,
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        else:
            prompt = prompt_service.obter_prompt_analisar_eficacia_legacy(
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        
        if not USE_MOCK_FORCADO:
            resposta_texto_ia = chamar_ia_otimizado(prompt, use_cache=False)
            analise = parsear_analise_textual(resposta_texto_ia)
        else:
            analise = detector_analisar_mock(request.produto_descricao, request.script)
        
        # LÓGICA REFINADA PARA PONTOS FORTES/FRACOS
        pontos_fortes = []
        pontos_fracos = []
        
        classificacao = analise.get("classificacao", "EQUILIBRADO")
        motivo = analise.get("motivo", "")
        pontuacao = analise.get("pontuacao_empatia", 50)
        script_original = request.script
        
        # 1. Usa pontos fortes detectados pelo parsing (mais específicos)
        pontos_fortes_detectados = analise.get("pontos_fortes_detectados", [])
        if pontos_fortes_detectados:
            pontos_fortes.extend(pontos_fortes_detectados)
        
        # 2. Análise inteligente do script original para pontos fortes
        script_lower = script_original.lower()
        palavras_script = script_original.split()
        
        # Pontos fortes baseados no conteúdo do script
        pontos_fortes_potenciais = []
        
        if "olá" in script_lower or "oi" in script_lower or "bom dia" in script_lower or "boa tarde" in script_lower:
            pontos_fortes_potenciais.append("Saudação adequada e educada")
            
        if "?" in script_original:
            pontos_fortes_potenciais.append("Estímulo à conversa com perguntas")
            if any(palavra in script_lower for palavra in ["gostaria", "poderia", "você"]):
                pontos_fortes_potenciais.append("Foco nas necessidades do cliente")
                
        if len(palavras_script) > 8 and len(palavras_script) < 50:
            pontos_fortes_potenciais.append("Script com extensão adequada")
            
        if "ajud" in script_lower or "convers" in script_lower or "entend" in script_lower:
            pontos_fortes_potenciais.append("Disposição para ajudar e entender")
            
        if any(palavra in script_lower for palavra in ["obrigado", "por favor", "agradeço"]):
            pontos_fortes_potenciais.append("Cortesia e educação")
            
        if any(palavra in script_lower for palavra in ["objetivo", "meta", "necessidade", "desafio"]):
            pontos_fortes_potenciais.append("Foco nos objetivos do cliente")
            
        # 3. Lógica baseada na classificação e pontuação
        if pontuacao >= 60:
            # Boa pontuação - enfatiza pontos fortes
            if not pontos_fortes:
                pontos_fortes.extend([
                    f"Alta empatia ({pontuacao}%)",
                    "Abordagem centrada no cliente",
                    f"Adaptado ao canal {request.canal}"
                ])
                
        elif pontuacao <= 30:
            # Baixa pontuação - foca em melhorias
            pontos_fracos.extend([
                f"Baixa empatia ({pontuacao}%)",
                classificacao
            ])
            if motivo:
                pontos_fracos.append(motivo)
        else:
            # Pontuação média - análise balanceada
            if any(termo in classificacao.upper() for termo in ["AGRESSIVO", "POUCA EMPATIA"]):
                pontos_fracos.extend([classificacao, motivo])
            elif any(termo in classificacao.upper() for termo in ["CONSULTIVO", "EMPÁTICO", "EQUILIBRADO"]):
                # Adiciona pontos fortes baseados na classificação positiva
                if "CONSULTIVO" in classificacao.upper():
                    pontos_fortes_potenciais.append("Abordagem consultiva eficaz")
                if "EMPÁTICO" in classificacao.upper():
                    pontos_fortes_potenciais.append("Conexão emocional estabelecida")
                if "EQUILIBRADO" in classificacao.upper():
                    pontos_fortes_potenciais.append("Equilíbrio entre persuasão e respeito")
        
        # 4. Adiciona pontos fortes potenciais (se não conflitarem com a análise)
        if pontuacao > 40:  # Só adiciona se a pontuação for razoável
            for ponto_forte in pontos_fortes_potenciais:
                if ponto_forte not in pontos_fortes:
                    pontos_fortes.append(ponto_forte)
        
        # 5. Adiciona indicadores de problema como pontos fracos
        indicadores = analise.get("indicadores_problema", [])
        if indicadores and indicadores != ["Sistema em ajuste"]:
            pontos_fracos.extend(indicadores)
        
        # 6. Fallbacks finais inteligentes
        if not pontos_fortes:
            if pontuacao >= 60:
                pontos_fortes = [
                    "Abordagem eficaz para o canal",
                    "Comunicação clara e objetiva",
                    "Foco no valor para o cliente"
                ]
            elif pontuacao >= 40:
                pontos_fortes = [
                    "Estrutura básica funcional",
                    f"Adaptado ao canal {request.canal}",
                    "Intenção de comunicação estabelecida"
                ]
            elif pontuacao >= 20:
                pontos_fortes = [
                    "Base para desenvolvimento",
                    "Potencial identificado para refinamento",
                    f"Adequação inicial ao canal {request.canal}"
                ]
            else:
                pontos_fortes = [
                    "Oportunidade de aprendizado prático",
                    "Base para construção de habilidades",
                    "Insights valiosos para melhoria"
                ]
        
        if not pontos_fracos:
            if pontuacao < 70:
                pontos_fracos = ["Oportunidades de refinamento identificadas"]
            else:
                pontos_fracos = ["Nenhum ponto fraco crítico identificado"]

        # Limita e garante qualidade
        pontos_fortes = [pf for pf in pontos_fortes if pf][:3]
        pontos_fracos = [pf for pf in pontos_fracos if pf][:3]

        # Session ID
        session_id = get_or_create_session_id(getattr(request, "session_id", None))
        
        # Salva no histórico
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        if user_id:
            try:
                await history_service.save_simulation_secure(
                    request=current_request,
                    modulo="script_variacoes",
                    produto_descricao=request.produto_descricao,
                    conversa={
                        "produto_descricao": request.produto_descricao,
                        "script": request.script,
                        "canal": request.canal
                    },
                    metricas=analise,
                    session_id=session_id
                )
            except Exception as save_error:
                logger.warning(f"Erro ao salvar análise no histórico: {save_error}")

        return ScriptsAnaliseResponse(
            pontuacao=pontuacao,
            pontos_fortes=pontos_fortes,
            pontos_fracos=pontos_fracos,
            sugestoes=[analise.get("sugestao", "Continue praticando."), f"Exemplo: {analise.get('exemplo_corrigido', 'N/A')}"],
            session_id=session_id,
            modo="mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        )
        
    except Exception as e:
        logger.error(f"Erro analisar script: {e}")
        raise HTTPException(status_code=500, detail="Erro ao analisar a eficácia do script.")

@router.post("/analisar-eficacia_OLD", response_model=ScriptsAnaliseResponse, include_in_schema=False, dependencies=[Depends(check_module_permission("scripts"))])
async def scripts_analisar_eficacia_old(request: ScriptsAnaliseRequest, current_request: Request):
    """
    Analisa a eficácia de um script de vendas, retornando uma análise estruturada
    com pontos fortes, fracos e sugestões.
    """
    
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_analisar_eficacia(
                request=current_request,
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        else:
            prompt = prompt_service.obter_prompt_analisar_eficacia_legacy(
                produto_descricao=request.produto_descricao,
                script=request.script,
                canal=request.canal
            )
        
        if not USE_MOCK_FORCADO:
            resposta_texto_ia = await chamar_ia_otimizado(prompt, use_cache=False)
            
            # CORREÇÃO: Log da resposta da IA para debug
            logger.debug(f"Resposta da IA para análise de script:\n{resposta_texto_ia}")
            
            analise = parsear_analise_textual(resposta_texto_ia)
        else:
            analise = detector_analisar_mock(request.produto_descricao, request.script)
        
        # CORREÇÃO: Mapeamento mais inteligente da análise para a resposta
        pontos_fortes = []
        pontos_fracos = []
        
        classificacao = analise.get("classificacao", "EQUILIBRADO")
        motivo = analise.get("motivo", "")
        pontuacao = analise.get("pontuacao_empatia", 50)
        
        # Lógica melhorada para classificar pontos fortes/fracos
        if pontuacao >= 70:
            pontos_fortes.extend([f"Alta empatia ({pontuacao}%)", classificacao])
            if motivo and "excelente" in motivo.lower() or "bom" in motivo.lower():
                pontos_fortes.append(motivo)
        elif pontuacao <= 30:
            pontos_fracos.extend([f"Baixa empatia ({pontuacao}%)", classificacao])
            if motivo:
                pontos_fracos.append(motivo)
        else:
            # Equilibrado - divide baseado na classificação
            if any(termo in classificacao.upper() for termo in ["AGRESSIVO", "POUCA EMPATIA"]):
                pontos_fracos.extend([classificacao, motivo])
            else:
                pontos_fortes.extend([classificacao, motivo])
        
        # Adiciona indicadores de problema como pontos fracos
        indicadores = analise.get("indicadores_problema", [])
        if indicadores and indicadores != ["Sistema em ajuste"]:
            pontos_fracos.extend(indicadores)
        
        # Garante listas não vazias
        if not pontos_fortes:
            pontos_fortes = ["Abordagem equilibrada", "Comunicação clara"]
        if not pontos_fracos:
            pontos_fracos = ["Nenhum ponto fraco crítico identificado"]

        # Session ID
        session_id = get_or_create_session_id(getattr(request, "session_id", None))
        
        # Salva no histórico
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        if user_id:
            try:
                await history_service.save_simulation_secure(
                    request=current_request,
                    modulo="scripts",
                    produto_descricao=request.produto_descricao,
                    conversa={
                        "produto_descricao": request.produto_descricao,
                        "script": request.script,
                        "canal": request.canal
                    },
                    metricas=analise,
                    session_id=session_id
                )
            except Exception as save_error:
                logger.warning(f"Erro ao salvar análise no histórico: {save_error}")

        return ScriptsAnaliseResponse(
            pontuacao=pontuacao,
            pontos_fortes=pontos_fortes[:3],  # Limita a 3 itens
            pontos_fracos=pontos_fracos[:3],  # Limita a 3 itens
            sugestoes=[analise.get("sugestao", "Continue praticando."), f"Exemplo: {analise.get('exemplo_corrigido', 'N/A')}"],
            session_id=session_id,
            modo="mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        )
        
    except Exception as e:
        logger.error(f"Erro analisar script: {e}")
        raise HTTPException(status_code=500, detail="Erro ao analisar a eficácia do script.")

# CORREÇÃO: Adiciona response_model para garantir a estrutura de retorno.
@router.post("/gerar-variacoes", response_model=ScriptsVariacoesResponse, dependencies=[Depends(check_module_permission("scripts"))])
async def scripts_gerar_variacoes(request: ScriptsVariacoesRequest, current_request: Request): # Mantém current_request
    try:
        prompt = await prompt_service.obter_prompt_gerar_variacoes(
            request=current_request,
            produto_descricao=request.produto_descricao,
            script_base=request.script_base,
            canal=request.canal,
            numero_variacoes=request.numero_variacoes
        )
        if not USE_MOCK_FORCADO:
            # CORREÇÃO: A função chamar_ia_otimizado é síncrona.
            resposta_ia = await chamar_ia_otimizado(prompt, use_cache=False)
            variacoes = parsing_utils.parse_script_variations(resposta_ia, request.numero_variacoes)
        else:
            variacoes = [{"nome": "Variação Mock", "script": f"Variação do script: {request.script_base}"}]
        
        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(getattr(request, "session_id", None))
        
        # Obtém o user_id do request.state (definido pelo middleware)
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        if not user_id:
            raise HTTPException(status_code=401, detail="Usuário não autenticado.")

        # Salva a interação no histórico
        try:
            await history_service.save_simulation_secure(
                usuario_id=user_id,
                modulo="script_variacoes",
                produto_descricao=request.produto_descricao,
                conversa={
                    "produto_descricao": request.produto_descricao,
                    "script_base": request.script_base,
                    "canal": request.canal,
                    "numero_variacoes": request.numero_variacoes
                },
                metricas={"variacoes": variacoes},
                session_id=session_id
            )
        except Exception as save_error:
            logger.warning(f"Erro ao salvar variações de script no histórico: {save_error}")

        return ScriptsVariacoesResponse(
            variacoes=variacoes, 
            session_id=session_id,
            modo="mock" if USE_MOCK_FORCADO else PROVIDER_NOME
        )
    except Exception as e:
        logger.error(f"Erro gerar variações: {e}")
        raise HTTPException(status_code=500, detail="Erro ao gerar variações do script.")
