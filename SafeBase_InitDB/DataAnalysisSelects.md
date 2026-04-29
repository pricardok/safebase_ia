# Data Analysis Queries - SafeBase InitDB

Este documento reúne `SELECT` úteis para analisar os dados carregados pelos processos do `SafeBase_InitDB`.
Use o banco `SafeBase` ou o banco onde as tabelas de coleta estão instaladas.

---

## 1. Auditoria de alterações de banco

### 1.1 Dados de alteração do banco
```sql
SELECT TOP 200 *
FROM dbo.ServerAudi
ORDER BY DataEvento DESC;
```

### 1.2 Ocorrências por banco
```sql
SELECT DatabaseName,
       COUNT(*) AS TotalEventos
FROM dbo.ServerAudi
GROUP BY DatabaseName
ORDER BY TotalEventos DESC;
```

### 1.3 Eventos de criação / exclusão / modificação
```sql
SELECT TOP 200 *
FROM dbo.ServerAudi
WHERE statement LIKE '%CREATE%'
   OR statement LIKE '%DROP%DATABASE%'
   OR statement LIKE '%MODIFY NAME%'
ORDER BY DataEvento DESC;
```

---

## 2. Histórico de tamanhos de tabelas

### 2.1 Últimos registros de crescimento de tabela
```sql
SELECT h.Dt_Referencia,
       s.Nm_Servidor,
       b.Nm_Database,
       t.Nm_Tabela,
       h.Nr_Tamanho_Total,
       h.Nr_Tamanho_Dados,
       h.Nr_Tamanho_Indice,
       h.Qt_Linhas,
       h.Nm_Drive
FROM dbo.HistoricoTamanhoTabela h
JOIN dbo.Servidor s ON h.Id_Servidor = s.Id_Servidor
JOIN dbo.BaseDados b ON h.Id_BaseDados = b.Id_BaseDados
JOIN dbo.Tabela t ON h.Id_Tabela = t.Id_Tabela
ORDER BY h.Dt_Referencia DESC;
```

### 2.2 Tabelas com maior crescimento recente
```sql
SELECT b.Nm_Database,
       t.Nm_Tabela,
       MAX(h.Nr_Tamanho_Total) AS UltimoTamanhoKB,
       MAX(h.Qt_Linhas) AS UltimoQtdLinhas,
       COUNT(*) AS Coletas
FROM dbo.HistoricoTamanhoTabela h
JOIN dbo.BaseDados b ON h.Id_BaseDados = b.Id_BaseDados
JOIN dbo.Tabela t ON h.Id_Tabela = t.Id_Tabela
GROUP BY b.Nm_Database, t.Nm_Tabela
ORDER BY UltimoTamanhoKB DESC;
```

### 2.3 Comparar crescimento por período
```sql
SELECT *
FROM dbo.CheckTableGrowth
ORDER BY ABS(Cresc_1_dia) DESC;
```

### 2.4 Top 10 tabelas por crescimento total
```sql
SELECT TOP 10 Nm_Database,
              Nm_Tabela,
              Tamanho_Atual,
              Cresc_1_dia,
              Cresc_15_dia,
              Cresc_30_dia,
              Cresc_60_dia
FROM dbo.CheckTableGrowth
ORDER BY ABS(Cresc_1_dia) DESC;
```

---

## 3. Fragmentação de índices

### 3.1 Registros de fragmentação recentes
```sql
SELECT TOP 200 *
FROM dbo.HistoricoFragmentacaoIndice
ORDER BY Dt_Referencia DESC;
```

### 3.2 Índices acima de 10% de fragmentação
```sql
SELECT TOP 200 *
FROM dbo.CheckFragmentacaoIndices
ORDER BY Avg_Fragmentation_In_Percent DESC, Page_Count DESC;
```

---

## 4. Utilização de arquivos e disco

### 4.1 Arquivos de dados (MDF/NDF)
```sql
SELECT *
FROM dbo.CheckArquivosDados
ORDER BY Nm_Database, Logical_Name;
```

