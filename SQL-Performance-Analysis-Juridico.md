# Análise de Performance e Índices — Módulo Jurídico

## Objetivo

Este documento fornece um passo a passo profissional para analisar e otimizar as consultas SQL do módulo jurídico, com foco em:
- identificação de problemas de performance;
- recomendações de índices;
- tratamento de SQL Injection;
- redução de custos de I/O e CPU.

---

## 1. Entendimento inicial

1.1. Liste as queries do módulo jurídico.
- Identifique consultas `RAW SQL` e funções inline (`INLINE FN`).
- Priorize queries com `WHERE`, `JOIN`, `ORDER BY`, `TOP`, `OFFSET`, `FETCH` e subqueries.

1.2. Mapeie os dados consultados.
- Quais tabelas aparecem com maior frequência no documento?
- Quais campos são usados em filtros, joins e ordenações?

1.3. Classifique por risco e impacto.
- Alto risco: SQL Injection com concatenação direta.
- Alto impacto: consultas que retornam muitos dados ou fazem `SELECT *` em tabelas grandes.

---

## 2. Coletar metadados dos objetos de banco

2.1. Verifique o esquema das tabelas e índices existentes.
- Use `sys.indexes`, `sys.index_columns`, `sys.stats`, `sys.objects`.
- Exemplo: 
  ```sql
  SELECT o.name, i.name, i.type_desc, c.name, ic.key_ordinal, ic.is_included_column
  FROM sys.indexes i
  JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
  JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
  JOIN sys.objects o ON i.object_id = o.object_id
  WHERE o.name IN ('JURIDICO_LOGINS', 'JURIDICO_ACESSO', 'PROCESSO', 'AUXILIO_FINANCEIRO');
  ```

2.2. Conheça os tipos de índice existentes.
- Clustered x Nonclustered.
- Índices cobertos (`INCLUDE`) para colunas de retorno.
- Índices filtrados quando aplicável.

---

## 3. Análise de planos de execução

3.1. Execute as queries com plano de execução real.
- `SET STATISTICS IO ON;`
- `SET STATISTICS TIME ON;`
- Ative o plano de execução atual no SSMS.

3.2. Identifique problemas comuns.
- Table scan / index scan em tabelas grandes.
- Key lookup repetido.
- Operators `HASH MATCH` ou `MERGE JOIN` dispendiosos.
- Predicados não sargable (uso de funções na coluna, `LIKE '%...%'`, `ISNULL`, `CONVERT`).

3.3. Priorize otimizações.
- Comece por queries com maior frequência e maior custo.
- Foco em filtros seletivos e joins de grandes tabelas.

---

## 4. Recomendações de índices por padrão

4.1. Filtros diretos e joins comuns.
- `JURIDICO_LOGINS(CPF, ativo)`
- `JURIDICO_ACESSO(login, ativo)`
- `PROCESSO_CAMPOS_OBRIGATORIOS(tabela)`
- `PROCESSO_OCR_LOGS_ARQUIVOS(CODIGO)`
- `PROCESSO_OCR_LOGS(ID_ARQUIVO, CODIGO)`
- `AUXILIO_FINANCEIRO(CODIGO)`
- `CLIENTE(CPF)`
- `PROCESSO(ID)` e `PROCESSO(numero_processo)`

4.2. Consultas `TOP 1 ... ORDER BY ... DESC`.
- Use índices compostos que contenham o campo de filtro e a ordem.
- Exemplo: `CONTROLE_CESSAO_CARTEIRA(Codigo_AF, ID DESC)`.

4.3. Consultas de paginação.
- A query de paginação em `Juridico_importacoes.php` deve usar índice em `PROCESSO_OCR_LOGS_ARQUIVOS(CODIGO DESC)`.
- Evite `ORDER BY` sem coluna indexada.

4.4. Índices para subqueries e buscas de existência.
- `Facta_Seguradora_Apolices_Prestamista(af)`
- `Facta_Seguradora_Apolices_Individual(af)`
- `Facta_Seguradora_Apolices_Associacao(af)`
- `Facta_Seguradora_Apolices_Fgts(af)`

---

## 5. Correções de SQL Injection e boas práticas de query

5.1. Substitua concatenação por parâmetros.
- Nunca faça: `WHERE CPF = '{$dados->login}'`
- Use prepared statements ou `sp_executesql` com parâmetros.

5.2. Revise consultas dinâmicas.
- Para `WHERE 1=1` + foreach dinâmico, mapeie explicitamente as colunas permitidas.
- Não concatene chaves de coluna diretamente.

5.3. Evite `SELECT *`.
- Liste apenas as colunas necessárias.
- Isso reduz I/O e melhora o aproveitamento de índices cobertos.

5.4. Normalizar duplicidades.
- Reutilize uma única função/exposição para consultas idênticas.
- Exemplo: `consulta()` e `consultaCPF()` usam a mesma query.

---

## 6. Teste e validação

6.1. Compare antes e depois.
- Use `SET STATISTICS IO/TIME ON` antes e depois da mudança.
- Capture tempo de execução, leituras lógicas e físicas.

6.2. Confirme o plano de execução.
- Verifique se a query agora utiliza índice em vez de scan completo.
- Confirme se key lookups desapareceram ou foram reduzidos.

6.3. Teste com dados representativos.
- Faça os testes em um ambiente com volume semelhante à produção.
- Se não houver dados reais, use amostras grandes e distribuições de valores.

---

## 7. Entrega prática para o módulo jurídico

7.1. Documente o índice sugerido e o motivo.
- Ex.: `CREATE NONCLUSTERED INDEX IX_JURIDICO_LOGINS_CPF_ATIVO ON JURIDICO_LOGINS (CPF, ativo);`
- Informe se o índice cobre colunas de retorno.

7.2. Implemente em ambiente de homologação.
- Avalie impacto de criação de índice em tabelas grandes.
- Monitore uso de disco e tempo de criação.

7.3. Acompanhe após deploy.
- Verifique planos de execução e estatísticas de uso.
- Remova índices não utilizados.

---

## 8. Prioridade de ação imediata

1. Corrigir SQL Injection em todas as queries RAW com concatenação direta.
2. Criar índices em colunas de filtro/joins frequentes.
3. Mudar `SELECT *` para projeções específicas.
4. Testar as consultas de maior custo com planos reais.
5. Consolidar queries duplicadas e revisar funções inline.

---

## 9. Exemplo de índice sugerido

```sql
CREATE NONCLUSTERED INDEX IX_JURIDICO_LOGINS_CPF_ATIVO
ON Facta_01_BaseDados.dbo.JURIDICO_LOGINS (CPF, ativo);

CREATE NONCLUSTERED INDEX IX_JURIDICO_ACESSO_LOGIN_ATIVO
ON Facta_01_BaseDados.dbo.JURIDICO_ACESSO (login, ativo);

CREATE NONCLUSTERED INDEX IX_PROCESSO_CAMPOS_OBRIGATORIOS_TABELA
ON PROCESSO_CAMPOS_OBRIGATORIOS (tabela);
```

---

## 10. Conclusão

A otimização deve ser feita em duas frentes:
- Estrutural: índices corretos e ordenação adequada.
- Aplicacional: eliminar SQL Injection, queries dinâmicas inseguras e `SELECT *`.

Seguindo este passo a passo, você terá uma base sólida para reduzir custo das consultas do módulo jurídico e aumentar a segurança do sistema.
