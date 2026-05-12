# Insight Queries para TraceQueriesDemoradas

Este documento traz sugestões de consultas para analisar os dados que estão sendo salvos pelo processo de trace de queries demoradas.

## 1. Consultar dados salvos recentemente

```sql
USE SafeBase;
GO

SELECT TOP 100
    Id,
    StartTime,
    EndTime,
    Duration,
    Reads,
    Writes,
    CPU,
    ServerName,
    DataBaseName,
    ApplicationName,
    HostName,
    LoginName,
    JobName,
    ClientIp,
    QueryHash,
    QueryPlanHash
FROM dbo.ResultadoTraceLog
ORDER BY StartTime DESC;
```

## 2. Top 20 queries mais demoradas

```sql
USE SafeBase;
GO

SELECT TOP 20
    TextData,
    Duration,
    StartTime,
    EndTime,
    ServerName,
    DataBaseName,
    ApplicationName,
    JobName,
    ClientIp
FROM dbo.ResultadoTraceLog
ORDER BY Duration DESC;
```

## 3. Contagem por aplicação e IP

```sql
USE SafeBase;
GO

SELECT
    ApplicationName,
    ClientIp,
    COUNT(*) AS TotalTraces,
    AVG(Duration) AS MediaDuration,
    MAX(Duration) AS MaxDuration
FROM dbo.ResultadoTraceLog
GROUP BY ApplicationName, ClientIp
ORDER BY TotalTraces DESC, MediaDuration DESC;
```

## 4. Queries por JobName

```sql
USE SafeBase;
GO

SELECT
    JobName,
    COUNT(*) AS TotalTraces,
    AVG(Duration) AS MediaDuration,
    SUM(CPU) AS TotalCPU,
    MAX(Duration) AS MaxDuration
FROM dbo.ResultadoTraceLog
GROUP BY JobName
ORDER BY TotalTraces DESC;
```

## 5. Piores ofensores por host

```sql
USE SafeBase;
GO

SELECT
    HostName,
    COUNT(*) AS TotalTraces,
    AVG(Duration) AS MediaDuration,
    MAX(Duration) AS MaxDuration,
    SUM(CPU) AS TotalCPU
FROM dbo.ResultadoTraceLog
GROUP BY HostName
ORDER BY MaxDuration DESC, MediaDuration DESC;
```

## 6. Categorizar hosts de funcionários

```sql
USE SafeBase;
GO

SELECT
    HostName,
    COUNT(*) AS TotalTraces,
    AVG(Duration) AS MediaDuration,
    MAX(Duration) AS MaxDuration,
    SUM(CPU) AS TotalCPU,
    CASE
        WHEN HostName IN ('VSLOC-055', 'FPC-1200') THEN 'Funcionario'
        WHEN HostName LIKE 'FPC-%' OR HostName LIKE 'VSLOC-%' THEN 'Funcionario'
        ELSE 'Outro'
    END AS CategoriaHost
FROM dbo.ResultadoTraceLog
WHERE StartTime >= DATEADD(WEEK, -1, GETDATE())
GROUP BY HostName
ORDER BY CategoriaHost, MaxDuration DESC, MediaDuration DESC;
```

## 7. Detalhes de hosts FPC/VSLOC

```sql
USE SafeBase;
GO

SELECT
    HostName,
    ApplicationName,
    LoginName,
    FORMAT(StartTime, 'yyyy-dd-MM') AS DataExecucao,
    COUNT(*) AS TotalTraces,
    SUM(CPU) AS TotalCPU,
    AVG(Duration) AS MediaDuration,
    MAX(Duration) AS MaxDuration,
    SUM(Reads) AS TotalReads,
    SUM(Writes) AS TotalWrites,
    STRING_AGG(
        CONCAT(
            'SPID=', CAST(td.SPID AS VARCHAR(10)),
            ';Duration=', CAST(td.Duration AS VARCHAR(20)),
            ';IP=', ISNULL(conn.client_net_address, 'NULL'),
            ';QueryHash=', ISNULL(CONVERT(VARCHAR(50), td.QueryHash, 1), 'NULL'),
            ';SQL=', LEFT(REPLACE(REPLACE(td.TextData, CHAR(13), ' '), CHAR(10), ' '), 200)
        ),
        ' | '
    ) WITHIN GROUP (ORDER BY td.Duration DESC) AS OffendersDetails
FROM dbo.ResultadoTraceLog td
OUTER APPLY (
    SELECT TOP 1 client_net_address
    FROM sys.dm_exec_connections c
    WHERE c.session_id = td.SPID
) conn
WHERE HostName LIKE 'FPC-%'
   OR HostName LIKE 'VSLOC-%'
GROUP BY HostName, ApplicationName, LoginName, FORMAT(StartTime, 'yyyy-dd-MM')
ORDER BY HostName, DataExecucao, TotalTraces DESC;
```

