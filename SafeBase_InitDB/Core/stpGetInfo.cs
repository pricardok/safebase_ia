using System.Data;
using System.Data.SqlClient;
using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using InitDB.Client;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using System.Net;

    public partial class StoredProcedures
    {
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void stpGetInfo(string What)

        {
            What = What.ToUpper();
            if (What == "DBA")
            {
                SendMessage.PostBack(@" 
                
            EXEC HELP
            
            ");
            }
            else
            {
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
            }

            string[,] strArray1 = new string[20, 3];
            string[] strArray2 = new string[2];
            strArray1[0, 0] = @"

            COMANDOS DISPONÍVEIS:
            " + Environment.NewLine;
            strArray1[0, 1] = @"
            HELP - Lista os comandos disponíveis para stpGetInfo 
            LIST - Lista os databases existentes
            STATUS - Analisa se as databases de usuário estão online
            RECOVERYMODEL - Analisa se o recovery model das bases de usuário estão como FULL
            READONLY - Analisa se existe alguma base de usuário em modo read only
            AUTOCLOSE - Analisa se alguma base de usuário está configurada para auto close
            AUTOSHRINK - Analisa se alguma base de usuário está configurada para executar auto shrink
            PAGEVERIFY - Analisa se alguma base de usuário não está configurada para usar CHECKSUM na verificação de páginas
            AUTOCREATESTATS - Analisa se alguma base de usuário não está configurada para criar estatísticas automaticamente
            AUTOUPDATESTATS - Analisa se aguma base de usuário não está configurada para atualizar estatísticas automaticamente
            AUTOUPDATESTATSASYNC - Analisa se alguma base de usuário está configurada para atualizar estatísticas em modo assíncrono
            BACKUP - Analisa a situação dos backups 
            DBCONFIG - Analisa somente as configurações das databases 
            INDEX - Analisa os índices das bases de dados (esta operação pode demorar em bases grandes) 
            MISSINGINDEX - Analisa os índices ausentes das bases de dados 
            STATS - Analisa as estatísticas das bases de dados 
            VLF - Analisa os Virtual Log Files das bases de dados 
            BLOCKS - Analisa a ocorrência de bloqueios no momento da execução do comando (esta operação não utiliza filtro de database) 
            MEMORY - Analisa as configurações de memória do servidor e do SQL Server (esta operação não utiliza filtro de database) 
            MAXDOP - Analisa as configurações de utilização do processador para queries que entram em paralelismo 
            TEMPDB - Analisa se os datafiles da TEMPDB estão de acordo com o número de processadores 
            FULL - Executa todas as análises (esta operação pode demorar em bases grandes) 
            <COMANDO>=<DATABASE> - Executa o comando especificado apenas na database informada. Ex: FULL=ECOMMERCE irá executar o comando FULL apenas na base CONTOSO";

            strArray1[0, 2] = "False";
            strArray1[1, 0] = Environment.NewLine + 
            Environment.NewLine + @"Coletando informações dos databases existentes: " + Environment.NewLine;
            strArray1[1, 1] = "SELECT d.name AS [Database], d.database_id AS [Database ID],        CONVERT(CHAR(10), d.create_date, 103) + ' ' + CONVERT(CHAR(8), d.create_date, 108) AS [Create Date],        d.compatibility_level AS [Compatibility Level],        d.collation_name AS [Collation Name] FROM sys.databases AS d WHERE <<<<DATABASE>>>> ORDER BY d.name";
            strArray1[1, 2] = "False";
            strArray1[2, 0] = Environment.NewLine + "Analisando status das bases de dados: " + Environment.NewLine;
            strArray1[2, 1] = "SELECT d.name AS [Database] ,d.state_desc AS [State Desc]      ,CASE WHEN d.state_desc = 'ONLINE' THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>> ORDER BY d.name ";
            strArray1[2, 2] = "False";
            strArray1[3, 0] = Environment.NewLine + "Analisando recovery model das bases de dados: " + Environment.NewLine;
            strArray1[3, 1] = "SELECT d.name AS [Database] ,d.recovery_model_desc AS [Recovery Model]      ,CASE WHEN d.recovery_model_desc = 'FULL' THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>> ORDER BY name ";
            strArray1[3, 2] = "False";
            strArray1[4, 0] = Environment.NewLine + "Analisando read only das bases de dados: " + Environment.NewLine;
            strArray1[4, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_read_only = 0 THEN 'NÃO' ELSE 'SIM' END AS [Read Only]      ,CASE WHEN d.is_read_only = 0 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>>ORDER BY d.name";
            strArray1[4, 2] = "False";
            strArray1[5, 0] = Environment.NewLine + "Analisando auto close das bases de dados: " + Environment.NewLine;
            strArray1[5, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_auto_close_on = 0 THEN 'NÃO' ELSE 'SIM' END AS [Auto Close]      ,CASE WHEN d.is_auto_close_on = 0 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>>ORDER BY d.name";
            strArray1[5, 2] = "False";
            strArray1[6, 0] = Environment.NewLine + "Analisando auto shrink das bases de dados: " + Environment.NewLine;
            strArray1[6, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_auto_shrink_on = 0 THEN 'NÃO' ELSE 'SIM' END AS [Auto Shrink]      ,CASE WHEN d.is_auto_shrink_on = 0 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>>ORDER BY d.name";
            strArray1[6, 2] = "False";
            strArray1[7, 0] = Environment.NewLine + "Analisando page verify das bases de dados: " + Environment.NewLine;
            strArray1[7, 1] = "SELECT d.name AS [Database] ,d.page_verify_option_desc AS [Page Verify]      ,CASE WHEN d.page_verify_option_desc = 'CHECKSUM' THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>>ORDER BY d.name";
            strArray1[7, 2] = "False";
            strArray1[8, 0] = Environment.NewLine + "Analisando auto create stats das bases de dados: " + Environment.NewLine;
            strArray1[8, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_auto_create_stats_on = 0 THEN 'NÃO' ELSE 'SIM' END AS [Auto Create Stats]      ,CASE WHEN d.is_auto_create_stats_on = 1 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>>ORDER BY d.name";
            strArray1[8, 2] = "False";
            strArray1[9, 0] = Environment.NewLine + "Analisando auto update stats das bases de dados: " + Environment.NewLine;
            strArray1[9, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_auto_update_stats_on = 0 THEN 'NÃO' ELSE 'SIM' END AS [Auto Update Stats]      ,CASE WHEN d.is_auto_update_stats_on = 1 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>> ORDER BY d.name";
            strArray1[9, 2] = "False";
            strArray1[10, 0] = Environment.NewLine + "Analisando auto update stats async das bases de dados: " + Environment.NewLine;
            strArray1[10, 1] = "SELECT d.name AS [Database] ,CASE WHEN d.is_auto_update_stats_async_on = 0 THEN 'NÃO' ELSE 'SIM' END AS [Auto Update Stats Async]      ,CASE WHEN d.is_auto_update_stats_async_on = 0 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM sys.databases AS d WHERE <<<<DATABASE>>>> ORDER BY d.name";
            strArray1[10, 2] = "False";
            strArray1[11, 0] = Environment.NewLine + "Analisando backups das bases de dados: " + Environment.NewLine;
            strArray1[11, 1] = ";WITH bkpfull (name, recoveryModel, dataBackup)  AS(      SELECT d.name,             d.recovery_model_desc,             MAX(ISNULL(f.backup_finish_date, '1900-01-01'))-- usado isnull para evitar warning de MAX com NULL na saida do resultado " + Environment.NewLine + "     FROM sys.databases AS d      LEFT JOIN msdb.dbo.backupset AS f ON f.database_name = d.name      WHERE <<<<DATABASE>>>> AND (f.type = 'D' OR f.type IS NULL)      GROUP BY d.name,               d.recovery_model_desc  ),  bkplog(name, recoveryModel, dataBackup)  AS(      SELECT d.name,             d.recovery_model_desc,             MAX(ISNULL(f.backup_finish_date, '1900-01-01'))-- usado isnull para evitar warning de MAX com NULL na saida do resultado " + Environment.NewLine + "     FROM sys.databases AS d      LEFT JOIN msdb.dbo.backupset AS f ON f.database_name = d.name      WHERE <<<<DATABASE>>>> AND f.type = 'L'      GROUP BY d.name,               d.recovery_model_desc  )  SELECT f.name AS [Database]        ,f.recoveryModel AS [Recovery Model]        ,CASE YEAR(f.dataBackup)WHEN 1900 THEN '-> VERIFICAR' ELSE CONVERT(VARCHAR(10), f.dataBackup, 103) + ' ' + CONVERT(VARCHAR(8), f.dataBackup, 108) END AS [Data backup full]        ,CASE WHEN f.recoveryModel = 'SIMPLE' THEN 'N/A' ELSE CASE WHEN l.dataBackup IS NULL THEN '-> VERIFICAR' ELSE CONVERT(VARCHAR(10), l.dataBackup, 103) + ' ' + CONVERT(VARCHAR(8), l.dataBackup, 108) END END AS [Data backup log] -- usado IS NULL pois no join das duas CTEs essa data virá como null nos casos de recovery model simple " + Environment.NewLine + " FROM bkpfull AS f  LEFT JOIN bkplog AS l ON l.name = f.name  ORDER BY f.name";
            strArray1[11, 2] = "False";
            strArray1[12, 0] = Environment.NewLine + "Analisando índices existentes das bases de dados: " + Environment.NewLine;
            strArray1[12, 1] = "USE <<<<DATABASE>>>>; SELECT i.[Database] ,i.Quantidade AS Indices       ,f.Quantidade AS Fragmentados   FROM(          SELECT DB_NAME() AS [Database]                ,COUNT(si.object_id) AS Quantidade        FROM sys.indexes si        INNER JOIN sys.objects so on si.object_id = so.object_id        WHERE so.type = 'U'        AND si.type <> 0      ) AS i  LEFT JOIN (          SELECT DB_NAME() AS[Database]              , COUNT(*) AS Quantidade          FROM sys.dm_db_index_physical_stats(db_id(), NULL, NULL, NULL, NULL) AS indexstats          INNER JOIN sys.databases AS d ON d.database_id = indexstats.database_id          WHERE d.state_desc = 'ONLINE'            AND indexstats.database_id = DB_ID()            AND indexstats.page_count >= 8            AND indexstats.avg_fragmentation_in_percent >= 20      ) AS f ON f.[Database] = i.[Database]";
            strArray1[12, 2] = "True";
            strArray1[13, 0] = Environment.NewLine + "Analisando indices inexistentes nas bases de dados: " + Environment.NewLine;
            strArray1[13, 1] = "USE <<<<DATABASE>>>>; SELECT DB_NAME() AS [Database] ,ISNULL(MIN(CASE WHEN MIGS.avg_total_user_cost BETWEEN 1.0 AND 1.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 1 MIN acessos]       ,ISNULL(AVG(CASE WHEN MIGS.avg_total_user_cost BETWEEN 1.0 AND 1.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 1 AVG acessos]       ,ISNULL(MAX(CASE WHEN MIGS.avg_total_user_cost BETWEEN 1.0 AND 1.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 1 MAX acessos]       ,DB_NAME() AS [Database]       ,ISNULL(MIN(CASE WHEN MIGS.avg_total_user_cost BETWEEN 2.0 AND 4.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 2 A 4 MIN acessos]       ,ISNULL(AVG(CASE WHEN MIGS.avg_total_user_cost BETWEEN 2.0 AND 4.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 2 A 4 AVG acessos]       ,ISNULL(MAX(CASE WHEN MIGS.avg_total_user_cost BETWEEN 2.0 AND 4.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 2 A 4 MAX acessos]       ,DB_NAME() AS [Database]       ,ISNULL(MIN(CASE WHEN MIGS.avg_total_user_cost BETWEEN 5.0 AND 9.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 5 A 9 MIN acessos]       ,ISNULL(AVG(CASE WHEN MIGS.avg_total_user_cost BETWEEN 5.0 AND 9.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 5 A 9 AVG acessos]       ,ISNULL(MAX(CASE WHEN MIGS.avg_total_user_cost BETWEEN 5.0 AND 9.99999999999999 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 5 A 9 MAX acessos]       ,DB_NAME() AS [Database]       ,ISNULL(MIN(CASE WHEN MIGS.avg_total_user_cost >= 10 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 10 + MIN acessos]       ,ISNULL(AVG(CASE WHEN MIGS.avg_total_user_cost >= 10 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 10 + AVG acessos]       ,ISNULL(MAX(CASE WHEN MIGS.avg_total_user_cost >= 10 THEN MIGS.user_seeks + MIGS.user_scans END), 0) AS [Custo 10 + MAX acessos] FROM sys.dm_db_missing_index_group_stats AS MIGS INNER JOIN sys.dm_db_missing_index_groups AS MIG ON MIGS.group_handle = MIG.index_group_handle INNER JOIN sys.dm_db_missing_index_details AS MID ON MIG.index_handle = MID.index_handle WHERE MIGS.last_user_seek >= DATEDIFF(month, GetDate(), -1) ";
            strArray1[13, 2] = "True";
            strArray1[14, 0] = Environment.NewLine + "Analisando estatísticas desatualizadas nas bases de dados: " + Environment.NewLine;
            strArray1[14, 1] = "USE <<<<DATABASE>>>>; SELECT b.Nome AS [Database] ,b.Quantidade       ,CASE WHEN b.DataAtualizacao IS NOT NULL THEN b.DataAtualizacao ELSE 'NÃO DISPONÍVEL' END  AS [Atualização mais antiga]       ,CASE WHEN b.Quantidade = 0 THEN 'OK' ELSE '-> VERIFICAR' END AS Resultado FROM ( SELECT a.Nome       ,COUNT(CASE WHEN a.rowmodctr > 0 AND DATEDIFF(DAY, a.DataAtualizacao, GETDATE()) > 1 THEN 1 ELSE NULL END)  AS Quantidade       ,CONVERT(CHAR(10), MIN(a.DataAtualizacao), 103) +' ' + CONVERT(CHAR(8), MIN(a.DataAtualizacao), 108) AS DataAtualizacao  FROM(    SELECT DB_NAME() AS Nome          ,si.rowmodctr           ,STATS_DATE(s.object_id, s.stats_id) AS DataAtualizacao     FROM sys.stats AS s     INNER JOIN sys.objects AS o ON o.object_id = s.object_id                                AND o.type = 'U'     INNER JOIN sys.sysindexes AS si ON si.name = s.name ) AS a GROUP BY a.Nome ) AS b ";
            strArray1[14, 2] = "True";
            strArray1[15, 0] = Environment.NewLine + "Analisando Virtual Log Files:" + Environment.NewLine;
            strArray1[15, 1] = "USE <<<<DATABASE>>>>; DECLARE @dbccLoginfo2012 TABLE(     RecoveryUnitId INT    ,fileid SMALLINT    ,file_size BIGINT    ,start_offset BIGINT    ,fseqno INT    ,[status] TINYINT    ,parity TINYINT    ,create_lsn NUMERIC(25, 0) ) INSERT INTO @dbccLoginfo2012 EXEC('DBCC LOGINFO') SELECT DB_NAME() AS [Database], COUNT(*) [VLF], CASE WHEN COUNT(*) <= 250 THEN 'OK' ELSE '-> VERIFICAR' END AS [Resultado] FROM @dbccLoginfo2012";
            strArray1[15, 2] = "True";
            strArray1[16, 0] = Environment.NewLine + "Analisando se existem bloqueios ocorrendo: ";
            strArray1[16, 1] = "SELECT r.session_id AS [Sessão], r.blocking_session_id AS [Bloqueada por],        s.host_name AS [Host cliente],        c.client_net_address [IP host cliente],        s.login_name [Usuário],        DB_NAME(r.database_id) AS [Database],        SUBSTRING(s.program_name, 1, (SELECT MAX(LEN(name)) AS Tamanho FROM sys.databases)) AS [Programa cliente],        CONVERT(CHAR(10), r.start_time, 103) + ' ' + CONVERT(CHAR(8), r.start_time, 108) AS [Data/Hora início],        CONVERT(CHAR(10), GETDATE(), 103) + ' ' + CONVERT(CHAR(8), GETDATE(), 108)  AS [Data/Hora agora],        DATEDIFF(MINUTE, r.start_time, GETDATE()) AS [Minutos em execução],        CASE r.transaction_isolation_level            WHEN 0 THEN 'Unspecified'            WHEN 1 THEN 'ReadUncomitted'            WHEN 2 THEN 'ReadCommitted'            WHEN 3 THEN 'Repeatable'            WHEN 4 THEN 'Serializable'            WHEN 5 THEN 'Snapshot'        END AS [Nível de isolamento],        r.status AS [Status],        r.command AS [Comando],        r.percent_complete AS [Percentual completo],        r.reads AS [Leituras],        r.writes AS [Escritas],        r.wait_time AS [Tempo aguardando],        r.total_elapsed_time AS [Tempo total executando],        r.row_count AS [Contagem de linhas],        est.text AS [Comando SQL (batch)] FROM sys.dm_exec_connections AS c(NOLOCK) LEFT JOIN sys.dm_exec_requests AS r(NOLOCK) ON c.session_id = r.session_id LEFT JOIN sys.dm_exec_sessions AS s(NOLOCK) ON s.session_id = r.session_id OUTER APPLY sys.dm_exec_sql_text(sql_handle) AS est WHERE r.session_id > 50 AND r.session_id <> @@SPID--AND r.command not in ('DB MIRROR', 'BRKR TASK', 'TASK MANAGER', 'FT GATHERER') ORDER BY r.start_time";
            strArray1[16, 2] = "False";
            strArray1[17, 0] = Environment.NewLine + "Analisando as configurações atuais de memória do servidor e do SQL Server (As configurações recomendadas nesta análise são genéricas e podem ser diferentes conforme os requisitos do ambiente): ";
            strArray1[17, 1] = "SELECT CAST(CAST(c.value AS BIGINT) AS DECIMAL(12, 0)) AS [Memória SQL(MB)]       ,CAST(m.total_physical_memory_kb / 1024.0 AS DECIMAL(12, 2)) AS [Memória total servidor(MB)]       ,CASE WHEN c.value = c.maximum THEN 'NÃO' ELSE 'SIM' END AS [Memória SQL Configurada?]       , CAST((CAST(m.total_physical_memory_kb AS BIGINT) * 0.80) / 1024.0 AS DECIMAL(12, 0)) AS [Configuração Recomendada(MB)] FROM sys.configurations AS c CROSS APPLY sys.dm_os_sys_memory AS m WHERE c.name = 'max server memory (MB)' ";
            strArray1[17, 2] = "False";
            strArray1[18, 0] = Environment.NewLine + "Analisando as configurações de paralelismo da instância (Max degree of parallelism): ";
            strArray1[18, 1] = "SELECT p.LogicalCPU AS [CPUs lógicas] ,p.PhysicalCPU AS [CPUs físicas]       ,p.NumaNodes AS [Numa nodes]       ,[cost threshold for parallelism] AS [Cost threshold for parallelism]       ,[max degree of parallelism] AS [MAXDOP]       ,CAST(CASE                 WHEN p.LogicalCPU > 8 THEN 8                 WHEN p.LogicalCPU <= 4 THEN 0                 WHEN p.LogicalCPU BETWEEN 5 AND 8 THEN CASE WHEN(p.LogicalCPU * 0.75) > (p.LogicalCPU / p.NumaNodes) THEN(p.LogicalCPU / p.NumaNodes) * 0.75 ELSE p.LogicalCPU * 0.75 END             END AS INT) AS [MAXDOP recomendado]       ,CASE            WHEN p.[max degree of parallelism] = CAST(CASE                                                          WHEN p.LogicalCPU > 8 THEN 8                                                           WHEN p.LogicalCPU <= 4 THEN 0                                                           WHEN p.LogicalCPU BETWEEN 5 AND 8 THEN CASE WHEN (p.LogicalCPU* 0.75) > (p.LogicalCPU / p.NumaNodes) THEN(p.LogicalCPU / p.NumaNodes) * 0.75 ELSE p.LogicalCPU * 0.75 END                                                 END AS INT) THEN 'OK' ELSE '-> VERIFICAR' END AS Resultado FROM(SELECT c.name            ,c.value            ,s.cpu_count AS LogicalCPU            ,s.cpu_count / s.hyperthread_ratio AS PhysicalCPU            ,(SELECT MAX(memory_node_id) + 1 FROM sys.dm_os_memory_clerks WHERE memory_node_id < 64) AS NumaNodes      FROM sys.configurations AS c      CROSS JOIN sys.dm_os_sys_info AS s      WHERE c.name IN ('max degree of parallelism', 'cost threshold for parallelism') ) a PIVOT(      MAX(value)      FOR name IN([max degree of parallelism], [cost threshold for parallelism]) ) p";
            strArray1[18, 2] = "False";
            strArray1[19, 0] = "Analisando datafiles da TEMPDB... ";
            strArray1[19, 1] = "USE tempdb; IF OBJECT_ID('tempdb..#tfile', 'U') IS NOT NULL  DROP TABLE tempdb..#tfile; CREATE TABLE #tfile(      name SYSNAME NULL     ,fileid SMALLINT NULL     ,filename NCHAR(260)NULL     , filegroup SYSNAME NULL     , size NVARCHAR(15)NULL     , maxsize NVARCHAR(15) NULL     , growth NVARCHAR(15) NULL     , usage VARCHAR(9) NULL ); DECLARE @cpu INT; SELECT @cpu = cpu_count FROM sys.dm_os_sys_info; SELECT COUNT(*) AS Datafiles       ,@cpu AS CPU       ,CASE WHEN @cpu <= 8 THEN @cpu ELSE 8 END AS [Datafiles recomendados]       ,CASE            WHEN @cpu <= 8 THEN                CASE WHEN COUNT(*) = @cpu THEN 'OK' ELSE '-> VERIFICAR' END            ELSE                CASE WHEN COUNT(*) = 8 THEN 'OK' ELSE '-> VERIFICAR' END        END AS Resultado ";
            strArray1[19, 2] = "False";
            if (What.IndexOf("=") > 0)
            {
                strArray2 = What.Split('=');
                strArray2[1] = strArray2[1].Trim().Replace("'", "").Replace(";", "");
                if (strArray2[1].Length == 0)
                {
                    SqlContext.Pipe.Send(@"
        ERRO: Informe uma base de dados para continuar ou execute apenas o comando sem o = ex: EXEC [dbo].[stpGetInfo] 'FULL'");
                    return;
                }
                SqlConnection sqlConnection = new SqlConnection("context connection=true");
                using (sqlConnection)
                {
                    sqlConnection.Open();
                    SqlCommand sqlCommand = new SqlCommand();
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandText = "SELECT name FROM sys.databases WHERE name = '" + strArray2[1] + "'";
                    if (!sqlCommand.ExecuteReader().HasRows)
                    {
                        SqlContext.Pipe.Send(@"
        ERRO: A database informada não existe. Utilize o comando abaixo para listar as databases existentes:

        EXEC [dbo].[stpGetInfo] 'LIST';");
                        sqlConnection.Close();
                        sqlConnection.Dispose();
                        return;
                    }
                    sqlConnection.Close();
                }
                sqlConnection.Dispose();
            }
            else
            {
                strArray2[0] = What;
                strArray2[1] = (string)null;
            }
            strArray2[0] = strArray2[0].ToUpper().Trim();
            switch (strArray2[0])
            {
                case "HELP":
                    SqlContext.Pipe.Send(strArray1[0, 0]);
                    SqlContext.Pipe.Send(strArray1[0, 1] + Environment.NewLine + Environment.NewLine);
                    break;
                case "LIST":
                    SqlContext.Pipe.Send(strArray1[1, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[1, 1], Convert.ToBoolean(strArray1[1, 2]), strArray2[1]);
                    break;
                case "STATUS":
                    SqlContext.Pipe.Send(strArray1[2, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[2, 1], Convert.ToBoolean(strArray1[2, 2]), strArray2[1]);
                    break;
                case "RECOVERYMODEL":
                    SqlContext.Pipe.Send(strArray1[3, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[3, 1], Convert.ToBoolean(strArray1[3, 2]), strArray2[1]);
                    break;
                case "READONLY":
                    SqlContext.Pipe.Send(strArray1[4, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[4, 1], Convert.ToBoolean(strArray1[4, 2]), strArray2[1]);
                    break;
                case "AUTOCLOSE":
                    SqlContext.Pipe.Send(strArray1[5, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[5, 1], Convert.ToBoolean(strArray1[5, 2]), strArray2[1]);
                    break;
                case "AUTOSHRINK":
                    SqlContext.Pipe.Send(strArray1[6, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[6, 1], Convert.ToBoolean(strArray1[6, 2]), strArray2[1]);
                    break;
                case "PAGEVERIFY":
                    SqlContext.Pipe.Send(strArray1[7, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[7, 1], Convert.ToBoolean(strArray1[7, 2]), strArray2[1]);
                    break;
                case "AUTOCREATESTATS":
                    SqlContext.Pipe.Send(strArray1[8, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[8, 1], Convert.ToBoolean(strArray1[8, 2]), strArray2[1]);
                    break;
                case "AUTOUPDATESTATS":
                    SqlContext.Pipe.Send(strArray1[9, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[9, 1], Convert.ToBoolean(strArray1[9, 2]), strArray2[1]);
                    break;
                case "AUTOUPDATESTATSASYNC":
                    SqlContext.Pipe.Send(strArray1[10, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[10, 1], Convert.ToBoolean(strArray1[10, 2]), strArray2[1]);
                    break;
                case "BACKUP":
                    SqlContext.Pipe.Send(strArray1[11, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[11, 1], Convert.ToBoolean(strArray1[11, 2]), strArray2[1]);
                    break;
                case "INDEX":
                    SqlContext.Pipe.Send(strArray1[12, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[12, 1], Convert.ToBoolean(strArray1[12, 2]), strArray2[1]);
                    break;
                case "MISSINGINDEX":
                    SqlContext.Pipe.Send(strArray1[13, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[13, 1], Convert.ToBoolean(strArray1[13, 2]), strArray2[1]);
                    break;
                case "STATS":
                    SqlContext.Pipe.Send(strArray1[14, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[14, 1], Convert.ToBoolean(strArray1[14, 2]), strArray2[1]);
                    break;
                case "VLF":
                    SqlContext.Pipe.Send(strArray1[15, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[15, 1], Convert.ToBoolean(strArray1[15, 2]), strArray2[1]);
                    break;
                case "BLOCKS":
                    SqlContext.Pipe.Send(strArray1[16, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[16, 1], Convert.ToBoolean(strArray1[16, 2]), strArray2[1]);
                    break;
                case "MEMORY":
                    SqlContext.Pipe.Send(strArray1[17, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[17, 1], Convert.ToBoolean(strArray1[17, 2]), strArray2[1]);
                    break;
                case "MAXDOP":
                    SqlContext.Pipe.Send(strArray1[18, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[18, 1], Convert.ToBoolean(strArray1[18, 2]), strArray2[1]);
                    break;
                case "TEMPDB":
                    SqlContext.Pipe.Send(strArray1[19, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[19, 1], Convert.ToBoolean(strArray1[19, 2]), strArray2[1]);
                    break;
                case "DBCONFIG":
                    SqlContext.Pipe.Send(strArray1[1, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[1, 1], Convert.ToBoolean(strArray1[1, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[2, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[2, 1], Convert.ToBoolean(strArray1[2, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[3, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[3, 1], Convert.ToBoolean(strArray1[3, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[4, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[4, 1], Convert.ToBoolean(strArray1[4, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[5, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[5, 1], Convert.ToBoolean(strArray1[5, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[6, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[6, 1], Convert.ToBoolean(strArray1[6, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[7, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[7, 1], Convert.ToBoolean(strArray1[7, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[8, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[8, 1], Convert.ToBoolean(strArray1[8, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[9, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[9, 1], Convert.ToBoolean(strArray1[9, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[10, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[10, 1], Convert.ToBoolean(strArray1[10, 2]), strArray2[1]);
                    SqlContext.Pipe.Send(strArray1[11, 0]);
                    StoredProcedures.ExecuteSQLGetInfo(strArray1[11, 1], Convert.ToBoolean(strArray1[11, 2]), strArray2[1]);
                    break;
                case "FULL":
                    for (int index = 1; index < strArray1.GetLength(0); ++index)
                    {
                        SqlContext.Pipe.Send(strArray1[index, 0]);
                        StoredProcedures.ExecuteSQLGetInfo(strArray1[index, 1], Convert.ToBoolean(strArray1[index, 2]), strArray2[1]);
                    }
                    break;
                default:
                    SqlContext.Pipe.Send("ERRO: Comando \"" + What + "\" não encontrado. Informe HELP para obter ajuda.");
                    break;
            }
            SqlContext.Pipe.Send(@" 
            © Copyright 2026 - SafeBase 2.0.8 - http://www.datatips.info/initdb");
        }
    //© Copyright 2020 Safeweb - GetInfoAnalysis 2.0.1 - http://www.datatips.info/initdb

    public static void ExecuteSQLGetInfo(string scriptLine, bool multipleDatabases = false, string filtroDatabase = null)
        {
            try
            {
                SqlConnection connection = new SqlConnection("context connection=true");
                using (connection)
                {
                    int tamanhoColuna = StoredProcedures.GetTamanhoColuna(connection);
                    bool flag = true;
                    List<string> stringList = new List<string>();
                    connection.Open();
                    if (multipleDatabases)
                    {
                        SqlCommand sqlCommand1 = new SqlCommand();
                        sqlCommand1.CommandType = CommandType.Text;
                        sqlCommand1.Connection = connection;
                        sqlCommand1.CommandText = @"
                                                    SELECT name FROM sys.databases 
                                                    WHERE state_desc = 'ONLINE' 
                                                    AND database_id > 4 " + (filtroDatabase != null ? @" 
                                                    AND name = '" + filtroDatabase + "'" : "") + @" 
                                                    AND name NOT IN (
												                    SELECT 
												                    ADC.database_name                               
												                    FROM sys.availability_groups_cluster as AGC                                                                            
												                    JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
												                    JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
												                    JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
												                    WHERE ARS.is_local = 1
												                    AND ARS.role_desc LIKE 'SECONDARY'
											                        )
                                                    ORDER BY name";
                        SqlDataReader sqlDataReader = sqlCommand1.ExecuteReader();
                        if (sqlDataReader.HasRows)
                        {
                            while (sqlDataReader.Read())
                                stringList.Add(sqlDataReader[0].ToString());
                            sqlDataReader.Close();
                            SqlCommand sqlCommand2 = new SqlCommand();
                            sqlCommand2.CommandType = CommandType.Text;
                            sqlCommand2.Connection = connection;
                            foreach (string newValue in stringList)
                            {
                                sqlCommand2.CommandText = scriptLine.Replace("<<<<DATABASE>>>>", newValue);
                                SqlDataReader reader = sqlCommand2.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        if (flag)
                                        {
                                            SqlContext.Pipe.Send(StoredProcedures.FormataLinhaTitulo(reader, tamanhoColuna));
                                            flag = false;
                                        }
                                        SqlContext.Pipe.Send(StoredProcedures.FormataLinhaDados(reader, tamanhoColuna));
                                    }
                                }
                                reader.Close();
                            }
                        }
                    }
                    else
                    {
                        SqlCommand sqlCommand = new SqlCommand();
                        sqlCommand.CommandType = CommandType.Text;
                        sqlCommand.Connection = connection;
                        sqlCommand.CommandText = scriptLine.Replace("<<<<DATABASE>>>>", filtroDatabase != null ? " d.name='" + filtroDatabase + "' " : " d.database_id > 4 ");
                        SqlDataReader reader = sqlCommand.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (flag)
                                {
                                    SqlContext.Pipe.Send(StoredProcedures.FormataLinhaTitulo(reader, tamanhoColuna));
                                    flag = false;
                                }
                                SqlContext.Pipe.Send(StoredProcedures.FormataLinhaDados(reader, tamanhoColuna));
                            }
                        }
                    }
                }
                SqlContext.Pipe.Send(Environment.NewLine + Environment.NewLine);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send(ex.ToString());
            }
            finally
            {
            }
        }

        public static string FormataLinhaTitulo(SqlDataReader reader, int tamanhoColuna)
        {
            string str = "";
            for (int ordinal = 0; ordinal < reader.FieldCount; ++ordinal)
                str = str + "#" + reader.GetName(ordinal).PadRight(tamanhoColuna - 1, ' ');
            return str;
        }

        public static string FormataLinhaDados(SqlDataReader reader, int tamanhoColuna)
        {
            string str = "";
            for (int index = 0; index < reader.FieldCount; ++index)
                str = str + " " + reader[index].ToString().PadRight(index + 1 < reader.FieldCount ? tamanhoColuna - 1 : reader[index].ToString().Length, '.');
            return str;
        }

        public static int GetTamanhoColuna(SqlConnection connection)
        {
            string str = "SELECT MAX(LEN(name)) AS Tamanho FROM sys.databases";
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.Connection = connection;
            sqlCommand.CommandText = str;
            connection.Open();
            SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
            sqlDataReader.Read();
            int num = sqlDataReader.GetInt32(0) + 10;
            connection.Close();
            return num;
        }
    }
