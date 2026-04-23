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
    public static void stpServerAlert(string ServerAlert, int what)
    {

        string type = "";

        if (what == 2)
        {
            type = "I";
        }
        if (what == 3)
        {
            type = "L";
        }

        ServerAlert = ServerAlert.ToUpper();

        // JOBS DE ALERTA

        if (ServerAlert == "GUIDE") // GUIA DE USO
        {
            SendMessage.PostBack(guide.Query());
        }

        if (ServerAlert == "QUEUE_DB") // FILAS MSSQL
        {
            ExecuteSql.ExecuteQuery(stpQueue.Query());
        }

        if (ServerAlert == "ALERT_DB_CHANGE") // ALERTA DE ALTERAÇOES DB -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaAlteracaoDB.Query());
        }

        if (ServerAlert == "ALERT_MSSQL_CONNECTIONS") // ALERTA DE CONEXOES MSSQL -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaConexaoSQLServer.Query());
        }

        if (ServerAlert == "ALERT_FILE_LOG_FULL") // ALERTA ARQUIVO DE LOG FULL -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaArquivoLogFull.Query());
        }

        if (ServerAlert == "ALERT_CHECKDB_CHECK") //  ALERTA - REALIZADA CHECKDB NAS BASES -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpCheckDatabases.Query());
        }

        if (ServerAlert == "ALERT_CHECKDB") // ALERTA DE BANCO DE DADOS CORROMPIDO - DEPENDE DA ALERT_CHECKDB_CHECK -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaCheckDB.Query());
        }

        if (ServerAlert == "ALERT_CONSUMPTION_CPU") //  ALERTA DE CONSUMO DE CPU -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaConsumoCPU.Query());
        }

        if (ServerAlert == "ALERT_DATABASE_CREATED") //  ALERTA DE DATABASE CRIADA -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaDatabaseCriada.Query());
        }

        if (ServerAlert == "ALERT_ERRO_DATABASE") //  ALERTA DE PAGINA CORROPIDA E STATUS DATABASE -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaErroBancoDados.Query());
        }

        if (ServerAlert == "ALERT_MSSQL_RESTART") //  ALERTA DE MSSQL REINICIADO -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaSQLServerReiniciado.Query());
        }

        if (ServerAlert == "ALERT_PROCESS_BLOCKED") //  ALERTA DE PROCESSO BLOQUEADO -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaProcessoBloqueado.Query());
        }

        if (ServerAlert == "ALERT_QUERY_DELAY") //  ALERTA DE QUERY DEMORADA -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaQueriesDemoradas.Query());
        }

        if (ServerAlert == "ALERT_CREATE_TRACE") //  ALERTA - CRIA TRACE DEPENDENCIA PARA ALERT_QUERY_DELAY-- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpCreateTrace.Query());
        }

        if (ServerAlert == "ALERT_NO_BACKUP") //  ALERTA DE DB SEM BACKUP -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaDatabaseSemBkp.Query());
        }

        if (ServerAlert == "ALERT_JOB_FAIL") //  ALERTA FALHA EM JOBS AGENT -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaJobFalha.Query());
        }

        if (ServerAlert == "ALERT_DISC_SPACE") //  ALERTA DE ESPAÇO EM DISCO -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaEspacoDisco.Query());
        }

        if (ServerAlert == "ALERT_TEMPDB_USE") //  ALERTA DE UTILIZACAO DE TEMPDB  -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaTempdbUtilizacaoArquivoMDF.Query());
        }

        if (ServerAlert == "ALERT_RUN_PROCESSES") //  ALERTA DE PROCESSOS EM EXECUÇÃO -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpEnviaEmailProcessosExecucao.Query());
        }

        if (ServerAlert == "ALERT_TEST_TRACE") //  ALERTA - TESTA O ALERTA DE TRACE -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpTesteTrace.Query());
        }

        if (ServerAlert == "ALERT_CHECK_FILE_BKP") //  ALERTA ALERTA DE ARQUIVOS DE BKP FALTANTES -- TABLE [Alerta] e [AlertaParametro]
        {
            
            ExecuteSql.ExecuteQuery(stpAlertaCheckFileBackup.Query(type));
        }

        if (ServerAlert == "ALERT_JOB_AGENDAMENTO_FAIL") //  ALERTA FALHA EM JOBS DE AGENDAMENTO INTERNO DA SAFEBASE -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaJobAgendamentoFalha.Query());
        }

        if (ServerAlert == "ALERT_FAILOVER") //  ALERTA FALHA EM JOBS DE AGENDAMENTO INTERNO DA SAFEBASE -- TABLE [Alerta] e [AlertaParametro]
        {
            ExecuteSql.ExecuteQuery(stpAlertaFailoverAlwaysOn.Query());
        }


        // TESTE DE FUNCIONAMENTO  

        if (ServerAlert == "TEST")
        {
            ExecuteSql.ExecuteQuery(stpTestTools.Query());
        }

       // else
        //{

          //  SendMessage.PostBack("XOPIN");

        //}

    }

}


