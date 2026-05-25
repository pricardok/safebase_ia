
--
SELECT * FROM [JobAudit]

-- Cria tabela de monitoramento no safebase
CREATE TABLE [dbo].[JobAudit] (
    [IdAuditoria] [INT] IDENTITY(1,1) PRIMARY KEY CLUSTERED NOT NULL,
    [DataEvento] [DATETIME] NULL DEFAULT (GETDATE()),
    [DsUsuario] [VARCHAR](50) NULL,
    [DsJob] [sysname] NULL,
    [DsHostname] [VARCHAR](50) NULL,
    [DsQuery] [VARCHAR](MAX) NULL,
    [FlSituacao] [TINYINT] NULL,
    [NotificacaoEnviadaAPI] bit DEFAULT 0
)
WITH (DATA_COMPRESSION=PAGE)

-- Cria a Trigger
USE [msdb]
GO

/***************************************************************************************************
-- Trigger para os Jobs
***************************************************************************************************/
IF ((SELECT COUNT(*) FROM sys.triggers WHERE name = 'trgJobsStatus') > 0) DROP TRIGGER dbo.trgJobsStatus
GO

CREATE TRIGGER trgJobsStatus ON sysjobs
AFTER INSERT, UPDATE, DELETE AS
BEGIN
    
    
    SET NOCOUNT ON  


    DECLARE 
        @UserName VARCHAR(50) = SYSTEM_USER, 
        @HostName VARCHAR(50) = HOST_NAME(),  
        @JobName sysname,  
        @New_Enabled INT,  
        @Old_Enabled INT,  
        @ExecStr VARCHAR(100),
        @Qry VARCHAR(MAX)

        
    SELECT @New_Enabled = [enabled] FROM Inserted
    SELECT @Old_Enabled = [enabled] FROM Deleted
    SELECT @JobName = [name] FROM Deleted


    IF (@JobName IS NULL)
        SELECT @JobName = [name] FROM Deleted


    -- Identificando a query executada
    CREATE TABLE #inputbuffer (
        [EventType] NVARCHAR(60), 
        [Parameters] INT, 
        [EventInfo] VARCHAR(MAX)
    )

    SET @ExecStr = 'DBCC INPUTBUFFER(' + STR(@@SPID) + ')'

    INSERT INTO #inputbuffer 
    EXEC (@ExecStr)

    SET @Qry = (SELECT EventInfo FROM #inputbuffer)


    -- Verifica se houve alteração de status
    IF (@New_Enabled != @Old_Enabled)
    BEGIN  
        

        IF (@New_Enabled = 1)
        BEGIN  

            INSERT INTO safebase.dbo.JobAudit ( DsUsuario, DsJob, DsHostname, DsQuery, FlSituacao )
            SELECT @UserName, @JobName, @HostName, @Qry, 1

        END  


        IF (@New_Enabled = 0)
        BEGIN  
            
            INSERT INTO safebase.dbo.JobAudit ( DsUsuario, DsJob, DsHostname, DsQuery, FlSituacao )
            SELECT @UserName, @JobName, @HostName, @Qry, 0

        END  

    END
    ELSE BEGIN

        INSERT INTO safebase.dbo.JobAudit ( DsUsuario, DsJob, DsHostname, DsQuery )
        SELECT @UserName, @JobName, @HostName, @Qry

    END

END
GO

/***************************************************************************************************
-- Trigger para os Schedules dos Jobs
***************************************************************************************************/
IF ((SELECT COUNT(*) FROM sys.triggers WHERE name = 'trgAuditSchedules') > 0) DROP TRIGGER dbo.trgAuditSchedules
GO

CREATE TRIGGER [dbo].[trgAuditSchedules] ON [dbo].[sysschedules]
AFTER UPDATE, DELETE 
AS
BEGIN
    
    
    SET NOCOUNT ON  


    DECLARE 
        @UserName VARCHAR(50) = SYSTEM_USER, 
        @HostName VARCHAR(50) = HOST_NAME(),  
        @JobName VARCHAR(MAX) = '',  
        @ExecStr VARCHAR(100),
        @Qry VARCHAR(MAX)


    IF ((SELECT COUNT(*) FROM Inserted) > 0)
    BEGIN

        SELECT @JobName += (CASE WHEN @JobName != '' THEN ' | ' ELSE '' END) + A.[name]
        FROM msdb.dbo.sysjobs A
        JOIN msdb.dbo.sysjobschedules B ON A.job_id = B.job_id
        JOIN Inserted C ON B.schedule_id = C.schedule_id

    END
    ELSE BEGIN

        SELECT @JobName += (CASE WHEN @JobName != '' THEN ' | ' ELSE '' END) + A.[name]
        FROM msdb.dbo.sysjobs A
        JOIN msdb.dbo.sysjobschedules B ON A.job_id = B.job_id
        JOIN Deleted C ON B.schedule_id = C.schedule_id

    END

        
    -- Identificando a query executada
    CREATE TABLE #inputbuffer (
        [EventType] NVARCHAR(60), 
        [Parameters] INT, 
        [EventInfo] VARCHAR(MAX)
    )

    SET @ExecStr = 'DBCC INPUTBUFFER(' + STR(@@SPID) + ')'

    INSERT INTO #inputbuffer 
    EXEC (@ExecStr)

    SET @Qry = (SELECT EventInfo FROM #inputbuffer)

    IF (@JobName != '')
    BEGIN
    
        INSERT INTO safebase.dbo.JobAudit ( DsUsuario, DsJob, DsHostname, DsQuery )
        SELECT @UserName, @JobName, @HostName, @Qry

    END


END
GO

ALTER TABLE [dbo].[sysschedules] ENABLE TRIGGER [trgAuditSchedules]
GO

/***************************************************************************************************
-- Trigger para os Schedules
***************************************************************************************************/
IF ((SELECT COUNT(*) FROM sys.triggers WHERE name = 'trgAuditJobsSchedules') > 0) DROP TRIGGER dbo.trgAuditJobsSchedules
GO

CREATE TRIGGER [dbo].[trgAuditJobsSchedules] ON [dbo].[sysjobschedules]  
AFTER INSERT 
AS
BEGIN
    
    
    SET NOCOUNT ON  


    DECLARE 
        @UserName VARCHAR(50) = SYSTEM_USER, 
        @HostName VARCHAR(50) = HOST_NAME(),  
        @JobName sysname,  
        @ExecStr VARCHAR(100),
        @Qry VARCHAR(MAX)


    IF ((SELECT COUNT(*) FROM Inserted) > 0)
    BEGIN

        SELECT @JobName = A.[name]
        FROM msdb.dbo.sysjobs A
        JOIN Inserted B ON A.job_id = B.job_id

    END
    ELSE BEGIN

        SELECT @JobName = A.[name]
        FROM msdb.dbo.sysjobs A
        JOIN Deleted B ON A.job_id = B.job_id

    END

        
    -- Identificando a query executada
    CREATE TABLE #inputbuffer (
        [EventType] NVARCHAR(60), 
        [Parameters] INT, 
        [EventInfo] VARCHAR(MAX)
    )

    SET @ExecStr = 'DBCC INPUTBUFFER(' + STR(@@SPID) + ')'

    INSERT INTO #inputbuffer 
    EXEC (@ExecStr)

    SET @Qry = (SELECT EventInfo FROM #inputbuffer)

    
    INSERT INTO safebase.dbo.JobAudit ( DsUsuario, DsJob, DsHostname, DsQuery )
    SELECT @UserName, @JobName, @HostName, @Qry


END
GO

ALTER TABLE [dbo].[sysjobschedules] ENABLE TRIGGER [trgAuditJobsSchedules]
GO