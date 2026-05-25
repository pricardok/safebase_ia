/*
=================================================================================
-- PACOTE COMPLETO DE MONITORAMENTO - DBA (COM NOMES DE JOBS)
-- IDENTIFICA: Host, Aplicação (com nome real do Job), Usuário, Banco, Query, Recursos
=================================================================================
*/



-- 2 VISÃO GERAL DA TABELA * FROM SIMPLES
SELECT TOP 1000
    *
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
AND HostName = 'FAC-ROBOS64'
ORDER BY DataColeta DESC;

-- 2.0 VISÃO GERAL DA TABELA (ÚLTIMOS 7 DIAS) COM CLASSIFICAÇÕES
PRINT '========================================';
PRINT '0. VISÃO GERAL - ÚLTIMOS 7 DIAS';
PRINT '========================================';
SELECT TOP 100
    DataColeta,
    HostName,
    sb.fn_GetJobNameFromAgent(ApplicationName) AS ApplicationName,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    sb.fn_GetCommandType(QueryText) AS TipoComando,
    UserName,
    DatabaseName,
    Duracao_Seg,
    CPU_Seg,
    Leituras_Logicas,
    LEFT(QueryText, 100) AS QueryPreview
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
ORDER BY DataColeta ,CPU_Seg DESC, Duracao_Seg ;
GO

-- 2.1 RANKING DE APLICAÇÕES OFENSORAS (COM TIPO)
PRINT '========================================';
PRINT '1. RANKING DE APLICAÇÕES OFENSORAS';
PRINT '========================================';
SELECT TOP 20
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), 'Desconhecido') AS Aplicacao,
    ApplicationName,
    [HostName],
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Pior_Query_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras,
    COUNT(DISTINCT HostName) AS Qtd_Hosts,
    COUNT(DISTINCT UserName) AS Qtd_Usuarios,
    CASE 
        WHEN SUM(Duracao_Seg) > 3600 THEN 'CRÍTICO (>1 hora/dia)'
        WHEN SUM(Duracao_Seg) > 600 THEN 'ATENÇÃO (>10 min/dia)'
        WHEN SUM(Duracao_Seg) > 60 THEN 'MONITORAR (>1 min/dia)'
        ELSE 'OK'
    END AS Classificacao
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY ApplicationName,[HostName]
ORDER BY Tempo_Total_Seg DESC;
GO

-- 2.2 RANKING DE HOSTS OFENSORES
PRINT '========================================';
PRINT '2. RANKING DE HOSTS OFENSORES';
PRINT '========================================';
SELECT TOP 20
    ISNULL(HostName, 'Desconhecido') AS Host,
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), 'Desconhecido') AS Aplicacao,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Pior_Query_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras,
    COUNT(DISTINCT DatabaseName) AS Bancos_Afetados,
    COUNT(DISTINCT UserName) AS Usuarios_Diferentes
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY HostName, ApplicationName
ORDER BY Tempo_Total_Seg DESC;
GO

-- 2.3 RANKING DE USUÁRIOS OFENSORES
PRINT '========================================';
PRINT '3. RANKING DE USUÁRIOS OFENSORES';
PRINT '========================================';
SELECT --TOP 20
    ISNULL(UserName, 'Desconhecido') AS Usuario,
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), 'Desconhecido') AS Aplicacao,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    ISNULL(HostName, 'Desconhecido') AS Host,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Pior_Query_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras
FROM dbo.HistoricoQueriesLentas
WHERE 1 = 1
AND DataColeta >= DATEADD(DAY, -7, GETDATE())
AND HostName LIKE 'FPC%' AND HostName <> 'FPC-2571'
GROUP BY UserName, ApplicationName, HostName
ORDER BY Tempo_Total_Seg DESC;
GO

-- 2.4 RANKING DE BANCOS MAIS AFETADOS
PRINT '========================================';
PRINT '4. RANKING DE BANCOS MAIS AFETADOS';
PRINT '========================================';
SELECT TOP 20
    ISNULL(DatabaseName, 'Desconhecido') AS Banco,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Pior_Query_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras,
    COUNT(DISTINCT ApplicationName) AS Aplicacoes_Diferentes,
    COUNT(DISTINCT HostName) AS Hosts_Diferentes
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY DatabaseName
ORDER BY Tempo_Total_Seg DESC;
GO

