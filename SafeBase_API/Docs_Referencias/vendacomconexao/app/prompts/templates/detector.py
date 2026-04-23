"""
Templates para Detector de Vendedor Chato 
"""

DETECTOR_ANALISAR = """
Analise esta mensagem de vendas para {produto_descricao}:

MENSAGEM: "{mensagem}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça uma análise COMPLETA e DETALHADA em português com este formato:

**Classificação:** [AGRESSIVO|POUCA EMPATIA|EQUILIBRADO|CONSULTIVO|EMPÁTICO]

**Motivo:** [Explicação detalhada da classificação, apontando trechos específicos da mensagem]

**Sugestão:** [Sugestão prática e específica de melhoria]

**Pontuação de Empatia:** [0-100]/100

**Nível de Pressão:** [BAIXO|MÉDIO|ALTO]

**Indicadores de Problema:**
- [Indicador 1: ex: Frases imperativas]
- [Indicador 2: ex: Tom alarmista] 
- [Indicador 3: ex: Foco excessivo no produto]

**Exemplo Corrigido:** [Exemplo completo de mensagem corrigida com abordagem melhorada]

{instrucao_ajuste_placeholder}

Regras:
- Seja DIRETO e CONSTRUTIVO
- Analise trechos específicos da mensagem
- Dê sugestões PRÁTICAS e aplicáveis
- Adapte ao contexto de {produto_descricao}
- Mantenha a análise textual rica e detalhada
"""

DETECTOR_ANALISAR_JSON = """
Para a mensagem sobre {produto_descricao}: "{mensagem}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Retorne APENAS JSON:

{{
    "classificacao": "AGRESSIVO|POUCA_EMPATIA|EQUILIBRADO|CONSULTIVO|EMPÁTICO",
    "motivo": "explicação detalhada",
    "sugestao": "sugestão prática",
    "pontuacao_empatia": 0-100,
    "nivel_pressao": "BAIXO|MÉDIO|ALTO",
    "indicadores_problema": ["indicador1", "indicador2", "indicador3"],
    "exemplo_corrigido": "mensagem corrigida"
}}

{instrucao_ajuste_placeholder}
"""

DETECTOR_CLASSIFICACOES = {
    "AGRESSIVO": "Tom impositivo, pressão excessiva, linguagem alarmista",
    "POUCA_EMPATIA": "Foco no produto, pouca escuta, linguagem genérica", 
    "EQUILIBRADO": "Bom equilíbrio entre persuasão e respeito",
    "CONSULTIVO": "Foco no cliente, perguntas, escuta ativa",
    "EMPÁTICO": "Alta conexão emocional, validação de sentimentos"
}