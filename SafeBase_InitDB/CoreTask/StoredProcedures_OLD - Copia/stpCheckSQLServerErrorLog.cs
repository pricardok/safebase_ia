using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckSQLServerErrorLog()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            IF (OBJECT_ID('tempdb..#TempLog') IS NOT NULL)
		            DROP TABLE #TempLog
	
	            CREATE TABLE #TempLog (
		            [LogDate]		DATETIME,
		            [ProcessInfo]	NVARCHAR(50),
		            [Text]			NVARCHAR(MAX)
	            )

	            IF (OBJECT_ID('tempdb..#logF') IS NOT NULL)
		            DROP TABLE #logF
	
	            CREATE TABLE #logF (
		            [ArchiveNumber] INT,
		            [LogDate]		DATETIME,
		            [LogSize]		INT 
	            )

	            -- Seleciona o número de arquivos.
	            --INSERT INTO #logF  
	            --EXEC sp_enumerrorlogs
	
	
	            -- Utilizar caso apresente erro no script acima
	            IF (OBJECT_ID('tempdb..#logFAux') IS NOT NULL)
		            DROP TABLE #logFAux
	
	            CREATE TABLE #logFAux (
		            [ArchiveNumber] INT,
		            [LogDate]		VARCHAR(20),
		            [LogSize]		INT 
	            )
	
	            -- Seleciona o número de arquivos.
	            INSERT INTO #logFAux  
	            EXEC sp_enumerrorlogs

	            insert into #logF
	            select ArchiveNumber, cast((substring(LogDate,7,4)+substring(LogDate,1,2)+substring(LogDate,4,2)) as datetime), LogSize
	            from #logFAux
	

	            DELETE FROM #logF
	            WHERE LogDate < GETDATE()-2

	            DECLARE @TSQL NVARCHAR(2000), @lC INT

	            SELECT @lC = MIN(ArchiveNumber) FROM #logF

	            -- Loop para realizar a leitura de todo o log
	            WHILE @lC IS NOT NULL
	            BEGIN
		              INSERT INTO #TempLog
		              EXEC sp_readerrorlog @lC
		              SELECT @lC = MIN(ArchiveNumber) FROM #logF
		              WHERE ArchiveNumber > @lC
	            END
	
	            TRUNCATE TABLE [dbo].[CheckSQLServerErrorLog]
	            TRUNCATE TABLE [dbo].[CheckSQLServerLoginFailed]
	            TRUNCATE TABLE [dbo].[CheckSQLServerLoginFailedEmail]

	            -- Login Failed
	            INSERT INTO [dbo].[CheckSQLServerLoginFailed]( [Text], [Qt_Erro] )
	            SELECT RTRIM([Text]), COUNT(*)
	            FROM #TempLog
	            WHERE [LogDate] >= GETDATE()-1
		            AND [Text] LIKE '%Login failed for user%'
	            GROUP BY [Text]
	
	            IF (@@ROWCOUNT <> 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckSQLServerLoginFailedEmail]( [Nr_Ordem], [Text], [Qt_Erro] )
		            SELECT TOP 10 1, [Text], [Qt_Erro]
		            FROM [dbo].[CheckSQLServerLoginFailed]
		            ORDER BY [Qt_Erro] DESC

		            INSERT INTO [dbo].[CheckSQLServerLoginFailedEmail]( [Nr_Ordem], [Text], [Qt_Erro] )
		            SELECT 2, 'TOTAL', SUM([Qt_Erro])
		            FROM [dbo].[CheckSQLServerLoginFailed]
	            END
	            ELSE
	            BEGIN
		            INSERT INTO [dbo].[CheckSQLServerLoginFailedEmail]( [Text], [Qt_Erro] )
		            SELECT 'Sem registro de Falha de Login', NULL
	            END
	
	            -- Error Log
	            INSERT INTO [dbo].[CheckSQLServerErrorLog]( [Dt_Log], [ProcessInfo], [Text] )
	            SELECT [LogDate], [ProcessInfo], [Text]
	            FROM #TempLog
	            WHERE [LogDate] >= GETDATE()-1
		            AND [ProcessInfo] <> 'Backup'
		            AND [Text] NOT LIKE '%CHECKDB%'
		            AND [Text] NOT LIKE '%Trace%'
		            AND [Text] NOT LIKE '%IDR%'
		            AND [Text] NOT LIKE 'AppDomain%'
		            AND [Text] NOT LIKE 'Unsafe assembly%'
		            AND [Text] NOT LIKE '%Login failed for user%'
		            AND [Text] NOT LIKE '%Error:%Severity:%State:%'
		            AND [Text] NOT LIKE '%Erro:%Gravidade:%Estado:%'
		            AND [Text] NOT LIKE '%No user action is required.%'
		            AND [Text] NOT LIKE '%no user action is required.%'
		
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckSQLServerErrorLog]( [Dt_Log], [ProcessInfo], [Text] )
		            SELECT NULL, NULL, 'Sem registro de Erro no Log'
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};