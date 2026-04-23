# app/services/utils_service.py
import json
import random
import time
from datetime import datetime
from typing import Dict, List, Any, Optional
import logging
from app.utils.parsing_utils import parsing_utils
from fastapi import Request
import re
import uuid


logger = logging.getLogger(__name__)

# ========== FUNÇÕES GERAIS ==========
def parsear_resposta_contexto(texto_resposta):
    """Parseia a resposta do ContextGuardian"""
    try:
        # Tenta extrair JSON
        start = texto_resposta.find('{')
        end = texto_resposta.rfind('}') + 1
        if start != -1 and end != 0:
            json_str = texto_resposta[start:end]
            return json.loads(json_str)
    except:
        pass

    # Fallback para parsing textual
    return {
        "alinhamento_contextual": 0.5,
        "ruptura_detectada": False,
        "sugestao_transicao": "Continue a conversa naturalmente.",
        "topicos_nao_abordados": [],
        "nivel_urgencia": "MEDIO"
    }

def parsear_resposta_predicao_objecoes(texto_resposta):
    """Parseia a resposta do ObjectionPredictor"""
    try:
        # Tenta extrair JSON
        start = texto_resposta.find('{')
        end = texto_resposta.rfind('}') + 1
        if start != -1 and end != 0:
            json_str = texto_resposta[start:end]
            return json.loads(json_str)
    except:
        pass

    # Fallback para parsing textual
    return {
        "objecoes_provaveis": [],
        "sinais_detectados": [],
        "abordagem_preventiva": "Mantenha a abordagem atual.",
        "nivel_risco": "MEDIO"
    }

def parsear_resposta_mudanca_emocional(texto_resposta):
    """Parseia a resposta do EmotionShift"""
    try:
        # Tenta extrair JSON
        start = texto_resposta.find('{')
        end = texto_resposta.rfind('}') + 1
        if start != -1 and end != 0:
            json_str = texto_resposta[start:end]
            return json.loads(json_str)
    except:
        pass

    # Fallback para parsing textual
    return {
        "mudanca_detectada": False,
        "ponto_virada": "",
        "direcao_mudanca": "",
        "emocao_antes": "",
        "emocao_depois": "",
        "fator_critico": "",
        "sugestao_ajuste_imediato": "Continue a conversa naturalmente.",
        "estrategia_recuperacao": "",
        "alerta_risco": "MEDIO",
        "probabilidade_recuperacao": 0.5
    }

def parsear_analise_resposta_conexao(texto_resposta: str) -> Dict[str, Any]:
    """
    Parseia a resposta da IA para a análise de resposta do módulo de conexão.
    Tenta extrair um JSON, mas tem um fallback robusto se falhar.
    """
    try:
        # Tenta extrair JSON do texto
        start = texto_resposta.find('{')
        end = texto_resposta.rfind('}') + 1
        if start != -1 and end != 0:
            json_str = texto_resposta[start:end]
            data = json.loads(json_str)
            # Garante que as chaves esperadas existam
            data.setdefault("pontuacao", 75)
            data.setdefault("feedback", "Análise parcial, continue praticando.")
            data.setdefault("sugestoes", [])
            return data
    except (json.JSONDecodeError, TypeError):
        logger.warning("Falha ao parsear JSON da análise de resposta. Usando fallback textual.")
        # Fallback se o JSON falhar: retorna uma estrutura padrão
        return {"pontuacao": 80, "feedback": "Resposta adequada para construir rapport.", "sugestoes": ["Valide o sentimento do cliente", "Explore mais a dor específica"]}

def parsear_analise_textual(texto_resposta):
    """Faz parsing inteligente do formato textual detalhado - VERSÃO FINAL"""
    try:
        logger.debug(f"Iniciando parsing da resposta da IA:\n---\n{texto_resposta}\n---")

        # Valores padrão
        analise = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando e ajustando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": []
        }

        def extrair_campo_seguro(campo, texto):
            try:
                padrao = rf"(?i)\*?\*?{re.escape(campo)}\*?\*?\s*:?\s*(.*?)(?=\n\s*\*?\*?\w|\Z)"
                match = re.search(padrao, texto, re.DOTALL)
                if match:
                    resultado = match.group(1).strip()
                    resultado = re.sub(r'^[\*\-\s]+|[\*\-\s]+$', '', resultado)
                    return resultado if resultado else None
                return None
            except Exception as e:
                logger.warning(f"Erro ao extrair campo {campo}: {e}")
                return None

        # Extração dos campos
        classificacao = extrair_campo_seguro("Classificação", texto_resposta)
        if classificacao:
            analise['classificacao'] = classificacao

        motivo = extrair_campo_seguro("Motivo", texto_resposta)
        if motivo:
            analise['motivo'] = motivo

        sugestao = extrair_campo_seguro("Sugestão", texto_resposta)
        if sugestao:
            analise['sugestao'] = sugestao

        nivel_pressao = extrair_campo_seguro("Nível de Pressão", texto_resposta)
        if nivel_pressao:
            analise['nivel_pressao'] = nivel_pressao

        exemplo_corrigido = extrair_campo_seguro("Exemplo Corrigido", texto_resposta)
        if exemplo_corrigido:
            analise['exemplo_corrigido'] = exemplo_corrigido

        # Tratamento especial para pontuação
        pontuacao_str = extrair_campo_seguro("Pontuação de Empatia", texto_resposta)
        if pontuacao_str:
            match = re.search(r'(\d+)', pontuacao_str)
            if match:
                analise['pontuacao_empatia'] = int(match.group(1))

        # Tratamento especial para indicadores de problema
        indicadores_str = extrair_campo_seguro("Indicadores de Problema", texto_resposta)
        if indicadores_str:
            indicadores = re.findall(r'[-*•]\s*(.*?)(?=\n|$)', indicadores_str)
            if indicadores:
                analise['indicadores_problema'] = [ind.strip() for ind in indicadores if ind.strip()]
            else:
                indicadores = [item.strip() for item in re.split(r'[,;\n]', indicadores_str) if item.strip()]
                if indicadores:
                    analise['indicadores_problema'] = indicadores

        # DETECÇÃO MELHORADA E MAIS REALISTA DE PONTOS FORTES
        pontos_fortes_detectados = []
        texto_lower = texto_resposta.lower()

        # Análise mais realista e menos rigorosa
        if any(termo in texto_lower for termo in ['adequado', 'eficaz', 'claro', 'objetivo', 'focado', 'direto', 'profissional']):
            if 'claro' in texto_lower or 'clareza' in texto_lower:
                pontos_fortes_detectados.append("Comunicação clara e objetiva")
            if 'profissional' in texto_lower:
                pontos_fortes_detectados.append("Tom profissional adequado")
            if 'focado' in texto_lower or 'direto' in texto_lower:
                pontos_fortes_detectados.append("Objetividade e foco na solução")

        # Detecção por elementos positivos no motivo
        if motivo:
            motivo_lower = motivo.lower()
            if any(termo in motivo_lower for termo in ['não agressivo', 'não pressiona', 'respeitoso', 'educado']):
                pontos_fortes_detectados.append("Abordagem respeitosa")
            if 'pergunta' in motivo_lower or 'question' in motivo_lower:
                pontos_fortes_detectados.append("Uso estratégico de perguntas")
            if 'convers' in motivo_lower or 'diálogo' in motivo_lower:
                pontos_fortes_detectados.append("Estímulo ao diálogo")

        # Detecção específica por classificação - MAIS REALISTA
        if "CONSULTIVO" in classificacao.upper():
            pontos_fortes_detectados.extend(["Abordagem centrada no cliente", "Foco em entender necessidades"])
        elif "EMPÁTICO" in classificacao.upper():
            pontos_fortes_detectados.extend(["Conexão emocional", "Sensibilidade às emoções"])
        elif "EQUILIBRADO" in classificacao.upper():
            pontos_fortes_detectados.extend(["Equilíbrio entre persuasão e respeito", "Tom adequado"])
        elif "POUCA EMPATIA" in classificacao.upper() and analise['pontuacao_empatia'] > 30:
            # Para "POUCA EMPATIA" com pontuação razoável, ainda há pontos positivos
            pontos_fortes_detectados.append("Estrutura básica funcional")

        # Análise da pontuação para detectar pontos fortes implícitos
        if analise['pontuacao_empatia'] >= 50:
            pontos_fortes_detectados.append("Nível de empatia adequado")
        if analise['pontuacao_empatia'] >= 60:
            pontos_fortes_detectados.append("Abordagem eficaz")
        if "genérico" not in texto_lower or analise['pontuacao_empatia'] > 40:
            pontos_fortes_detectados.append("Potencial para personalização")

        analise['pontos_fortes_detectados'] = list(set(pontos_fortes_detectados))

        logger.debug(f"✅ Parsing concluído: {analise['classificacao']} - Pontuação: {analise['pontuacao_empatia']}")
        return analise

    except Exception as e:
        logger.error(f"❌ Erro crítico no parsing textual: {e}")
        
        fallback = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": ["Comunicação básica funcional"]
        }

        texto_lower = texto_resposta.lower()
        if any(palavra in texto_lower for palavra in ['agressivo', 'pressão', 'urgente', 'compre agora']):
            fallback["classificacao"] = "AGRESSIVO"
            fallback["pontuacao_empatia"] = 10
            fallback["nivel_pressao"] = "ALTO"
        elif any(palavra in texto_lower for palavra in ['empático', 'entender', 'compreender']):
            fallback["classificacao"] = "EMPÁTICO" 
            fallback["pontuacao_empatia"] = 85
            fallback["nivel_pressao"] = "BAIXO"
            fallback["pontos_fortes_detectados"] = ["Abordagem empática", "Conexão emocional"]
        elif any(palavra in texto_lower for palavra in ['pouca empatia', 'genérico', 'impessoal']):
            fallback["classificacao"] = "POUCA EMPATIA"
            fallback["pontuacao_empatia"] = 25
            fallback["nivel_pressao"] = "MÉDIO"

        return fallback

