using System;
using System.Collections.Generic;
using System.Text;
using SafeBase_Installer.Core;

namespace SafeBase_Installer
{
    class CreateVwDB
    {
        public static string Query(string use)
        {
            return
            @"
            USE "+ use + @"
            GO

            -- CheckDirBackup
            create view vwCheckDirBackup
            as
            SELECT TOP 1   
	            ParametersXML.value('(/Customer/BackupFull/BackupPath)[1]', 'varchar(max)')
	              +CASE WHEN RIGHT((ParametersXML.value('(/Customer/BackupFull/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	              +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath
            FROM [dbo].[ConfigDB]
            
            GO
            -- vwCheckDisc
            CREATE view [dbo].[vwCheckDisc] 
            AS
            SELECT DISTINCT dovs.logical_volume_name AS LogicalName,
                dovs.volume_mount_point AS Drive,
                CONVERT(INT,dovs.available_bytes/1048576.0) AS FreeSpaceInMB
            FROM sys.master_files mf
                CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.FILE_ID) dovs
            
            GO
            -- vwCheckBackup 
            CREATE view [dbo].[vwCheckBackup] 
            AS
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
				            (Convert(varchar(35), ServerProperty('machinename')) + '-' + @@ServiceName) AS Servidor
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
   
			            UNION ALL
    
			            SELECT
				            (Convert(varchar(35), ServerProperty('machinename')) + '-' + @@ServiceName) as Servidor
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
			
                ) as A
            INNER JOIN [sys].[databases] B ON A.Banco = B.name
            WHERE B.state_desc <> 'OFFLINE'
            AND B.NAME NOT IN (SELECT 
					            ADC.database_name                               
				            FROM sys.availability_groups_cluster as AGC                                                                            
				            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
				            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
				            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
				            WHERE ARS.is_local = 1
				            AND ARS.role_desc LIKE 'SECONDARY')

        
            GO
            -- vwHistoricoFragmentacaoIndice
            CREATE VIEW [dbo].[vwHistoricoFragmentacaoIndice]
            AS
            SELECT	A.Dt_Referencia, B.Nm_Servidor, C.Nm_Database, D.Nm_Tabela, A.Nm_Indice, A.Nm_Schema, 
		            A.Avg_Fragmentation_In_Percent, A.Page_Count, A.Fill_Factor, A.Fl_Compressao
            FROM HistoricoFragmentacaoIndice A
	            join Servidor B on A.Id_Servidor = B.Id_Servidor
	            join BaseDados C on A.Id_BaseDados = C.Id_BaseDados
	            join Tabela D on A.Id_Tabela = D.Id_Tabela

            GO
            -- vwCheckProgBackup
            CREATE VIEW vwCheckProgBackup 
            AS
            SELECT session_id AS SPID, 
                   command, 
                   a.text AS query,
	               SUBSTRING(
			            REPLACE(a.text , ' ',''),
			            CHARINDEX('[' ,REPLACE(a.text , ' ',''))+1,
			            CHARINDEX(']',
				            SUBSTRING(
				            REPLACE(a.text , ' ',''),
				            CHARINDEX('[' ,REPLACE(a.text , ' ',''))+2,
				            LEN(REPLACE(a.text , ' ','')))
				            )) AS data_base,
                   start_time, 
                   percent_complete, 
                   DATEADD(second, estimated_completion_time / 1000, GETDATE()) AS estimated_completion_time
            FROM sys.dm_exec_requests r
                 CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) a
            WHERE r.command IN('BACKUP DATABASE', 'RESTORE DATABASE','BACKUP LOG');


            GO
            -- vwTamanhoTabela
            CREATE VIEW [dbo].[vwTamanhoTabela]
            AS
            SELECT	A.Dt_Referencia, B.Nm_Servidor, C.Nm_Database,D.Nm_Tabela ,A.Nm_Drive, 
		            A.Nr_Tamanho_Total, A.Nr_Tamanho_Dados, A.Nr_Tamanho_Indice, A.Qt_Linhas
            FROM HistoricoTamanhoTabela A
	            join Servidor B on A.Id_Servidor = B.Id_Servidor
	            join BaseDados C on A.Id_BaseDados = C.Id_BaseDados
	            join Tabela D on A.Id_Tabela = D.Id_Tabela
  			
            GO
            -- vwCheckAG
            CREATE VIEW vwCheckAG AS
            SELECT top 1000000000
              ar.replica_server_name,
              adc.database_name, 
              ag.name AS ag_name, 
              drs.is_local, 
              drs.is_primary_replica, 
              drs.synchronization_state_desc, 
              drs.is_commit_participant, 
              drs.synchronization_health_desc, 
              drs.recovery_lsn, 
              drs.truncation_lsn
            FROM sys.dm_hadr_database_replica_states AS drs
            INNER JOIN sys.availability_databases_cluster AS adc 
              ON drs.group_id = adc.group_id AND 
              drs.group_database_id = adc.group_database_id
            INNER JOIN sys.availability_groups AS ag
              ON ag.group_id = drs.group_id
            INNER JOIN sys.availability_replicas AS ar 
              ON drs.group_id = ar.group_id AND 
              drs.replica_id = ar.replica_id
            ORDER BY 
              ag.name, 
              ar.replica_server_name, 
              adc.database_name;

            GO
            -- vwCheckSession
            CREATE VIEW vwCheckSession as
            SELECT  spid,
                    sp.[status],
                    loginame [Login],
                    hostname, 
                    blocked BlkBy,
                    sd.name DBName, 
                    cmd Command,
                    cpu CPUTime,
                    physical_io DiskIO,
                    last_batch LastBatch,
                    [program_name] ProgramName   
            FROM master.dbo.sysprocesses sp 
            JOIN master.dbo.sysdatabases sd ON sp.dbid = sd.dbid

            GO
            create view vwCheckSizeLog as
            SELECT 
                db_name(sf.dbid) as [Database_Name],
                sf.name as [File_Name],
                (sf.size/128.0 - CAST(FILEPROPERTY(file_name(fileid), 'SpaceUsed') AS int)/128.0) AS 'Available_Space_MB'
            FROM    master..sysaltfiles sf
            inner join sys.databases sd on sf.dbid = database_id
            WHERE   groupid = 0
            and db_name(sf.dbid) not in('model','msdb','master')
            and sd.state_desc like 'ONLINE'
            and (sf.size/128.0 - CAST(FILEPROPERTY(file_name(fileid), 'SpaceUsed') AS int)/128.0) >= '246.578125'


            GO
            create view vwCheckUpTime
            as
            SELECT create_date  AS StartTime
            FROM sys.databases
            WHERE name = 'tempdb';


            ";

        }
    }
}
