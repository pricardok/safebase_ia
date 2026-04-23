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
    public static void stpServerLoads(string ServerLoad)
    {

        ServerLoad = ServerLoad.ToUpper();

        //get SQL Server Version
        int Version = 0;
        try
        {
            string ProductVersion = ExecuteSql.Reader_FirstRowColumnOnly("", "SELECT SERVERPROPERTY('ProductVersion')");
            ProductVersion = ProductVersion.Substring(0, ProductVersion.IndexOf('.'));
            Version = Convert.ToInt32(ProductVersion);
        }
        catch (Exception e)
        {
            SendMessage.PostBack(e.ToString());
        }
        /**
         * 9  - SQL Server 2005
         * 10 - SQL Server 2008 R2
         * 11 - SQL Server 2012
         * 12 - SQL Server 2014
         * 13 - SQL Server 2016
         * 14 - SQL Server 2017 
         * 14 - SQL Server 2019 
        **/

        if (ServerLoad == "GUIDE") // GUIA DE USO
        {
            SendMessage.PostBack(guide.Query());
        }

        if (ServerLoad == "LOADS_DB_CHANGE") // CARREGA DADOS DE ALTERAÇÕES DB -- TABLE [ServerAudi]
        {
            ExecuteSql.ExecuteQuery(stpCargaAlteracaoDB.Query());
        }

        if (ServerLoad == "LOADS_FRAGM_INDEX") // CARREGA DADOS DE FRAGMENTAÇÃO DE INDICES -- TABLE [Servidor] e [BaseDados] e [Tabela] e [HistoricoFragmentacaoIndice]
        {
            ExecuteSql.ExecuteQuery(stpCargaFragmentacaoIndice.Query());
        }

        if (ServerLoad == "LOADS_HIST_USAGE_ARCHIVE") // CARREGA DADOS DE UTILIZAÇÃO DE ARQUIVOS -- TABLE [HistoricoUtilizacaoArquivo]
        {
            ExecuteSql.ExecuteQuery(stpCargaHistoricoUtilizacaoArquivo.Query());
        }

        if (ServerLoad == "LOADS_HIST_WS") // CARREGA DADOS DE UTILIZAÇÃO WAITS STATS -- TABLE [HistoricoWaitsStats]
        {
            ExecuteSql.ExecuteQuery(stpCargaHistoricoWaitsStats.Query());
        }

        if (ServerLoad == "LOADS_TABLES_SIZE") // CARREGA DADOS DE TAMANHO DE TABELAS -- TABLE [Servidor] e [BaseDados] e [Tabela] e [HistoricoTamanhoTabela]
        {
            ExecuteSql.ExecuteQuery(stpCargaTamanhosTabelas.Query());
        }

        if (ServerLoad == "LOADS_ACCOUNTANTS") // CARREGA DADOS DE CONTADORES -- TABLE [ContadorRegistro] 
        {
            ExecuteSql.ExecuteQuery(stpCargaContadores.Query());
        }

        if (Version >= 11) // VERIFICA VERSAO DO MSSQL PARA RODAR ETAPA
        {
            if (ServerLoad == "LOADS_DB_ERROR_HISTORY") // CARREGA HISTORICO DE ERROS -- TABLE [ContadorRegistro] 
            {
                ExecuteSql.ExecuteQuery(stpCargaHistoricoErrosDB.Query());
            }

            if (ServerLoad == "LOADS_LOG_ALWAYSON") // CARREGA INFORMAÇÕES DE ALWAYSON -- TABLE [HistoricoAlwaysOn] 
            {
                ExecuteSql.ExecuteQuery(stpCargaLogAlwaysOn.Query());
            }
        }
        else
        {
            SendMessage.PostBack("Versão do MSSQL não suportada");
        }
        
        // TESTE DE FUNCIONAMENTO 

        if (ServerLoad == "TEST")
        {
            ExecuteSql.ExecuteQuery(stpTestTools.Query());
        }

    }

}


