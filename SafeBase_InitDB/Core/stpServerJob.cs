using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.IO;
using InitDB.Client;
using System.Xml;


public partial class StoredProcedures
{

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpServerJob(string ServerTask)
    {

        ServerTask = ServerTask.ToUpper();

        // GUIA DE USO
        if (ServerTask == "GUIDE") 
        {
            SendMessage.PostBack(guide.Query());
        }

        else
        {

            // CHECK LIST
            if (ServerTask == "CHECK_LIST") // JOB DE ENVIO DE CHECKLIST DIARIO
            {
                // VALIDA CRIAÇÃO DE DIRETORIOS DE LOGS E AFINS 
                Core.ExecuteCheckDir();

                // Envia checklist
                ExecuteSql.ExecuteQuery(stpEnviaCheckList.Query());
            }

            if (ServerTask == "CHECK_LIST_JOBS_SLOW") // LISTA JOBS LENTAS --TABLE[CheckJobDemorados]
            {
                ExecuteSql.ExecuteQuery(stpCheckJobDemorados.Query());
            }

            if (ServerTask == "CHECK_LIST_MSSQL_CONNECTIONS") // LISTA CONEXOES MSSQL -- TABLE [CheckConexaoAberta]
            {
                ExecuteSql.ExecuteQuery(stpCheckConexaoAberta.Query());
            }

            if (ServerTask == "CHECK_LIST_FRAGMENTATION_INDEX") // LISTA INDICES FRAGMENTADOS  -- TABLE [CheckFragmentacaoIndices]
            {
                ExecuteSql.ExecuteQuery(stpCheckFragmentacaoIndices.Query());
            }

            if (ServerTask == "CHECK_LIST_ALERT") // LISTA REGISTRO DE ALERTAS  -- TABLE [CheckAlertaSemClear] e [CheckAlerta]
            {
                ExecuteSql.ExecuteQuery(stpCheckAlerta.Query());
            }

            if (ServerTask == "CHECK_LIST_JOBS_RUN") // LISTA REGISTRO DE JOBS EM EXECUÇÃO  -- TABLE [CheckJobsRunning]
            {
                ExecuteSql.ExecuteQuery(stpCheckJobsRunning.Query());
            }

            if (ServerTask == "CHECK_LIST_JOBS_CHANGED") // LISTA REGISTRO DE JOBS EM ALTERADOS-- TABLE[CheckAlteracaoJobs]
            {
                ExecuteSql.ExecuteQuery(stpCheckAlteracaoJobs.Query());
            }

            if (ServerTask == "CHECK_LIST_JOBS_FAILED") // LISTA REGISTRO DE JOBS EM ALTERADOS  -- TABLE [CheckJobsFailed]
            {
                ExecuteSql.ExecuteQuery(stpCheckJobsFailed.Query());
            }

            if (ServerTask == "CHECK_LIST_QUERIES_RUNNING") //LISTA REGISTRO DE QUERIES EM EXECUÇÃO  -- TABLE [CheckQueriesRunning]
            { 
                ExecuteSql.ExecuteQuery(stpCheckQueriesRunning.Query());
            }

            if (ServerTask == "CHECK_LIST_BACKUP") // LISTA REGISTRO DE BACKUPS REALIZADOS  -- TABLE [CheckBackupsExecutados]
            {
                ExecuteSql.ExecuteQuery(stpCheckBackupsExecutados.Query());
            }

            if (ServerTask == "CHECK_LIST_NO_BACKUP") // LISTA REGISTRO DE BASES SEM BACKUPS REALIZADOS  -- TABLE [CheckDatabasesSemBackup]
            {
                ExecuteSql.ExecuteQuery(stpCheckDatabasesSemBackup.Query());
            }

            if (ServerTask == "CHECK_LIST_USE_FILE") // LISTA REGISTRO DE UTILIZAÇÃO DE ARQUIVOS DB  -- TABLE [CheckUtilizacaoArquivoWrites] e [CheckUtilizacaoArquivoReads]
            {
                ExecuteSql.ExecuteQuery(stpCheckList_Utilizacao_Arquivo.Query());
            }

            if (ServerTask == "CHECK_LIST_GROWTH_TABLE") // LISTA REGISTRO DE CRESCIMENTO DE TABELAS  -- TABLE [CheckTableGrowth] e [CheckTableGrowthEmail]
            {
                ExecuteSql.ExecuteQuery(stpCheckTableGrowth.Query());
            }

            if (ServerTask == "CHECK_LIST_GROWTH_DATABASE") // LISTA REGISTRO DE CRESCIMENTO DOS BANCOS  -- TABLE [CheckDatabaseGrowth] e [CheckDatabaseGrowthEmail]
            {
                ExecuteSql.ExecuteQuery(stpCheckDatabaseGrowth.Query());
            }

            if (ServerTask == "CHECK_LIST_USED_FILE") // LISTA REGISTRO DE UTILIZAÇÃO DE ARQUIVOS LDF MDF  -- TABLE [CheckArquivosDados] e [CheckArquivosLog]
            {
                ExecuteSql.ExecuteQuery(stpCheckList_Arquivos_MDF_LDF.Query());
            }

            if (ServerTask == "CHECK_LIST_USED_DISC") // LISTA REGISTRO DE UTILIZAÇÃO DE ESPAÇO EM DISCO  -- TABLE [CheckEspacoDisco]
            {
                ExecuteSql.ExecuteQuery(stpCheckEspacoDisco.Query());
            }

            if (ServerTask == "CHECK_LIST_ACCOUNTANTS") // LISTA REGISTRO DE UTILIZAÇÃO CONTADORES DB  -- TABLE [ContadorRegistro]
            {
                ExecuteSql.ExecuteQuery(stpCheckContadores.Query());
            }

            if (ServerTask == "CHECK_LIST_WAITS_STATS") // LISTA REGISTRO WAITS STATS DB  -- TABLE [CheckWaitsStats]
            {
                ExecuteSql.ExecuteQuery(stpCheckWaitsStats.Query());
            }

            if (ServerTask == "CHECK_LIST_SQL_ERROR") // LISTA REGISTRO DE ERRO SQL  -- TABLE [CheckSQLServerErrorLog] e [CheckSQLServerLoginFailed] e [CheckSQLServerLoginFailedEmail]
            {
                ExecuteSql.ExecuteQuery(stpCheckSQLServerErrorLog.Query());
            }

            if (ServerTask == "CHECK_LIST_SQL_TRACELOG") // LISTA REGISTRO SQL TRACELOG QUERIES  -- TABLE [CheckDBControllerQueries] e [CheckDBControllerQueriesGeral]
            {
                ExecuteSql.ExecuteQuery(stpCheckSQLTraceLogQueries.Query());
            }

            // TESTE DE FUNCIONAMENTO 

            if (ServerTask == "TEST") // REALIZA TESTE DA FERRAMENTA 
            {
                ExecuteSql.ExecuteQuery(stpTestTools.Query());
            }

            // if (ServerTask != null) { SendMessage.PostBack("COMANDO NA IDENTIFICADO"); }
            /*
            else
            {
                SendMessage.PostBack("COMANDO NA IDENTIFICADO");
            } */
        }

    }

}


