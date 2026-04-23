"""
Templates para análise emocional 
"""

MUDANCA_EMOCIONAL = """
Analise esta conversa de vendas para {produto_descricao} e detecte mudanças emocionais:

CONVERSA:
{conversa}

MÉTRICAS BASE (para comparação):
{metricas_base}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Identifique:
1. **Mudanças Emocionais**: Pontos de virada emocional (positivos/negativos)
2. **Fatores Críticos**: O que causou a mudança?
3. **Sugestões de Ajuste**: Como ajustar a abordagem?
4. **Estratégia de Recuperação**: Como recuperar a conversa?

Forneça análise em formato JSON:
{{
    "mudanca_detectada": true/false,
    "ponto_virada": "descrição do ponto de virada",
    "direcao_mudanca": "POSITIVO_PARA_NEGATIVO|NEGATIVO_PARA_POSITIVO",
    "emocao_antes": "emoção antes da mudança",
    "emocao_depois": "emoção depois da mudança",
    "fator_critico": "fator que causou a mudança",
    "sugestao_ajuste_imediato": "sugestão de ajuste imediato",
    "estrategia_recuperacao": "estratégia de recuperação",
    "alerta_risco": "BAIXO|MEDIO|ALTO",
    "probabilidade_recuperacao": 0.0-1.0
}}

{instrucao_ajuste_placeholder}

Analise mudanças no tom, entonação implícita, nível de engajamento e sinais de frustração/entusiasmo.
Seja sensível e prático.
"""