### 4.2 Arquivos de log (LDF)
```sql
SELECT *
FROM dbo.CheckArquivosLog
ORDER BY Nm_Database, Logical_Name;
```

### 4.3 Uso de espaço em disco
```sql
SELECT *
FROM dbo.CheckEspacoDisco
ORDER BY [Used (%)] DESC;
```

### 4.4 Histórico de utilização de arquivos de I/O
```sql
SELECT *
FROM dbo.HistoricoUtilizacaoArquivo
ORDER BY Dt_Registro DESC;
```

### 4.5 Análise de leituras/escritas por arquivo
```sql
SELECT *
FROM dbo.CheckUtilizacaoArquivoReads
ORDER BY num_of_reads DESC;

SELECT *
FROM dbo.CheckUtilizacaoArquivoWrites
ORDER BY num_of_writes DESC;
```

---

## 5. Backups

### 5.1 Backups realizados nas últimas 24 horas
```sql
SELECT *
FROM dbo.CheckBackupsExecutados
ORDER BY Backup_Start_Date DESC;
```

### 5.2 Bancos sem backup detectado
```sql
SELECT *
FROM dbo.CheckDatabasesSemBackup;
```

### 5.3 Histórico de backup por banco
```sql
SELECT *
FROM dbo.CheckDatabasesHistoricoBackup
ORDER BY Banco, DataFull DESC;
```

---

## 6. Alterações e disponibilidade de banco

### 6.1 Alterações de jobs nas últimas 24 horas
```sql
SELECT *
FROM dbo.CheckAlteracaoJobs
ORDER BY Dt_Modificacao DESC, Dt_Criacao DESC;
```

### 6.2 Conexões abertas por login
```sql
SELECT *
FROM dbo.CheckConexaoAberta
ORDER BY session_count DESC;
```

### 6.3 Top 10 logins ativos
```sql
SELECT *
FROM dbo.CheckConexaoAberta_Email;
```

---

## 7. Saúde de jobs e execução

### 7.1 Jobs que falharam
```sql
SELECT *
FROM dbo.CheckJobsFailed
ORDER BY Dt_Execucao DESC;
```

### 7.2 Jobs demorados
```sql
SELECT *
FROM dbo.CheckJobDemorados
ORDER BY Run_Duration DESC;
```

### 7.3 Jobs em execução há mais de 10 minutos
```sql
SELECT *
FROM dbo.CheckJobsRunning
ORDER BY Dt_Inicio DESC;
```

---

## 8. Alertas

### 8.1 Alertas recentes
```sql
SELECT *
FROM dbo.CheckAlerta
ORDER BY Dt_Alerta DESC;
```

### 8.2 Alertas ainda sem clear
```sql
SELECT *
FROM dbo.CheckAlertaSemClear
ORDER BY Dt_Alerta DESC;
```

### 8.3 Verificar configuração e parâmetros de alerta
```sql
SELECT *
FROM dbo.AlertaParametro;
```

---

## 9. Erros e falhas de login

### 9.1 Erros de servidor SQL
```sql
SELECT TOP 200 *
FROM dbo.CheckSQLServerErrorLog
ORDER BY Dt_Log DESC;
```

### 9.2 Falhas de login SQL
```sql
SELECT *
FROM dbo.CheckSQLServerLoginFailed
ORDER BY Qt_Erro DESC;
```

### 9.3 Resumo de falhas de login
```sql
SELECT *
FROM dbo.CheckSQLServerLoginFailedEmail;
```

---

## 10. Query tracing e performance

### 10.1 Queries lentas por prefixo
```sql
SELECT *
FROM dbo.CheckDBControllerQueries
ORDER BY Total DESC, Media DESC;
```

### 10.2 Volume de trace por dia
```sql
SELECT *
FROM dbo.CheckDBControllerQueriesGeral
ORDER BY Data DESC;
```

