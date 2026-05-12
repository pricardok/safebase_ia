USE [SafeBase]
GO

ALTER PROCEDURE [sb].[stpTraceQueriesDemoradas]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id_AlertaParametro INT = (
        SELECT Id_AlertaParametro
        FROM [dbo].AlertaParametro (NOLOCK)
        WHERE Nm_Alerta = 'Trace Queries Demoradas'
          AND Ativo = 1
    );

    DECLARE @Ds_Caminho_Base VARCHAR(256) = (
        SELECT Ds_Caminho
        FROM [dbo].AlertaParametro (NOLOCK)
        WHERE Nm_Alerta = 'CheckList'
    );

    DECLARE @Trace_Id INT;
    DECLARE @Path VARCHAR(MAX);
    DECLARE @CaminhoPath NVARCHAR(256);
    DECLARE @ca NVARCHAR(512);

    -- Pega o caminho base dos logs
    SELECT @CaminhoPath = Ds_Caminho_Log
    FROM [dbo].[AlertaParametro]
    WHERE [Id_AlertaParametro] = @Id_AlertaParametro;

    SET @ca = RTRIM(@Ds_Caminho_Base);
    IF RIGHT(@ca, 1) IN ('\\', '/')
        SET @ca = LEFT(@ca, LEN(@ca) - 1);

    SET @CaminhoPath = LTRIM(@CaminhoPath);
    IF LEFT(@CaminhoPath, 1) IN ('\\', '/')
        SET @CaminhoPath = SUBSTRING(@CaminhoPath, 2, LEN(@CaminhoPath) - 1);

    SET @ca = @ca + '\\' + @CaminhoPath;

    -- Normaliza o caminho do arquivo de trace
    WHILE RIGHT(@ca, 8) = '.trc.trc'
        SET @ca = LEFT(@ca, LEN(@ca) - 4);

    IF RIGHT(@ca, 4) <> '.trc'
        SET @ca = @ca + '.trc';

    DECLARE @NormalizedPath NVARCHAR(512) = LOWER(REPLACE(@ca, '.trc.trc', '.trc'));

    -- Busca traces existentes com o mesmo caminho normalizado
    SELECT TOP 1
        @Trace_Id = id,
        @Path = [path]
    FROM sys.traces
    WHERE LOWER(REPLACE([path], '.trc.trc', '.trc')) LIKE '%' + @NormalizedPath;

    -- =============================================
    -- FECHA E PROCESSA O TRACE ANTIGO SE EXISTIR
    -- =============================================
    IF (@Trace_Id IS NOT NULL)
    BEGIN
        EXEC sys.sp_trace_setstatus @Trace_Id = @Trace_Id, @status = 0;
        WAITFOR DELAY '00:00:01';

        EXEC sys.sp_trace_setstatus @Trace_Id = @Trace_Id, @status = 2;
        WAITFOR DELAY '00:00:01';

        IF (OBJECT_ID('dbo.ResultadoTraceLog') IS NULL)
        BEGIN
            CREATE TABLE [safebase].[dbo].[ResultadoTraceLog] (
                [TextData] [text] NULL,
                [NTUserName] [varchar](128) NULL,
                [HostName] [varchar](128) NULL,
                [ApplicationName] [varchar](128) NULL,
                [LoginName] [varchar](128) NULL,
                [SPID] [int] NULL,
                [Duration] [numeric](15, 2) NULL,
                [StartTime] [datetime] NULL,
                [EndTime] [datetime] NULL,
                [Reads] [int] NULL,
                [Writes] [int] NULL,
                [CPU] [int] NULL,
                [ServerName] [varchar](128) NULL,
                [DataBaseName] [varchar](128) NULL,
                [RowCounts] [int] NULL,
                [SessionLoginName] [varchar](128) NULL,
                [JobName] [varchar](256) NULL,
                [ClientIp] [varchar](48) NULL,
                [QueryPlan] [xml] NULL,
                [QueryHash] [varbinary](8) NULL,
                [QueryPlanHash] [varbinary](8) NULL
            )
            WITH (DATA_COMPRESSION = PAGE);

            CREATE CLUSTERED INDEX [SK01_Traces]
                ON [safebase].[dbo].[ResultadoTraceLog] ([StartTime])
                WITH (FILLFACTOR=80, STATISTICS_NORECOMPUTE=ON, DATA_COMPRESSION = PAGE)
                ON [PRIMARY];

            CREATE NONCLUSTERED INDEX [IX_ResultadoTraceLog_JobName]
                ON [safebase].[dbo].[ResultadoTraceLog] ([JobName])
                WITH (DATA_COMPRESSION = PAGE);

            CREATE NONCLUSTERED INDEX [IX_ResultadoTraceLog_StartTime_Job]
                ON [safebase].[dbo].[ResultadoTraceLog] ([StartTime], [JobName])
                WITH (DATA_COMPRESSION = PAGE);
        END

        CREATE TABLE #TraceData (
            TextData NTEXT,
            NTUserName NVARCHAR(128),
            HostName NVARCHAR(128),
            ApplicationName NVARCHAR(128),
            LoginName NVARCHAR(128),
            SPID INT,
            Duration BIGINT,
            StartTime DATETIME,
            EndTime DATETIME,
            Reads BIGINT,
            Writes BIGINT,
            CPU INT,
            ServerName NVARCHAR(128),
            DatabaseName NVARCHAR(128),
            RowCounts BIGINT,
            SessionLoginName NVARCHAR(128)
        );

        INSERT INTO #TraceData
        SELECT
            TextData, NTUserName, HostName, ApplicationName, LoginName, SPID,
            Duration, StartTime, EndTime, Reads, Writes, CPU, ServerName,
            DatabaseName, RowCounts, SessionLoginName
        FROM ::fn_trace_gettable(@Path, DEFAULT)
        WHERE Duration IS NOT NULL
          AND Reads < 100000000
          AND StartTime > ISNULL((SELECT MAX(StartTime) FROM dbo.ResultadoTraceLog), '1900-01-01');

        INSERT INTO [safebase].[dbo].[ResultadoTraceLog] (
            TextData, NTUserName, HostName, ApplicationName, LoginName, SPID,
            Duration, StartTime, EndTime, Reads, Writes, CPU, ServerName,
            DataBaseName, RowCounts, SessionLoginName, JobName, ClientIp, QueryPlan,
            QueryHash, QueryPlanHash
        )
        SELECT
            td.TextData,
            td.NTUserName,
            td.HostName,
            td.ApplicationName,
            td.LoginName,
            td.SPID,
            CAST(td.Duration / 1000.0 / 1000.0 AS NUMERIC(15, 2)) Duration,
            td.StartTime,
            td.EndTime,
            td.Reads,
            td.Writes,
            td.CPU,
            td.ServerName,
            td.DatabaseName,
            td.RowCounts,
            td.SessionLoginName,
            [dbo].[fn_GetJobNameBySPID](td.SPID) AS JobName,
            conn.client_net_address AS ClientIp,
            qs.query_plan AS QueryPlan,
            qs.query_hash AS QueryHash,
            qs.query_plan_hash AS QueryPlanHash
        FROM #TraceData td
        OUTER APPLY (
            SELECT TOP 1
                qs.query_hash,
                qs.query_plan_hash,
                qp.query_plan
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
            CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
            WHERE qt.text = CAST(td.TextData AS NVARCHAR(MAX))
              AND qs.last_execution_time BETWEEN DATEADD(second, -30, td.StartTime)
                                            AND DATEADD(second, 30, td.StartTime)
            ORDER BY qs.last_execution_time DESC
        ) qs
        OUTER APPLY (
            SELECT TOP 1 client_net_address
            FROM sys.dm_exec_connections c
            WHERE c.session_id = td.SPID
        ) conn;

        DROP TABLE #TraceData;

        BEGIN TRY
            EXEC dbo.stpDeleteFile @Path;
        END TRY
        BEGIN CATCH
            -- Ignora se o arquivo não existir ou já estiver excluído
        END CATCH
    END

    -- Se ainda existir algum trace registrado para o mesmo caminho, encerra e remove todos
    DECLARE @ExistingTraceId INT;
    DECLARE @ExistingPath NVARCHAR(512);

    WHILE EXISTS (
        SELECT 1
        FROM sys.traces
        WHERE LOWER(REPLACE([path], '.trc.trc', '.trc')) LIKE '%' + @NormalizedPath
    )
    BEGIN
        SELECT TOP 1
            @ExistingTraceId = id,
            @ExistingPath = [path]
        FROM sys.traces
        WHERE LOWER(REPLACE([path], '.trc.trc', '.trc')) LIKE '%' + @NormalizedPath;

        BEGIN TRY
            EXEC sys.sp_trace_setstatus @ExistingTraceId, 0;
            WAITFOR DELAY '00:00:02';
            EXEC sys.sp_trace_setstatus @ExistingTraceId, 2;
            WAITFOR DELAY '00:00:02';
        END TRY
        BEGIN CATCH
            -- Ignora erro e continua para tentar encerrar outros traces
        END CATCH;

        BEGIN TRY
            IF @ExistingPath IS NOT NULL
                EXEC dbo.stpDeleteFile @ExistingPath;
        END TRY
        BEGIN CATCH
            -- Ignora se a exclusão do arquivo falhar
        END CATCH;
    END

    -- Remove o arquivo de destino antes de criar o novo trace
    BEGIN TRY
        EXEC dbo.stpDeleteFile @ca;
    END TRY
    BEGIN CATCH
        -- Ignora se o arquivo não existir ou a exclusão falhar
    END CATCH;

    BEGIN TRY
        DECLARE @DoublePath NVARCHAR(512) = @ca + '.trc';
        IF @DoublePath <> @ca
            EXEC dbo.stpDeleteFile @DoublePath;
    END TRY
    BEGIN CATCH
        -- Ignora se o arquivo não existir ou a exclusão falhar
    END CATCH;

    -- =============================================
    -- CRIA O NOVO TRACE
    -- =============================================
    DECLARE
        @resource INT,
        @maxfilesize BIGINT = 50,
        @on BIT = 1,
        @bigintfilter BIGINT = (1000000 * 7); -- 7 segundos

    SET @Trace_Id = NULL;

    EXEC @resource = sys.sp_trace_create @Trace_Id OUTPUT, 0, @ca, @maxfilesize, NULL;

    IF (@resource = 0)
    BEGIN
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 1, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 6, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 8, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 10, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 11, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 12, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 13, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 14, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 15, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 16, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 17, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 18, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 26, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 35, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 40, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 48, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 10, 64, @on;

        EXEC sys.sp_trace_setevent @Trace_Id, 12, 1, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 6, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 8, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 10, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 11, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 12, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 13, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 14, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 15, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 16, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 17, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 18, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 26, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 35, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 40, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 48, @on;
        EXEC sys.sp_trace_setevent @Trace_Id, 12, 64, @on;

        EXEC sys.sp_trace_setfilter @Trace_Id, 13, 0, 4, @bigintfilter;
        EXEC sys.sp_trace_setstatus @Trace_Id, 1;

        PRINT 'Trace criado com sucesso! ID: ' + CAST(@Trace_Id AS VARCHAR(20));
    END
    ELSE
    BEGIN
        PRINT 'Erro ao criar trace. Código: ' + CAST(@resource AS VARCHAR(20));
    END
END
GO
