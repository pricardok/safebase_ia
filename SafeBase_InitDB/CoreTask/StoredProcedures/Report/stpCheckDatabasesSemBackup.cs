using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckDatabasesSemBackup
    {
        public static string Query()
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckDatabasesSemBackup',GETDATE())";
			@"  SET NOCOUNT ON
                DECLARE @Dt_Referencia DATETIME
	            SELECT @Dt_Referencia = GETDATE()
	
	            -- Verifica as databases sem backup nas últimas 16 horas
	            IF ( OBJECT_ID('tempdb..#CheckDatabasesSemBackup') IS NOT NULL)
	            DROP TABLE #CheckDatabasesSemBackup

	            SELECT A.name AS Nm_Database
	            INTO #CheckDatabasesSemBackup
	            FROM [sys].[databases] A
	            LEFT JOIN [msdb].[dbo].[backupset] B ON B.[database_name] = A.name AND [type] IN ('D','I','L')
											            and [backup_start_date] >= DATEADD(hh, -16, @Dt_Referencia)
	            WHERE	B.[database_name] IS NULL AND A.[name] NOT IN ('tempdb','ReportServerTempDB') AND state_desc <> 'OFFLINE'
				AND A.[name] NOT IN (
									SELECT 
										ADC.database_name                               
									FROM sys.availability_groups_cluster as AGC                                                                            
									JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
									JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
									JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
									WHERE ARS.is_local = 1
									AND ARS.role_desc LIKE 'SECONDARY'
									)
	
	            TRUNCATE TABLE [dbo].[CheckDatabasesSemBackup]
	
	            INSERT INTO [dbo].[CheckDatabasesSemBackup] (Nm_Database)
	            select Nm_Database 
	            from #CheckDatabasesSemBackup
			  
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckDatabasesSemBackup] ( Nm_Database )
		            SELECT 'Sem registro de Databases Sem Backup nas últimas 16 horas.'
	            END

                TRUNCATE TABLE [dbo].[CheckDatabasesHistoricoBackup]

			    INSERT INTO [dbo].[CheckDatabasesHistoricoBackup] ( 
					      [Servidor]
					      ,[Banco]
					      ,[UltimoFull]
					      ,[DataFull]
					      ,[TamanhoFull_MB]
					      ,[UltimoDiff]
					      ,[DataDiff]
					      ,[UltimoFullDiff]
					      ,[TamanhoDiff_MB]
					      ,[UltimoLog_Min]
					      ,[DataLog]
					      ,[TamanhoLog_MB] )
					SELECT 
					      [Servidor]
					      ,[Banco]
					      ,[UltimoFull]
					      ,[DataFull]
					      ,[TamanhoFull_MB]
					      ,[UltimoDiff]
					      ,[DataDiff]
					      ,[UltimoFullDiff]
					      ,[TamanhoDiff_MB]
					      ,[UltimoLog_Min]
					      ,[DataLog]
					      ,[TamanhoLog_MB] 
				      FROM  (
							    SELECT 
									    (Convert(varchar(35), ServerProperty('machinename')) + '\' + @@ServiceName) AS Servidor
									    ,FR.Database_Name as 'Banco'
									    ,DateDiff(Day, FR.Backup_Finish_Date, GetDate()) As 'UltimoFull'
									    ,FR.Backup_Finish_Date  As 'DataFull'
									    ,Convert(Char,Convert(Numeric(12,2),(FR.Backup_Size / 1024 / 1024))) As TamanhoFull_MB
									    ,DateDiff(Day, DR.Backup_Finish_Date, GetDate()) As 'UltimoDiff'
									    ,DR.Backup_Finish_Date  As 'DataDiff'
									    ,Case 
										    When DR.Backup_Finish_Date Is Null Then Null
										    Else DateDiff(Day, FR.Backup_Finish_Date, DR.Backup_Finish_Date)
									    End As 'UltimoFullDiff'
									    ,Convert(Char,Convert(Numeric(12,2),(DR.Backup_Size / 1024 / 1024))) As TamanhoDiff_MB
									    ,DateDiff(Minute, TR.Backup_Finish_Date, GetDate()) As 'UltimoLog_Min'
									    ,TR.Backup_Finish_Date As 'DataLog'
									    ,Convert(Char,Convert(Numeric(12,2),(TR.Backup_Size / 1024 / 1024))) As TamanhoLog_MB
								    FROM 
									    msdb.dbo.backupset As FR
								    LEFT OUTER JOIN
									    msdb.dbo.backupset As TR
								    ON
									    TR.Database_Name = FR.Database_Name
								    AND TR.Type = 'L'
								    AND TR.Backup_Finish_Date =
									    (
										    (SELECT Max(Backup_Finish_Date) 
										    FROM    msdb.dbo.backupset B2 
										    WHERE   B2.Database_Name = FR.Database_Name 
										    And B2.Type = 'L')
									    )
								    LEFT OUTER JOIN
									    msdb.dbo.backupset As DR
								    ON
									    DR.Database_Name = FR.Database_Name
								    AND DR.Type = 'I'
								    AND DR.Backup_Finish_Date =
									    (
										    (SELECT Max(Backup_Finish_Date) 
										    FROM    msdb.dbo.backupset B2 
										    WHERE B2.Database_Name = FR.Database_Name 
										    And B2.Type = 'I')
									    )
								    WHERE
									    FR.Type = 'D' -- full backups only
								    AND FR.Backup_Finish_Date = 
									    (
										    SELECT Max(Backup_Finish_Date) 
										    FROM msdb.dbo.backupset B2 
										    WHERE B2.Database_Name = FR.Database_Name 
										    And   B2.Type = 'D'
									    )
								    And FR.Database_Name In (SELECT name FROM master.dbo.sysdatabases) 
								    And FR.Database_Name Not In ('tempdb','model')
    
								    UNION ALL
    
								    SELECT
									    (Convert(varchar(35), ServerProperty('machinename')) + '\' + @@ServiceName) as Servidor
									    ,Name
									    ,NULL
									    ,NULL
									    ,NULL 
									    ,NULL
									    ,NULL 
									    ,NULL
									    ,NULL
									    ,NULL
									    ,NULL
									    ,NULL
								    FROM 
									    master.dbo.sysdatabases As Record
								    WHERE
									    Name Not In(SELECT DISTINCT Database_Name FROM msdb.dbo.backupset)
								    And Name Not In('tempdb','model')
						    ) as A
				    INNER JOIN  [sys].[databases] B ON A.Banco = B.name
				    WHERE B.state_desc <> 'OFFLINE'
	            ";

        }
    }
}
