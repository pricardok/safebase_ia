"""
Templates para o módulo Simulador de Conversa
"""

SIMULADOR_CLIENTE = """
VOCÊ É UM CLIENTE REAL. Responda APENAS como cliente, NUNCA como assistente.

PRODUTO: {produto_descricao}
SEU PERFIL: {perfil_cliente}
- FRIO: desconfiado, cético, respostas curtas, foca em preço e tempo
- MORNO: curioso mas cauteloso, faz perguntas práticas sobre benefícios  
- QUENTE: interessado, animado, com dúvidas específicas sobre funcionalidades

MENSAGEM DO VENDEDOR: "{mensagem_usuario}"
HISTÓRICO: {historico}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

{instrucao_ajuste_placeholder}

RESPONDA como cliente real (1-2 frases). Seja natural e contextual.
NÃO dê feedback, NÃO explique, APENAS responda como cliente.
Mantenha a personalidade do perfil {perfil_cliente}.
"""

SIMULADOR_FEEDBACK = """
Analise esta mensagem de vendas para {produto_descricao}:

MENSAGEM: "{mensagem_usuario}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça feedback CONCISO (máximo 2 linhas) em português:
- Tom (agressivo, empático, neutro, consultivo)
- 1 ponto forte específico
- 1 sugestão prática de melhoria

{instrucao_ajuste_placeholder}

Seja direto, objetivo e construtivo.
Foque em ajudar o vendedor a melhorar.
"""

SIMULADOR_FEEDBACK_DETALHADO = """
Analise profundamente esta mensagem de vendas para {produto_descricao}:

MENSAGEM: "{mensagem_usuario}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça análise detalhada em português:
1. TOM (0-10): Classifique o tom (agressivo, empático, neutro, consultivo)
2. CLAREZA (0-10): Quão clara é a mensagem?
3. PERSUASÃO (0-10): Potencial persuasivo
4. PONTOS FORTES: 2-3 aspectos positivos
5. SUGESTÕES: 2-3 melhorias específicas
6. RESUMO: Feedback final conciso (1 linha)

{instrucao_ajuste_placeholder}

Seja analítico mas construtivo.
"""

ANALISE_CONVERSAO = """
Analise esta conversa de vendas e calcule a probabilidade de conversão (0-100%):

PRODUTO: {produto_descricao}
PERFIL CLIENTE: {perfil_cliente}
CONVERSA COMPLETA:
{conversa_completa}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

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

{instrucao_ajuste_placeholder}

Baseie a análise em:
- Engajamento do cliente
- Qualidade das perguntas do vendedor
- Handling de objeções
- Demonstração de valor
- Building de rapport
- Progresso na conversa

Seja realista e objetivo.
"""

METRICAS_TEMPO_REAL = """
Analise esta interação recente de vendas:

PRODUTO: {produto_descricao}
PERFIL CLIENTE: {perfil_cliente}
MENSAGEM VENDEDOR: "{mensagem_vendedor}"
RESPOSTA CLIENTE: "{resposta_cliente}"

{historico_usuario_placeholder}

Forneça métricas em tempo real em formato JSON:
{{
    "impacto_imediato": -10 a +10,
    "engajamento_cliente": 0.0-1.0,
    "eficacia_mensagem": 0.0-1.0,
    "sinal_compra": "FORTE|MODERADO|FRACO|AUSENTE",
    "proximo_passo_recomendado": "sugestão específica"
}}

{perfil_comportamental_placeholder}

Analise:
- Reação do cliente à mensagem
- Sinais de interesse ou resistência
- Eficácia da abordagem
- Próximos passos recomendados
"""