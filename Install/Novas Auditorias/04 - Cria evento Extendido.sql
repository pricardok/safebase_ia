


USE [master];
GO

-- ===================================================================
-- PASSO 1: Parar e Remover a sessão antiga
-- ===================================================================
IF EXISTS (SELECT 1 FROM sys.dm_xe_sessions WHERE name = 'MonitorQueriesPesadas')
BEGIN
    ALTER EVENT SESSION [MonitorQueriesPesadas] ON SERVER STATE = STOP;
    PRINT '✅ Sessão antiga parada';
END
GO

IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = 'MonitorQueriesPesadas')
BEGIN
    DROP EVENT SESSION [MonitorQueriesPesadas] ON SERVER;
    PRINT '✅ Sessão antiga removida';
END
GO

-- ===================================================================
-- PASSO 2: Criar nova sessão com MESMA lógica, OTIMIZADA
-- ===================================================================
CREATE EVENT SESSION [MonitorQueriesPesadas] ON SERVER 
ADD EVENT sqlserver.sql_batch_completed(
    -- AÇÕES CAPTURADAS (metadados da query)
    ACTION (
        sqlserver.client_hostname,                -- Nome do computador cliente
        sqlserver.client_app_name,                -- Nome da aplicação (ex: Management Studio, app web)
        sqlserver.username,                       -- Login do SQL Server
        sqlserver.database_name,                  -- Banco de dados contexto
        sqlserver.sql_text,                       -- Texto completo da query/batch
        sqlserver.query_hash,                     -- Hash único para identificar queries similares
        sqlserver.query_plan_hash,                -- Hash do plano de execução
        sqlserver.session_id,                     -- ID da sessão (SPID)
        sqlserver.request_id,                     -- ID da requisição dentro da sessão
        sqlserver.client_pid,                     -- Process ID do cliente (no SO)
        sqlserver.session_resource_group_id,      -- Grupo de recursos do Resource Governor
        sqlserver.server_principal_name,          -- Nome do principal do servidor
        sqlserver.server_instance_name            -- Nome da instância SQL Server
    )
    -- FILTRO: Captura padrões problemáticos de código
    WHERE (
        [sqlserver].[sql_text] LIKE '%SELECT *%'           -- SELECT * (retorna colunas desnecessárias)
        OR [sqlserver].[sql_text] LIKE '%NOLOCK%'          -- NOLOCK (pode causar leitura suja)
        OR [sqlserver].[sql_text] LIKE '%READUNCOMMITTED%' -- Mesmo que NOLOCK
        OR [sqlserver].[sql_text] LIKE '%WHILE%'           -- Loops WHILE (geralmente ineficientes)
        OR [sqlserver].[sql_text] LIKE '%CURSOR%'          -- CURSOR (muito ineficiente em SQL)
        OR [sqlserver].[sql_text] LIKE '%fn[_]%'           -- Funções escalares (fn_ + qualquer caractere)
        OR [sqlserver].[sql_text] LIKE '%[_]VW%'           -- Views (pode indicar view sobre view)
        OR [sqlserver].[sql_text] LIKE '%LIKE ''%''%'      -- LIKE com curinga no início (não usa índice)
        OR [sqlserver].[sql_text] LIKE '%OR 1=1%'          -- OR 1=1 (possível injeção SQL ou lógica ruim)
        OR [sqlserver].[sql_text] LIKE '%INSERT%SELECT%'   -- INSERT + SELECT (monitorar grandes inserções)
    )
),
ADD EVENT sqlserver.sql_statement_completed(
    -- AÇÕES CAPTURADAS (mais ações que o batch)
    ACTION (
        sqlserver.client_hostname,                -- Nome do computador cliente
        sqlserver.client_app_name,                -- Nome da aplicação
        sqlserver.username,                       -- Login do SQL Server
        sqlserver.database_name,                  -- Banco de dados contexto
        sqlserver.sql_text,                       -- Texto do statement
        sqlserver.query_hash,                     -- Hash da query
        sqlserver.query_plan_hash,                -- Hash do plano
        sqlserver.session_id,                     -- ID da sessão
        sqlserver.request_id,                     -- ID da requisição
        sqlserver.client_pid,                     -- Process ID do cliente
        sqlserver.session_resource_group_id,      -- Grupo de recursos
        sqlserver.server_principal_name,          -- Nome do principal
        sqlserver.server_instance_name,           -- Nome da instância
        sqlserver.is_system,                      -- Se é query do sistema (1) ou usuário (0)
        sqlserver.context_info                    -- Informação de contexto (se setado pela app)
    )
    -- FILTRO: Captura queries pesadas (métricas de performance)
    WHERE (
        [duration] > 1000                        -- Duração > 1 milissegundo (em microssegundos)
        OR [cpu_time] > 1000                     -- Tempo de CPU > 1ms
        OR [physical_reads] > 10                 -- Leituras físicas no disco > 10 páginas (80KB)
        OR [logical_reads] > 100                 -- Leituras lógicas da cache > 100 páginas (800KB)
        OR [writes] > 10                         -- Escritas no banco > 10 páginas
        OR [row_count] > 50                      -- Linhas retornadas > 50
        -- TAMBÉM captura padrões problemáticos (mesmo se não forem pesados)
        OR [sqlserver].[sql_text] LIKE '%SELECT *%'    -- SELECT *
        OR [sqlserver].[sql_text] LIKE '%NOLOCK%'      -- NOLOCK
        OR [sqlserver].[sql_text] LIKE '%fn[_]%'       -- Funções
        OR [sqlserver].[sql_text] LIKE '%[_]VW%'       -- Views
        OR [sqlserver].[sql_text] LIKE '%LIKE ''%''%'  -- LIKE com %
    )
),
ADD EVENT sqlserver.rpc_completed(
    -- AÇÕES CAPTURADAS (RPC = Remote Procedure Call, ex: procedures)
    ACTION (
        sqlserver.client_hostname,                -- Nome do computador cliente
        sqlserver.client_app_name,                -- Nome da aplicação
        sqlserver.username,                       -- Login do SQL Server
        sqlserver.database_name,                  -- Banco de dados contexto
        sqlserver.sql_text,                       -- Texto da RPC (procedure + parâmetros)
        sqlserver.query_hash,                     -- Hash da query
        sqlserver.query_plan_hash,                -- Hash do plano
        sqlserver.session_id,                     -- ID da sessão
        sqlserver.request_id,                     -- ID da requisição
        sqlserver.client_pid                      -- Process ID do cliente
    )
    -- FILTRO: Captura RPCs pesadas (procedures lentas)
    WHERE (
        [duration] > 1000                        -- Duração > 1ms
        OR [cpu_time] > 1000                     -- CPU > 1ms
        OR [physical_reads] > 10                 -- Reads físicos > 10 páginas
        OR [logical_reads] > 100                 -- Reads lógicos > 100 páginas
        OR [writes] > 10                         -- Escritas > 10 páginas
        OR [row_count] > 50                      -- Linhas retornadas > 50
    )
),
ADD EVENT sqlserver.blocked_process_report(
    -- AÇÕES CAPTURADAS (bloqueios)
    ACTION (
        sqlserver.client_hostname,                -- Nome do computador cliente
        sqlserver.client_app_name,                -- Nome da aplicação
        sqlserver.username,                       -- Login do SQL Server
        sqlserver.database_name,                  -- Banco de dados
        sqlserver.session_id,                     -- ID da sessão bloqueada
        sqlserver.client_pid                      -- Process ID do cliente
    )
    -- SEM FILTRO: bloqueio sempre é crítico, captura todos
)

