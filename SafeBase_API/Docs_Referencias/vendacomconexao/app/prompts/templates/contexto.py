"""
Templates para análise de contexto
"""

CONTEXTO_ALINHAMENTO = """
Analise esta conversa de vendas para {produto_descricao} e identifique:

1. **Alinhamento Contextual**: O vendedor está acompanhando o contexto implícito e explícito da conversa?
2. **Rupturas de Contexto**: Houve mudanças abruptas de assunto ou desconexões?
3. **Tópicos Não Abordados**: Quais preocupações do cliente não foram abordadas?
4. **Sugestão de Transição**: Como realinhar a conversa de forma natural?

CONVERSA:
{conversa}

HISTÓRICO DE CONTEXTO:
{historico_contexto}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça análise em formato JSON:
{{
    "alinhamento_contextual": 0.0-1.0,
    "ruptura_detectada": true/false,
    "sugestao_transicao": "sugestão de transição natural",
    "topicos_nao_abordados": ["tópico1", "tópico2"],
    "nivel_urgencia": "BAIXO|MEDIO|ALTO"
}}

{instrucao_ajuste_placeholder}

Seja específico e construtivo.
Analise especialmente se o vendedor está respondendo às preocupações implícitas do cliente.
"""