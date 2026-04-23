"""
Templates para Quebra de Objeções - com 5 abordagens
"""


OBJECOES_QUEBRAR = """
Para a objeção "{objecao}" relacionada a {produto_descricao}, 
gere 5 respostas de CONVERSA  em português, cada uma com uma abordagem distinta,
focadas em entender, ajudar e avançar com respeito,
não em convencer ou empurrar venda.
atuando como um vendedor consultivo, empático e estratégico, 
adaptando a resposta EXCLUSIVAMENTE ao tipo de objeção apresentada.


CONTEXTO DO CLIENTE:
{historico_usuario_placeholder}
{perfil_comportamental_placeholder}

PRIMEIRO: Identifique o tipo exato da objeção:
- PREÇO: se mencionar custo, valor, orçamento, caro
- ADIAMENTO: se mencionar vou pensar, depois, mais tarde, não é prioridade agora
- TERCEIROS: se mencionar já tem fornecedor, precisa consultar alguém, falar com marido, parceiro, socio
- DESCONFIANÇA: se mencionar segurança, confiança, golpe
- OUTRA: especifique

SEGUNDO: Crie 5 respostas ÚNICAS seguindo estas regras:

REGRA CRÍTICA DE OBEDIÊNCIA À OBJEÇÃO:

Se for OBJEÇÃO DE PREÇO:
- NÃO fale de terceiros
- NÃO fale de "decida no seu tempo"
- NÃO desvie para outra objeção
• Foque em VALOR percebido, custo-benefício e impacto prático 
• Ajude o cliente a comparar o investimento com o custo de continuar igual 
• NÃO mencione terceiros, decisões futuras ou quem já faz  
• NÃO desvie para decisão é sua no seu tempo  

Se a objeção for ADIAMENTO ("vou pensar", "depois vejo", "preciso analisar"):
- NÃO fale de benefícios técnicos do produto
- NÃO fale de autoridade, estudos, especialistas, composição ou qualidade
- NÃO tente convencer
- NÃO fale de preço se o cliente não falou
- Foque SOMENTE em:
  • entender o que a pessoa precisa pensar  
  • descobrir dúvidas escondidas  
  • manter conversa aberta sem pressão  

Se for OBJEÇÃO DE TERCEIROS:
• Valide quem já ajuda ou a conversa com outra pessoa  
• Posicione-se como complemento, não substituição  
• Reforce autonomia e decisão pessoal  

Se for OBJEÇÃO DE DESCONFIANÇA:
• Valide o cuidado  
• Esclareça que não é golpe  
• Oriente sobre segurança, procedência e canais oficiais  
• Forneça informações verificáveis
• Ofereça garantias concretas

Se misturar tipos de objeção, a resposta será considerada ERRADA.
  
TERCEIRO: Crie as 5 respostas DISTINTAS:

1. EMPATIA:  
   - Valide a objeção específica apresentada  
   - Demonstre compreensão real do motivo do cliente  
   - Nunca misture contextos de outras objeções  
   - Sempre finalize convidando o cliente a compartilhar dúvidas relacionadas àquela objeção  

2. VALOR:
   - Trabalhe o valor diretamente ligado à objeção  
   - Conecte {produto_descricao} ao benefício prático esperado  
   - Mostre sentido financeiro, emocional ou estratégico  
   - Finalize com uma pergunta que ajude o cliente a avaliar custo x benefício 

3. PROVA SOCIAL:
   - Traga exemplos de pessoas que tinham A MESMA objeção  
   - Mostre mudança de percepção, não pressão  
   - Finalize perguntando se o cliente se reconhece nessa situação 

4. URGÊNCIA:
   - Mostre o custo de não agir em relação àquela objeção  
   - Traga timing lógico, não emocional  
   - Finalize com pergunta sobre momento ou prioridade  

5. AUTORIDADE:
   - Traga clareza técnica ou estratégica ligada à objeção  
   - Oriente sem impor  
   - Finalize com uma pergunta aberta e coerente com o contexto  

{instrucao_ajuste_placeholder}

REGRAS DE FORMATAÇÃO CRÍTICAS:
1. NÃO use números (1., 2., 3.) antes das respostas
2. NÃO use parênteses ou símbolos extras
3. Cada resposta deve ser um PARÁGRAFO CONTÍNUO
4. Use apenas o formato EXATO abaixo:

FORMATO EXATO DE RESPOSTA (COPIE E COLE ESTE ESQUELETO):
[EMPATIA]: [Escreva aqui a resposta empática em 3-5 frases, terminando com uma pergunta.]
[VALOR]: [Escreva aqui a resposta de valor em 3-5 frases, terminando com uma pergunta.]
[PROVA_SOCIAL]: [Escreva aqui a resposta com prova social em 3-5 frases, terminando com uma pergunta.]
[URGENCIA]: [Escreva aqui a resposta de urgência em 3-5 frases, terminando com uma pergunta.]
[AUTORIDADE]: [Escreva aqui a resposta de autoridade em 3-5 frases, terminando com uma pergunta.]

EXEMPLOS SIMPLIFICADOS PARA para objeção "vou pensar":
[EMPATIA]: Entendo, decidir com calma é importante. Ninguém gosta de se sentir apressado. Quando você diz que vai pensar, 
é mais sobre tirar uma dúvida ou sobre sentir mais segurança? O que você sente que ainda precisa clarear?

[VALOR]: Pensar é saudável, mas pensar com direção ajuda mais. Às vezes a gente só precisa organizar o que pesa mais na decisão. 
O que hoje mais influencia se você vai seguir ou não com isso?

[PROVA_SOCIAL]: Muita gente me diz “vou pensar” quando ainda não conseguiu colocar em palavras o que incomoda. Depois que conversa um pouco mais, 
percebe que era só uma dúvida simples. Você sente que é algo pequeno ou algo mais importante?

[URGENCIA]:  Não é sobre decidir agora, é sobre não deixar essa decisão virar mais um “depois”. Quando você imagina esse assunto 
voltando daqui a um mês, você acha que ele vai estar mais claro ou igual a agora?

[AUTORIDADE]: Tomar boas decisões não é sobre velocidade, é sobre clareza. Quanto mais claro estiver pra você, mais tranquilo fica decidir. 
O que hoje falta pra essa decisão ficar clara de verdade?

REGRAS DE CONTEÚDO:
1. Cada resposta deve ser COMPLETAMENTE DIFERENTE
2. Todas terminam com PERGUNTAS DIFERENTES
3. Seja específico para {produto_descricao}
4. Use linguagem natural e conversacional
5. Evite jargões técnicos complexos
6. Proibido frases genéricas como:
  desenvolvido por especialistas,  equipe de especialistas, produto inovador, qualidade incomparável
7. Nunca responda uma objeção com argumento de outra


SEJA EXTREMAMENTE ESPECÍFICO para {produto_descricao}.  
Nunca misture argumentos de objeções diferentes.  
Nunca dilua uma objeção forte com argumentos genéricos.  
Sempre responda exatamente ao que o cliente disse.  
Sempre finalize cada resposta com uma pergunta que ajude o cliente a refletir — não a se defender.

SEMPRE:
- Responda exatamente ao que o cliente disse  
- Não invente dores  
- Não force venda  
- Gere clareza, não pressão  

O tom deve ser humano, consultivo e alinhado à venda com conexão.
O objetivo é gerar clareza e decisão consciente — não convencer à força.
"""