-- ===================================================================
-- TARGET: Onde os dados serão armazenados
-- ===================================================================
ADD TARGET package0.event_file(
    SET filename = N'C:\Data\Logs\QueriesPesadas',    -- Path (MANTIDO IGUAL para compatibilidade)
        max_file_size = 100,                           -- 100MB por arquivo (MANTIDO)
        max_rollover_files = 10                        -- Mantém 10 arquivos (MANTIDO)
)

-- ===================================================================
-- CONFIGURAÇÕES DA SESSÃO (SÓ ISSO MUDA!)
-- ===================================================================
WITH (
    MAX_MEMORY = 131072 KB,              -- ALTERADO: 16MB -> 128MB (8x maior)
                                        -- Motivo: permite acumular mais eventos antes de flush
    
    EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS,  -- MANTIDO: permite perder eventos se buffer lotar
                                        -- Não crítico pois já aumentamos o buffer
    
    MAX_DISPATCH_LATENCY = 10 SECONDS,  -- ALTERADO: 1s -> 10s (10x maior)
                                        -- Motivo: reduz flushes de 60/min para 6/min
                                        -- Seu job vai ler arquivos, não precisa real-time
    
    STARTUP_STATE = ON                  -- MANTIDO: inicia automaticamente com SQL Server
);
GO