def parsear_analise_textual_old3(texto_resposta):
    """Faz parsing inteligente do formato textual detalhado - REFINADO"""
    try:
        logger.debug(f"Iniciando parsing da resposta da IA:\n---\n{texto_resposta}\n---")

        # Valores padrão
        analise = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando e ajustando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": []
        }

        def extrair_campo_seguro(campo, texto):
            try:
                padrao = rf"(?i)\*?\*?{re.escape(campo)}\*?\*?\s*:?\s*(.*?)(?=\n\s*\*?\*?\w|\Z)"
                match = re.search(padrao, texto, re.DOTALL)
                if match:
                    resultado = match.group(1).strip()
                    resultado = re.sub(r'^[\*\-\s]+|[\*\-\s]+$', '', resultado)
                    return resultado if resultado else None
                return None
            except Exception as e:
                logger.warning(f"Erro ao extrair campo {campo}: {e}")
                return None

        # Extração dos campos
        classificacao = extrair_campo_seguro("Classificação", texto_resposta)
        if classificacao:
            analise['classificacao'] = classificacao

        motivo = extrair_campo_seguro("Motivo", texto_resposta)
        if motivo:
            analise['motivo'] = motivo

        sugestao = extrair_campo_seguro("Sugestão", texto_resposta)
        if sugestao:
            analise['sugestao'] = sugestao

        nivel_pressao = extrair_campo_seguro("Nível de Pressão", texto_resposta)
        if nivel_pressao:
            analise['nivel_pressao'] = nivel_pressao

        exemplo_corrigido = extrair_campo_seguro("Exemplo Corrigido", texto_resposta)
        if exemplo_corrigido:
            analise['exemplo_corrigido'] = exemplo_corrigido

        # Tratamento especial para pontuação
        pontuacao_str = extrair_campo_seguro("Pontuação de Empatia", texto_resposta)
        if pontuacao_str:
            match = re.search(r'(\d+)', pontuacao_str)
            if match:
                analise['pontuacao_empatia'] = int(match.group(1))

        # Tratamento especial para indicadores de problema
        indicadores_str = extrair_campo_seguro("Indicadores de Problema", texto_resposta)
        if indicadores_str:
            indicadores = re.findall(r'[-*•]\s*(.*?)(?=\n|$)', indicadores_str)
            if indicadores:
                analise['indicadores_problema'] = [ind.strip() for ind in indicadores if ind.strip()]
            else:
                indicadores = [item.strip() for item in re.split(r'[,;\n]', indicadores_str) if item.strip()]
                if indicadores:
                    analise['indicadores_problema'] = indicadores

        # DETECÇÃO MELHORADA DE PONTOS FORTES
        pontos_fortes_detectados = []
        texto_lower = texto_resposta.lower()
        
        # Análise mais sofisticada do conteúdo
        if any(termo in texto_lower for termo in ['excelente', 'ótimo', 'bom', 'positivo', 'adequado', 'eficaz', 'forte']):
            if 'direto' in texto_lower and 'ponto' in texto_lower:
                pontos_fortes_detectados.append("Objetividade e foco na solução")
            if 'claro' in texto_lower or 'clareza' in texto_lower:
                pontos_fortes_detectados.append("Clareza na comunicação")
            if 'profissional' in texto_lower:
                pontos_fortes_detectados.append("Tom profissional adequado")
            if 'consultivo' in texto_lower:
                pontos_fortes_detectados.append("Abordagem consultiva")
            if 'empático' in texto_lower:
                pontos_fortes_detectados.append("Empatia na abordagem")
            if 'personaliz' in texto_lower:
                pontos_fortes_detectados.append("Personalização da mensagem")
            if 'valor' in texto_lower and 'agreg' in texto_lower:
                pontos_fortes_detectados.append("Foco em agregar valor")
            if 'pergunta' in texto_lower or 'question' in texto_lower:
                pontos_fortes_detectados.append("Uso estratégico de perguntas")
            if 'convers' in texto_lower or 'diálogo' in texto_lower:
                pontos_fortes_detectados.append("Estímulo ao diálogo")
            if 'respeito' in texto_lower:
                pontos_fortes_detectados.append("Tom respeitoso")
            if 'objetivo' in texto_lower:
                pontos_fortes_detectados.append("Foco nos objetivos do cliente")
                
        # Detecção específica por classificação
        if "CONSULTIVO" in classificacao.upper():
            pontos_fortes_detectados.append("Abordagem centrada no cliente")
        if "EMPÁTICO" in classificacao.upper():
            pontos_fortes_detectados.append("Conexão emocional estabelecida")
        if "EQUILIBRADO" in classificacao.upper():
            pontos_fortes_detectados.append("Equilíbrio entre persuasão e respeito")
            
        analise['pontos_fortes_detectados'] = list(set(pontos_fortes_detectados))  # Remove duplicatas

        logger.debug(f"✅ Parsing concluído: {analise['classificacao']} - Pontuação: {analise['pontuacao_empatia']}")
        return analise

    except Exception as e:
        logger.error(f"❌ Erro crítico no parsing textual: {e}")
        
        fallback = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": ["Comunicação básica funcional"]
        }

        texto_lower = texto_resposta.lower()
        if any(palavra in texto_lower for palavra in ['agressivo', 'pressão', 'urgente', 'compre agora']):
            fallback["classificacao"] = "AGRESSIVO"
            fallback["pontuacao_empatia"] = 10
            fallback["nivel_pressao"] = "ALTO"
        elif any(palavra in texto_lower for palavra in ['empático', 'entender', 'compreender']):
            fallback["classificacao"] = "EMPÁTICO" 
            fallback["pontuacao_empatia"] = 85
            fallback["nivel_pressao"] = "BAIXO"
            fallback["pontos_fortes_detectados"] = ["Abordagem empática", "Conexão emocional"]
        elif any(palavra in texto_lower for palavra in ['pouca empatia', 'genérico', 'impessoal']):
            fallback["classificacao"] = "POUCA EMPATIA"
            fallback["pontuacao_empatia"] = 25
            fallback["nivel_pressao"] = "MÉDIO"

        return fallback