PREDICAO_OBJECOES = """
Analise esta conversa de vendas para {produto_descricao} no nicho {nicho} e preveja objeções:

PERFIL DO CLIENTE: {perfil_cliente}
CONVERSA:
{conversa}

{historico_usuario_placeholder}
{perfil_comportamental_placeholder}

Com base nos padrões linguísticos e comportamentais, preveja as objeções mais prováveis e sugira abordagens preventivas.

Forneça análise em formato JSON:
{{
    "objecoes_provaveis": [
        {{
            "tipo": "preço|adiamento|terceiros|desconfiança|necessidade|complexidade",
            "probabilidade": 0.0-1.0,
            "sinais_detectados": ["sinal específico da conversa", "outro sinal"],
            "abordagem_preventiva": "abordagem específica para prevenir"
        }}
    ],
    "sinais_detectados": ["sinal1", "sinal2"],
    "abordagem_preventiva": "abordagem geral preventiva",
    "nivel_risco": "BAIXO|MEDIO|ALTO"
}}

{instrucao_ajuste_placeholder}

Seja específico: cite trechos exatos da conversa como evidência.
"""

OBJECOES_COMUNS = {
    "preço": "está muito caro / não tenho esse orçamento",
    "pensar": "vou pensar / preciso consultar / depois eu vejo",
    "concorrência": "já tenho fornecedor / estou satisfeito com o atual",
    "necessidade": "não preciso disso agora / não é prioridade",
    "confiança": "não conheço / nunca ouvi falar / é golpe?",
    "complexidade": "parece complicado / não tenho tempo para aprender"
}