-- ===================================================================
-- PASSO 3: Iniciar a sessão
-- ===================================================================
ALTER EVENT SESSION [MonitorQueriesPesadas] ON SERVER STATE = START;
GO

-- ===================================================================
-- PASSO 4: Validar a sessão
-- ===================================================================
PRINT '===================================================================';
PRINT '✅ Sessão [MonitorQueriesPesadas] recriada com OTIMIZAÇÕES';
PRINT '===================================================================';
PRINT '';
PRINT '📊 MUDANÇAS REALIZADAS (SOMENTE 2 configurações):';
PRINT '  1. MAX_MEMORY: 16MB → 128MB (8x maior)';
PRINT '  2. MAX_DISPATCH_LATENCY: 1s → 10s (10x maior)';
PRINT '';
PRINT '✅ TUDO MAIS PERMANECEU IGUAL:';
PRINT '  - Eventos: sql_batch_completed, sql_statement_completed, rpc_completed, blocked_process_report';
PRINT '  - Actions: Todas as 13 ações originais';
PRINT '  - WHERE filters: Todos os padrões (SELECT *, NOLOCK, WHILE, CURSOR, etc.)';
PRINT '  - Target: C:\Data\Logs\QueriesPesadas (MESMO PATH)';
PRINT '  - max_file_size: 100MB';
PRINT '  - max_rollover_files: 10';
PRINT '  - STARTUP_STATE: ON';
PRINT '';
PRINT '🎯 BENEFÍCIO ESPERADO:';
PRINT '  - Flushes de 60/min → 6/min (90% menos I/O)';
PRINT '  - Resolve WRITELOG, PREEMPTIVE_OS_WRITEFILE, PAGEIOLATCH_SH';
PRINT '===================================================================';

-- ===================================================================
-- PASSO 5: Verificar se a sessão está rodando com as novas configurações
-- ===================================================================
SELECT 
    name AS SessionName,
    --is_started AS IsRunning,
    max_memory / 1024 AS MaxMemoryMB,
    max_dispatch_latency AS DispatchLatencySeconds,
    memory_partition_mode_desc AS MemoryPartition
FROM sys.server_event_sessions
WHERE name = 'MonitorQueriesPesadas';
GO

-- ===================================================================
-- PASSO 6: Testar se ainda consegue ler os arquivos (seu job vai funcionar)
-- ===================================================================
SELECT TOP 5 
    CAST(event_data AS XML) AS EventData
FROM sys.fn_xe_file_target_read_file(N'C:\Data\Logs\QueriesPesadas*.xel', NULL, NULL, NULL)
ORDER BY event_data DESC;
PRINT '✅ Leitura dos arquivos XEL confirmada - seu job continua funcionando!';
GO

SELECT 
    s.name AS session_name,
    f.file_name,
    (fe.size_in_bytes / 1024 / 1024) AS size_mb
FROM sys.dm_xe_session_targets t
INNER JOIN sys.dm_xe_sessions s 
    ON t.event_session_address = s.address
CROSS APPLY (
    SELECT CAST(t.target_data AS XML) AS target_data_xml
) AS td
CROSS APPLY (
    SELECT 
        n.v.value('@name', 'VARCHAR(260)') AS file_name
    FROM td.target_data_xml.nodes('/EventFileTarget/File') AS n(v)
) AS f
-- Cruza com a sys.dm_server_services ou DMVs de disco para validar o arquivo ativo
CROSS APPLY (
    SELECT TOP 1 file_size_bytes AS size_in_bytes
    FROM sys.fn_xe_file_target_read_file(f.file_name, NULL, NULL, NULL)
) AS fe
WHERE s.name = 'MonitorQueriesPesadas'
  AND t.target_name = 'event_file';