def parsear_analise_textual_OLD2(texto_resposta):
    """Faz parsing inteligente do formato textual detalhado - MELHORADO"""
    try:
        logger.debug(f"Iniciando parsing da resposta da IA:\n---\n{texto_resposta}\n---")

        # Valores padrão
        analise = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando e ajustando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": []  # NOVO: para capturar pontos fortes explícitos
        }

        def extrair_campo_seguro(campo, texto):
            try:
                padrao = rf"(?i)\*?\*?{re.escape(campo)}\*?\*?\s*:?\s*(.*?)(?=\n\s*\*?\*?\w|\Z)"
                match = re.search(padrao, texto, re.DOTALL)
                if match:
                    resultado = match.group(1).strip()
                    resultado = re.sub(r'^[\*\-\s]+|[\*\-\s]+$', '', resultado)
                    return resultado if resultado else None
                return None
            except Exception as e:
                logger.warning(f"Erro ao extrair campo {campo}: {e}")
                return None

        # Extração dos campos
        classificacao = extrair_campo_seguro("Classificação", texto_resposta)
        if classificacao:
            analise['classificacao'] = classificacao

        motivo = extrair_campo_seguro("Motivo", texto_resposta)
        if motivo:
            analise['motivo'] = motivo

        sugestao = extrair_campo_seguro("Sugestão", texto_resposta)
        if sugestao:
            analise['sugestao'] = sugestao

        nivel_pressao = extrair_campo_seguro("Nível de Pressão", texto_resposta)
        if nivel_pressao:
            analise['nivel_pressao'] = nivel_pressao

        exemplo_corrigido = extrair_campo_seguro("Exemplo Corrigido", texto_resposta)
        if exemplo_corrigido:
            analise['exemplo_corrigido'] = exemplo_corrigido

        # Tratamento especial para pontuação
        pontuacao_str = extrair_campo_seguro("Pontuação de Empatia", texto_resposta)
        if pontuacao_str:
            match = re.search(r'(\d+)', pontuacao_str)
            if match:
                analise['pontuacao_empatia'] = int(match.group(1))

        # Tratamento especial para indicadores de problema
        indicadores_str = extrair_campo_seguro("Indicadores de Problema", texto_resposta)
        if indicadores_str:
            indicadores = re.findall(r'[-*•]\s*(.*?)(?=\n|$)', indicadores_str)
            if indicadores:
                analise['indicadores_problema'] = [ind.strip() for ind in indicadores if ind.strip()]
            else:
                indicadores = [item.strip() for item in re.split(r'[,;\n]', indicadores_str) if item.strip()]
                if indicadores:
                    analise['indicadores_problema'] = indicadores

        # NOVO: Detecção de pontos fortes no texto da análise
        pontos_fortes_detectados = []
        texto_lower = texto_resposta.lower()
        
        # Detecta elogios e aspectos positivos
        if any(termo in texto_lower for termo in ['excelente', 'ótimo', 'bom', 'positivo', 'adequado', 'eficaz']):
            if 'direto' in texto_lower and 'ponto' in texto_lower:
                pontos_fortes_detectados.append("Objetividade e foco")
            if 'claro' in texto_lower or 'clareza' in texto_lower:
                pontos_fortes_detectados.append("Clareza na comunicação")
            if 'profissional' in texto_lower:
                pontos_fortes_detectados.append("Tom profissional")
            if 'consultivo' in texto_lower:
                pontos_fortes_detectados.append("Abordagem consultiva")
            if 'empático' in texto_lower:
                pontos_fortes_detectados.append("Empatia na abordagem")
            if 'personaliz' in texto_lower:
                pontos_fortes_detectados.append("Personalização")
            if 'valor' in texto_lower and 'agreg' in texto_lower:
                pontos_fortes_detectados.append("Foco em agregar valor")
                
        analise['pontos_fortes_detectados'] = pontos_fortes_detectados

        logger.debug(f"✅ Parsing concluído: {analise['classificacao']} - Pontuação: {analise['pontuacao_empatia']}")
        return analise

    except Exception as e:
        logger.error(f"❌ Erro crítico no parsing textual: {e}")
        
        fallback = {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis.",
            "pontos_fortes_detectados": ["Comunicação básica funcional"]
        }

        texto_lower = texto_resposta.lower()
        if any(palavra in texto_lower for palavra in ['agressivo', 'pressão', 'urgente', 'compre agora']):
            fallback["classificacao"] = "AGRESSIVO"
            fallback["pontuacao_empatia"] = 10
            fallback["nivel_pressao"] = "ALTO"
        elif any(palavra in texto_lower for palavra in ['empático', 'entender', 'compreender']):
            fallback["classificacao"] = "EMPÁTICO" 
            fallback["pontuacao_empatia"] = 85
            fallback["nivel_pressao"] = "BAIXO"
            fallback["pontos_fortes_detectados"] = ["Abordagem empática", "Conexão emocional"]
        elif any(palavra in texto_lower for palavra in ['pouca empatia', 'genérico', 'impessoal']):
            fallback["classificacao"] = "POUCA EMPATIA"
            fallback["pontuacao_empatia"] = 25
            fallback["nivel_pressao"] = "MÉDIO"

        return fallback

