"""
Templates para o módulo de Scripts 
"""

SCRIPTS_OTIMIZAR = """
Otimize este script de vendas:

PRODUTO: {produto_descricao}
CANAL: {canal}
OBJETIVO: {objetivo}
SCRIPT ORIGINAL: "{script_original}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça:
1. Script otimizado (mais persuasivo, natural e eficaz)
2. Lista de melhorias aplicadas (3-5 itens)

{instrucao_ajuste_placeholder}

Foque em:
- Clareza e persuasão
- Adequação ao canal {canal}
- Chamada para ação eficaz
- Tom empático e autêntico
- Geração de valor claro

Canais e suas características:
- INSTAGRAM: visual, engajamento rápido, tom casual
- WHATSAPP: pessoal, direto, resposta rápida  
- EMAIL: formal, estruturado, informativo
- TELEFONE: empático, claro, envolvente
- PRESENCIAL: detalhado, pessoal, convincente

Formato: 
SCRIPT OTIMIZADO: [script completo]
MELHORIAS: [lista numerada]
"""
 
SCRIPTS_ANALISAR_EFICACIA = """
Analise a eficácia deste script de vendas e forneça um feedback estruturado.

**Produto/Serviço:** {produto_descricao}
**Canal:** {canal}
**Script Original:** "{script}"

**Exemplos de Classificação Correta:**
- Um script como "Olá! Vi que você busca uma transição de carreira. Gostaria de conversar para entender seus objetivos e ver como posso ajudar?" DEVE ser classificado como "CONSULTIVO".
- Um script como "Compre agora nosso curso e mude de vida! Oferta imperdível!" DEVE ser classificado como "AGRESSIVO".

{historico_usuario_placeholder}
{perfil_comportamental_placeholder}
{instrucao_ajuste_placeholder}

**Análise Detalhada (responda exatamente neste formato):**
**Classificação:** (AGRESSIVO, POUCA EMPATIA, EQUILIBRADO, CONSULTIVO, EMPÁTICO)
**Motivo:** (Explique o porquê da classificação em 2-3 linhas)
**Sugestão:** (Dê uma sugestão clara e acionável para melhorar)
**Pontuação de Empatia:** (0-100)
**Nível de Pressão:** (BAIXO, MÉDIO, ALTO)
**Indicadores de Problema:**
- (Liste 2-3 pontos específicos, como "Uso de clichês", "Falta de personalização")
**Exemplo Corrigido:** (Forneça uma versão alternativa e melhorada do script)
"""

SCRIPTS_GERAR_VARIACOES = """
Gere EXATAMENTE {numero_variacoes} variações DISTINTAS e CRIATIVAS deste script:

PRODUTO: {produto_descricao}
CANAL: {canal}
SCRIPT BASE: "{script_base}"

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

REGRAS IMPORTANTES:
- Cada variação deve ser COMPLETAMENTE DIFERENTE das outras
- Mude a abordagem, tom, estrutura e palavras-chave
- Mantenha a mesma intenção do script original
- Adapte ao canal {canal}
- Seja criativo e original

{instrucao_ajuste_placeholder}

ABORDAGENS SUGERIDAS (use diferentes combinações):
1. EMPÁTICA: foco em conexão emocional, entender necessidades
2. BENEFÍCIOS: foco em valor e resultados tangíveis  
3. PROVA SOCIAL: foco em casos reais, depoimentos, social proof
4. URGÊNCIA: foco em oportunidade limitada, escassez
5. EDUCATIVA: foco em ensinar, informar, agregar valor
6. STORYTELLING: foco em contar histórias, criar narrativa
7. CURIOSIDADE: foco em despertar interesse, fazer perguntas
8. SOLUÇÃO: foco em resolver problemas específicos

FORMATO DE RESPOSTA OBRIGATÓRIO:
VARIAÇÃO 1:
NOME: [Nome criativo da abordagem - máximo 3 palavras]
SCRIPT: [Script completo e diferente - mínimo 2 linhas]

VARIAÇÃO 2:
NOME: [Nome criativo da abordagem - máximo 3 palavras] 
SCRIPT: [Script completo e diferente - mínimo 2 linhas]

VARIAÇÃO 3:
NOME: [Nome criativo da abordagem - máximo 3 palavras]
SCRIPT: [Script completo e diferente - mínimo 2 linhas]

NÃO REPITA o script base. Crie variações genuinamente diferentes.
"""