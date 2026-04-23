using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using InitDB.Client;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpToZabbix(int job, string name)
    {

        //name = "";
        string scriptLine;

        using (SqlConnection connection = new SqlConnection("context connection=true"))
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = connection;

            switch (job)
            {
                case 0: //Discover DBName
                    scriptLine = StoredProcedures.stpTZ_GuideMonitoring();
                    SendMessage.PostBack(guidemonitoring.Query());
                    break;
                 case 1: //Discover DBName
                    scriptLine = StoredProcedures.stpTZ_DiscoverDBName(name);
                    break;
                case 2: //Discover JobName
                    scriptLine = StoredProcedures.stpTZ_DiscoverJobName();
                    break;
                case 3: //BackupFull
                    scriptLine = StoredProcedures.stpTZ_CheckBackups();
                    cmd.Parameters.Add(new SqlParameter("@DatabaseName", name));
                    cmd.Parameters.Add(new SqlParameter("@backupType", "D"));
                    break;
                case 4: //BackupLog
                    scriptLine = StoredProcedures.stpTZ_CheckBackups();
                    cmd.Parameters.Add(new SqlParameter("@DatabaseName", name));
                    cmd.Parameters.Add(new SqlParameter("@backupType", "L"));
                    break;
                case 5: //BackupDiff
                    scriptLine = StoredProcedures.stpTZ_CheckBackups();
                    cmd.Parameters.Add(new SqlParameter("@DatabaseName", name));
                    cmd.Parameters.Add(new SqlParameter("@backupType", "I"));
                    break;
                case 6: //Verion MSSQL
                    scriptLine = "SELECT SERVERPROPERTY('PRODUCTVERSION')";
                    break;
                case 7: //Check Jobs
                    scriptLine = StoredProcedures.stpTZ_CheckJobs();
                    cmd.Parameters.Add(new SqlParameter("@JobName", name));
                    break;
                case 8: //Discover AlwaysOn
                    scriptLine = StoredProcedures.stpTZ_DiscoverAlwaysOn();
                    break;
                case 9: //Check AlwaysOn
                    scriptLine = StoredProcedures.stpTZ_CheckAlwaysOn();
                    cmd.Parameters.Add(new SqlParameter("@DatabaseName", name));
                    break;
                case 10: //Check Aquivos de Backup
                    scriptLine = StoredProcedures.stpTZ_DiscoverFileBackup();
                    cmd.Parameters.Add(new SqlParameter("@File", name));
                    break;
                default:
                    scriptLine = "";
                    break;
            }

            cmd.CommandText = scriptLine;

            try
            {
                connection.Open();
                cmd.CommandType = CommandType.Text;
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        SendMessage.PostBack(String.Format("{0}", reader[0]));
                    }
                }
                else
                {
                    SendMessage.PostBack("No rows found.");
                }

            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
    }

    private static string stpTZ_GuideMonitoring()
    {
        string output =
                    @"        
                        select 'Return'
                    ";
        return output;

    }

    private static string stpTZ_DiscoverAlwaysOn()
    {
        string output =
                   @"
                    SET NOCOUNT ON  
                    DECLARE @Result varchar(max)    

                    SET @Result = '{\""data\"":['   
                    SELECT @Result = @Result + '{\""{#DBNAME}\"":\""' + DB_NAME(database_id) + '\""},' FROM sys.dm_hadr_database_replica_states WHERE is_local = 1

                    SET @Result = LEFT(@Result, LEN(@Result) - 1)
                    SET @Result = @Result + ']}' collate sql_latin1_general_cp1251_cs_as

                    SELECT @Result
                    ";

        return output;
    }

    private static string stpTZ_DiscoverFileBackup()
    {
        string output =
                   @"

	                IF(OBJECT_ID('tempdb..#tb_check_file_bkp') IS NOT NULL)
		                DROP TABLE #tb_check_file_bkp;
	                CREATE TABLE #tb_check_file_bkp
	                    ([sqldb] NVARCHAR(400),
		                [dir] NVARCHAR(900));

	                DECLARE @BX NVARCHAR(1000) = (SELECT TOP 1   
									                ParametersXML.value('(/Customer/BackupFull/BackupPath)[1]', 'varchar(max)')
									                +CASE WHEN RIGHT((ParametersXML.value('(/Customer/BackupFull/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
									                +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath                                                                 
							                  FROM [dbo].[ConfigDB])

	                DECLARE @F NVARCHAR(767)
	                DECLARE cursor_d CURSOR
                    FOR 

	                    SELECT RTRIM(name) name 
	                    FROM sys.databases sd
	                    INNER JOIN fncListarDiretorio (''+@BX+'', '*') ld ON sd.name = ld.arquivo
	                    WHERE sd.state_desc not in ('OFFLINE','RESTORING','RECOVERY_PENDING') and sd.is_in_standby = 0 and sd.is_read_only = 0 and sd.database_id > 4 and [name] not like 'SafeBase'
						AND sd.NAME NOT IN (SELECT 
												ADC.database_name                               
											FROM sys.availability_groups_cluster as AGC                                                                            
											JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
											JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
											JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
											WHERE ARS.is_local = 1
											AND ARS.role_desc LIKE 'SECONDARY')
	
                    OPEN cursor_d;
                    FETCH NEXT FROM cursor_d INTO @F
                    WHILE @@FETCH_STATUS = 0

                        BEGIN 
                            DECLARE @TypeBackup VARCHAR(30) = CASE @File
										                             WHEN 'D' THEN '.bak'
										                             WHEN 'I' THEN '.dif'
										                             WHEN 'L' THEN '.trn'
										                             else '.bak'
									                               END 

                            --IF NOT EXISTS (SELECT top 1 Arquivo FROM dbo.fncListarDiretorio (''+@BX + @F+'', '*') WHERE Extensao like @TypeBackup order by  DataCriacao desc)
			                IF NOT EXISTS (	SELECT Arquivo 
						            FROM dbo.fncListarDiretorio (''+@BX + @F+'', '*') A
						            WHERE Extensao like @TypeBackup 
						            AND @F in (
									            SELECT RTRIM(Banco) 
									            FROM vwcheckbackup 
									            WHERE banco LIKE @F -- AND DataLog is not null -- AND DataDiff is not null
									            )
						            AND ((@TypeBackup = '.bak')
							            OR (@TypeBackup = '.dif' AND CONVERT(char(10), DataCriacao,126)  = CONVERT(char(10), GetDate(),126))
							            OR (@TypeBackup = '.trn' AND CONVERT(char(10), DataCriacao,126)  = CONVERT(char(10), GetDate(),126)
														            AND @F IN (SELECT RTRIM(Banco) FROM vwcheckbackup where DataLog is not null)
							            )
						            ))
                            BEGIN

				                --PRINT 'NAO ENCONTREI O AQUIVO DE BACKUP FULL'
				                insert #tb_check_file_bkp ([sqldb],[dir])
				                select @F,+@BX + @F

			                END
			                ELSE
			                BEGIN

				                PRINT 'AQUIVO DE BACKUP: '+@F+' - OK'

			                END
			
                            FETCH NEXT FROM cursor_d into @F
			
                        END;

                    CLOSE cursor_d;
                    DEALLOCATE cursor_d;

		                IF EXISTS(select * from #tb_check_file_bkp where [sqldb] <> '')
			                BEGIN
	
				                SET NOCOUNT ON  
				                DECLARE @Result varchar(max)    

				                SET @Result = '{\""data\"":['   
				                SELECT @Result = @Result + '{\""''{#DBNAME}''\"":\""' + [sqldb] + '\""},' FROM #tb_check_file_bkp

				                SET @Result = LEFT(@Result, LEN(@Result) - 1)
				                SET @Result = @Result + ']}'

				                SELECT @Result

			                END
		                ELSE
			                DECLARE @ResultN varchar(max) = '0'
			                SELECT @ResultN
                    ";

        return output;
    }

    private static string stpTZ_CheckAlwaysOn()
    {
        string output =
                @"
                DECLARE @Result int                                                                             
                                                                                                        
                SELECT                                                                                          
                                                                                                        
                    @Result = DATEDIFF(MINUTE,[AOSecondary].[SyncLastCommit],[AOPrimary].[SyncLastCommit])      
                FROM                                                                                            
                    (SELECT DB_NAME(database_id) AS[Database], last_commit_time AS[SyncLastCommit]              
                                                                                                        
                        FROM sys.dm_hadr_database_replica_states                                                
                                                                                                        
                        WHERE is_local = 1                                                                      
                    )[AOPrimary]                                                                                
                INNER JOIN                                                                                      
                    (SELECT DB_NAME(database_id) AS [Database], last_commit_time AS [SyncLastCommit]            
                        FROM sys.dm_hadr_database_replica_states                                                
                        WHERE is_local= 0                                                                       
                    ) [AOSecondary]                                                                             
                    ON[AOPrimary].[Database]=[AOSecondary].[Database]                                           
                    WHERE[AOPrimary].[Database]=@DatabaseName                                                   
                                                                                                        
                SELECT ISNULL(@Result,0)  
                ";

        return output;
    }

    private static string stpTZ_CheckBackups()
    {
        string output = @"
                        SET NOCOUNT ON

                        DECLARE @Result int

                        SELECT TOP 1 @Result = DATEDIFF(MINUTE, backup_finish_date, GETDATE())
                        FROM sys.databases as sdb
                        INNER JOIN msdb.dbo.backupset as db ON sdb.name = db.database_name
                        WHERE sdb.name = @DatabaseName       AND
                              db.type  = @backupType         AND
                              sdb.state_desc = 'ONLINE'
                        AND NOT(sdb.recovery_model_desc = 'SIMPLE' AND db.type = 'L')
						AND sdb.name not in ( 
                                    SELECT  
                                        ADC.database_name                               
                                    FROM sys.availability_groups_cluster as AGC                                                                            
                                    JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
                                    JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
                                    JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
                                    WHERE ARS.is_local = 1 
							        AND role_desc = 'SECONDARY')
                        ORDER BY backup_finish_date DESC

                        SELECT ISNULL(@Result,0)
                        ";
        return output;
    }

    private static string stpTZ_CheckJobs()
    {
        string output = @"
                        SET NOCOUNT ON  

                        DECLARE @Result int 

                        SELECT @Result = last_run_outcome   
                        FROM msdb.dbo.sysjobs j 
                        LEFT JOIN msdb.dbo.sysjobservers jh ON j.job_id = jh.job_id 
                        WHERE   
                            j.name collate sql_latin1_general_cp1251_cs_as/*Remove Acento*/ = @JobName  AND j.enabled = 1   

                        SELECT isnull(@Result,0) 
                        ";

        return output;
    }

    private static string stpTZ_DiscoverDBName(string key)
    {
        string output = @"
                        SET NOCOUNT ON  
                        DECLARE @Result varchar(max)    

                        SET @Result = '{\""data\"":['   
                        SELECT @Result = @Result + '{\""''{#DBNAME}''\"":\""' + name + '\""},' FROM sys.databases

                        SET @Result = LEFT(@Result, LEN(@Result) - 1)
                        SET @Result = @Result + ']}'

                        SELECT @Result
                        ";

        return output;
    }

    private static string stpTZ_DiscoverJobName()
    {
        string output =
            @"
            SET NOCOUNT ON  
            DECLARE @Result varchar(max)    

            SET @Result = '{\""data\"":['   
            SELECT @Result = @Result + '{\""{#JOBNAME}\"":\""' + name + '\""},' FROM msdb.dbo.sysjobs WHERE enabled = 1 ORDER BY name

            SET @Result = LEFT(@Result, LEN(@Result) - 1)
            SET @Result = @Result + ']}'

            SELECT @Result  ";

        return output;
    }

 


}
