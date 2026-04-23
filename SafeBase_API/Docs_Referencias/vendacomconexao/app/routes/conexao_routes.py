from fastapi import APIRouter, Request, HTTPException, Depends
import logging
import json
import re
import random
from datetime import datetime

from app.models import ConexaoPerguntasRequest, ConexaoAnaliseRequest, ConexaoDialogoRequest, RoteiroAbordagemRequest, RoteiroAbordagemResponse
from app.services.prompt_service import prompt_service
from app.services.history_service import history_service
from app.dependencies import check_module_permission, chamar_ia_otimizado
from app.config import USE_MOCK_FORCADO, PROVIDER_NOME
from app.services.utils_service import parsear_analise_resposta_conexao, should_use_personalized_context, get_or_create_session_id

logger = logging.getLogger(__name__)
router = APIRouter(
    prefix="/conexao",
    tags=["Conexão"]
)

@router.post("/gerar-perguntas", dependencies=[Depends(check_module_permission("conexao"))])
async def conexao_gerar_perguntas(request: ConexaoPerguntasRequest, current_request: Request):
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_gerar_perguntas(
                request=current_request,
                produto_descricao=request.produto_descricao,
                contexto_cliente=request.contexto_cliente
            )
        else:
            prompt = f"Para vender {request.produto_descricao} para: {request.contexto_cliente}\n\nGere 5 perguntas abertas e exploratórias para descobrir dores reais."
        
        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            perguntas = [linha.strip() for linha in resultado.split('\n') if linha.strip() and any(c.isalnum() for c in linha)]
        else:
            perguntas = [f"O que mais te incomoda atualmente com [problema que {request.produto_descricao} resolve]?"]

        dicas = f"Use estas perguntas para entender profundamente as necessidades antes de oferecer {request.produto_descricao}."

        # --- INÍCIO DA LÓGICA DE HISTÓRICO ---
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        session_id = get_or_create_session_id(request.session_id) # Usar session_id da requisição

        await history_service.save_simulation_secure(
            usuario_id=user_id,
            modulo="conexao_gerar_perguntas",
            produto_descricao=request.produto_descricao,
            conversa={"perguntas_geradas": perguntas, "dicas": dicas},
            metricas={"contexto_cliente": request.contexto_cliente},
            session_id=session_id
        )
        # --- FIM DA LÓGICA DE HISTÓRICO ---

        return {"perguntas": perguntas, "dicas": dicas, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
    except Exception as e:
        logger.error(f"Erro gerar perguntas: {e}")
        dicas = "Perguntas geradas em modo de contingência."
        return {"perguntas": [f"Qual seu maior desafio com [área do {request.produto_descricao}]?"], "dicas": dicas, "modo": "mock-fallback"}

@router.post("/analisar-resposta", dependencies=[Depends(check_module_permission("conexao"))])
async def conexao_analisar_resposta(request: ConexaoAnaliseRequest, current_request: Request):
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_analisar_resposta(
                request=current_request,
                produto_descricao=request.produto_descricao,
                pergunta=request.pergunta,
                resposta_vendedor=request.resposta_vendedor
            )
        else:
            prompt = prompt_service.obter_prompt_analisar_resposta_legacy(
                produto_descricao=request.produto_descricao,
                pergunta=request.pergunta,
                resposta_vendedor=request.resposta_vendedor
            )
        
        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            data = parsear_analise_resposta_conexao(resultado)
        else:
            data = {"pontuacao": random.randint(65, 90), "feedback": "Boa resposta!", "sugestoes": ["Faça uma pergunta de follow-up."]}
        
        # --- INÍCIO DA LÓGICA DE HISTÓRICO ---
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None
        session_id = get_or_create_session_id(request.session_id) # Usar session_id da requisição

        await history_service.save_simulation_secure(
            usuario_id=user_id,
            modulo="conexao_analisar_resposta",
            produto_descricao=request.produto_descricao,
            conversa={"pergunta_feita": request.pergunta, "resposta_analisada": request.resposta_vendedor},
            metricas={"pontuacao": data.get("pontuacao"), "feedback": data.get("feedback"), "sugestoes": data.get("sugestoes")},
            session_id=session_id
        )
        # --- FIM DA LÓGICA DE HISTÓRICO ---

        return {**data, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
    except Exception as e:
        logger.error(f"Erro analisar resposta: {e}")
        return {"pontuacao": 70, "feedback": "Análise indisponível.", "sugestoes": [], "modo": "mock-fallback"}

@router.post("/simular-dialogo", dependencies=[Depends(check_module_permission("conexao"))])
async def conexao_simular_dialogo(request: ConexaoDialogoRequest, current_request: Request):
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_simular_dialogo(
                request=current_request,
                produto_descricao=request.produto_descricao,
                cenario=request.cenario,
                abordagem=request.abordagem
            )
        else:
            prompt = prompt_service.obter_prompt_simular_dialogo_legacy(
                produto_descricao=request.produto_descricao,
                cenario=request.cenario,
                abordagem=request.abordagem
            )
        
        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
            
            # Correção 2.0: Tornar o parsing mais robusto com regex e fallback
            analise_match = re.search(r'\*\*ANÁLISE:\*\*(.*)', resultado, re.DOTALL | re.IGNORECASE)
            if analise_match:
                analise = analise_match.group(1).strip()
                # Remove a parte da análise do resultado para processar apenas o diálogo
                resultado = resultado[:analise_match.start()]
            else:
                analise = "Diálogo simulado."

            linhas = [linha.strip() for linha in resultado.strip().split('\n') if linha.strip()]
            dialogo = []
            for linha in linhas:
                linha_upper = linha.upper()
                if "VENDEDOR:" in linha_upper:
                    dialogo.append({"tipo": "vendedor", "mensagem": re.sub(r'\*+\s*VENDEDOR:\s*\*+', '', linha, flags=re.IGNORECASE).strip()})
                elif "CLIENTE:" in linha_upper:
                    dialogo.append({"tipo": "cliente", "mensagem": re.sub(r'\*+\s*CLIENTE:\s*\*+', '', linha, flags=re.IGNORECASE).strip()})
                elif "**CENÁRIO:" not in linha and "**ABORDAGEM:" not in linha:
                    dialogo.append({"tipo": "vendedor", "mensagem": linha}) # Fallback para vendedor para evitar "sistema"
        else:
            dialogo = [{"tipo": "vendedor", "mensagem": "Olá!"}, {"tipo": "cliente", "mensagem": "Oi."}]
            analise = "Diálogo mock."

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)

        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

        await history_service.save_simulation_secure(
            usuario_id=user_id,
            modulo="conexao_dialogo",
            produto_descricao=request.produto_descricao,
            conversa={"dialogo": dialogo},
            metricas={"cenario": request.cenario, "abordagem": request.abordagem},
            session_id=session_id
        )
        
        return {"dialogo": dialogo, "analise": analise, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
    except Exception as e:
        logger.error(f"Erro simular diálogo: {e}")
        return {"dialogo": [], "analise": "Erro na simulação.", "modo": "mock-fallback", "session_id": request.session_id or "error"}


@router.post("/roteiro-abordagem", dependencies=[Depends(check_module_permission("conexao"))])
async def conexao_roteiro_abordagem(request: RoteiroAbordagemRequest, current_request: Request):
    try:
        if should_use_personalized_context(current_request):
            prompt = await prompt_service.obter_prompt_roteiro_abordagem(
                request=current_request,
                produto_descricao=request.produto_descricao,
                tipo_pessoa=request.tipo_pessoa,
                nome_cliente=request.nome_cliente
            )
        else:
            prompt = prompt_service.renderizar_prompt(
                "conexao_roteiro_abordagem",
                produto_descricao=request.produto_descricao,
                tipo_pessoa=request.tipo_pessoa,
                nome_cliente=request.nome_cliente or ""
            )

        if not USE_MOCK_FORCADO:
            resultado = await chamar_ia_otimizado(prompt)
        else:
            resultado = f"Roteiro mock para {request.produto_descricao} ({request.tipo_pessoa})"

        # Parser simples: extrai os blocos numerados 1..10 em um dict
        roteiro_estruturado = {}
        for i, key in enumerate(["inicio_variacoes", "pergunta_confirmacao", "reforco_valor", "pergunta_consultiva", "validacao_resposta", "pergunta_estrategica", "normalizacao", "revelacao_sutil", "convite", "fechamento_opcoes"], start=1):
            m = re.search(rf"{i}\.\s*(.*?)(?=(?:\n\s*\d+\.)|\Z)", resultado, flags=re.DOTALL)
            trecho = m.group(1).strip() if m else ""
            roteiro_estruturado[key] = trecho

        # Garante que temos uma session_id para a conversa
        session_id = get_or_create_session_id(request.session_id)
        user_id = current_request.state.current_user_id if hasattr(current_request.state, 'current_user_id') else None

        await history_service.save_simulation_secure(
            usuario_id=user_id,
            modulo="roteiro_abordagem",
            produto_descricao=request.produto_descricao,
            perfil_cliente=request.tipo_pessoa,
            conversa={"prompt": prompt, "roteiro_ia": resultado, "roteiro_estruturado": roteiro_estruturado},
            metricas={"tipo_pessoa": request.tipo_pessoa},
            session_id=session_id
        )

        return {"roteiro_ia": resultado, "roteiro_estruturado": roteiro_estruturado, "modo": "mock" if USE_MOCK_FORCADO else PROVIDER_NOME, "session_id": session_id}
    except Exception as e:
        logger.error(f"Erro gerar roteiro de abordagem: {e}")
        return {"roteiro_ia": "Roteiro indisponível.", "roteiro_estruturado": {}, "modo": "mock-fallback"}