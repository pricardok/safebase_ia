using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class guide
    {
        public static string Query()
        {
            return
            @"

    -- Possibilidade de Uso Functions e Storage Procedures:

    -- JOBS PARA CHECK LIST DIARIO
    EXEC [dbo].[stpServerJob] 'CHECK_LIST';                     -- ENVIA CHECKLIST DIARIO 
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_JOBS_SLOW';           -- LISTA JOBS LENTAS -- TABLE [CheckJobDemorados]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_MSSQL_CONNECTIONS';   -- LISTA CONEXOES MSSQL -- TABLE [CheckConexaoAberta]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_FRAGMENTATION_INDEX'; -- LISTA INDICES FRAGMENTADOS  -- TABLE [CheckFragmentacaoIndices]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_ALERT';               -- LISTA ALERTAS  -- TABLE [CheckAlertaSemClear] e [CheckAlerta]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_JOBS_RUN';            -- LISTA JOBS EM EXECUÇÃO  -- TABLE [CheckJobsRunning]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_JOBS_CHANGED';        -- LISTA JOBS EM ALTERADOS  -- TABLE [CheckAlteracaoJobs]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_JOBS_FAILED';         -- LISTA JOBS EM ALTERADOS  -- TABLE [CheckJobsFailed]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_QUERIES_RUNNING';     -- LISTA QUERIES EM EXECUÇÃO  -- TABLE [CheckQueriesRunning]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_BACKUP';              -- LISTA BACKUPS REALIZADOS  -- TABLE [CheckBackupsExecutados]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_NO_BACKUP';           -- LISTA BASES SEM BACKUPS REALIZADOS  -- TABLE [CheckDatabasesSemBackup]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_USE_FILE';            -- LISTA UTILIZAÇÃO DE ARQUIVOS DB  -- TABLE [CheckUtilizacaoArquivoWrites] e [CheckUtilizacaoArquivoReads]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_GROWTH_TABLE';        -- LISTA CRESCIMENTO DE TABELAS  -- TABLE [CheckTableGrowth] e [CheckTableGrowthEmail]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_GROWTH_DATABASE';     -- LISTA CRESCIMENTO DOS BANCOS  -- TABLE [CheckDatabaseGrowth] e [CheckDatabaseGrowthEmail]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_USED_FILE';           -- LISTA UTILIZAÇÃO DE ARQUIVOS LDF MDF  -- TABLE [CheckArquivosDados] e [CheckArquivosLog]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_USED_DISC';           -- LISTA UTILIZAÇÃO DE ESPAÇO EM DISCO  -- TABLE [CheckEspacoDisco]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_ACCOUNTANTS';         -- LISTA UTILIZAÇÃO CONTADORES DB  -- TABLE [ContadorRegistro]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_WAITS_STATS';         -- LISTA WAITS STATS DB  -- TABLE [CheckWaitsStats]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_SQL_ERROR';           -- LISTA DE ERRO SQL  -- TABLE [CheckSQLServerErrorLog],[CheckSQLServerLoginFailed] e [CheckSQLServerLoginFailedEmail]
    EXEC [dbo].[stpServerJob] 'CHECK_LIST_SQL_TRACELOG';        -- LISTA SQL TRACELOG QUERIES  -- TABLE [CheckDBControllerQueries] e [CheckDBControllerQueriesGeral]


    -- JOBS DE CARGA DE DADOS
    EXEC [dbo].[stpServerLoads] 'LOADS_TABLES_SIZE';            -- CARGA TAMANHO DE TABELAS -- TABLE [Servidor],[BaseDados],[Tabela] e [HistoricoTamanhoTabela]
    EXEC [dbo].[stpServerLoads] 'LOADS_HIST_WS';                -- CARGA UTILIZAÇÃO WAITS STATS -- TABLE [HistoricoWaitsStats]
    -- EXEC [dbo].[stpServerLoads] 'LOADS_FRAGM_INDEX';         -- CARGA FRAGMENTAÇÃO DE INDICES -- DESABILITADO
    EXEC [sb].[stpHistoricoFragmentacaoIndice]                  -- CARGA FRAGMENTAÇÃO DE INDICES -- TABLE [Servidor],[BaseDados],[Tabela] e [HistoricoFragmentacaoIndice]
    EXEC [dbo].[stpServerLoads] 'LOADS_HIST_USAGE_ARCHIVE';     -- CARGA UTILIZAÇÃO ARQUIVOS -- TABLE [HistoricoUtilizacaoArquivo]
    EXEC [dbo].[stpServerLoads] 'LOADS_ACCOUNTANTS';            -- CARGA CONTADORES -- TABLE [ContadorRegistro]     
    EXEC [dbo].[stpServerLoads] 'LOADS_DB_ERROR_HISTORY';       -- CARGA HISTORICO ERROS -- TABLE [HistoricoErrosDB]
    EXEC [dbo].[stpServerLoads] 'LOADS_DB_CHANGE';              -- CARGA ALTERAÇÕES DB -- TABLE [ServerAudi] 
    EXEC [dbo].[stpServerLoads] 'LOADS_LOG_ALWAYSON';           -- CARGA ALWAYSON -- TABLE [HistoricoAlwaysOn] 

    -- JOBS DE TESTE
    EXEC [dbo].[stpServerJob] 'TEST';                           -- TB [Testedb] 
 
    
    -- JOBS DE ALERTA - TABELAS [Alerta] e [AlertaParametro] 
    EXEC [dbo].[stpServerAlert] 'ALERT_DB_CHANGE';              -- ALERTA DE ALTERAÇOES DB 
    EXEC [dbo].[stpServerAlert] 'ALERT_MSSQL_CONNECTIONS';      -- ALERTA DE CONEXOES MSSQL 
    EXEC [dbo].[stpServerAlert] 'ALERT_FILE_LOG_FULL';          -- ALERTA ARQUIVO DE LOG FULL
    EXEC [dbo].[stpServerAlert] 'ALERT_CHECKDB_CHECK';          -- ALERTA - REALIZADA CHECKDB NAS BASES
    EXEC [dbo].[stpServerAlert] 'ALERT_CHECKDB';                -- ALERTA DE BANCO DE DADOS CORROMPIDO - DEPENDE DA ALERT_CHECKDB_CHECK
    EXEC [dbo].[stpServerAlert] 'ALERT_CONSUMPTION_CPU';        -- ALERTA DE CONSUMO DE CPU
    EXEC [dbo].[stpServerAlert] 'ALERT_DATABASE_CREATED';       -- ALERTA DE DATABASE CRIADA
    EXEC [dbo].[stpServerAlert] 'ALERT_ERRO_DATABASE';          -- ALERTA DE PAGINA CORROPIDA E STATUS DATABASE
    EXEC [dbo].[stpServerAlert] 'ALERT_MSSQL_RESTART';          -- ALERTA DE MSSQL REINICIADO
    EXEC [dbo].[stpServerAlert] 'ALERT_PROCESS_BLOCKED';        -- ALERTA DE PROCESSO BLOQUEADO
    EXEC [dbo].[stpServerAlert] 'ALERT_QUERY_DELAY';            -- ALERTA DE QUERY DEMORADA
    EXEC [dbo].[stpServerAlert] 'ALERT_CREATE_TRACE';           -- CRIA TRACE, DEPENDENCIA PARA ALERTA DE QUERY DEMORADA
    EXEC [dbo].[stpServerAlert] 'ALERT_TEST_TRACE';             -- ALERTA - TESTA O ALERTA DE TRACE 
    EXEC [dbo].[stpServerAlert] 'ALERT_NO_BACKUP';              -- ALERTA DE DB SEM BACKUP
    EXEC [dbo].[stpServerAlert] 'ALERT_JOB_FAIL';               -- ALERTA FALHA EM JOBS AGENT
    EXEC [dbo].[stpServerAlert] 'ALERT_DISC_SPACE';             -- ALERTA DE ESPAÇO EM DISCO
    EXEC [dbo].[stpServerAlert] 'ALERT_TEMPDB_USE';             -- ALERTA DE UTILIZACAO DE TEMPDB
    EXEC [dbo].[stpServerAlert] 'ALERT_RUN_PROCESSES';          -- ALERTA DE PROCESSOS EM EXECUÇÃO
    EXEC [dbo].[stpServerAlert] 'QUEUE_DB';                     -- ALERTA DE FILAS MSSQL
    EXEC [dbo].[stpServerAlert] 'ALERT_CHECK_FILE_BKP';         -- ALERTA ARQUIVOS DE BACKUPS FALTANTES   1 = FULL, 2 = DIFF, 3 = LOG


    -- OBTENHA INFORMAÇÕES DE BANCO
    EXEC [dbo].[stpGetInfo] 'HELP';                             -- OBETENHA INFORMAÇOES DO MSSQL


    -- JOBS AGENDADAS DO MSSQL:
    -- OPERAÇÕES MSSQL AGENT
    DBMaintenancePlan.AlertDB.No.RealTime                       -- ALERTAS EM HORARIO NAO COMERCIAL
    DBMaintenancePlan.AlertDB.RealTime                          -- ALERTAS EM REAL TIME
    DBMaintenancePlan.CheckListDB                               -- ENVIA CHECK LIST
    DBMaintenancePlan.Data.Load                                 -- REALIZA CARGA DE DADOS PARA CHECK LIST E AFINS
    DBMaintenancePlan.StartBackup.Diff                          -- REALIZA BACKUP DIFF
    DBMaintenancePlan.StartBackup.Full                          -- REALIZA BACKUP FULL
    DBMaintenancePlan.StartBackup.Log                           -- REALIZA BACKUP DE LOG
    DBMaintenancePlan.UpdateStats                               -- REALIZA UPDATE STATISTICS
    DBMaintenancePlan.ShrinkingLogFiles                         -- REALIZA SHIRINK LOG FILES 
    DBMaintenancePlan.CheckDB                                   -- REALIZA CHECK DB


        ";

        }
    }

    class guidemonitoring
    {
        public static string Query()
        {
            return
            @"
    
    -- Possibilidade de Uso Para Monitoramento:

    EXEC [dbo].[stpToZabbix] 1,''                               -- DISCOVER DBName
    EXEC [dbo].[stpToZabbix] 2,''                               -- DISCOVER JobName
    EXEC [dbo].[stpToZabbix] 3,'NOME DO BANCO'                  -- VERIFICA BackupFull 
    EXEC [dbo].[stpToZabbix] 4,'NOME DO BANCO'                  -- VERIFICA BackupLog
    EXEC [dbo].[stpToZabbix] 5,'NOME DO BANCO'                  -- VERIFICA BackupDiff
    EXEC [dbo].[stpToZabbix] 6,''                               -- VERSION MSSQL
    EXEC [dbo].[stpToZabbix] 7,''                               -- CHECK JOBS MSSQL
    EXEC [dbo].[stpToZabbix] 8,''                               -- DISCOVER ALWAYSON
    EXEC [dbo].[stpToZabbix] 9,''                               -- CHECK ALWAYSON
    EXEC [dbo].[stpToZabbix] 10,''                              -- CHECK FILE:  D = BackupFull, I = BackupDiff, L = BackupLog

        ";

        }
    }
}
