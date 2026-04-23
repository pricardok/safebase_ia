USE [msdb]
GO

/****** Object:  Job [MON - CheckList do Banco de Dados]    Script Date: 17/03/2020 14:56:40 ******/
BEGIN TRANSACTION
DECLARE @ReturnCode INT
SELECT @ReturnCode = 0
/****** Object:  JobCategory [Database Maintenance]    Script Date: 17/03/2020 14:56:40 ******/
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'Database Maintenance' AND category_class=1)
BEGIN
EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'Database Maintenance'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

END

DECLARE @jobId BINARY(16)
EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name=N'MON - CheckList do Banco de Dados', 
		@enabled=1, 
		@notify_level_eventlog=0, 
		@notify_level_email=0, 
		@notify_level_netsend=0, 
		@notify_level_page=0, 
		@delete_level=0, 
		@description=N'JOB responsável por enviar o E-Mail com o CheckList do Banco de Dados.', 
		@category_name=N'Database Maintenance', 
		@owner_login_name=N'sa', @job_id = @jobId OUTPUT
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
/****** Object:  Step [MON - Carga Tabelas CheckList]    Script Date: 17/03/2020 14:56:40 ******/
EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'MON - Carga Tabelas CheckList', 
		@step_id=1, 
		@cmdexec_success_code=0, 
		@on_success_action=3, 
		@on_success_step_id=0, 
		@on_fail_action=2, 
		@on_fail_step_id=0, 
		@retry_attempts=0, 
		@retry_interval=0, 
		@os_run_priority=0, @subsystem=N'TSQL', 
		@command=N'EXEC [dbo].[stpServerJob] ''CHECK_LIST_USED_DISC''; 			-- Espaço em Disco
EXEC [dbo].[stpServerJob] ''CHECK_LIST_USED_FILE''; 			-- Arquivos MDF e LDF
EXEC [dbo].[stpServerJob] ''CHECK_LIST_GROWTH_DATABASE''; 	-- Crescimento das Bases
EXEC [dbo].[stpServerJob] ''CHECK_LIST_GROWTH_TABLE'';		-- Crescimento das Tabelas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_USE_FILE'';			-- Utilizacao Arquivos
EXEC [dbo].[stpServerJob] ''CHECK_LIST_NO_BACKUP'';			-- Databases Sem Backup
EXEC [dbo].[stpServerJob] ''CHECK_LIST_BACKUP''; 				-- Backups Executados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_QUERIES_RUNNING'';		-- Queries em Execução
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_FAILED'';			-- Jobs Failed
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_CHANGED'';		-- Jobs Alterados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_SLOW''			-- Jobs Demorados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_RUN''				-- Jobs em Execução
EXEC [dbo].[stpServerJob] ''CHECK_LIST_SQL_TRACELOG'';		-- Queries Demoradas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_ACCOUNTANTS'';			-- Contadores
EXEC [dbo].[stpServerJob] ''CHECK_LIST_MSSQL_CONNECTIONS''	-- Conexões Abertas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_FRAGMENTATION_INDEX'';	-- Fragmentacao Índice
EXEC [dbo].[stpServerJob] ''CHECK_LIST_WAITS_STATS'';			-- Waits Stats
EXEC [dbo].[stpServerJob] ''CHECK_LIST_ALERT'';				-- Alertas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_SQL_ERROR''; 			-- Error Log SQL', 
		@database_name=N'SafeBase', 
		@flags=0
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
/****** Object:  Step [MON - Envio de E-mail em HTML com o CheckList do Banco de Dados]    Script Date: 17/03/2020 14:56:40 ******/
EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'MON - Envio de E-mail em HTML com o CheckList do Banco de Dados', 
		@step_id=2, 
		@cmdexec_success_code=0, 
		@on_success_action=1, 
		@on_success_step_id=0, 
		@on_fail_action=2, 
		@on_fail_step_id=0, 
		@retry_attempts=0, 
		@retry_interval=0, 
		@os_run_priority=0, @subsystem=N'TSQL', 
		@command=N'EXEC [dbo].[stpEnviaCheckListDiarioHTMLn]', 
		@database_name=N'SafeBase', 
		@flags=0
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule @job_id=@jobId, @name=N'DIÁRIO - 07:00', 
		@enabled=1, 
		@freq_type=4, 
		@freq_interval=1, 
		@freq_subday_type=1, 
		@freq_subday_interval=0, 
		@freq_relative_interval=0, 
		@freq_recurrence_factor=0, 
		@active_start_date=20171029, 
		@active_end_date=99991231, 
		@active_start_time=70000, 
		@active_end_time=235959, 
		@schedule_uid=N'5db1dad0-4ec4-4cb2-8bb4-6841a8a90cfc'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'(local)'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
COMMIT TRANSACTION
GOTO EndSave
QuitWithRollback:
    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
EndSave:
GO