## 8. Top 10 queries por host

```sql
USE SafeBase;
GO

WITH HostQueryStats AS (
    SELECT
        HostName,
        TextData,

        Duration,
        ROW_NUMBER() OVER (PARTITION BY HostName ORDER BY Duration DESC) AS RowNum
    FROM dbo.ResultadoTraceLog
)

SELECT
    HostName,
    TextData,
    Duration
FROM HostQueryStats
WHERE RowNum <= 10
ORDER BY HostName, Duration DESC;
```

## 8. Identificar arquivos de trace que geram erros ou delays

```sql
USE SafeBase;
GO

SELECT
    JobName,
    ApplicationName,
    ClientIp,
    COUNT(*) AS TotalRegistros,
    AVG(Duration) AS MediaDuration,
    MAX(Duration) AS MaxDuration,
    MIN(StartTime) AS PrimeiroRegistro,
    MAX(StartTime) AS UltimoRegistro
FROM dbo.ResultadoTraceLog
GROUP BY JobName, ApplicationName, ClientIp
HAVING AVG(Duration) > 7000
ORDER BY MaxDuration DESC;
```

## 9. Comparar query hash e plano hash

```sql
USE SafeBase;
GO

SELECT
    QueryHash,
    QueryPlanHash,
    COUNT(*) AS TotalExecucoes,
    AVG(Duration) AS MediaDuration,
    MIN(Duration) AS MenorDuration,
    MAX(Duration) AS MaiorDuration
FROM dbo.ResultadoTraceLog
GROUP BY QueryHash, QueryPlanHash
ORDER BY TotalExecucoes DESC;
```

## 10. Identificar traços que ainda não têm IP resolvido

```sql
USE SafeBase;
GO

SELECT
    COUNT(*) AS TotalSemIP,
    COUNT(*) * 1.0 / (SELECT COUNT(*) FROM dbo.ResultadoTraceLog) AS PercentualSemIP
FROM dbo.ResultadoTraceLog
WHERE ClientIp IS NULL;
```

## 11. Análise de tendências por hora do dia

```sql
USE SafeBase;
GO

SELECT
    DATEPART(HOUR, StartTime) AS Hora,
    COUNT(*) AS TotalTraces,
    AVG(Duration) AS MediaDuration
FROM dbo.ResultadoTraceLog
GROUP BY DATEPART(HOUR, StartTime)
ORDER BY Hora;
```

## 12. Validação de planos de query diferentes para mesma query hash

```sql
USE SafeBase;
GO

SELECT
    QueryHash,
    QueryPlanHash,
    COUNT(*) AS TotalRegistros,
    AVG(Duration) AS MediaDuration
FROM dbo.ResultadoTraceLog
GROUP BY QueryHash, QueryPlanHash
HAVING COUNT(DISTINCT QueryPlanHash) > 1
ORDER BY QueryHash;
```

## 13. Detalhes completos de uma query específica

```sql
USE SafeBase;
GO

SELECT TOP 1
    TextData,
    Duration,
    StartTime,
    EndTime,
    Reads,
    Writes,
    CPU,
    ServerName,
    DataBaseName,
    ApplicationName,
    HostName,
    LoginName,
    JobName,
    ClientIp,
    QueryHash,
    QueryPlanHash
FROM dbo.ResultadoTraceLog
WHERE QueryHash = 0x0123456789ABCDEF -- substitua pelo hash desejado
ORDER BY StartTime DESC;
```

---

### Observações

- `ClientIp` só estará preenchido se a sessão correspondente ainda existir em `sys.dm_exec_connections` no momento do processamento.
- Se a coluna `JobName` ou `ClientIp` ainda não existir na tabela real do banco, será preciso ajustar o `ALTER TABLE` correspondente antes de usar as consultas.
- Use `TOP` e `ORDER BY` para restringir resultados em tabelas volumosas.