-- 2.5 RANKING POR TIPO DE COMANDO
PRINT '========================================';
PRINT '5. RANKING POR TIPO DE COMANDO';
PRINT '========================================';
SELECT TOP 10
    sb.fn_GetCommandType(QueryText) AS TipoComando,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY sb.fn_GetCommandType(QueryText)
ORDER BY Tempo_Total_Seg DESC;
GO

-- ============================================================
-- PARTE 3: ANÁLISES DETALHADAS
-- ============================================================

-- 3.1 QUERIES MAIS LENTAS (COM TODAS CLASSIFICAÇÕES)
PRINT '========================================';
PRINT '6. TOP 30 QUERIES MAIS LENTAS (DETALHADO)';
PRINT '========================================';
SELECT TOP 30
    FORMAT(DataColeta, 'dd/MM HH:mm:ss') AS DataHora,
    CAST(Duracao_Seg AS DECIMAL(10,2)) AS [Segundos],
    CAST(CPU_Seg AS DECIMAL(10,2)) AS [CPU_Seg],
    ISNULL(HostName, '?') AS Host,
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), '?') AS App,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    sb.fn_GetCommandType(QueryText) AS TipoComando,
    ISNULL(UserName, '?') AS Usuario,
    ISNULL(DatabaseName, '?') AS Banco,
    SessionId,
    ClientPID,
    CASE 
        WHEN Duracao_Seg > 10 THEN 'CRÍTICA'
        WHEN Duracao_Seg > 5 THEN 'MUITO LENTA'
        WHEN Duracao_Seg > 1 THEN 'LENTA'
        ELSE 'ATENÇÃO'
    END AS Nivel,
    LEFT(QueryText, 200) AS Query
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
ORDER BY Duracao_Seg DESC;
GO

-- 3.2 QUERIES COM MAIS I/O (SCANS PROBLEMÁTICOS)
PRINT '========================================';
PRINT '7. TOP 30 QUERIES COM MAIS I/O';
PRINT '========================================';
SELECT TOP 30
    FORMAT(DataColeta, 'dd/MM HH:mm:ss') AS DataHora,
    CAST(Leituras_Logicas / 1000000.0 AS DECIMAL(18,2)) AS [Leituras_Milhoes],
    CAST(Duracao_Seg AS DECIMAL(10,2)) AS [Segundos],
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), '?') AS App,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    ISNULL(HostName, '?') AS Host,
    ISNULL(UserName, '?') AS Usuario,
    CASE 
        WHEN Leituras_Logicas > 10000000 THEN 'SCAN GIGANTE (>10M)'
        WHEN Leituras_Logicas > 1000000 THEN 'SCAN GRANDE (>1M)'
        WHEN Leituras_Logicas > 100000 THEN 'MUITAS LEITURAS (>100K)'
        ELSE 'NORMAL'
    END AS Impacto,
    LEFT(QueryText, 200) AS Query
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
  AND Leituras_Logicas > 100000
ORDER BY Leituras_Logicas DESC;
GO

-- 3.3 QUERIES COM PADRÕES PROBLEMÁTICOS
PRINT '========================================';
PRINT '8. QUERIES COM PADRÕES PROBLEMÁTICOS';
PRINT '========================================';
SELECT 
    FORMAT(DataColeta, 'dd/MM HH:mm:ss') AS DataHora,
    CAST(Duracao_Seg AS DECIMAL(10,2)) AS [Segundos],
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), '?') AS App,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    ISNULL(HostName, '?') AS Host,
    Problema,
    LEFT(QueryText, 200) AS Query