def parsear_analise_textual_old(texto_resposta):
    """Faz parsing inteligente do formato textual detalhado, restaurado do backup para maior robustez."""
    try:
        # Log da resposta completa da IA para depuração
        logger.debug(f"Iniciando parsing da resposta da IA:\n---\n{texto_resposta}\n---")

        # Valores padrão
        analise = {
            "classificacao": "N/A",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando e ajustando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO",
            "indicadores_problema": ["Análise em processamento"],
            "exemplo_corrigido": "Mensagem de exemplo não disponível no momento."
        }

        # Extração usando regex, que é mais flexível que split
        def extrair_campo(campo, texto):
            # A regex busca pelo nome do campo (com ou sem negrito) e captura o texto até o próximo campo ou o final.
            # CORREÇÃO: A compilação da regex foi ajustada para ser mais robusta e evitar o erro "no such group".
            padrao = re.compile(r"\*\*{}:\*\*\s*(.*?)(?=\n\*\*|$)".format(re.escape(campo)), re.DOTALL | re.IGNORECASE)
            match = padrao.search(texto)
            return match.group(1).strip() if match else None

        analise['classificacao'] = extrair_campo("Classificação", texto_resposta) or analise['classificacao']
        analise['motivo'] = extrair_campo("Motivo", texto_resposta) or analise['motivo']
        analise['sugestao'] = extrair_campo("Sugestão", texto_resposta) or analise['sugestao']
        analise['nivel_pressao'] = extrair_campo("Nível de Pressão", texto_resposta) or analise['nivel_pressao']
        analise['exemplo_corrigido'] = extrair_campo("Exemplo Corrigido", texto_resposta) or analise['exemplo_corrigido']

        # Tratamento especial para campos numéricos e listas
        pontuacao_str = extrair_campo("Pontuação de Empatia", texto_resposta)
        if pontuacao_str:
            match = re.search(r'\d+', pontuacao_str)
            if match:
                analise['pontuacao_empatia'] = int(match.group(1))

        indicadores_str = extrair_campo("Indicadores de Problema", texto_resposta)
        if indicadores_str:
            # Extrai itens que começam com '-' ou '*'
            indicadores = re.findall(r'[-*]\s*(.*)', indicadores_str)
            if indicadores:
                analise['indicadores_problema'] = [ind.strip() for ind in indicadores]

        return analise
    except Exception as e:
        # Log aprimorado em caso de erro, incluindo a resposta completa
        logger.error(f"Erro no parsing textual: {e}. Resposta da IA que causou o erro:\n---\n{texto_resposta}\n---")
        # Fallback básico
        return {
            "classificacao": "EQUILIBRADO",
            "motivo": "Análise detalhada não disponível.",
            "sugestao": "Continue praticando sua abordagem.",
            "pontuacao_empatia": 50,
            "nivel_pressao": "MÉDIO", 
            "indicadores_problema": ["Sistema em ajuste"],
            "exemplo_corrigido": "Em breve teremos exemplos corrigidos disponíveis."
        }

async def analisar_probabilidade_conversao(produto_descricao, conversa_completa, perfil_cliente, use_mock_forcado, provider_nome, chamar_ia_otimizado_func):
    """Analisa a probabilidade de conversão baseada na conversa"""
    
    try:
        # Formatar conversa para texto
        conversa_texto = ""
        for i, msg in enumerate(conversa_completa):
            speaker = "VENDEDOR" if msg.get("tipo") == "usuario" else "CLIENTE"
            conversa_texto += f"{i+1}. {speaker}: {msg.get('texto', '')}\n"
        
        prompt = f"""
Analise esta conversa de vendas e calcule a probabilidade de conversão (0-100%):

PRODUTO: {produto_descricao}
PERFIL CLIENTE: {perfil_cliente}
CONVERSA COMPLETA:
{conversa_texto}

Forneça análise em formato JSON:
{{
    "probabilidade": 0-100,
    "nivel": "BAIXO|MEDIO|ALTO|MUITO_ALTO",
    "metricas": {{
        "engajamento": 0.0-1.0,
        "eficacia": 0.0-1.0,
        "tom": "AGRESSIVO|NEUTRO|EMPATICO|CONSULTIVO",
        "objection_handling": 0.0-1.0,
        "value_proposition": 0.0-1.0,
        "rapport_building": 0.0-1.0
    }},
    "sugestoes": [
        "sugestão 1",
        "sugestão 2", 
        "sugestão 3"
    ],
    "tendencias": {{
        "evolucao": "POSITIVA|NEGATIVA|ESTAVEL",
        "momentum": 0.0-1.0
    }}
}}

Baseie a análise em:
- Engajamento do cliente
- Qualidade das perguntas do vendedor
- Handling de objeções
- Demonstração de valor
- Building de rapport
- Progresso na conversa

Seja realista e objetivo.
"""
        
        if not use_mock_forcado:
            resultado = await chamar_ia_otimizado_func(prompt, use_cache=False)
            
            try:
                data = json.loads(resultado)
                return {
                    "probabilidade": data.get("probabilidade", 50),
                    "nivel": data.get("nivel", "MEDIO"),
                    "metricas": data.get("metricas", {}),
                    "sugestoes": data.get("sugestoes", []),
                    "tendencias": data.get("tendencias", {})
                }
            except:
                # Fallback se JSON falhar
                return {
                    "probabilidade": 50,
                    "nivel": "MEDIO",
                    "metricas": {
                        "engajamento": 0.5,
                        "eficacia": 0.5,
                        "tom": "NEUTRO"
                    },
                    "sugestoes": ["Continue a conversa para análise mais precisa"],
                    "tendencias": {"evolucao": "estavel"}
                }
        else:
            # Mock inteligente baseado no perfil e histórico
            base_prob = {
                "frio": random.randint(15, 40),
                "morno": random.randint(35, 65),
                "quente": random.randint(60, 85)
            }
            
            prob = base_prob.get(perfil_cliente, 50)
            variacao = random.randint(-10, 10)
            prob_final = max(5, min(95, prob + variacao))
            
            return {
                "probabilidade": prob_final,
                "nivel": "ALTO" if prob_final > 65 else "MEDIO" if prob_final > 35 else "BAIXO",
                "metricas": {
                    "engajamento": prob_final / 100,
                    "eficacia": random.uniform(0.3, 0.8),
                    "tom": random.choice(["EMPATICO", "NEUTRO", "CONSULTIVO"]),
                    "objection_handling": random.uniform(0.2, 0.9),
                    "value_proposition": random.uniform(0.3, 0.9)
                },
                "sugestoes": [
                    "Mantenha o tom consultivo",
                    "Foque nos benefícios específicos",
                    "Ouça ativamente as necessidades"
                ],
                "tendencias": {
                    "evolucao": "positiva" if variacao > 0 else "negativa" if variacao < -5 else "estavel",
                    "momentum": random.uniform(0.1, 0.9)
                }
            }
            
    except Exception as e:
        logger.error(f"Erro análise preditiva: {e}")
        return {
            "probabilidade": 50,
            "nivel": "MEDIO",
            "metricas": {},
            "sugestoes": ["Sistema de análise temporariamente indisponível"],
            "tendencias": {}
        }

