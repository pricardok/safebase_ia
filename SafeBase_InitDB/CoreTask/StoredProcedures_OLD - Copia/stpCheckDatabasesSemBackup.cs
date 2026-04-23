using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckDatabasesSemBackup()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                DECLARE @Dt_Referencia DATETIME
	            SELECT @Dt_Referencia = GETDATE()
	
	            -- Verifica as databases sem backup nas últimas 16 horas
	            IF ( OBJECT_ID('tempdb..#CheckDatabasesSemBackup') IS NOT NULL)
	            DROP TABLE #CheckDatabasesSemBackup

	            SELECT A.name AS Nm_Database
	            INTO #CheckDatabasesSemBackup
	            FROM [sys].[databases] A
	            LEFT JOIN [msdb].[dbo].[backupset] B ON B.[database_name] = A.name AND [type] IN ('D','I')
											            and [backup_start_date] >= DATEADD(hh, -16, @Dt_Referencia)
	            WHERE	B.[database_name] IS NULL AND A.[name] NOT IN ('tempdb','ReportServerTempDB') AND state_desc <> 'OFFLINE'
	
	            TRUNCATE TABLE [dbo].[CheckDatabasesSemBackup]
	
	            INSERT INTO [dbo].[CheckDatabasesSemBackup] (Nm_Database)
	            select Nm_Database 
	            from #CheckDatabasesSemBackup
			  
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckDatabasesSemBackup] ( Nm_Database )
		            SELECT 'Sem registro de Databases Sem Backup nas últimas 16 horas.'
	            END
	            ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};