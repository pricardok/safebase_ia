"""
Registry central de todos os prompts do sistema
"""

from app.prompts.templates import simulador, conexao, scripts, objecoes, detector, contexto, emocional, whatsapp

# Registry com todos os prompts disponíveis
PROMPT_REGISTRY = {
    # Simulador
    "simulador_cliente": simulador.SIMULADOR_CLIENTE,
    "simulador_feedback": simulador.SIMULADOR_FEEDBACK,
    "simulador_feedback_detalhado": simulador.SIMULADOR_FEEDBACK_DETALHADO,
    
    # Conexão
    "conexao_gerar_perguntas": conexao.CONEXAO_GERAR_PERGUNTAS,
    "conexao_analisar_resposta": conexao.CONEXAO_ANALISAR_RESPOSTA,
    "conexao_simular_dialogo": conexao.CONEXAO_SIMULAR_DIALOGO,
    "conexao_roteiro_abordagem": conexao.CONEXAO_ROTEIRO_ABORDAGEM,
    
    # Scripts
    "scripts_otimizar": scripts.SCRIPTS_OTIMIZAR,
    "scripts_analisar_eficacia": scripts.SCRIPTS_ANALISAR_EFICACIA,
    "scripts_gerar_variacoes": scripts.SCRIPTS_GERAR_VARIACOES,
    
    # Objeções
    "objecoes_quebrar": objecoes.OBJECOES_QUEBRAR,

    # Vededor chato (Detector)
    "detector_analisar": detector.DETECTOR_ANALISAR,

    # Contexto
    "contexto_alinhamento": contexto.CONTEXTO_ALINHAMENTO,

    # Predicações
    "predicao_objecoes": objecoes.PREDICAO_OBJECOES, 

    # Emocional
    "mudanca_emocional": emocional.MUDANCA_EMOCIONAL,
    # WhatsApp agent
    "whatsapp_conversar": whatsapp.WHATSAPP_CONVERSAR,
    "whatsapp_responder_pergunta": whatsapp.WHATSAPP_RESPONDER_PERGUNTA,
    "whatsapp_consultor_vendas": whatsapp.WHATSAPP_CONSULTOR_VENDAS,

}

