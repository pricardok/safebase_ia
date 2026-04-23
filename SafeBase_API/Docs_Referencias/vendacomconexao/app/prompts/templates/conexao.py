"""
Templates para o módulo de Conexão
"""

CONEXAO_GERAR_PERGUNTAS = """
Para vender {produto_descricao} para: {contexto_cliente}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Gere 5 perguntas poderosas em português para descobrir dores reais.

{instrucao_ajuste_placeholder}

Regras:
- Perguntas abertas e exploratórias
- Foco em problemas, necessidades e emoções
- Linguagem natural, empática e conversacional
- Específicas para o contexto do cliente
- Evite perguntas fechadas (sim/não)

Formato: lista numerada com perguntas completas.
"""

CONEXAO_ANALISAR_RESPOSTA = """
Analise esta resposta de vendedor:

PERGUNTA: "{pergunta}"
RESPOSTA: "{resposta_vendedor}"
CONTEXTO: Venda de {produto_descricao}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Forneça análise em formato JSON:
{{
    "pontuacao": 0-100 (quão boa foi a resposta),
    "feedback": "feedback detalhado em 1-2 frases",
    "sugestoes": ["sugestão de melhoria 1", "sugestão de melhoria 2", "Exemplo de Resposta: [aqui você escreve uma resposta completa e pronta para o vendedor usar]"],
    "nivel": "INICIANTE|INTERMEDIARIO|AVANCADO" 
}}

{instrucao_ajuste_placeholder}

Critérios de avaliação:
- Escuta ativa (0-25 pontos)
- Empatia e conexão (0-25 pontos) 
- Clareza e foco (0-25 pontos)
- Valor entregue (0-25 pontos)
"""

CONEXAO_SIMULAR_DIALOGO = """
Simule um diálogo de vendas consultivo para {produto_descricao}.

CENÁRIO: {cenario}
ABORDAGEM: {abordagem}

{historico_usuario_placeholder}

{perfil_comportamental_placeholder}

Gere 4-6 trocas de mensagem entre VENDEDOR e CLIENTE.
Seja realista, natural e contextual.

{instrucao_ajuste_placeholder}

CENÁRIOS POSSÍVEIS:
- inicial: primeiro contato
- objeção: cliente com resistência  
- aprofundamento: explorando necessidades
- fechamento: finalizando a venda

ABORDAGENS:
- consultiva: foco em entender necessidades
- empática: foco em conexão emocional
- valor: foco em benefícios e resultados
- solução: foco em resolver problemas

Formato:
VENDEDOR: [mensagem]
CLIENTE: [mensagem]
VENDEDOR: [mensagem]
CLIENTE: [mensagem]

Inclua uma análise breve no final: ANÁLISE: [análise em 1-2 frases]
"""

CONEXAO_ROTEIRO_ABORDAGEM = """
Você vai criar um ROTEIRO DE ABORDAGEM completo, humano e estratégico.
O objetivo é gerar conexão, entender a pessoa e levar para uma conversa de valor,
sem parecer vendedor chato e sem falar de preço no início.

CONTEXTO:
- O QUE ESTÁ SENDO VENDIDO: {produto_descricao}
- TIPO DE PESSOA: {tipo_pessoa}
  (pessoa fria | pessoa conhecida | pessoa de remarketing | pessoa de anúncio)

MISSÃO:
Criar um roteiro em etapas que:
- Comece leve
- Gere conexão
- Entenda a realidade da pessoa
- Mostre valor sem pressão
- Convide para o próximo passo

PASSO 1 — DEFINIR O PERFIL PELO {produto_descricao}:

Use SOMENTE o que estiver escrito em {produto_descricao}.

- Se mencionar lugar físico (estética, restaurante, bar, loja, clínica, oficina, imobiliária etc):
  → PERFIL = ESTABELECIMENTO

- Se mencionar profissão (advogado, mecânico, corretor, designer, professor etc):
  → PERFIL = PROFISSIONAL

- Se não mencionar lugar nem profissão:
  → PERFIL = PESSOA COMUM

Nunca invente profissão, hobby ou contexto.

PASSO 2 — AJUSTAR TOM PELO {tipo_pessoa}:

- Pessoa fria:
  Mais explicativo, educado, sem intimidade.

- Pessoa conhecida:
  Mais próxima, usando referência à relação.

- Remarketing:
  Reconhecer que já houve contato anterior.

- Veio de anúncio:
  Mencionar que ela demonstrou interesse.

PASSO 3 — ESTRUTURA DO ROTEIRO:

O roteiro deve seguir exatamente esta lógica:

1. Início leve + conexão  
   Gere 2 ou 3 variações:
   - Uma descontraída  
   - Uma profissional  
   - Uma direta  
   Sempre adaptadas ao PERFIL e ao {tipo_pessoa}.

2. Pergunta de confirmação  
   Uma pergunta simples para garantir que está falando com a pessoa certa.

3. Reforço de valor  
   Reconheça algo positivo no contexto dela (sem bajular e sem inventar).

4. Pergunta consultiva  
   Pergunta que ajude a entender como ela faz hoje.

5. Validação da resposta  
   Duas respostas possíveis:
   - Se ela faz sozinha
   - Se já tem ajuda

6. Pergunta estratégica  
   Uma pergunta que leve ao primeiro “sim”, mostrando um limite natural do jeito atual.

7. Normalização  
   Mostre que a dificuldade é comum e compreensível.

8. Revelação sutil  
   Mostre quem você é e como ajuda, sem tom vendedor.

9. Convite de baixo risco  
   Convide para uma conversa, análise ou demonstração gratuita.

10. Fechamento com opções  
   Ofereça duas opções de data ou formato.

PASSO 4 — REGRAS CRÍTICAS:

- Não falar de preço no início
- Não parecer propaganda
- Não usar linguagem de vendedor
- Não inventar contexto
- Sempre focar na pessoa antes do produto
- Linguagem simples, humana e conversada

FORMATO DE SAÍDA (RETORNE TAMBÉM UM JSON NO CAMPO `roteiro_estruturado`):

📌 Roteiro de Abordagem – {produto_descricao}

1. Início leve + conexão (3 variações)
Opção descontraída:
"..."

Opção profissional:
"..."

Opção direta:
"..."

2. Pergunta de confirmação
"..."

3. Reforço de valor
"..."

4. Pergunta consultiva
"..."

5. Validação da resposta
Se fizer sozinho:
"..."
Se já tiver ajuda:
"..."

6. Pergunta estratégica
"..."

7. Normalização
"..."

8. Revelação sutil
"..."

9. Convite de baixo risco
"..."

10. Fechamento com opções
"..."

OBJETIVO FINAL:
Criar uma conversa que:
- Faça a pessoa se sentir vista
- Gere confiança
- Mostre valor real
- Leve naturalmente ao próximo passo
Sem pressão e sem empurrar venda.
"""