def parsear_respostas_ia(texto_resposta):
    """Parseia o texto da IA para extrair as 5 respostas formatadas.

    Tornar o parser tolerante a variações de formato (HTML, bullets, listas numeradas,
    headings inline e textos em uma única linha). Se não identificar seções, usa
    fallback por sentenças para preencher as respostas.
    """
    respostas = {
        "empatica": "",
        "valor": "",
        "prova_social": "",
        "urgencia": "",
        "autoridade": ""
    }

    if not texto_resposta:
        return respostas

    try:
        import re
        import unicodedata

        def normalize(s: str) -> str:
            # Remove acentos e coloca em maiúsculas para comparação robusta
            return unicodedata.normalize('NFKD', s).encode('ASCII', 'ignore').decode().upper()

        # Converte para string e normaliza HTML para preservar quebras de item
        texto = str(texto_resposta)
        # Substitui fechamentos de <li> por nova linha para separar itens
        texto = re.sub(r'</li\s*>', '\n', texto, flags=re.IGNORECASE)
        texto = re.sub(r'<br\s*/?>', '\n', texto, flags=re.IGNORECASE)
        # Remove qualquer outra tag HTML
        texto = re.sub(r'<[^>]+>', '', texto)

        # Normaliza bullets comuns para linhas separadas (mantém marcador • para identificação)
        texto = re.sub(r'[\u2022\*\-]\s+', '\n• ', texto)  # • * - bullets

        # Padrão para detectar headings (com ou sem número, com ou sem colchetes)
        heading_re = re.compile(r'(?:\d+\s*[\.|\)]\s*)?\[?\s*(EMPATIA|VALOR|PROVA[_\s]?SOCIAL|PROVA SOCIAL|URGENCIA|URGÊNCIA|AUTORIDADE)\s*\]?\s*:?', flags=re.IGNORECASE)

        # Primeiro, tenta detectar listas numeradas inline do tipo '1. ... 2. ...'
        inline_numbered = re.split(r'(?<=\.)\s*(?=\d+\s*[\.|\)])', texto)
        if len(inline_numbered) >= 5 and all(part.strip() for part in inline_numbered[:5]):
            keys = ['empatica', 'valor', 'prova_social', 'urgencia', 'autoridade']
            for i, key in enumerate(keys):
                if i < len(inline_numbered):
                    respostas[key] = re.sub(r'^\d+\s*[\.|\)]\s*', '', inline_numbered[i]).strip()
            return respostas

        current_key = None
        linhas = texto.split('\n')

        for linha in linhas:
            raw = linha.strip()
            if not raw:
                continue

            m = heading_re.search(raw)
            if m:
                # Texto antes do heading (se existir) pertence à chave atual
                if m.start() > 0:
                    prefix = raw[:m.start()].strip()
                    if prefix:
                        if current_key:
                            respostas[current_key] = (respostas[current_key] + ' ' + prefix).strip() if respostas[current_key] else prefix
                        else:
                            low = prefix.lower()
                            if any(k in low for k in ['cliente', 'clientes', 'exemplo', 'caso', 'depoimento', 'testemunho']):
                                guess = 'prova_social'
                            elif any(k in low for k in ['valor', 'benefício', 'beneficios', 'roi', 'vantagem']):
                                guess = 'valor'
                            elif any(k in low for k in ['urg', 'agora', 'tempo', 'portunidade']):
                                guess = 'urgencia'
                            else:
                                guess = 'empatica'
                            respostas[guess] = (respostas[guess] + ' ' + prefix).strip() if respostas[guess] else prefix

                heading = m.group(1)
                heading_norm = normalize(heading)

                if 'EMPATIA' in heading_norm:
                    current_key = 'empatica'
                elif 'VALOR' in heading_norm and 'PROVA' not in heading_norm:
                    current_key = 'valor'
                elif 'PROVA' in heading_norm:
                    current_key = 'prova_social'
                elif 'URGEN' in heading_norm:
                    current_key = 'urgencia'
                elif 'AUTOR' in heading_norm:
                    current_key = 'autoridade'

                rest = raw[m.end():].strip()
                if rest:
                    respostas[current_key] = (respostas[current_key] + ' ' + rest).strip() if respostas[current_key] else rest

            else:
                # Linha sem heading: verifica se é um número/bullet no começo
                num_match = re.match(r'^(?:\d+\s*[\.|\)]\s*)(.*)', raw)
                if num_match:
                    remaining_keys = [k for k, v in respostas.items() if not v]
                    if remaining_keys:
                        respostas[remaining_keys[0]] = num_match.group(1).strip()
                        current_key = remaining_keys[0]
                    else:
                        respostas['empatica'] += ' ' + raw
                elif raw and raw[0] in ('•', '-', '*'):
                    remaining_keys = [k for k, v in respostas.items() if not v]
                    if remaining_keys:
                        respostas[remaining_keys[0]] = raw[1:].strip()
                        current_key = remaining_keys[0]
                    else:
                        respostas['empatica'] += ' ' + raw
                else:
                    if current_key:
                        respostas[current_key] = (respostas[current_key] + ' ' + raw).strip() if respostas[current_key] else raw
                    else:
                        respostas['empatica'] = (respostas['empatica'] + ' ' + raw).strip() if respostas['empatica'] else raw

        # Limpeza final: remove aspas, asteriscos e espaços extras
        for key in respostas:
            cleaned_text = respostas[key].strip()
            respostas[key] = cleaned_text.strip('"').strip("'").replace('*', '').strip()

        # Se todas as respostas estiverem vazias, tenta fallback por sentenças
        if all(not v for v in respostas.values()):
            sentences = re.split(r'(?<=[\.\?\!])\s+', texto)
            sentences = [s.strip() for s in sentences if s.strip()]
            if len(sentences) >= 5:
                respostas['empatica'] = sentences[0]
                respostas['valor'] = sentences[1]
                respostas['prova_social'] = sentences[2]
                respostas['urgencia'] = sentences[3]
                respostas['autoridade'] = sentences[4]
            else:
                # Erro: não detectou headings nem sentenças suficientes
                raise ValueError('Parser não detectou headings nem sentenças suficientes no texto da IA')
    except Exception as e:
        logger.error(f"Erro ao parsear respostas: {e}")
        # Fallback: divide em blocos separados por duas quebras de linha
        import re
        partes = [p.strip() for p in re.split(r'\n\s*\n', str(texto_resposta)) if p.strip()]
        if len(partes) >= 5:
            respostas = {
                "empatica": partes[0],
                "valor": partes[1],
                "prova_social": partes[2],
                "urgencia": partes[3],
                "autoridade": partes[4]
            }
        else:
            # Tenta dividir por números (1. 2. 3.)
            try:
                parts_num = re.split(r'\d+\s*[\.|\)]\s*', str(texto_resposta))
                parts_num = [p.strip() for p in parts_num if p.strip()]
                if len(parts_num) >= 5:
                    respostas = {
                        "empatica": parts_num[0],
                        "valor": parts_num[1],
                        "prova_social": parts_num[2],
                        "urgencia": parts_num[3],
                        "autoridade": parts_num[4]
                    }
            except Exception:
                pass

    return respostas


