using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateJobsAgentStep
    {
        public static string Query(string server,string use)
        {
            return
            @"

            /*
            -- ALERTA EM REAL TIME
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.AlertDB.RealTime', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'


SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

-- Executado a cada minuto

EXEC [dbo].[stpServerAlert] ''ALERT_FILE_LOG_FULL'';          -- ALERTA ARQUIVO DE LOG FULL

EXEC [dbo].[stpServerAlert] ''ALERT_ERRO_DATABASE'';          -- ALERTA DE PAGINA CORROPIDA E STATUS DATABASE

--EXEC [dbo].[stpServerAlert] ''QUEUE_DB'';                   -- ALERTA DE FILAS

--exec [dbo].[stpQueueInfoSendMail]


-- Executa de segunda a sabado as 7 da manha
IF ( (DATEPART(HOUR, GETDATE()) = 7 ) AND (DATEPART(DW, GETDATE()) >= 2 AND DATEPART(DW, GETDATE()) <= 7))
    BEGIN
        EXEC [dbo].[stpServerAlert] ''ALERT_CHECK_FILE_BKP'';         -- ALERTA ARQUIVOS DE BACKUPS FALTANTES  -- BACKUP FULL = 1
        EXEC [dbo].[stpServerAlert] ''ALERT_CHECK_FILE_BKP'',2;       -- ALERTA ARQUIVOS DE BACKUPS FALTANTES  -- BACKUP DIFF = 2
    END

-- Executado apenas no horario comercial
IF ( DATEPART(HOUR, GETDATE()) >= 6 AND DATEPART(HOUR, GETDATE()) < 23 )
    BEGIN
        EXEC [dbo].[stpServerAlert] ''ALERT_PROCESS_BLOCKED'';        -- ALERTA DE PROCESSO BLOQUEADO
        EXEC [dbo].[stpServerAlert] ''ALERT_CONSUMPTION_CPU'';        -- ALERTA DE CONSUMO DE CPU
    END

-- Executado a cada 5 minutos
IF ( DATEPART(mi, GETDATE()) %5 = 0 )
    BEGIN
        EXEC [dbo].[stpServerAlert] ''ALERT_CREATE_TRACE'';           -- CRIA TRACE, DEPENDENCIA PARA ALERTA DE QUERY DEMORADA
        EXEC [dbo].[stpServerAlert] ''ALERT_DISC_SPACE'';             -- ALERTA DE ESPAÇO EM DISCO
        EXEC [dbo].[stpServerAlert] ''ALERT_TEMPDB_USE'';             -- ALERTA DE UTILIZACAO DE TEMPDB
        EXEC [dbo].[stpServerAlert] ''ALERT_QUERY_DELAY'';            -- ALERTA DE QUERY DEMORADA
        EXEC [dbo].[stpServerLoads] ''LOADS_LOG_ALWAYSON'';           -- CARREGA INFORMAÇÕES DE ALWAYSON, APENAS QUANDO HABILITADO
        EXEC [dbo].[stpGetCheckLoginCA]                               -- ALTERAÇÃO OU CRIAÇÃO DE LOGINS E USUARIOS  
    END

-- Executado a cada 1 hora
IF ( DATEPART(mi, GETDATE()) %59 = 0 )
    BEGIN
        EXEC [dbo].[stpServerAlert] ''ALERT_MSSQL_CONNECTIONS'';      -- ALERTA DE CONEXOES MSSQL 
    END

-- Executado a cada 20 minutos
IF ( DATEPART(mi, GETDATE()) %20 = 0 )
    BEGIN
        EXEC [dbo].[stpServerAlert] ''ALERT_MSSQL_RESTART'';          -- ALERTA DE MSSQL REINICIADO
    END

-- Executado a cada 40 minutos
IF ( DATEPART(mi, GETDATE()) %40 = 0 )
    BEGIN
        EXEC [dbo].[stpGetCheckLogin] 1                               -- ALERTA DE ACESSO
    END


'
            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.AlertDB.RealTime', 	-- Nome da Job
				@category = 'Database Maintenance',		        -- Categoria
				@owner_name = 'sa', 					        -- Owner login				
				@name_step = 'Step Real Time', 			        -- Nome do step da job
				@mycommand = @cmd, 						        -- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '4',
				@al_freq_subday_interval = '1',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20171030',
				@al_active_end_date = '99991231',
				@al_active_start_time = '100',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   


            /*
            -- ALERTA EM NÃO REAL TIME
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.AlertDB.No.RealTime')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.AlertDB.No.RealTime', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'


-- Executado todos os dias as 2hs da manha

EXEC [dbo].[stpServerAlert] ''ALERT_DATABASE_CREATED''       -- ALERTA DE DATABASE CRIADA

EXEC [dbo].[stpServerAlert] ''ALERT_NO_BACKUP'';              -- ALERTA DE DB SEM BACKUP

EXEC [dbo].[stpServerAlert] ''ALERT_JOB_FAIL'';               -- ALERTA FALHA EM JOBS AGENT

EXEC [dbo].[stpServerAlert] ''ALERT_DB_CHANGE'';              -- ALERTA DE ALTERAÇOES DB

--EXEC [dbo].[stpServerAlert] ''ALERT_RUN_PROCESSES'';        -- ALERTA DE PROCESSOS EM EXECUÇÃO

EXEC [dbo].[stpServerAlert] ''ALERT_CHECKDB_CHECK'';          -- ALERTA - REALIZADA CHECKDB NAS BASES

EXEC [dbo].[stpServerAlert] ''ALERT_CHECKDB'';                -- ALERTA DE BANCO DE DADOS CORROMPIDO - DEPENDE DA ALERT_CHECKDB_CHECK


'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.AlertDB.No.RealTime', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step No Real Time', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20200326',
				@al_active_end_date = '99991231',
				@al_active_start_time = '20000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   


            /*
            -- CARGA DE DADOS 
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Data.Load')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.Data.Load', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'

-- Loads com feedback

PRINT ''Iniciando carga de dados...''

PRINT ''1/7 - Carregando alteracoes nos Bancos...''
EXEC [dbo].[stpServerLoads] ''LOADS_DB_CHANGE''

PRINT ''2/7 - Carregando Fragmentacao de indices...''
-- EXEC [dbo].[stpServerLoads] ''LOADS_FRAGM_INDEX''
EXEC [sb].[stpHistoricoFragmentacaoIndice]

PRINT ''3/7 - Carregando utilizacao de arquivos...''
EXEC [dbo].[stpServerLoads] ''LOADS_HIST_USAGE_ARCHIVE''

PRINT ''4/7 - Carregando WAITS STATS...''
EXEC [dbo].[stpServerLoads] ''LOADS_HIST_WS''

PRINT ''5/7 - Carregando tamanho das tabelas...''
EXEC [dbo].[stpServerLoads] ''LOADS_TABLES_SIZE''

PRINT ''6/7 - Carregando Contadores...''
EXEC [dbo].[stpServerLoads] ''LOADS_ACCOUNTANTS''

PRINT ''7/7 - Carregando Historico de erros...''
EXEC [dbo].[stpServerLoads] ''LOADS_DB_ERROR_HISTORY''

PRINT ''Carga concluída com sucesso!''

'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.Data.Load', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step Data Load', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20171029',
				@al_active_end_date = '99991231',
				@al_active_start_time = '5000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   


           /*
            -- ENVIA CHECK LIST
            */
            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckListDB')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.CheckListDB', @delete_unused_schedule = 1
            END

            DECLARE @cmd01 varchar (max) = N'
-- CheckList

EXEC [dbo].[stpServerJob] ''CHECK_LIST_USED_DISC''; 		-- Espaco em Disco
EXEC [dbo].[stpServerJob] ''CHECK_LIST_USED_FILE''; 		-- Arquivos MDF e LDF
EXEC [dbo].[stpServerJob] ''CHECK_LIST_GROWTH_DATABASE''; 	-- Crescimento das Bases
EXEC [dbo].[stpServerJob] ''CHECK_LIST_GROWTH_TABLE'';		-- Crescimento das Tabelas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_USE_FILE'';		-- Utilizacao Arquivos
EXEC [dbo].[stpServerJob] ''CHECK_LIST_NO_BACKUP'';		-- Databases Sem Backup
EXEC [dbo].[stpServerJob] ''CHECK_LIST_BACKUP''; 			-- Backups Executados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_SQL_TRACELOG'';		-- Lista sql Tracelog
EXEC [dbo].[stpServerJob] ''CHECK_LIST_QUERIES_RUNNING'';	-- Queries em Execusao
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_FAILED'';		-- Jobs Failed
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_CHANGED'';		-- Jobs Alterados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_SLOW''		-- Jobs Demorados
EXEC [dbo].[stpServerJob] ''CHECK_LIST_JOBS_RUN''		-- Jobs em Execusao
EXEC [dbo].[stpServerJob] ''CHECK_LIST_SQL_TRACELOG'';		-- Queries Demoradas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_ACCOUNTANTS'';		-- Contadores
EXEC [dbo].[stpServerJob] ''CHECK_LIST_MSSQL_CONNECTIONS''	-- Conexoes Abertas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_FRAGMENTATION_INDEX'';	-- Fragmentacao indice
EXEC [dbo].[stpServerJob] ''CHECK_LIST_WAITS_STATS'';		-- Waits Stats
EXEC [dbo].[stpServerJob] ''CHECK_LIST_ALERT'';			-- Alertas
EXEC [dbo].[stpServerJob] ''CHECK_LIST_SQL_ERROR''; 		-- Error Log SQL

'

DECLARE @cmd02 varchar (max) = N'
EXEC [dbo].[stpServerJob] ''CHECK_LIST''; 	-- Envia CheckList
'

            EXEC [dbo].[stpAddJobQuickMulti]  
				@job = 'DBMaintenancePlan.CheckListDB', 		-- Nome da Job
				@category = 'Database Maintenance',			-- Categoria
				@owner_name = 'sa', 						-- Owner login				
				@name_step_01 = 'Carga Tabelas CheckList', 	-- Nome do step da job
				@name_step_02 = 'Envio de E-mail em HTML', 	-- Nome do step da job
				@mycommand_01 = @cmd01, 							-- Comando T-SQL
				@mycommand_02 = @cmd02,
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20171029',
				@al_active_end_date = '99991231',
				@al_active_start_time = '70000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO  

            /*
            -- BACKUP FULL
            */
 
			IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Full')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Full', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'

-- Executado BACKUP FULL
/*

OBS: O BackupFull pode ser realizado seguindo os parametros:

- EXECUTE [dbo].[stpStartBackupDB]  ''BackupFull'' ou EXECUTE [dbo].[stpStartBackupDB]  ''BackupFull'', 1 
NO comando acima, primeiro realiza Backup Full de todas as bases e apos deletar arquivos antigos com base nas configurações da tabela [dbo].[ConfigDB]

- EXECUTE [dbo].[stpStartBackupDB]  ''BackupFull'', 2
NO comando acima, a cada backup de base realizado o mesmo deleta os arquivos mais antigos, ou seja, ele nao faz todos os backups e depois deleta, a cada backup o mesmo deleta a base mais antiga. Comando recomendado em ambientes com pouco espaço em disco de backups.

- EXECUTE [dbo].[stpStartBackupDB]  ''BackupFull'', 3
O comando acima deve ser utilizado em ultimo caso, onde o espaço em disco é critico, pois ele deleta o backup anterior para criar um novo, ou seja, enquanto o backup estives em andamento voce nao tera o arquivo antigo.

- EXECUTE [dbo].[stpStartBackupDB]  ''BackupFull'', 4; EXECUTE [dbo].[stpStartDeleteOldBackups]  ''BackupFull'' 
Ao utilizar o comando acima é necessario ulizar um complemento para que seja realizado o expurgo de backups antigos, note que um segundo EXECUTE é utilizado apos o ponto e virgula. Essa opção é valida para backups BackupFull, BackupDifferential e BackupLog
Após o backup, seja ele full, diff ou de log os backups antigos serão deletados automaticamente com base nas configurações da tabela [dbo].[ConfigDB]

*/

EXECUTE [dbo].[stpStartBackupDB] ''BackupFull'', 4; EXECUTE [dbo].[stpStartDeleteOldBackups]  ''BackupFull'';

'
            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.StartBackup.Full', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step BackupFull', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '8',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '1',
				@al_active_start_date = '20180509',
				@al_active_end_date = '99991231',
				@al_active_start_time = '0',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   

            /*
            -- BACKUP LOG
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Log')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Log', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado BACKUP LOG
EXECUTE [dbo].[stpStartBackupDB] @BackupType = ''BackupLog'', @type = 4; EXECUTE [dbo].[stpStartDeleteOldBackups]  ''BackupLog'';

'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.StartBackup.Log', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step BackupLog', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '8',
				@al_freq_interval = '126',
				@al_freq_subday_type = '8',
				@al_freq_subday_interval = '3',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '1',
				@al_active_start_date = '20191126',
				@al_active_end_date = '99991231',
				@al_active_start_time = '30000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   

            /*
            -- BACKUP Differential
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.StartBackup.Diff')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.StartBackup.Diff', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado BACKUP Differential
EXECUTE [dbo].[stpStartBackupDB] @BackupType = ''BackupDifferential'', @type = 4; EXECUTE [dbo].[stpStartDeleteOldBackups]  ''BackupDifferential'';

'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.StartBackup.Diff', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step BackupDifferential', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '8',
				@al_freq_interval = '126',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '1',
				@al_active_start_date = '20190630',
				@al_active_end_date = '99991231',
				@al_active_start_time = '0',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   

            /*
            -- UpdateStats
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.UpdateStats')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.UpdateStats', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado UpdateStats
--EXECUTE [dbo].[stpStartUpdateStats]
-- Variável
DECLARE @TSQL nvarchar(2000)

-- Retirando bancos de dados do sistema e bancos de dados de usuários da execução
SET @TSQL = ''
IF (DB_ID(''''?'''') > 4
   AND ''''?'''' NOT IN(''''safebase'''',''''distribution'''',''''SSISDB'''',''''ReportServer'''',''''ReportServertempdb'''')
   )
BEGIN
   PRINT ''''********** Rebuilding statistics on database: [?] ************''''
   USE [?]; exec sp_updatestats
END
''
-- Executando TSQL para cada banco de dados
EXEC sp_MSforeachdb @TSQL
'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.UpdateStats', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step UpdateStats', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '7',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20161001',
				@al_active_end_date = '99991231',
				@al_active_start_time = '30000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   

            /*
            -- ShrinkingLogFiles
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.ShrinkingLogFiles')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.ShrinkingLogFiles', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado ShrinkingLogFiles
EXECUTE [dbo].[stpStartShrinkingLogFiles] 500
'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.ShrinkingLogFiles', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step ShrinkingLogFiles', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20161001',
				@al_active_end_date = '99991231',
				@al_active_start_time = '50000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO 

            /*
            -- CheckDB
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.CheckDB')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.CheckDB', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado CheckDB
EXECUTE [dbo].[stpStartCheckDB]
'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.CheckDB', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step CheckDB', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '8',
				@al_freq_interval = '64',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '1',
				@al_active_start_date = '20161001',
				@al_active_end_date = '99991231',
				@al_active_start_time = '190000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   

            /*
            -- Defraging
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.Defraging')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.Defraging', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado Defraging

EXECUTE dbo.stpRunDefraging
  @executeSQL = 1
, @printCommands = 0
, @debugMode = 1
, @printFragmentation = 1
, @forceRescan = 1
, @maxDopRestriction = 1
, @minPageCount = 24
, @maxPageCount = NULL
, @minFragmentation = 10
, @rebuildThreshold = 30
--, @defragDelay = ''00:00:05''
, @defragOrderColumn = ''fragmentation''
, @ScanMode = ''LIMITED''
, @defragSortOrder = ''DESC''
, @excludeMaxPartition = 1
, @timeLimit = 180;

'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.Defraging', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step Defraging', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '7',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20161001',
				@al_active_end_date = '99991231',
				@al_active_start_time = '20000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO  

            /*
            -- DELETA LIXO TMP
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.RemoveTrash')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.RemoveTrash', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado todos os dias as 2hs da manha
TRUNCATE table LogQueue;

DBCC CHECKIDENT (''LogQueue'', RESEED, 1);
'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.RemoveTrash', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step Remove Trash', 			-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '4',
				@al_freq_interval = '1',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '0',
				@al_active_start_date = '20200326',
				@al_active_end_date = '99991231',
				@al_active_start_time = '20000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   


            /*
            -- SourceControl
            */

            IF EXISTS (SELECT [name] FROM msdb.dbo.sysjobs where name like 'DBMaintenancePlan.SourceControl')
            BEGIN
                EXEC msdb.dbo.sp_delete_job @job_name = N'DBMaintenancePlan.SourceControl', @delete_unused_schedule = 1
            END

            DECLARE @cmd varchar (max) = N'
-- Executado stpSourceControl
EXEC [dbo].[stpSourceControl]
'

            EXEC [dbo].[stpAddJobQuick]  
				@job = 'DBMaintenancePlan.SourceControl', 		-- Nome da Job
				@category = 'Database Maintenance',		-- Categoria
				@owner_name = 'sa', 					-- Owner login				
				@name_step = 'Step SourceControl', 		-- Nome do step da job
				@mycommand = @cmd, 						-- Comando T-SQL
				@al_freq_type = '8',
				@al_freq_interval = '64',
				@al_freq_subday_type = '1',
				@al_freq_subday_interval = '0',
				@al_freq_relative_interval = '0',
				@al_freq_recurrence_factor = '1',
				@al_active_start_date = '20161001',
				@al_active_end_date = '99991231',
				@al_active_start_time = '190000',
				@al_active_end_time = '235959',
				@servername = @@SERVERNAME --'" + server + @"' 
            GO   


            ";

        }
    }
}
