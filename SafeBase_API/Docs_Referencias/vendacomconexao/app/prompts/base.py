"""
Templates base e constantes para prompts
"""

# Constantes para perfis e contextos
PERFIS_CLIENTE = {
    'frio': {
        'descricao': 'desconfiado, cético, respostas curtas, foca em preço/tempo',
        'tom': 'reservado, direto, prático'
    },
    'morno': {
        'descricao': 'curioso mas cauteloso, faz 1-2 perguntas sobre benefícios',
        'tom': 'interessado, questionador, prático'
    },
    'quente': {
        'descricao': 'entusiasmado, pergunta sobre funcionalidades/prazos, interessado',
        'tom': 'animado, engajado, curioso'
    }
}

CANAIS_VENDA = {
    'instagram': 'redes sociais, visual, engajamento rápido',
    'whatsapp': 'conversa pessoal, direta, rápida',
    'email': 'formal, estruturado, informativo',
    'telefone': 'pessoal, empático, direto',
    'presencial': 'pessoal, detalhado, envolvente'
}

# Template base para todos os prompts
BASE_TEMPLATE = """
Contexto: Sistema de treinamento de vendas Venda+
Instrução: {instrucao}
Regras: {regras}
Formato: {formato}
"""