### 10.3 Queries em execução longa
```sql
SELECT *
FROM dbo.CheckQueriesRunning
ORDER BY start_time DESC;
```

### 10.4 Queries lentas e contadores por hora
```sql
SELECT *
FROM dbo.CheckContadores
ORDER BY Hora, Nm_Contador;

SELECT *
FROM dbo.CheckContadoresEmail;
```

### 10.5 Query demoradas (alerta)
```sql
SELECT TOP 200 *
FROM dbo.ResultadoTraceLog
ORDER BY Duration DESC;
```

### 10.6 Consultas lentas em foco
```sql
SELECT TOP 200 StartTime,
       DataBaseName,
       Duration,
       Reads,
       Writes,
       CPU,
       TextData
FROM dbo.ResultadoTraceLog
WHERE StartTime >= DATEADD(MINUTE, -60, GETDATE())
ORDER BY Duration DESC;
```

---

## 11. Wait stats e histórico de espera

### 11.1 Wait stats resumidos
```sql
SELECT *
FROM dbo.CheckWaitsStats
ORDER BY DIf_Percentage DESC;
```

### 11.2 Histórico de waits
```sql
SELECT *
FROM dbo.HistoricoWaitsStats
ORDER BY Id_HistoricoWaitsStats DESC;
```

---

## 12. AlwaysOn e alta disponibilidade

### 12.1 Histórico AlwaysOn
```sql
SELECT *
FROM dbo.HistoricoAlwaysOn
ORDER BY Dt_Registro DESC;
```

### 12.2 Verificar se AlwaysOn está habilitado
```sql
SELECT TOP 1 *
FROM dbo.HistoricoAlwaysOn;
```

---

## 13. Contadores brutos

### 13.1 Contadores de sistema
```sql
SELECT c.Id_ContadorRegistro,
       c.Dt_Log,
       t.Nm_Contador,
       c.Valor
FROM dbo.ContadorRegistro c
LEFT JOIN dbo.Contador t ON c.Id_Contador = t.Id_Contador
ORDER BY c.Dt_Log DESC;
```

### 13.2 Valores agregados por contador
```sql
SELECT t.Nm_Contador,
       AVG(c.Valor) AS MediaValor,
       MAX(c.Valor) AS MaxValor,
       MIN(c.Valor) AS MinValor
FROM dbo.ContadorRegistro c
JOIN dbo.Contador t ON c.Id_Contador = t.Id_Contador
GROUP BY t.Nm_Contador
ORDER BY MediaValor DESC;
```

---

## 14. Uso específico de tabelas de relatório

### 14.1 Dados baseados em `CheckDatabaseGrowth`
```sql
SELECT *
FROM dbo.CheckDatabaseGrowth
ORDER BY Cresc_1_dia DESC;
```

### 14.2 Crescimento de banco para email
```sql
SELECT *
FROM dbo.CheckDatabaseGrowthEmail;
```

### 14.3 Crescimento de tabela para email
```sql
SELECT *
FROM dbo.CheckTableGrowthEmail;
```

### 14.4 Trace e query geral
```sql
SELECT *
FROM dbo.SQLTraceLog
ORDER BY StartTime DESC;
```

---

## 15. Consulta rápida de status geral

### 15.1 Ver todos os checks sem limpar
```sql
SELECT COUNT(*) AS TotalAlertas FROM dbo.CheckAlerta;
SELECT COUNT(*) AS TotalJobsFailed FROM dbo.CheckJobsFailed;
SELECT COUNT(*) AS TotalBackupExec FROM dbo.CheckBackupsExecutados;
SELECT COUNT(*) AS TotalDatabasesNoBackup FROM dbo.CheckDatabasesSemBackup;
```

### 15.2 Consistência de dados de alerta
```sql
SELECT COUNT(*) AS TotalAlertas,
       SUM(CASE WHEN Dt_Alerta IS NULL THEN 1 ELSE 0 END) AS SemData,
       COUNT(DISTINCT Nm_Alerta) AS AlertasDistintos
FROM dbo.CheckAlerta;
```