def parsear_script_otimizado(texto_resposta: str) -> Dict[str, Any]:
    """Parseia a resposta da IA para extrair o script otimizado e as melhorias."""
    try:
        script_otimizado = ""
        melhorias = []

        # Extrai o script otimizado
        script_match = re.search(r"\*\*?SCRIPT OTIMIZADO:\*\*?(.*?)(\*\*?MELHORIAS:\*\*?|$)", texto_resposta, re.DOTALL | re.IGNORECASE)
        if script_match:
            script_otimizado = script_match.group(1).strip()
        else: # Fallback se "MELHORIAS" não for encontrado
            script_match_fallback = re.search(r"\*\*?SCRIPT OTIMIZADO:\*\*?(.*)", texto_resposta, re.DOTALL | re.IGNORECASE)
            if script_match_fallback:
                script_otimizado = script_match_fallback.group(1).strip()

        # Extrai as melhorias
        melhorias_match = re.search(r"\*\*?MELHORIAS:\*\*?(.*)", texto_resposta, re.DOTALL | re.IGNORECASE)
        if melhorias_match:
            melhorias_texto = melhorias_match.group(1).strip()
            # Limpa asteriscos, números de lista e outros caracteres de formatação
            melhorias = [re.sub(r'^\*+\s*|\d+\.\s*', '', m).replace('**', '').strip() for m in melhorias_texto.split('\n') if m.strip()]

        return {"script_otimizado": script_otimizado or texto_resposta, "melhorias": melhorias or ["Análise não disponível"]}
    except Exception as e:
        logger.error(f"Erro ao parsear script otimizado: {e}")
        return {"script_otimizado": texto_resposta, "melhorias": []}

def parsear_variacoes_script(texto_resposta: str, numero_variacoes: int) -> List[Dict[str, str]]:
    """Parseia a resposta da IA para extrair as variações de script."""
    try:
        variacoes = []
        # Divide o texto em blocos de "VARIAÇÃO X:"
        blocos = re.split(r'VARIAÇÃO \d+:', texto_resposta, flags=re.IGNORECASE)[1:]

        for i, bloco in enumerate(blocos):
            if len(variacoes) >= numero_variacoes:
                break
            
            nome_match = re.search(r'NOME:(.*?)\n', bloco, re.DOTALL | re.IGNORECASE)
            script_match = re.search(r'SCRIPT:(.*)', bloco, re.DOTALL | re.IGNORECASE)

            nome = nome_match.group(1).strip() if nome_match else f"Variação {i+1}"
            script = script_match.group(1).strip() if script_match else bloco.strip()

            if script:
                variacoes.append({"nome": nome, "script": script})
        return variacoes
    except Exception as e:
        logger.error(f"Erro ao parsear variações de script: {e}")
        return [{"nome": "Erro de Parsing", "script": texto_resposta}]

# ========== SISTEMA MOCK ==========
def gerar_resposta_mock_simulador(produto_descricao, perfil, mensagem):
    respostas_contextuais = {
        'frio': [
            f"Hum, {produto_descricao}... Pode me explicar melhor?",
            f"Não estou muito convencido sobre {produto_descricao.lower()}.",
            f"Qual o valor disso? {produto_descricao} parece caro.",
            f"Já vi opções similares. O que tem de diferente?",
            f"Vou pensar sobre {produto_descricao.lower()}."
        ],
        'morno': [
            f"Interessante esse {produto_descricao.lower()}! Como funciona?",
            f"Gostei! Tem algum caso de sucesso com {produto_descricao.lower()}?",
            f"Parece bom! Qual o investimento para {produto_descricao.lower()}?",
            f"{produto_descricao} resolve mesmo [problema relacionado]?",
            f"Conte-me mais sobre os benefícios."
        ],
        'quente': [
            f"Perfeito! Preciso mesmo de {produto_descricao.lower()}!",
            f"Finalmente uma solução decente! Como começar?",
            f"Adorei {produto_descricao}! Tem garantia?",
            f"Quanto tempo para ver resultados com {produto_descricao.lower()}?",
            f"Pode me enviar mais detalhes? Estou interessado!"
        ]
    }
    
    base_respostas = respostas_contextuais.get(perfil, respostas_contextuais['morno'])
    resposta = random.choice(base_respostas)
    
    time.sleep(0.3 + random.random() * 0.7)
    
    return resposta

def gerar_feedback_mock(produto_descricao, mensagem):
    """Gera feedback contextualizado realista"""
    
    analises = [
        f"✅ Bom tom para {produto_descricao.lower()}. Continue focando nos benefícios.",
        f"📈 Para {produto_descricao.lower()}, tente ser mais específico sobre resultados.",
        f"🎯 Abordagem adequada. Para {produto_descricao.lower()}, adicione prova social.",
        f"💡 Mensagem ok. Para vender {produto_descricao.lower()}, mostre mais empatia.",
        f"🚀 Excelente! Para {produto_descricao.lower()}, mantenha este tom consultivo."
    ]
    
    return random.choice(analises)

def quebrar_objecao_mock(produto_descricao, objecao):
    """Gera respostas inteligentes para objeções - ATUALIZADO com 5 abordagens"""
    
    templates = {
        "preço": {
            "empatica": f"Entendo perfeitamente sua preocupação com o investimento em {produto_descricao.lower()}. Muitos clientes sentem o mesmo no início.",
            "valor": f"Pense no retorno: {produto_descricao} economiza tempo/dinheiro que compensa o investimento rapidamente.",
            "prova_social": f"Clientes como [Cliente A] viram ROI de 3x em 2 meses com {produto_descricao.lower()}.",
            "urgencia": f"Esta condição especial de preço é válida apenas até sexta-feira para os primeiros 10 clientes.",
            "autoridade": f"Estudos mostram que soluções como {produto_descricao} aumentam eficiência em 47% em média."
        },
        "pensar": {
            "empatica": f"Compreendo querer analisar com calma {produto_descricao.lower()}. É uma decisão importante.",
            "valor": f"Enquanto pensa, considere o custo de adiar os benefícios que {produto_descricao} traz.",
            "prova_social": f"A Maria quase perdeu a oportunidade de {produto_descricao.lower()} - hoje diz que mudou seu negócio.",
            "urgencia": f"Posso garantir estas condições só até amanhã - depois o programa muda.",
            "autoridade": f"90% dos que adiam {produto_descricao.lower()} depois se arrependem da demora."
        },
        "fornecedor": {
            "empatica": f"Que bom que já tem uma solução! Muitos clientes vêm de outros fornecedores para {produto_descricao.lower()}.",
            "valor": f"{produto_descricao} oferece [diferencial exclusivo] que outros não têm, gerando [benefício extra].",
            "prova_social": f"O João migrou do [concorrente] para {produto_descricao.lower()} e viu crescimento de 35%.",
            "urgencia": f"Esta migração assistida é gratuita só este mês para novos clientes.",
            "autoridade": f"Somos certificados em [certificação] com 98% de satisfação em pesquisas independentes."
        }
    }
    
    objecao_lower = objecao.lower()
    for key in templates:
        if key in objecao_lower:
            return templates[key]
    
    # Fallback genérico
    return {
        "empatica": f"Entendo completamente sua preocupação sobre {produto_descricao.lower()}.",
        "valor": f"{produto_descricao} resolve exatamente isso com benefícios mensuráveis.",
        "prova_social": f"Muitos clientes tinham essa mesma dúvida sobre {produto_descricao.lower()} e hoje são os maiores fãs.",
        "urgencia": f"Esta oportunidade de testar {produto_descricao.lower()} com condições especiais é limitada.",
        "autoridade": f"Baseado em dados de 2024, {produto_descricao} tem 94% de eficácia comprovada."
    }