FROM (
    SELECT 
        DataColeta,
        Duracao_Seg,
        ApplicationName,
        HostName,
        QueryText,
        CASE 
            WHEN QueryText LIKE '%SELECT *%' THEN 'SELECT * (evitar)'
            WHEN QueryText LIKE '%NOLOCK%' THEN 'NOLOCK (pode causar leitura suja)'
            WHEN QueryText LIKE '%[_]fn[_]%' OR QueryText LIKE '%dbo.fn[_]%' THEN 'FUNÇÃO ESCALAR (lenta)'
            WHEN QueryText LIKE '%[_]VW%' OR QueryText LIKE '%[_]view%' THEN 'VIEW (pode estar mal indexada)'
            WHEN QueryText LIKE '%INSERT%SELECT%' THEN 'INSERT SELECT (pode travar)'
            WHEN QueryText LIKE '%LIKE ''%%%''%' THEN 'LIKE com % (não usa índice)'
            WHEN QueryText LIKE '%ORDER BY%' AND QueryText NOT LIKE '%TOP%' THEN 'ORDER BY sem TOP'
            WHEN QueryText LIKE '%CURSOR%' THEN 'CURSOR (lento e bloqueante)'
            WHEN QueryText LIKE '%WHILE%' THEN 'WHILE LOOP (prefira set-based)'
            ELSE NULL
        END AS Problema
    FROM dbo.HistoricoQueriesLentas
    WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
) t
WHERE Problema IS NOT NULL
ORDER BY Duracao_Seg DESC;
GO

-- ============================================================
-- PARTE 4: ANÁLISE DE TENDÊNCIA E PADRÕES
-- ============================================================

-- 4.1 ANÁLISE POR HORA DO DIA
PRINT '========================================';
PRINT '9. ANÁLISE POR HORA DO DIA';
PRINT '========================================';
SELECT 
    DATEPART(HOUR, DataColeta) AS Hora,
    COUNT(*) AS Qtd_Queries,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Avg_Duracao_Seg,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Max_Duracao_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras,
    COUNT(DISTINCT sb.fn_GetJobNameFromAgent(ApplicationName)) AS Apps_Diferentes,
    REPLICATE('|', CAST(COUNT(*) / 10 AS INT)) AS Frequencia
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY DATEPART(HOUR, DataColeta)
ORDER BY Hora;
GO

-- 4.2 ANÁLISE POR TIPO DE APLICAÇÃO POR HORA
PRINT '========================================';
PRINT '10. ANÁLISE POR TIPO DE APLICAÇÃO X HORA';
PRINT '========================================';
SELECT 
    DATEPART(HOUR, DataColeta) AS Hora,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    COUNT(*) AS Qtd_Queries,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Avg_Duracao_Seg
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY DATEPART(HOUR, DataColeta), sb.fn_GetAppType(ApplicationName)
ORDER BY Hora, TipoApp;
GO

-- 4.3 EVOLUÇÃO DIÁRIA (ÚLTIMOS 7 DIAS)
PRINT '========================================';
PRINT '11. EVOLUÇÃO DIÁRIA - ÚLTIMOS 7 DIAS';
PRINT '========================================';
SELECT 
    CAST(DataColeta AS DATE) AS Dia,
    COUNT(*) AS Qtd_Queries,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Avg_Duracao,
    CAST(MAX(Duracao_Seg) AS DECIMAL(10,2)) AS Max_Duracao,
    CAST(AVG(CPU_Seg) AS DECIMAL(10,2)) AS Avg_CPU,
    SUM(Leituras_Logicas) AS Total_Leituras,
    COUNT(DISTINCT sb.fn_GetJobNameFromAgent(ApplicationName)) AS Apps_Diferentes,
    COUNT(DISTINCT HostName) AS Hosts_Diferentes,
    COUNT(DISTINCT UserName) AS Usuarios_Diferentes
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
GROUP BY CAST(DataColeta AS DATE)
ORDER BY Dia DESC;
GO

-- ============================================================
-- PARTE 5: DASHBOARD EXECUTIVO
-- ============================================================
PRINT '========================================';
PRINT '12. DASHBOARD EXECUTIVO';
PRINT '========================================';

-- Visão geral
SELECT 'RESUMO PERÍODO' AS Categoria, '' AS Metrica, '' AS Valor
UNION ALL
SELECT '------------------', '', ''
UNION ALL
SELECT 'TOTAL GERAL', 
    'Total de queries lentas',
    CAST(COUNT(*) AS VARCHAR) 
