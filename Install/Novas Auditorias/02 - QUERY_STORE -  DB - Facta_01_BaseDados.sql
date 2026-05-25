-- PASSO 1: Use o banco master e limpe completamente
USE [master];
GO

-- Força desativação total
ALTER DATABASE [Facta_01_BaseDados] SET QUERY_STORE = OFF;
GO

-- Aguarda 5 segundos
WAITFOR DELAY '00:00:05';
GO

-- PASSO 2: Remove todos os dados residuais
ALTER DATABASE [Facta_01_BaseDados] SET QUERY_STORE CLEAR;
GO

-- PASSO 3: Ativa com configuração robusta (VERSÃO CORRIGIDA)
ALTER DATABASE [Facta_01_BaseDados] SET QUERY_STORE = ON;
GO

ALTER DATABASE [Facta_01_BaseDados] SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 4096,
    QUERY_CAPTURE_MODE = AUTO,
    SIZE_BASED_CLEANUP_MODE = AUTO,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 15),
    INTERVAL_LENGTH_MINUTES = 30,
    DATA_FLUSH_INTERVAL_SECONDS = 900
);
GO

-- PASSO 4: Verifica se tudo deu certo
USE [Facta_01_BaseDados];
GO
SELECT 
    desired_state_desc AS [Status Configurado],
    actual_state_desc AS [Status Atual],
    current_storage_size_mb AS [Espaço Usado (MB)],
    max_storage_size_mb AS [Limite (MB)],
    query_capture_mode_desc AS [Modo Captura],
    (SELECT STALE_QUERY_THRESHOLD_DAYS FROM sys.database_query_store_options) AS [Retenção (dias)]
FROM sys.database_query_store_options;
GO


-- Top 20 queries que mais consumiram CPU nas últimas 24h
-- Ideal para: "Time dev, essas queries estão derretendo a CPU do servidor"
SELECT TOP 20
    q.query_id,
    qt.query_sql_text AS [Query Text],
    OBJECT_NAME(q.object_id) AS [Objeto/Proc],
    q.last_execution_time AS [Última Execução],
    SUM(rs.avg_cpu_time * rs.count_executions) / 1000000.0 AS [Total CPU (segundos)],
    SUM(rs.count_executions) AS [Execuções],
    AVG(rs.avg_cpu_time / 1000.0) AS [CPU Médio (ms)],
    MAX(rs.max_cpu_time / 1000.0) AS [CPU Máximo (ms)]
FROM sys.query_store_query_text qt
INNER JOIN sys.query_store_query q ON qt.query_text_id = q.query_text_id
INNER JOIN sys.query_store_plan p ON q.query_id = p.query_id
INNER JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
WHERE rs.last_execution_time > DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY q.query_id, qt.query_sql_text, OBJECT_NAME(q.object_id), q.last_execution_time
ORDER BY [Total CPU (segundos)] DESC;
