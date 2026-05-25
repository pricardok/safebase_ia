# Plano de dados de negócio (categorias comerciais)

Este documento descreve **o que falta implementar** para as categorias de negócio no SafeBase (consignado, FGTS, seguros etc.) e **por que** precisamos dessas tabelas para o chat/IA.

---

## 1) Por que criar essas tabelas?
Hoje a IA só consegue responder com base nos dados normalizados de **DBA** (jobs, waits, alertas, backups). Para as categorias de negócio, **não existem dados no banco**, então a IA responde apenas com contexto genérico.

Para gerar respostas reais (com números e tendências), é necessário:
1. Criar tabelas específicas de negócio no banco central.
2. Ingerir dados dessas áreas (ERP/CRM/relatórios internos).
3. Normalizar os dados da mesma forma que fazemos com os dados de DBA.

---

## 2) Categorias de negócio previstas
- **emprestimo_consignado**
- **saque_aniversario_fgts**
- **credito_trabalhador_clt**
- **loas_bpc**
- **seguros**
- **bi**

---

## 3) Estrutura mínima recomendada (por categoria)
Sugestão de padrão de tabelas por categoria:

### 3.1 Tabela principal de fatos
Exemplo: `dados_negocio_consignado`

**Campos mínimos sugeridos**:
- `id` (PK)
- `categoria_codigo` (ex: emprestimo_consignado)
- `data_referencia` (date)
- `volume_propostas`
- `volume_contratacoes`
- `ticket_medio`
- `taxa_aprovacao`
- `receita_estimada`
- `status`
- `origem` (ex: ERP, planilha, API externa)
- `criado_em`
- `atualizado_em`

### 3.2 Tabela de eventos (opcional)
Ex: `dados_negocio_eventos`

Campos:
- `id`
- `categoria_codigo`
- `tipo_evento` (ex: campanha, redução taxa, mudança regra)
- `descricao`
- `data_evento`

---

## 4) Como alimentar esses dados
Possibilidades:
- **Integração direta com ERP/CRM** via APIs
- **Carga CSV/Excel** recorrente (ETL)
- **Integração com Data Warehouse / BI**

---

## 5) Uso esperado pela IA
Quando o usuário perguntar:
> "Mostra um gráfico de contratações do consignado por mês"

A IA deve:
1. Buscar dados em `dados_negocio_consignado`
2. Gerar `chart` estruturado
3. Retornar insights baseados em tendência

---

## 6) Próximos passos sugeridos
1. Definir modelo mínimo de dados por categoria
2. Criar tabelas no banco central
3. Criar pipeline de ingestão
4. Criar normalizador (similar ao de DBA)
5. Ajustar `/ia/query` para ler esses dados

---

## 7) Observação importante
Sem essas tabelas, **o chat por categoria de negócio não terá dados reais**.
As respostas serão apenas heurísticas, não análises reais.

---

Se quiser, posso:
- gerar o SQL dessas tabelas
- criar um modelo de ingestão padrão
- definir quais métricas fazem mais sentido por categoria