FROM dbo.HistoricoQueriesLentas WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
UNION ALL
SELECT 'TOTAL GERAL',
    'Tempo total (segundos)',
    CAST(CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
UNION ALL
SELECT 'TOTAL GERAL',
    'Média por query (seg)',
    CAST(CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
UNION ALL
SELECT 'TOTAL GERAL',
    'CPU total (segundos)',
    CAST(CAST(SUM(CPU_Seg) AS DECIMAL(18,2)) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
UNION ALL
SELECT 'TOTAL GERAL',
    'Tipos de aplicação',
    CAST(COUNT(DISTINCT sb.fn_GetAppType(ApplicationName)) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
UNION ALL
SELECT '', '', ''
UNION ALL
SELECT 'TOP OFENSORES', '', ''
UNION ALL
SELECT 'TOP OFENSORES',
    'Pior Aplicação',
    (SELECT TOP 1 sb.fn_GetJobNameFromAgent(ApplicationName) FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY ApplicationName ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT 'TOP OFENSORES',
    'Pior Tipo App',
    (SELECT TOP 1 sb.fn_GetAppType(ApplicationName) FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY ApplicationName ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT 'TOP OFENSORES',
    'Pior Host',
    (SELECT TOP 1 HostName FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY HostName ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT 'TOP OFENSORES',
    'Pior Usuário',
    (SELECT TOP 1 UserName FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY UserName ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT 'TOP OFENSORES',
    'Pior Banco',
    (SELECT TOP 1 DatabaseName FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY DatabaseName ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT 'TOP OFENSORES',
    'Tipo comando mais pesado',
    (SELECT TOP 1 sb.fn_GetCommandType(QueryText) FROM dbo.HistoricoQueriesLentas 
     WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) 
     GROUP BY sb.fn_GetCommandType(QueryText) ORDER BY SUM(Duracao_Seg) DESC)
UNION ALL
SELECT '', '', ''
UNION ALL
SELECT 'ALERTAS', '', ''
UNION ALL
SELECT 'ALERTAS',
    'Queries > 10 segundos',
    CAST(COUNT(*) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas 
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) AND Duracao_Seg > 10
UNION ALL
SELECT 'ALERTAS',
    'Queries > 5 segundos',
    CAST(COUNT(*) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas 
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) AND Duracao_Seg > 5
UNION ALL
SELECT 'ALERTAS',
    'Queries com scan > 1M reads',
    CAST(COUNT(*) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas 
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) AND Leituras_Logicas > 1000000
UNION ALL
SELECT 'ALERTAS',
    'Queries com scan > 10M reads',
    CAST(COUNT(*) AS VARCHAR)
FROM dbo.HistoricoQueriesLentas 
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE()) AND Leituras_Logicas > 10000000;
GO

-- ============================================================
-- PARTE 6: ALERTAS EM TEMPO REAL (ÚLTIMOS 15 MINUTOS)
-- ============================================================
PRINT '========================================';
PRINT '13. ALERTAS EM TEMPO REAL (ÚLTIMOS 15 MIN)';
PRINT '========================================';

DECLARE @UltimosMinutos INT = 15;

SELECT 
    FORMAT(DataColeta, 'HH:mm:ss') AS Hora,
    CAST(Duracao_Seg AS DECIMAL(10,2)) AS [Segundos],
    CAST(CPU_Seg AS DECIMAL(10,2)) AS [CPU_Seg],
    ISNULL(HostName, '?') AS Host,
    ISNULL(sb.fn_GetJobNameFromAgent(ApplicationName), '?') AS App,
    sb.fn_GetAppType(ApplicationName) AS TipoApp,
    sb.fn_GetCommandType(QueryText) AS TipoComando,
    ISNULL(UserName, '?') AS Usuario,
    ISNULL(DatabaseName, '?') AS Banco,
    SessionId,
    ClientPID,
    CASE 
        WHEN Duracao_Seg > 10 THEN 'ALERTA CRÍTICO (>10s)'
        WHEN Duracao_Seg > 5 THEN 'ALERTA ALTO (>5s)'
        WHEN Duracao_Seg > 2 THEN 'ALERTA MÉDIO (>2s)'
        ELSE 'INFO'
    END AS Alerta,
    LEFT(QueryText, 150) AS Query
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(MINUTE, -@UltimosMinutos, GETDATE())
ORDER BY Duracao_Seg DESC;
GO

-- ============================================================
-- PARTE 7: QUERIES REGRESSIVAS (PIORARAM COM O TEMPO)
-- ============================================================
PRINT '========================================';
PRINT '14. QUERIES COM REGRESSÃO (PIORARAM >50%)';
PRINT '========================================';

WITH Regressao AS (
    SELECT 
        QueryHash,
        MIN(QueryText) AS ExemploQuery,
        MIN(sb.fn_GetJobNameFromAgent(ApplicationName)) AS Aplicacao,
        MIN(sb.fn_GetAppType(ApplicationName)) AS TipoApp,
        AVG(CASE WHEN DataColeta >= DATEADD(DAY, -1, GETDATE()) THEN Duracao_Seg END) AS Duracao_UltimoDia,
        AVG(CASE WHEN DataColeta < DATEADD(DAY, -1, GETDATE()) AND DataColeta >= DATEADD(DAY, -7, GETDATE()) THEN Duracao_Seg END) AS Duracao_SemanaAnterior,
        COUNT(*) AS TotalExecucoes
    FROM dbo.HistoricoQueriesLentas
    WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
      AND QueryHash IS NOT NULL
      AND QueryHash != '0x0000000000000000'
    GROUP BY QueryHash
    HAVING AVG(CASE WHEN DataColeta >= DATEADD(DAY, -1, GETDATE()) THEN Duracao_Seg END) >
           AVG(CASE WHEN DataColeta < DATEADD(DAY, -1, GETDATE()) THEN Duracao_Seg END) * 1.5
           AND COUNT(*) > 5
)
SELECT TOP 20
    CAST(Duracao_UltimoDia AS DECIMAL(10,2)) AS [Duracao_Atual_Seg],
    CAST(Duracao_SemanaAnterior AS DECIMAL(10,2)) AS [Duracao_Anterior_Seg],
    CAST((Duracao_UltimoDia / NULLIF(Duracao_SemanaAnterior, 0) - 1) * 100 AS DECIMAL(10,1)) AS [Piorou_%],
    TotalExecucoes,
    Aplicacao,
    TipoApp,
    LEFT(ExemploQuery, 200) AS Query
FROM Regressao
WHERE Duracao_SemanaAnterior > 0.1
ORDER BY [Piorou_%] DESC;
GO

-- ============================================================
-- PARTE 8: EXTRA - QUERIES COM SELECT * (VIA EXTRACT DO XMLData)
-- ============================================================
PRINT '========================================';
PRINT '15. QUERIES COM SELECT * (ANTIPADRÃO)';
PRINT '========================================';

SELECT TOP 50
    FORMAT(DataColeta, 'dd/MM HH:mm:ss') AS DataHora,
    HostName,
    ApplicationName,
    UserName,
    DatabaseName,
    Duracao_Seg,
    Leituras_Logicas,
    -- Extrair tabela do SELECT *
    CASE 
        WHEN QueryText LIKE '%SELECT *%FROM%' THEN
            LTRIM(RTRIM(SUBSTRING(QueryText, 
                CHARINDEX('FROM', UPPER(QueryText)) + 4,
                CHARINDEX(' ', SUBSTRING(QueryText, CHARINDEX('FROM', UPPER(QueryText)) + 4, 100)))))
        ELSE 'N/A'
    END AS Tabela,
    LEFT(QueryText, 200) AS Query
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
  AND (QueryText LIKE '%SELECT * %' OR QueryText LIKE '%SELECT *%FROM%')
  AND QueryText NOT LIKE '%COUNT%'  -- Exclui COUNT(*)
ORDER BY Leituras_Logicas DESC;
GO

-- ============================================================
--  QUERIES COM MAIS LEITURAS (POSSÍVEL FALTA DE ÍNDICE)
-- ============================================================
PRINT '========================================';
PRINT '15.  QUERIES COM MAIS LEITURAS LÓGICAS (>100K)';
PRINT '========================================';

SELECT TOP 30
    FORMAT(DataColeta, 'dd/MM HH:mm:ss') AS DataHora,
    HostName,
    ApplicationName,
    UserName,
    DatabaseName,
    Duracao_Seg,
    Leituras_Logicas,
    CASE 
        WHEN Leituras_Logicas > 10000000 THEN 'SCAN GIGANTE'
        WHEN Leituras_Logicas > 1000000 THEN 'SCAN GRANDE'
        ELSE 'MUITAS LEITURAS'
    END AS Impacto,
    -- Sugestão de índice
    CASE 
        WHEN QueryText LIKE '%WHERE ARQUIVO%' THEN 'SUGESTÃO: Criar índice em ARQUIVO'
        WHEN QueryText LIKE '%WHERE CODIGO%' THEN 'SUGESTÃO: Criar índice em CODIGO'
        WHEN QueryText LIKE '%WHERE NOME%' THEN 'SUGESTÃO: Criar índice em NOME'
        ELSE 'SUGESTÃO: Analisar plano de execução'
    END AS SugestaoIndice,
    LEFT(QueryText, 200) AS Query
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
  AND Leituras_Logicas > 100000
ORDER BY Leituras_Logicas DESC;
GO

-- ============================================================
-- DETALHAMENTO POR APLICAÇÃO/HOST ESPECÍFICO
-- ============================================================
PRINT '========================================';
PRINT '16. ANÁLISE DETALHADA: Python em FAC-ROBOS64';
PRINT '========================================';

SELECT 
    HostName,
    ApplicationName,
    DatabaseName,
    COUNT(*) AS Qtd_Queries,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras,
    -- Contagem de SELECT *
    SUM(CASE WHEN QueryText LIKE '%SELECT *%' THEN 1 ELSE 0 END) AS Qtd_SelectStar,
    -- Queries com WHERE (bom sinal)
    SUM(CASE WHEN QueryText LIKE '%WHERE%' THEN 1 ELSE 0 END) AS Qtd_ComWhere,
    -- Queries sem WHERE (pode ser full scan)
    SUM(CASE WHEN QueryText LIKE '%SELECT%' AND QueryText NOT LIKE '%WHERE%' AND QueryText NOT LIKE '%INSERT%' THEN 1 ELSE 0 END) AS Qtd_SemWhere
FROM dbo.HistoricoQueriesLentas
WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
  AND HostName = 'FAC-ROBOS64'
  AND ApplicationName = 'Python'
GROUP BY HostName, ApplicationName, DatabaseName;
GO

-- ============================================================
-- EXTRAIR NOMES DE TABELAS DAS QUERIES
-- ============================================================
PRINT '========================================';
PRINT '17. TOP 20 TABELAS MAIS ACESSADAS (POR QUERIES LENTAS)';
PRINT '========================================';

WITH TabelasExtraidas AS (
    SELECT 
        DataColeta,
        Duracao_Seg,
        Leituras_Logicas,
        QueryText,
        -- Extrai o primeiro nome de tabela após FROM ou JOIN
        CASE 
            WHEN UPPER(QueryText) LIKE '%FROM%' AND UPPER(QueryText) NOT LIKE '%FROM DUAL%' THEN
                LTRIM(RTRIM(
                    LEFT(
                        LTRIM(RTRIM(SUBSTRING(QueryText, CHARINDEX('FROM', UPPER(QueryText)) + 4, 100))),
                        CHARINDEX(' ', LTRIM(RTRIM(SUBSTRING(QueryText, CHARINDEX('FROM', UPPER(QueryText)) + 4, 100))) + ' ') - 1
                    )
                ))
            WHEN UPPER(QueryText) LIKE '%JOIN%' THEN
                LTRIM(RTRIM(
                    LEFT(
                        LTRIM(RTRIM(SUBSTRING(QueryText, CHARINDEX('JOIN', UPPER(QueryText)) + 4, 100))),
                        CHARINDEX(' ', LTRIM(RTRIM(SUBSTRING(QueryText, CHARINDEX('JOIN', UPPER(QueryText)) + 4, 100))) + ' ') - 1
                    )
                ))
            ELSE NULL
        END AS Tabela
    FROM dbo.HistoricoQueriesLentas
    WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
      AND QueryText LIKE '%SELECT%'
)
SELECT TOP 20
    Tabela,
    COUNT(*) AS Qtd_Consultas,
    CAST(SUM(Duracao_Seg) AS DECIMAL(18,2)) AS Tempo_Total_Seg,
    CAST(AVG(Duracao_Seg) AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    SUM(Leituras_Logicas) AS Total_Leituras
FROM TabelasExtraidas
WHERE Tabela IS NOT NULL 
  AND Tabela NOT LIKE '%(%'  -- Remove parênteses
  AND LEN(Tabela) < 100       -- Nomes de tabela válidos
GROUP BY Tabela
ORDER BY Tempo_Total_Seg DESC;
GO

-- ============================================================
-- RECOMENDAÇÕES POR TABELA (SIMULA DMV DE ÍNDICES)
-- ============================================================
PRINT '========================================';
PRINT '18. RECOMENDAÇÕES DE ÍNDICES POR TABELA';
PRINT '========================================';

-- Simula o que o Query Store faria para sugerir índices
WITH PossiveisIndices AS (
    SELECT 
        DatabaseName,
        -- Extrai nome da tabela
        CASE 
            WHEN QueryText LIKE '%FROM PROCESSO_OCR_LOGS_ARQUIVOS%' THEN 'PROCESSO_OCR_LOGS_ARQUIVOS'
            WHEN QueryText LIKE '%FROM CLICKSIGN_FLUXO_ASSINATURAS%' THEN 'CLICKSIGN_FLUXO_ASSINATURAS'
            WHEN QueryText LIKE '%FROM AUXILIO_FINANCEIRO%' THEN 'AUXILIO_FINANCEIRO'
            WHEN QueryText LIKE '%FROM SITE_DOCUMENTO_TABELA%' THEN 'SITE_DOCUMENTO_TABELA'
            ELSE 'OUTRA_TABELA'
        END AS Tabela,
        -- Extrai coluna do WHERE
        CASE 
            WHEN QueryText LIKE '%WHERE ARQUIVO%' THEN 'ARQUIVO'
            WHEN QueryText LIKE '%WHERE CODIGO%' THEN 'CODIGO'
            WHEN QueryText LIKE '%WHERE DATA%' THEN 'DATA_CADASTRO, DATAINICIO, DATAFIM'
            WHEN QueryText LIKE '%WHERE CONVENIO%' THEN 'CONVENIO'
            WHEN QueryText LIKE '%WHERE AVERBADOR%' THEN 'AVERBADOR'
            ELSE 'COLUNA NÃO IDENTIFICADA'
        END AS ColunaSugerida,
        COUNT(*) AS Qtd_Consultas,
        SUM(Leituras_Logicas) AS Total_Leituras,
        AVG(Duracao_Seg) AS Tempo_Medio_Seg
    FROM dbo.HistoricoQueriesLentas
    WHERE DataColeta >= DATEADD(DAY, -7, GETDATE())
      AND Leituras_Logicas > 10000
      AND QueryText LIKE '%SELECT%'
    GROUP BY DatabaseName, 
        CASE 
            WHEN QueryText LIKE '%FROM PROCESSO_OCR_LOGS_ARQUIVOS%' THEN 'PROCESSO_OCR_LOGS_ARQUIVOS'
            WHEN QueryText LIKE '%FROM CLICKSIGN_FLUXO_ASSINATURAS%' THEN 'CLICKSIGN_FLUXO_ASSINATURAS'
            WHEN QueryText LIKE '%FROM AUXILIO_FINANCEIRO%' THEN 'AUXILIO_FINANCEIRO'
            WHEN QueryText LIKE '%FROM SITE_DOCUMENTO_TABELA%' THEN 'SITE_DOCUMENTO_TABELA'
            ELSE 'OUTRA_TABELA'
        END,
        CASE 
            WHEN QueryText LIKE '%WHERE ARQUIVO%' THEN 'ARQUIVO'
            WHEN QueryText LIKE '%WHERE CODIGO%' THEN 'CODIGO'
            WHEN QueryText LIKE '%WHERE DATA%' THEN 'DATA_CADASTRO, DATAINICIO, DATAFIM'
            WHEN QueryText LIKE '%WHERE CONVENIO%' THEN 'CONVENIO'
            WHEN QueryText LIKE '%WHERE AVERBADOR%' THEN 'AVERBADOR'
            ELSE 'COLUNA NÃO IDENTIFICADA'
        END
)
SELECT 
    Tabela,
    ColunaSugerida,
    Qtd_Consultas,
    Total_Leituras,
    CAST(Tempo_Medio_Seg AS DECIMAL(10,2)) AS Tempo_Medio_Seg,
    'CREATE NONCLUSTERED INDEX IX_' + Tabela + '_' + REPLACE(ColunaSugerida, ', ', '_') + 
    ' ON ' + Tabela + ' (' + ColunaSugerida + ')' AS ScriptIndiceSugerido
FROM PossiveisIndices
WHERE Tabela != 'OUTRA_TABELA'
ORDER BY Total_Leituras DESC;
GO