---

## 16. Análises cruzadas

### 16.1 Correlacionar queries lentas com consumo de CPU
```sql
SELECT c.Hora,
       SUM(CASE WHEN c.Nm_Contador = 'CPU' THEN c.Media ELSE 0 END) AS CPU,
       SUM(CASE WHEN c.Nm_Contador = 'BatchRequests' THEN c.Media ELSE 0 END) AS BatchRequests,
       COUNT(q.StartTime) AS QueriesLentas
FROM dbo.CheckContadores c
LEFT JOIN dbo.ResultadoTraceLog q
    ON DATEPART(HOUR, q.StartTime) = c.Hora
GROUP BY c.Hora
ORDER BY c.Hora;
```

### 16.2 Correlacionar queries demoradas com waits elevados
```sql
WITH QueryLentas AS (
    SELECT DATEADD(HOUR, DATEDIFF(HOUR, 0, StartTime), 0) AS Hora,
           COUNT(*) AS TotalQueriesLentas
    FROM dbo.ResultadoTraceLog
    WHERE StartTime >= DATEADD(HOUR, -6, GETDATE())
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, StartTime), 0)
)
SELECT *
FROM QueryLentas
ORDER BY Hora;
```

```sql
SELECT *
FROM dbo.CheckWaitsStats
ORDER BY DIf_Percentage DESC;
```

> Compare os resultados por intervalo de tempo para identificar se picos de waits coincidem com aumento de queries lentas.

### 16.3 Relacionar jobs longos e queries em execução longa
```sql
SELECT j.Nm_JOB,
       j.Dt_Inicio,
       j.Qt_Duracao,
       q.database_name,
       q.start_time,
       q.status,
       q.sql_command
FROM dbo.CheckJobsRunning j
CROSS APPLY (
    SELECT TOP 1 *
    FROM dbo.CheckQueriesRunning r
    WHERE r.start_time <= CONVERT(DATETIME, j.Dt_Inicio + ':00', 120)
      AND r.start_time >= DATEADD(HOUR, -1, CONVERT(DATETIME, j.Dt_Inicio + ':00', 120))
    ORDER BY r.start_time DESC
) q;
```

### 16.4 Crescimento de banco vs. bancos sem backup
```sql
SELECT g.Nm_Database,
       g.Tamanho_Atual,
       g.Cresc_1_dia,
       g.Cresc_15_dia,
       b.Nm_Database AS SemBackup
FROM dbo.CheckDatabaseGrowth g
LEFT JOIN dbo.CheckDatabasesSemBackup b
    ON g.Nm_Database = b.Nm_Database
ORDER BY g.Cresc_1_dia DESC;
```

### 16.5 Espaço de arquivo vs. tamanho de tabelas
```sql
SELECT a.Nm_Database,
       a.Total_Reservado,
       a.Total_Utilizado,
       a.Espaco_Livre_MB,
       t.Tamanho_Atual,
       t.Cresc_1_dia
FROM dbo.CheckArquivosDados a
LEFT JOIN dbo.CheckDatabaseGrowth t
    ON a.Nm_Database = t.Nm_Database
ORDER BY a.Espaco_Livre_MB ASC, t.Cresc_1_dia DESC;
```

### 16.6 Alertas recentes com impacto de performance
```sql
SELECT a.Nm_Alerta,
       a.Ds_Mensagem,
       a.Dt_Alerta,
       c.Nm_Contador,
       c.Media
FROM dbo.CheckAlerta a
LEFT JOIN dbo.CheckContadores c
    ON DATEPART(HOUR, a.Dt_Alerta) = c.Hora
WHERE c.Nm_Contador IN ('CPU', 'BatchRequests')
ORDER BY a.Dt_Alerta DESC;
```

---

> Use este arquivo como base para criar painéis ou relatórios no SQL Server Management Studio. Se você quiser, posso também gerar um conjunto de `CREATE VIEW` para cada área de análise.