def detector_analisar_mock(produto_descricao, mensagem):
    """Mock inteligente e detalhado para o detector de vendedor chato"""
    mensagem_lower = mensagem.lower()
    produto_lower = produto_descricao.lower()
    
    if any(palavra in mensagem_lower for palavra in ['compre agora', 'oferta', 'última', 'urgente', 'não perca', 'vagas limitadas', '!!!']):
        classificacao = "AGRESSIVO"
        motivo = f"A mensagem sobre {produto_descricao} utiliza termos de urgência excessiva ('compre agora', 'oferta') que pressionam o cliente. Essa abordagem pode afastar potenciais compradores por criar desconfiança."
        sugestao = f"Substitua a abordagem agressiva por uma consultiva. Em vez de pressionar, foque em como {produto_descricao} resolve problemas específicos do cliente."
        pontuacao_empatia = 15
        nivel_pressao = "ALTO"
        indicadores_problema = [
            "Termos de urgência excessiva",
            "Pressão por tempo limitado", 
            "Linguagem alarmista com múltiplas exclamações",
            "Foco na transação e não na solução"
        ]
        exemplo_corrigido = f"Olá! Notei que você pode ter interesse em {produto_descricao}. Ele ajuda empresas como a sua a [benefício específico]. Posso compartilhar alguns casos de sucesso?"
    
    elif len(mensagem) < 40 or any(palavra in mensagem_lower for palavra in ['compre', 'garantido', 'melhor', 'top', 'incrível']):
        classificacao = "POUCA EMPATIA"
        motivo = f"A mensagem sobre {produto_descricao} é muito genérica e focada no produto. Não demonstra compreensão das necessidades específicas do cliente ou pesquisa prévia sobre seu contexto."
        sugestao = f"Personalize a mensagem para {produto_descricao}. Faça perguntas sobre os desafios do cliente e mostre como seu produto se alinha às necessidades específicas dele."
        pontuacao_empatia = 25
        nivel_pressao = "MÉDIO"
        indicadores_problema = [
            "Mensagem muito curta e genérica",
            "Falta de personalização",
            "Foco excessivo em adjetivos superlativos", 
            "Pouca demonstração de interesse genuíno"
        ]
        exemplo_corrigido = f"Olá! Como você está? Vi que você trabalha com [área do cliente]. {produto_descricao} pode te ajudar especificamente com [benefício relacionado]. Conte-me mais sobre seus desafios atuais?"
    
    elif any(palavra in mensagem_lower for palavra in ['como posso ajudar', 'qual seu desafio', 'conte-me mais', 'o que você busca']):
        classificacao = "CONSULTIVO"
        motivo = f"Excelente abordagem para {produto_descricao}! A mensagem demonstra genuíno interesse em entender as necessidades do cliente antes de oferecer soluções."
        sugestao = f"Mantenha esta abordagem consultiva para {produto_descricao}. Continue fazendo perguntas qualificadas e ouvindo ativamente as respostas."
        pontuacao_empatia = 85
        nivel_pressao = "BAIXO"
        indicadores_problema = [
            "Abordagem centrada no cliente",
            "Perguntas abertas para entender necessidades",
            "Tom respeitoso e não intrusivo",
            "Foco em construir relacionamento"
        ]
        exemplo_corrigido = f"Sua mensagem já está excelente! Continue com esta abordagem consultiva para {produto_descricao}. Mantenha o foco em entender profundamente as necessidades antes de sugerir soluções."
    
    elif any(palavra in mensagem_lower for palavra in ['entendo', 'compreendo', 'imagino', 'sinto muito']):
        classificacao = "EMPÁTICO"
        motivo = f"Abordagem muito empática para {produto_descricao}! A mensagem demonstra compreensão emocional e valida os sentimentos do cliente, criando uma conexão genuína."
        sugestao = f"Continue desenvolvendo esta conexão empática com clientes interessados em {produto_descricao}. A validação emocional é poderosa para construir confiança."
        pontuacao_empatia = 90
        nivel_pressao = "BAIXO"
        indicadores_problema = [
            "Validação emocional do cliente",
            "Linguagem de compreensão e apoio",
            "Conexão genuína antes da venda",
            "Paciência e escuta ativa"
        ]
        exemplo_corrigido = f"Sua abordagem empática está perfeita para {produto_descricao}! Continue validando as emoções do cliente enquanto guia a conversa para soluções."
    
    else:
        classificacao = "EQUILIBRADO"
        motivo = f"A mensagem sobre {produto_descricao} mantém um bom equilíbrio entre persuasão e respeito. Demonstra interesse sem ser intrusivo e apresenta o produto de forma clara."
        sugestao = f"Mantenha o bom trabalho com {produto_descricao}! Para melhorar ainda mais, tente adicionar uma pergunta específica sobre os desafios do cliente."
        pontuacao_empatia = 65
        nivel_pressao = "BAIXO"
        indicadores_problema = [
            "Tom profissional e respeitoso",
            "Abordagem não intrusiva",
            "Clareza na comunicação",
            "Bom equilíbrio entre informação e persuasão"
        ]
        exemplo_corrigido = f"Sua mensagem sobre {produto_descricao} já está boa! Para torná-la ainda mais eficaz, você poderia adicionar: 'Qual é o maior desafio que você enfrenta atualmente com [área relacionada]?'"
    
    return {
        "classificacao": classificacao,
        "motivo": motivo,
        "sugestao": sugestao,
        "pontuacao_empatia": pontuacao_empatia,
        "nivel_pressao": nivel_pressao,
        "indicadores_problema": indicadores_problema,
        "exemplo_corrigido": exemplo_corrigido
    }

def gerar_mock_contexto(produto_descricao, conversa, historico_contexto):
    """Gera mock para ContextGuardian"""
    # Análise simples baseada no conteúdo
    conversa_texto = str(conversa).lower()
    
    # Detecta possíveis rupturas
    ruptura = False
    sugestao = f"Para {produto_descricao}, foque nos benefícios principais."
    topicos = ["Preço", "Garantia", "Suporte"]
    
    if "preço" in conversa_texto and "valor" not in conversa_texto:
        ruptura = True
        sugestao = f"O cliente mencionou preço. Conecte o valor de {produto_descricao} com os benefícios mencionados."
    
    return {
        "alinhamento_contextual": 0.7 if not ruptura else 0.3,
        "ruptura_detectada": ruptura,
        "sugestao_transicao": sugestao,
        "topicos_nao_abordados": topicos[:1],
        "nivel_urgencia": "ALTO" if ruptura else "MEDIO"
    }