# Metadados dos prompts 
PROMPT_METADATA = {
    "simulador_cliente": {
        "modulo": "simulador",
        "versao": "1.0",
        "descricao": "Gera resposta de cliente realista",
        "tags": ["cliente", "resposta", "realista"],
        "parametros": ["produto_descricao", "perfil_cliente", "mensagem_usuario", "historico"]
    },
    "simulador_feedback": {
        "modulo": "simulador", 
        "versao": "1.0",
        "descricao": "Feedback conciso para mensagem de venda",
        "tags": ["feedback", "analise", "vendas"],
        "parametros": ["produto_descricao", "mensagem_usuario"]
    },
    "simulador_feedback_detalhado": {
        "modulo": "simulador",
        "versao": "1.0",
        "descricao": "Feedback detalhado para mensagem de venda",
        "tags": ["feedback", "analise", "vendas", "detalhado"],
        "parametros": ["produto_descricao", "mensagem_usuario"]
    },
    "conexao_gerar_perguntas": {
        "modulo": "conexao",
        "versao": "1.0",
        "descricao": "Gera perguntas personalizadas para descobrir dores do cliente",
        "tags": ["perguntas", "conexao", "dores"],
        "parametros": ["produto_descricao", "contexto_cliente"]
    },
    "conexao_analisar_resposta": {
        "modulo": "conexao",
        "versao": "1.0",
        "descricao": "Analisa a resposta do vendedor a uma pergunta de conexão",
        "tags": ["analise", "resposta", "conexao"],
        "parametros": ["produto_descricao", "pergunta", "resposta_vendedor"]
    },
    "conexao_simular_dialogo": {
        "modulo": "conexao",
        "versao": "1.0",
        "descricao": "Simula um diálogo de vendas consultivo",
        "tags": ["dialogo", "simulacao", "conexao"],
        "parametros": ["produto_descricao", "cenario", "abordagem"]
    },
    "conexao_roteiro_abordagem": {
        "modulo": "conexao",
        "versao": "1.0",
        "descricao": "Gera um roteiro de abordagem humano e estratégico (variações, perguntas e fechamento)",
        "tags": ["roteiro", "abordagem", "conexao"],
        "parametros": ["produto_descricao", "tipo_pessoa", "nome_cliente"]
    },
    "scripts_otimizar": {
        "modulo": "scripts",
        "versao": "1.0",
        "descricao": "Otimiza um script de vendas para um canal e objetivo específicos",
        "tags": ["scripts", "otimizacao", "canal"],
        "parametros": ["produto_descricao", "script_original", "canal", "objetivo"]
    },
    "scripts_analisar_eficacia": {
        "modulo": "scripts",
        "versao": "1.0",
        "descricao": "Analisa a eficácia de um script de vendas",
        "tags": ["scripts", "analise", "eficacia"],
        "parametros": ["produto_descricao", "script", "canal"]
    },
    "scripts_gerar_variacoes": {
        "modulo": "scripts",
        "versao": "1.0",
        "descricao": "Gera variações de um script de vendas",
        "tags": ["scripts", "variacoes", "geracao"],
        "parametros": ["produto_descricao", "script_base", "canal", "numero_variacoes"]
    },
    "detector_analisar": {
        "modulo": "detector",
        "versao": "1.1",  
        "descricao": "Análise estruturada e detalhada de mensagens de venda",
        "tags": ["detector", "analise", "tom", "empatia", "vendas"],
        "parametros": ["produto_descricao", "mensagem"]
    },  
    "contexto_alinhamento": {
        "modulo": "analise",
        "versao": "1.0",
        "descricao": "Analisa alinhamento contextual e detecta rupturas na conversa",
        "tags": ["contexto", "alinhamento", "ruptura"],
        "parametros": ["produto_descricao", "conversa", "historico_contexto"]
    },  
    "predicao_objecoes": {
        "modulo": "objecoes",
        "versao": "1.0",
        "descricao": "Prevê objeções com base em padrões comportamentais",
        "tags": ["predicao", "objecoes", "prevencao"],
        "parametros": ["produto_descricao", "conversa", "nicho", "perfil_cliente"]
    },
    "mudanca_emocional": {
        "modulo": "analise",
        "versao": "1.0",
        "descricao": "Detecta mudanças emocionais e sugere ajustes",
        "tags": ["emocional", "mudanca", "ajuste"],
        "parametros": ["produto_descricao", "conversa", "metricas_base"]
    },          
    "objecoes_quebrar": {
        "modulo": "objecoes",
        "versao": "1.0",
        "descricao": "Gera respostas para objeções do cliente",
        "tags": ["objecoes", "respostas", "quebra"],
        "parametros": ["produto_descricao", "objecao"]
    },
    "whatsapp_conversar": {
        "modulo": "whatsapp",
        "versao": "1.0",
        "descricao": "Agente de conversação específico para WhatsApp combinando habilidades de simulador, conexão, detector e quebra de objeções",
        "tags": ["whatsapp", "conversacao", "agente"],
        "parametros": ["produto_descricao", "mensagem_usuario", "historico"]
    },
    "whatsapp_responder_pergunta": {
        "modulo": "whatsapp",
        "versao": "1.0",
        "descricao": "Gera respostas curtas (curta, média, persuasiva) para perguntas de clientes",
        "tags": ["whatsapp", "resposta", "pergunta"],
        "parametros": ["produto_descricao", "pergunta", "historico"]
    },
    "whatsapp_consultor_vendas": {
        "modulo": "whatsapp",
        "versao": "1.0",
        "descricao": "Consultor de vendas que fornece passos acionáveis e recusa tópicos fora do escopo",
        "tags": ["consultor", "vendas", "whatsapp"],
        "parametros": ["topico", "contexto", "historico"]
    }
}

# ========== FUNÇÕES PARA ENDPOINTS ==========

def get_all_prompts():
    """Retorna todos os prompts do registry - PARA ENDPOINTS"""
    return {
        name: {
            "modulo": metadata.get("modulo", "geral"),
            "descricao": metadata.get("descricao", ""),
            "versao": metadata.get("versao", "1.0"),
            "tags": metadata.get("tags", []),
            "parametros": metadata.get("parametros", [])
        }
        for name, metadata in PROMPT_METADATA.items()
    }

def get_prompts_by_module(modulo: str):
    """Retorna prompts filtrados por módulo - PARA ENDPOINTS"""
    return {
        name: info 
        for name, info in get_all_prompts().items() 
        if info.get("modulo") == modulo
    }

def get_prompt_template(prompt_name: str):
    """Retorna o template de um prompt específico"""
    return PROMPT_REGISTRY.get(prompt_name)

def get_prompt_parameters(prompt_name: str):
    """Retorna os parâmetros necessários para um prompt"""
    metadata = PROMPT_METADATA.get(prompt_name, {})
    return metadata.get("parametros", [])

# ========== FUNÇÕES ==========

def listar_prompts_por_modulo(modulo: str = None):
    """Lista todos os prompts, filtrando por módulo se especificado"""
    if modulo:
        return {k: v for k, v in PROMPT_REGISTRY.items() 
                if PROMPT_METADATA.get(k, {}).get('modulo') == modulo}
    return PROMPT_REGISTRY

def obter_prompt(nome: str):
    """Obtém um prompt pelo nome"""
    if nome not in PROMPT_REGISTRY:
        raise ValueError(f"Prompt '{nome}' não encontrado. Prompts disponíveis: {list(PROMPT_REGISTRY.keys())}")
    return PROMPT_REGISTRY[nome]

def obter_metadados(nome: str):
    """Obtém metadados de um prompt"""
    return PROMPT_METADATA.get(nome, {})