def gerar_mock_predicao_objecoes(produto_descricao, conversa, nicho, perfil_cliente):
    """Gera mock para ObjectionPredictor"""
    conversa_texto = str(conversa).lower()
    
    objecoes = []
    sinais = []
    
    # Detecta padrões comuns
    if any(palavra in conversa_texto for palavra in ['caro', 'preço', 'custo', 'valor']):
        objecoes.append({
            "tipo": "PREÇO",
            "probabilidade": 0.8,
            "sinais_detectados": ["mencionou custo", "comparou com outros"],
            "abordagem_preventiva": f"Antecipe o valor de {produto_descricao} focando no ROI."
        })
        sinais.append("preço")
    
    if any(palavra in conversa_texto for palavra in ['pensar', 'depois', 'consultar']):
        objecoes.append({
            "tipo": "TIMING",
            "probabilidade": 0.6,
            "sinais_detectados": ["demonstrou indecisão", "mencionou consultar terceiros"],
            "abordagem_preventiva": "Crie urgência com benefícios de ação imediata."
        })
        sinais.append("timing")
    
    return {
        "objecoes_provaveis": objecoes,
        "sinais_detectados": sinais,
        "abordagem_preventiva": f"Para {produto_descricao}, foque em benefícios tangíveis e casos de sucesso.",
        "nivel_risco": "ALTO" if objecoes else "BAIXO"
    }

def gerar_mock_mudanca_emocional(produto_descricao, conversa, metricas_base):
    """Gera mock inteligente para EmotionShift"""
    # Analisa a conversa para detectar mudanças reais
    conversa_texto = str(conversa).lower()
    
    # Detecta padrões emocionais
    mudanca = False
    ponto_virada = ""
    direcao = ""
    
    # Padrões de mudança negativa
    if any(palavra in conversa_texto for palavra in ['orçamento', 'caro', 'custo', 'não sei', 'talvez', 'próximo mês']):
        mudanca = True
        ponto_virada = "Menção a restrições orçamentárias"
        direcao = "POSITIVO_PARA_NEGATIVO"
        emocao_antes = "ENTUSIASTICO" 
        emocao_depois = "CÉTICO"
        fator_critico = "Preocupação financeira"
        sugestao = f"Para {produto_descricao}, apresente opções de pagamento flexíveis e destaque o ROI."
        estrategia = "Focar no retorno financeiro e casos de sucesso com métricas claras"
        alerta = "ALTO"
        probabilidade = 0.6
    
    # Padrões de mudança positiva  
    elif any(palavra in conversa_texto for palavra in ['interessante', 'gostei', 'ótimo', 'perfeito']):
        mudanca = True
        ponto_virada = "Expressão de interesse genuíno"
        direcao = "NEUTRO_PARA_POSITIVO"
        emocao_antes = "CURIOSO"
        emocao_depois = "ENTUSIASTICO"
        fator_critico = "Reconhecimento de valor"
        sugestao = f"Capitalize o interesse em {produto_descricao} com proposta clara de next steps."
        estrategia = "Aproveitar o momentum positivo para fechamento"
        alerta = "BAIXO"
        probabilidade = 0.8
    
    else:
        # Sem mudança detectada
        mudanca = False
        ponto_virada = ""
        direcao = ""
        emocao_antes = "NEUTRO"
        emocao_depois = "NEUTRO"
        fator_critico = "Conversa em andamento normal"
        sugestao = f"Continue explorando necessidades para {produto_descricao}."
        estrategia = "Manter abordagem consultiva"
        alerta = "BAIXO"
        probabilidade = 0.5
    
    return {
        "mudanca_detectada": mudanca,
        "ponto_virada": ponto_virada,
        "direcao_mudanca": direcao,
        "emocao_antes": emocao_antes,
        "emocao_depois": emocao_depois,
        "fator_critico": fator_critico,
        "sugestao_ajuste_imediato": sugestao,
        "estrategia_recuperacao": estrategia,
        "alerta_risco": alerta,
        "probabilidade_recuperacao": probabilidade
    }

def should_use_personalized_context(request: Request) -> bool:
    """Determina se deve usar contexto personalizado baseado no tipo de autenticação"""
    return hasattr(request.state, 'user') and request.state.user is not None

def get_or_create_session_id(session_id: Optional[str]) -> str:
    """
    Retorna o session_id fornecido ou cria um novo se não for fornecido.
    Isso automatiza o início de uma nova sessão de conversa para clientes da API.
    """
    if session_id and isinstance(session_id, str) and len(session_id.strip()) > 0:
        return session_id
    return str(uuid.uuid4())

async def executar_copiloto_cognitivo(
    request: Request,
    produto_descricao: str,
    conversa_atual: List[Dict[str, str]],
    session_id: Optional[str] = None,
    user_id: Optional[int] = None ) -> Dict[str, Any]:
    """
    Executa de forma consolidada os três módulos do Co-piloto Cognitivo.
    Agora com memória de sessão: se um session_id for fornecido, ele busca
    o histórico completo da sessão para uma análise mais profunda.
    """
    from app.routes.analise_routes import analisar_contexto_alinhamento, predizer_objecoes, analisar_mudanca_emocional
    from app.models import ContextoAlinhamentoRequest, PredicaoObjecoesRequest, MudancaEmocionalRequest
    from app.database import get_user_simulations

    conversa_para_analise = conversa_atual

    # Lógica de Memória de Sessão
    if session_id and user_id is not None:
        try:
            # Busca todas as interações da sessão no banco
            todas_interacoes_db = get_user_simulations(user_id, limit=100) # Limite alto para pegar a sessão inteira
            interacoes_sessao = [sim for sim in todas_interacoes_db if str(sim.get("session_id")) == session_id]
            
            if interacoes_sessao:
                # Reconstrói a conversa completa a partir do histórico salvo
                conversa_reconstruida = []
                interacoes_sessao.sort(key=lambda x: x['data_criacao']) # Ordena pela data
                for interacao in interacoes_sessao:
                    if 'mensagens' in interacao.get('conversa', {}):
                        conversa_reconstruida.extend(interacao['conversa']['mensagens'])
                
                conversa_para_analise = conversa_reconstruida + conversa_atual
        except Exception as e:
            logger.warning(f"Falha ao reconstruir histórico da sessão para o Co-piloto: {e}")

    copiloto_results = {}

    try:
        # 1. ContextGuardian
        contexto_req = ContextoAlinhamentoRequest(produto_descricao=produto_descricao, conversa=conversa_para_analise)
        copiloto_results["contexto_alinhamento"] = await analisar_contexto_alinhamento(contexto_req, request)

        # 2. ObjectionPredictor
        # (Simulando nicho e perfil para a chamada, idealmente viriam da sessão)
        objecao_req = PredicaoObjecoesRequest(
            produto_descricao=produto_descricao,
            conversa=conversa_para_analise,
            nicho="Tecnologia",
            perfil_cliente="Morno"
        )
        copiloto_results["predicao_objecoes"] = await predizer_objecoes(objecao_req, request)

        # 3. EmotionShift
        emocional_req = MudancaEmocionalRequest(produto_descricao=produto_descricao, conversa=conversa_para_analise)
        copiloto_results["mudanca_emocional"] = await analisar_mudanca_emocional(emocional_req, request)

        # --- Lógica do Playbook Dinâmico ---
        # Após todas as análises, o sistema recomenda um playbook estratégico.
        try:
            from app.services.playbook_service import playbook_service
            
            playbook_recomendado = playbook_service.recomendar_playbook(copiloto_results)
            copiloto_results["playbook_recomendado"] = playbook_recomendado

        except Exception as e:
            logger.warning(f"Falha ao gerar recomendação de Playbook: {e}")
            copiloto_results["playbook_recomendado"] = None

    except Exception as e:
        logger.error(f"Erro ao executar o Co-piloto Cognitivo: {e}")

    return copiloto_results
