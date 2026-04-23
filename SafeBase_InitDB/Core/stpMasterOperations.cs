using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.IO;
using InitDB.Client;
using System.Net;
using System.Text;

public partial class StoredProcedures
{  
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void Help()
    {
        SendMessage.PostBack(@"

        © Copyright Safebase - Version 2.0.8

        OBS: Em caso de dúvidas acesse o link abaixo para ler a documentação da ferramenta.
        http://www.datatips.info/initdb 
    

        Guia - Metodos de uso Functions e Storage Procedure:
        EXEC [dbo].[stpServerJob] 'GUIDE'; 


        Guia - Metodos de uso para Monitoramento Zabbix:
        EXEC [dbo].[stpToZabbix] 0,''; 


        Guia - Metodos de uso para Backup e Delete Backup:
        EXECUTE [dbo].[stpStartBackupDB]  'Help'    
        EXECUTE [dbo].[stpStartDeleteOldBackups] 'Help'; 


        Guia - Obtenha informações da Instância
        EXEC [dbo].[stpGetInfo] 'HELP';

        ");
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartBackupDB(string BackupType, int type = 1)
    {
        string OperationUID = Guid.NewGuid().ToString();
        string TypeXML_JSON = "";
        string JSON_Message = "";
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, false);

        Message = "Finished.";

        // get SQL Server Version
        int Version = 0;
        try
        {
            string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
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


        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

        if (BackupType == "Help")
        {
            SendMessage.PostBack(@"

            EXECUTE [dbo].[stpStartBackupDB]  'BackupFull' 
            EXECUTE [dbo].[stpStartBackupDB]  'BackupDifferential' 
            EXECUTE [dbo].[stpStartBackupDB]  'BackupLog'

            OBS: O BackupFull pode ser realizado seguindo os parametros:

                - EXECUTE [dbo].[stpStartBackupDB]  'BackupFull' ou EXECUTE [dbo].[stpStartBackupDB]  'BackupFull', 1 
                NO comando acima, primeiro realiza Backup Full de todas as bases e apos deletar arquivos antigos com base nas configurações da tabela [dbo].[ConfigDB]

                - EXECUTE [dbo].[stpStartBackupDB]  'BackupFull', 2
                NO comando acima, a cada backup de base realizado o mesmo deleta os arquivos mais antigos, ou seja, ele nao faz todos os backups e depois deleta, a cada backup
                o mesmo deleta a base mais antiga. Comando recomendado em ambientes com pouco espaço em disco de backups.

                - EXECUTE [dbo].[stpStartBackupDB]  'BackupFull', 3
                O comando acima deve ser utilizado em ultimo caso, onde o espaço em disco é critico, pois ele deleta o backup anterior para criar um novo, ou seja, 
                enquanto o backup estives em andamento voce nao tera o arquivo antigo.

                - EXECUTE [dbo].[stpStartBackupDB]  'BackupFull', 4; EXECUTE [dbo].[stpStartDeleteOldBackups]  'BackupFull' 
                Ao utilizar o comando acima é necessario ulizar um complemento para que seja realizado o expurgo de backups antigos, note que um segundo EXECUTE é utilizado apos o 
                ponto e virgula. Essa opção é valida para backups BackupFull, BackupDifferential e BackupLog
                

            Após o backup, seja ele full, diff ou de log os backups antigos serão deletados automaticamente desde que sejam utilizados as opções 1, 2 ou 3, com base nas configurações da tabela [dbo].[ConfigDB], 

            Duvidas chame o DBA :D ");
        }
        else
        {

            try
            {

                if (Version >= 30) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".ExcludeDB') AS [ExcludeDB],
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".WITH') AS [WITH],
                              SERVERPROPERTY('ProductVersion') as ProductVersion 
                          FROM [dbo].[ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1   
	                          ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')
	                            +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,
                              ParametersXML.value('(/Customer/" + BackupType + @"/ExcludeDB)[1]', 'varchar(max)') as ExcludeDB,                         
                              ParametersXML.value('(/Customer/" + BackupType + @"/WITH)[1]', 'varchar(max)') as [WITH],                                 
                              SERVERPROPERTY('ProductVersion') as ProductVersion                                                                   
                          FROM [dbo].[ConfigDB]
                          ";
                }

                DataTable ConfigDB = ExecuteSql.Reader(OperationUID, scriptLine);

                string BackupPath = ConfigDB.Rows[0][0].ToString();
                string[] ExcludeDB = ConfigDB.Rows[0][1].ToString().Split(';');
                string WITH = ConfigDB.Rows[0][2].ToString();
                string[] ProductVersion = ConfigDB.Rows[0][3].ToString().Split('.');

                if (Int32.Parse(ProductVersion[0]) >= 11)
                    scriptLine = @"
                            SELECT  RTRIM(name) as name, 
                                    state_desc, 
                                    recovery_model_desc, 
                                    is_in_standby, 
                                    isnull(db_name(source_database_id), '') as source_database, 
                                    SERVERPROPERTY('IsHadrEnabled') as IsHadrEnabled, 
                                    sys.fn_hadr_backup_is_preferred_replica(name) as prefered_replica 
                            FROM sys.databases WHERE Name <> 'tempdb' 
                              ";
                else
                    scriptLine = @"
                            SELECT RTRIM(name) as name, 
                                   state_desc, 
                                   recovery_model_desc, 
                                   is_in_standby, 
                                   isnull(db_name(source_database_id),'') as source_database 
                            FROM sys.databases WHERE Name <> 'tempdb' ";

                DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

                string DBIsHadrEnabled = "";
                foreach (DataRow row in Databases.Rows)
                {
                    try
                    {
                        DBIsHadrEnabled = row["IsHadrEnabled"].ToString();
                    }
                    catch
                    {

                    }
                }

                DataTable AlwaysOnDBDetails = null;
                if (DBIsHadrEnabled == "1")
                {
                    scriptLine = @"
                            SELECT AGC.name, 
                                RCS.replica_server_name, 
                                ARS.is_local, 
                                ARS.role_desc, 
                                ADC.database_name                               
                            FROM sys.availability_groups_cluster as AGC                                                                            
                            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
                            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
                            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
                            WHERE ARS.is_local = 1 
                              ";

                    AlwaysOnDBDetails = ExecuteSql.Reader(OperationUID, scriptLine);

                }


                foreach (DataRow row in Databases.Rows)
                {

                    WITH = ConfigDB.Rows[0][2].ToString();

                    string DBName = row["name"].ToString();
                    string DBStateDesc = row["state_desc"].ToString();
                    string DBRecoveryModel = row["recovery_model_desc"].ToString();
                    string DBis_in_standby = row["is_in_standby"].ToString();
                    string DBsource_database = row["source_database"].ToString();

                    //Is Prefered Replica?
                    string DBprefered_replica = "True";
                    try
                    {
                        //For SQL Server 2008, it will fail and set Yes
                        DBprefered_replica = row["prefered_replica"].ToString();
                    }
                    catch { }

                    string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string DBMessage = "";

                    //FOR ALWAYSON PRIMARY OR SECONDARY
                    string DBrole_desc = "PRIMARY";
                    if (DBIsHadrEnabled == "1")
                    {
                        foreach (DataRow r in AlwaysOnDBDetails.Rows)
                        {
                            if (r["database_name"].ToString() == DBName)
                                DBrole_desc = r["role_desc"].ToString();
                        }
                    }

                    if (DBStateDesc == "OFFLINE") DBMessage = "DB is Offline"; 
                    else if (DBStateDesc == "RESTORING") DBMessage = "DB is in Restoring";  
                    else if (Array.IndexOf(ExcludeDB, DBName) >= 0) DBMessage = "Not Allowed in Config";  
                    else if (DBis_in_standby == "1") DBMessage = "DB is in Warm StandBy";  
                    else if (DBsource_database != "") DBMessage = "Snapshot of " + DBsource_database; 
                    else if (DBprefered_replica.ToUpper() == "FALSE") DBMessage = "AlwaysOn not Prefered Replica"; 
                    else if (BackupType == "BackupDifferential" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY") DBMessage = "BackupDiff not supported on Secondary";
                    else if (BackupType == "BackupLog" && DBRecoveryModel == "SIMPLE") DBMessage = "Recovery Model is Simple";
                    else if (BackupType == "BackupLog" && DBName.ToUpper() == "MASTER") DBMessage = "Master dosen't take log";

                    else 
                    {

                        if (type == 1)
                        {
                            string FilePath = BackupPath + DBName;

                            if (Directory.Exists(FilePath) == false)
                                Directory.CreateDirectory(FilePath);

                            if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                            {
                                if (WITH == "")
                                    WITH = "WITH COPY_ONLY";
                                else
                                    WITH = WITH + ", COPY_ONLY";
                            }

                            string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                            switch (BackupType)
                            {
                                case "BackupFull":
                                    FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupFull';";
                                    break;

                                case "BackupDifferential":
                                    FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupDifferential';";
                                    break;

                                case "BackupLog":
                                    FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                    scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupLog';";
                                    break;
                            }

                            try
                            {
                                ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                                DBMessage = "Succeeded";
                            }
                            catch (Exception ex)
                            {
                                DBMessage = ReplaceChars(ex.Message.ToString());
                                Message = "Failed";
                                ToThrow = ex;
                            }

                        }

                        if (type == 2)
                        {
                            string FilePath = BackupPath + DBName;

                            if (Directory.Exists(FilePath) == false)
                                Directory.CreateDirectory(FilePath);

                            if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                            {
                                if (WITH == "")
                                    WITH = "WITH COPY_ONLY";
                                else
                                    WITH = WITH + ", COPY_ONLY";
                            }

                            string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                            switch (BackupType)
                            {
                                case "BackupFull":
                                    FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [SafeBase].[dbo].[stpStartDeleteBackupsCustom]  'BackupFull', '" + DBName + "'; ";
                                    break;

                                case "BackupDifferential":
                                    FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupDifferential';";
                                    break;

                                case "BackupLog":
                                    FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                    scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupLog';";
                                    break;
                            }

                            try
                            {
                                ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                                DBMessage = "Succeeded";
                            }
                            catch (Exception ex)
                            {
                                DBMessage = ReplaceChars(ex.Message.ToString());
                                Message = "Failed";
                                ToThrow = ex;
                            }

                        }

                        if (type == 3)
                        {
                            string FilePath = BackupPath + DBName;

                            if (Directory.Exists(FilePath) == false)
                                Directory.CreateDirectory(FilePath);

                            if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                            {
                                if (WITH == "")
                                    WITH = "WITH COPY_ONLY";
                                else
                                    WITH = WITH + ", COPY_ONLY";
                            }

                            string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                            switch (BackupType)
                            {
                                case "BackupFull":
                                    FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                    scriptLine = "EXECUTE [SafeBase].[dbo].[stpStartDeleteBackupsCustom]  'BackupFull', '" + DBName + "'; BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; ";
                                    break;

                                case "BackupDifferential":
                                    FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupDifferential';";
                                    break;

                                case "BackupLog":
                                    FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                    scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; EXECUTE [dbo].[stpStartDeleteOldBackups] @BackupType = 'BackupLog';";
                                    break;
                            }

                            try
                            {
                                ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                                DBMessage = "Succeeded";
                            }
                            catch (Exception ex)
                            {
                                DBMessage = ReplaceChars(ex.Message.ToString());
                                Message = "Failed";
                                ToThrow = ex;
                            }

                        }

                        if (type == 4)
                        {
                            string FilePath = BackupPath + DBName;

                            if (Directory.Exists(FilePath) == false)
                                Directory.CreateDirectory(FilePath);

                            if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                            {
                                if (WITH == "")
                                    WITH = "WITH COPY_ONLY";
                                else
                                    WITH = WITH + ", COPY_ONLY";
                            }

                            string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                            switch (BackupType)
                            {
                                case "BackupFull":
                                    FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + ";";
                                    break;

                                case "BackupDifferential":
                                    FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                    scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + ";";
                                    break;

                                case "BackupLog":
                                    FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                    scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + ";";
                                    break;
                            }

                            try
                            {
                                ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                                DBMessage = "Succeeded";
                            }
                            catch (Exception ex)
                            {
                                DBMessage = ReplaceChars(ex.Message.ToString());
                                Message = "Failed";
                                ToThrow = ex;
                            }

                        }

                    }

                    string EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    JSON_Message = @"{
                               ""Message"":{ 
                               ""@MessageStatus"": " + DBMessage + @",
                               ""@Database"": " + DBName + @",
                               ""@TypeXML_JSON"": " + TypeXML_JSON + @",
                               ""@DateTimeStarted"": " + StartTime + @",
                               ""@DateTimeFinished"": " + EndTime + @"}}
                               ";

                    XMLMessage = XMLMessage +
                                    "<Message Status='" + DBMessage + "' " +
                                    "Database='" + DBName + "' " +
                                    "TypeXML_JSON='" + TypeXML_JSON + "' " +
                                    "DateTimeStarted='" + StartTime + "' " +
                                    "DateTimeFinished='" + EndTime + "'" +
                                    " /> ";

                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, true);

                SendMessage.ThrowIfNeeded(ToThrow);
            }

        }
    }

    public static void stpStartBackupCustom(string BackupType, string db)
    {

        string OperationUID = Guid.NewGuid().ToString();
        string TypeXML_JSON = "";
        string JSON_Message = "";
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        //SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, false);

        Message = "Finished.";

        // get SQL Server Version
        int Version = 0;
        try
        {
            string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
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


        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

        if (BackupType == "Help")
        {
            SendMessage.PostBack(@"

            EXECUTE [dbo].[stpStartBackupCustom]  'BackupFull' , 'NOME DO BANCO'
            EXECUTE [dbo].[stpStartBackupCustom]  'BackupDifferential' , 'NOME DO BANCO'
            EXECUTE [dbo].[stpStartBackupCustom]  'BackupLog', 'NOME DO BANCO'

            EX:
            EXECUTE [dbo].[stpStartBackupCustom]  'BackupFull' , 'TesteBD'

            ");
        }
        else
        {

            try  // Operation = [BackupFull,BackupDifferential,BackupLog]
            {

                if (Version >= 30) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".ExcludeDB') AS [ExcludeDB],
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".WITH') AS [WITH],
                              SERVERPROPERTY('ProductVersion') as ProductVersion 
                          FROM [dbo].[ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1   
	                          ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')
	                            +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,
                              ParametersXML.value('(/Customer/" + BackupType + @"/ExcludeDB)[1]', 'varchar(max)') as ExcludeDB,                         
                              ParametersXML.value('(/Customer/" + BackupType + @"/WITH)[1]', 'varchar(max)') as [WITH],                                 
                              SERVERPROPERTY('ProductVersion') as ProductVersion                                                                   
                          FROM [dbo].[ConfigDB]
                          ";
                }

                DataTable ConfigDB = ExecuteSql.Reader(OperationUID, scriptLine);

                string BackupPath = ConfigDB.Rows[0][0].ToString();
                string[] ExcludeDB = ConfigDB.Rows[0][1].ToString().Split(';');
                string WITH = ConfigDB.Rows[0][2].ToString();
                string[] ProductVersion = ConfigDB.Rows[0][3].ToString().Split('.');

                if (Int32.Parse(ProductVersion[0]) >= 11)
                    scriptLine = @"
                            SELECT  RTRIM(name) as name, 
                                    state_desc, 
                                    recovery_model_desc, 
                                    is_in_standby, 
                                    isnull(db_name(source_database_id), '') as source_database, 
                                    SERVERPROPERTY('IsHadrEnabled') as IsHadrEnabled, 
                                    sys.fn_hadr_backup_is_preferred_replica(name) as prefered_replica 
                            FROM sys.databases WHERE Name = '"+db+"' ";
                else
                    scriptLine = @"
                            SELECT RTRIM(name) as name, 
                                   state_desc, 
                                   recovery_model_desc, 
                                   is_in_standby, 
                                   isnull(db_name(source_database_id),'') as source_database 
                            FROM sys.databases WHERE Name = '" + db + "' ";

                DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

                string DBIsHadrEnabled = "";
                foreach (DataRow row in Databases.Rows)
                {
                    try
                    {
                        DBIsHadrEnabled = row["IsHadrEnabled"].ToString();
                    }
                    catch
                    {

                    }
                }

                DataTable AlwaysOnDBDetails = null;
                if (DBIsHadrEnabled == "1")
                {
                    scriptLine = @"
                            SELECT AGC.name, 
                                RCS.replica_server_name, 
                                ARS.is_local, 
                                ARS.role_desc, 
                                ADC.database_name                               
                            FROM sys.availability_groups_cluster as AGC                                                                            
                            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
                            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
                            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
                            WHERE ARS.is_local = 1 
                              ";

                    AlwaysOnDBDetails = ExecuteSql.Reader(OperationUID, scriptLine);
                }


                foreach (DataRow row in Databases.Rows)
                {
                    WITH = ConfigDB.Rows[0][2].ToString();

                    string DBName = row["name"].ToString();
                    string DBStateDesc = row["state_desc"].ToString();
                    string DBRecoveryModel = row["recovery_model_desc"].ToString();
                    string DBis_in_standby = row["is_in_standby"].ToString();
                    string DBsource_database = row["source_database"].ToString();

                    //Is Prefered Replica?
                    string DBprefered_replica = "True";
                    try
                    {
                        //For SQL Server 2008, it will fail and set Yes
                        DBprefered_replica = row["prefered_replica"].ToString();
                    }
                    catch { }

                    string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string DBMessage = "";

                    //FOR ALWAYSON PRIMARY OR SECONDARY
                    string DBrole_desc = "PRIMARY";
                    if (DBIsHadrEnabled == "1")
                    {
                        foreach (DataRow r in AlwaysOnDBDetails.Rows)
                        {
                            if (r["database_name"].ToString() == DBName)
                                DBrole_desc = r["role_desc"].ToString();
                        }
                    }

                    if (DBStateDesc == "OFFLINE") DBMessage = "DB is Offline";  
                    else if (DBStateDesc == "RESTORING") DBMessage = "DB is in Restoring"; 
                    else if (Array.IndexOf(ExcludeDB, DBName) >= 0) DBMessage = "Not Allowed in Config";  
                    else if (DBis_in_standby == "1") DBMessage = "DB is in Warm StandBy"; 
                    else if (DBsource_database != "") DBMessage = "Snapshot of " + DBsource_database; 
                    else if (DBprefered_replica.ToUpper() == "FALSE") DBMessage = "AlwaysOn not Prefered Replica";  
                    else if (BackupType == "BackupDifferential" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY") DBMessage = "BackupDiff not supported on Secondary";
                    else if (BackupType == "BackupLog" && DBRecoveryModel == "SIMPLE") DBMessage = "Recovery Model is Simple";
                    else if (BackupType == "BackupLog" && DBName.ToUpper() == "MASTER") DBMessage = "Master dosen't take log";

                    else //Execute Backup
                    {
                        string FilePath = BackupPath + DBName;

                        if (Directory.Exists(FilePath) == false)
                            Directory.CreateDirectory(FilePath);

                        if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                        {
                            if (WITH == "")
                                WITH = "WITH COPY_ONLY";
                            else
                                WITH = WITH + ", COPY_ONLY";
                        }

                        string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                        switch (BackupType)
                        {
                            case "BackupFull":
                                FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;

                            case "BackupDifferential":
                                FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;

                            case "BackupLog":
                                FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;
                        }

                        try
                        {
                            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                            DBMessage = "Succeeded";
                        }
                        catch (Exception ex)
                        {
                            DBMessage = ReplaceChars(ex.Message.ToString());
                            Message = "Failed";
                            ToThrow = ex;
                        }
                    }

                    string EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    JSON_Message = @"{
                               ""Message"":{ 
                               ""@MessageStatus"": " + DBMessage + @",
                               ""@Database"": " + DBName + @",
                               ""@TypeXML_JSON"": " + TypeXML_JSON + @",
                               ""@DateTimeStarted"": " + StartTime + @",
                               ""@DateTimeFinished"": " + EndTime + @"}}
                               ";

                    XMLMessage = XMLMessage +
                                    "<Message Status='" + DBMessage + "' " +
                                    "Database='" + DBName + "' " +
                                    "TypeXML_JSON='" + TypeXML_JSON + "' " +
                                    "DateTimeStarted='" + StartTime + "' " +
                                    "DateTimeFinished='" + EndTime + "'" +
                                    " /> ";

                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                //SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, true);

                SendMessage.ThrowIfNeeded(ToThrow);
            }

        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartDefraging()
    {
        string OperationUID = Guid.NewGuid().ToString();
        string XMLMessage = "";
        Exception ToThrow = null;
        string scriptLine;
        try
        {
            string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            //get defrag command
            scriptLine =
            "   SELECT TOP 1                                                                                                                   " +
            "     'EXECUTE dbo.stpRunDefraging '                                                                                                 " +
            "   + '  @database				= ''' + ParametersXML.value('(/Customer/IndexDefrag/Database)[1]',              'varchar(max)') + ''''  " +
            "   + ', @debugMode			    = ''' + ParametersXML.value('(/Customer/IndexDefrag/DebugMode)[1]',             'varchar(max)') + ''''  " +
            "   + ', @defragDelay			= ''' + ParametersXML.value('(/Customer/IndexDefrag/DefragDelay)[1]',           'varchar(max)') + ''''  " +
            "   + ', @defragOrderColumn	    = ''' + ParametersXML.value('(/Customer/IndexDefrag/DefragOrderColumn)[1]',     'varchar(max)') + ''''  " +
            "   + ', @defragSortOrder		= ''' + ParametersXML.value('(/Customer/IndexDefrag/DefragSortOrder)[1]',       'varchar(max)') + ''''  " +
            "   + ', @excludeMaxPartition	= ''' + ParametersXML.value('(/Customer/IndexDefrag/ExcludeMaxPartition)[1]',   'varchar(max)') + ''''  " +
            "   + ', @executeSQL			= ''' + ParametersXML.value('(/Customer/IndexDefrag/ExecuteSQL)[1]',            'varchar(max)') + ''''  " +
            "   + ', @forceRescan			= ''' + ParametersXML.value('(/Customer/IndexDefrag/ForceRescan)[1]',           'varchar(max)') + ''''  " +
            "   + ', @maxDopRestriction	    = ''' + ParametersXML.value('(/Customer/IndexDefrag/MaxDopRestriction)[1]',     'varchar(max)') + ''''  " +
            "   + ', @maxPageCount			= ''' + ParametersXML.value('(/Customer/IndexDefrag/MaxPageCount)[1]',          'varchar(max)') + ''''  " +
            "   + ', @minFragmentation		= ''' + ParametersXML.value('(/Customer/IndexDefrag/MinFragmentation)[1]',      'varchar(max)') + ''''  " +
            "   + ', @minPageCount			= ''' + ParametersXML.value('(/Customer/IndexDefrag/MinPageCount)[1]',          'varchar(max)') + ''''  " +
            "   + ', @onlineRebuild		    = ''' + ParametersXML.value('(/Customer/IndexDefrag/OnlineRebuild)[1]',         'varchar(max)') + ''''  " +
            "   + ', @printCommands		    = ''' + ParametersXML.value('(/Customer/IndexDefrag/PrintCommands)[1]',         'varchar(max)') + ''''  " +
            "   + ', @printFragmentation	= ''' + ParametersXML.value('(/Customer/IndexDefrag/PrintFragmentation)[1]',    'varchar(max)') + ''''  " +
            "   + ', @rebuildThreshold		= ''' + ParametersXML.value('(/Customer/IndexDefrag/RebuildThreshold)[1]',      'varchar(max)') + ''''  " +
            "   + ', @scanMode				= ''' + ParametersXML.value('(/Customer/IndexDefrag/ScanMode)[1]',              'varchar(max)') + ''''  " +
            "   + ', @sortInTempDB			= ''' + ParametersXML.value('(/Customer/IndexDefrag/SortInTempDB)[1]',          'varchar(max)') + ''''  " +
            "   + ', @tableName			    = ''' + ParametersXML.value('(/Customer/IndexDefrag/TableName)[1]',             'varchar(max)') + ''''  " +
            "   + ', @timeLimit			    = ''' + ParametersXML.value('(/Customer/IndexDefrag/TimeLimit)[1]',             'varchar(max)') + ''''  " +
            "   FROM[dbo].[ConfigDB]                                                                                                             ";

            scriptLine = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);

            //Defrag process start here
            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, false);


            //get defrag result in XML format
            scriptLine =@"
                          SET DATEFORMAT YMD; 
                          SELECT ISNULL( (SELECT  indexDefrag_id, databaseID, databaseName, objectID,             
                                                  objectName, indexID, indexName, partitionNumber,                
                                                  fragmentation, page_count, dateTimeStart, dateTimeEnd,          
                                                  durationSeconds, sqlStatement, errorMessage,       [fillfactor] 
                                          FROM dbo.HistIndexDefragLog                                             
                                          WHERE dateTimeStart > '" + StartTime + @"' AND dateTimeEnd < GETDATE()   
                                          FOR XML RAW('defragged')                                                
                          				),' <defragged/> ')    ";


            XMLMessage = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);
        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);

            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            ToThrow = ex;
        }
        finally
        {
            //SendMessage.PostLogMSG("IndexDefrag", OperationUID, Message, XMLMessage, true);

            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartDeleteBackupsCustom(string BackupType, string db)
    {
        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

            string OperationUID = Guid.NewGuid().ToString();
            string TypeXML_JSON = "";
            string JSON_Message = "";
            string XMLMessage = "";
            Exception ToThrow = null;
            string Message = "Starting";

            string scriptLine;
            string[] files;

            SendMessage.PostLogMSG("DeleteOldBackup", OperationUID, Message, XMLMessage, false);

            Message = "Finished";

            try
            {
                string BackupPath = "";
                int DeleteOlderThan = 0;
                string ServerName = "";
                string ServiceName = "";
                string FileExtension;

                switch (BackupType)
                {
                    case "BackupLog":
                        FileExtension = ".TRN";
                        break;
                    case "BackupDifferential":
                        FileExtension = ".DIF";
                        break;
                    default:
                        FileExtension = ".BAK";
                        break;
                };

                int Version = 0;
                try
                {
                    string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
                    ProductVersion = ProductVersion.Substring(0, ProductVersion.IndexOf('.'));
                    Version = Convert.ToInt32(ProductVersion);
                }
                catch (Exception e)
                {
                    SendMessage.PostBack(e.ToString());
                }
                /**
                     * 9  - SQL Server 2005
                     * 10 - SQL Server 2008 R2s
                     * 11 - SQL Server 2012
                     * 12 - SQL Server 2014
                     * 13 - SQL Server 2016
                     * 14 - SQL Server 2017 
                     * 14 - SQL Server 2019 
                **/

                if (Version >= 20) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".DeleteOlderThan') AS DeleteOlderThan,
                              @@ServerName as ServerName,
                              @@SERVICENAME as ServiceName
                          FROM [ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1
                              ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)') +
                                +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END  + @@ServerName + '\\' +                                                                                                        +
                                +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,        +
                              ParametersXML.value('(/Customer/" + BackupType + @"/DeleteOlderThan)[1]', 'int') as DeleteOlderThan,  
                              + @@ServerName as ServerName,   
                              + @@SERVICENAME as ServiceName
                          FROM [dbo].[ConfigDB]  
                          ";
                }

                DataTable Data = ExecuteSql.Reader(OperationUID, scriptLine);

                BackupPath = Data.Rows[0][0].ToString();
                DeleteOlderThan = 10; //(int)Data.Rows[0][1];
                ServerName = Data.Rows[0][2].ToString();
                ServiceName = Data.Rows[0][3].ToString();
                TypeXML_JSON = TypeXML_JSON + "_Time_delete_" + DeleteOlderThan;

                //Get the list of all databases and start deleting them
                scriptLine = "SELECT name FROM sys.databases where name like '"+db+"' ORDER BY name";

                DataTable DBs = ExecuteSql.Reader(OperationUID, scriptLine);

                foreach (DataRow DB in DBs.Rows)
                {
                    string BackupPathDatabase = BackupPath + DB[0].ToString();

                    if (Directory.Exists(BackupPathDatabase) == false)
                        Directory.CreateDirectory(BackupPathDatabase);

                    files = Directory.GetFiles(BackupPathDatabase);

                    XMLMessage = XMLMessage + @"<Folder Path= '" + BackupPathDatabase + "' " + "TypeXML_JSON='" + TypeXML_JSON + "' > ";

                    foreach (string file in files)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);

                            if (fi.LastWriteTime < DateTime.Now.AddMinutes(-DeleteOlderThan) && fi.Extension == FileExtension)
                            {
                                SendMessage.PostBack("Deleting file: " + fi.ToString());
                                fi.Delete();
                                XMLMessage = XMLMessage + "<File Path= '" + fi.ToString() + "'></File>";
                            }
                        }
                        catch (Exception ex)
                        {
                            SendMessage.PostBack(ex.Message);
                            XMLMessage = XMLMessage + "<File Path= '" + file.ToString() + "'>";
                            XMLMessage = XMLMessage + "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                            XMLMessage = XMLMessage + "</File > ";
                        }
                    }

                    XMLMessage = XMLMessage + "</Folder>";
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                SendMessage.PostLogMSG("DeleteOldBackup", OperationUID, Message, XMLMessage, true);
                SendMessage.ThrowIfNeeded(ToThrow);
            }
        }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartDeleteOldBackups(string BackupType)
    {
        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

        if (BackupType == "Help")
        {
            SendMessage.PostBack(@"

            OBS: Os Comandos abaixo deletam os arquivos de backup antigos com base nas configurações da tabela [dbo].[ConfigDB]

            EXECUTE [dbo].[stpStartDeleteOldBackups]  'BackupFull' 
            EXECUTE [dbo].[stpStartDeleteOldBackups]  'BackupDifferential' 
            EXECUTE [dbo].[stpStartDeleteOldBackups]  'BackupLog'");
        }
        else
        {
            string OperationUID = Guid.NewGuid().ToString();
            string TypeXML_JSON = "";
            string JSON_Message = "";
            string XMLMessage = "";
            Exception ToThrow = null;
            string Message = "Starting";

            string scriptLine;
            string[] files;

            SendMessage.PostLogMSG("DeleteOldBackup", OperationUID, Message, XMLMessage, false);

            Message = "Finished";

            try
            {
                string BackupPath = "";
                int DeleteOlderThan = 0;
                string ServerName = "";
                string ServiceName = "";
                string FileExtension;

                switch (BackupType)
                {
                    case "BackupLog":
                        FileExtension = ".TRN";
                        break;
                    case "BackupDifferential":
                        FileExtension = ".DIF";
                        break;
                    default:
                        FileExtension = ".BAK";
                        break;
                };

                int Version = 0;
                try
                {
                    string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
                    ProductVersion = ProductVersion.Substring(0, ProductVersion.IndexOf('.'));
                    Version = Convert.ToInt32(ProductVersion);
                }
                catch (Exception e)
                {
                    SendMessage.PostBack(e.ToString());
                }
                /**
                     * 9  - SQL Server 2005
                     * 10 - SQL Server 2008 R2s
                     * 11 - SQL Server 2012
                     * 12 - SQL Server 2014
                     * 13 - SQL Server 2016
                     * 14 - SQL Server 2017 
                     * 14 - SQL Server 2019 
                **/

                if (Version >= 20) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".DeleteOlderThan') AS DeleteOlderThan,
                              @@ServerName as ServerName,
                              @@SERVICENAME as ServiceName
                          FROM [ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1
                              ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)') +
                                +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END  + @@ServerName + '\\' +                                                                                                        +
                                +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,        +
                              ParametersXML.value('(/Customer/" + BackupType + @"/DeleteOlderThan)[1]', 'int') as DeleteOlderThan,  
                              + @@ServerName as ServerName,   
                              + @@SERVICENAME as ServiceName
                          FROM [dbo].[ConfigDB]  
                          ";
                }

                DataTable Data = ExecuteSql.Reader(OperationUID, scriptLine);

                BackupPath = Data.Rows[0][0].ToString();
                DeleteOlderThan = (int)Data.Rows[0][1];
                ServerName = Data.Rows[0][2].ToString();
                ServiceName = Data.Rows[0][3].ToString();
                TypeXML_JSON = TypeXML_JSON + "_Time_delete_" + DeleteOlderThan;

                //Get the list of all databases and start deleting them
                scriptLine = "SELECT name FROM sys.databases ORDER BY name";

                DataTable DBs = ExecuteSql.Reader(OperationUID, scriptLine);

                foreach (DataRow DB in DBs.Rows)
                {
                    string BackupPathDatabase = BackupPath + DB[0].ToString();

                    if (Directory.Exists(BackupPathDatabase) == false)
                        Directory.CreateDirectory(BackupPathDatabase);

                    files = Directory.GetFiles(BackupPathDatabase);

                    XMLMessage = XMLMessage + @"<Folder Path= '" + BackupPathDatabase + "' " + "TypeXML_JSON='" + TypeXML_JSON + "' > ";

                    foreach (string file in files)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(file);

                            if (fi.LastWriteTime < DateTime.Now.AddMinutes(-DeleteOlderThan) && fi.Extension == FileExtension)
                            {
                                SendMessage.PostBack("Deleting file: " + fi.ToString());
                                fi.Delete();
                                XMLMessage = XMLMessage + "<File Path= '" + fi.ToString() + "'></File>";
                            }
                        }
                        catch (Exception ex)
                        {
                            SendMessage.PostBack(ex.Message);
                            XMLMessage = XMLMessage + "<File Path= '" + file.ToString() + "'>";
                            XMLMessage = XMLMessage + "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                            XMLMessage = XMLMessage + "</File > ";
                        }
                    }

                    XMLMessage = XMLMessage + "</Folder>";
                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                SendMessage.PostLogMSG("DeleteOldBackup", OperationUID, Message, XMLMessage, true);
                SendMessage.ThrowIfNeeded(ToThrow);
            }
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendNotification(int Id_AlertaParametro, string ProfileDBMail, string mailDestination, string BodyFormatMail, string Subject, string Importance, string HTML, string MntMsg, string CanalTelegram, string Ds_Menssageiro_02, string Teams)
    {
        string OperationUID = Guid.NewGuid().ToString();
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG("SendNotification", OperationUID, Message, XMLMessage, false);

        Message = "Finished";

        try
        {
            //Get the list of all databases
            scriptLine = @"  
             DECLARE @Id_AlertaParametro INT = " + Id_AlertaParametro + @"		    -- Numero do Id de Alerta ([dbo].[AlertaParametro])
	                ,@ProfileDBMail VARCHAR(50) = " + ProfileDBMail + @"		    -- Nome do profile de envio
	                ,@EmailDestination VARCHAR(1000) = " + mailDestination + @"	    -- Destinatarios de email 
	                ,@BodyFormatMail VARCHAR(20) = " + BodyFormatMail + @"		    -- Formato do email (html)
	                ,@Subject VARCHAR(600) = " + Subject + @"			            -- Assunto do email
	                ,@Importance AS VARCHAR(6) = " + Importance + @"		        -- Importancia do email (High)
	                ,@HTML VARCHAR(MAX)	= " + HTML + @"				                -- Corpo do email
	                ,@MntMsg VARCHAR(200) = " + MntMsg + @"				            -- Menssagem (Email, Telegram, Teamns)
	                ,@CanalTelegram VARCHAR(100) = " + CanalTelegram + @"		    -- Canal do Telegram
	                ,@Ds_Menssageiro_02 VARCHAR (30) = " + Ds_Menssageiro_02 + @"	-- Canal do Teamns
	                ,@Teams INT	= " + Teams + @"						            -- Grupo de envio do Teamns

            BEGIN
                /*******************************************************************************************************************************
                --	ALERTA - ENVIA O EMAIL E MENSSAGEIROS
                *******************************************************************************************************************************/
                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                    INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                    WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Email'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN

                    EXEC [msdb].[dbo].[sp_send_dbmail]
                            @profile_name = @ProfileDBMail,
                            @recipients = @EmailDestination,
                            @body_format = @BodyFormatMail,
                            @subject = @Subject,
                            @importance = @Importance,
                            @body = @HTML;

                END

	            -- Parametro Menssageiro
                SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Telegram'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN
                    -- Envio do Telegram    
                    EXEC dbo.StpSendMsgTelegram 
                            @Destino = @CanalTelegram,
                            @Msg = @MntMsg
                END

                IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                WHERE B.Ativo = 1
			                    AND B.Des LIKE '%Teams'
			                    AND [Id_AlertaParametro] = @Id_AlertaParametro
			                )
                BEGIN
                    -- MS TEAMS
                    SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                    EXEC [dbo].[stpSendMsgTeams]
	                        @msg = @MntMsg,
	                        @channel = @Ds_Menssageiro_02,
                            @ap = @Teams
                END
            END

            ";

            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);

            XMLMessage = XMLMessage + "<exec command='" + scriptLine + "' status='Done'/>";


        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);

            SendMessage.PostBack(XMLMessage);
            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            Message = "Failed";

            ToThrow = ex;
        }
        finally
        {
            SendMessage.PostBack("---> iniciou");
            SendMessage.PostLogMSG("SendNotification", OperationUID, Message, XMLMessage, true);
            SendMessage.PostBack("---> terminou");
            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartShrinkingLogFiles(int Shrinkfile)
    {
        string OperationUID = Guid.NewGuid().ToString();
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG("ShrinkingLogFiles", OperationUID, Message, XMLMessage, false);

        Message = "Finished";

        try
        {
            //Get the list of all databases
            scriptLine = @"
                            SELECT f.name AS filename, 
                                   d.name AS databasename
                            FROM msdb.sys.master_files f
                                 INNER JOIN master.sys.databases d ON d.database_id = f.database_id
                            WHERE f.type = 1
                                  /*log*/
                                  AND f.state = 0
                                  /*Online*/
                                  AND d.state = 0
                                  AND d.recovery_model_desc <> 'SIMPLE'
                                  AND d.[name] NOT IN (SELECT 
									                    ADC.database_name                               
								                    FROM sys.availability_groups_cluster as AGC                                                                            
								                    JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
								                    JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
								                    JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
								                    WHERE ARS.is_local = 1
								                    AND ARS.role_desc LIKE 'SECONDARY'); 
                          ";

            DataTable LogFiles = ExecuteSql.Reader(OperationUID, scriptLine);

            foreach (DataRow row in LogFiles.Rows)
            {
                scriptLine = "USE [" + row["databasename"].ToString() + "]; " +
                                "DBCC SHRINKFILE ([" + row["filename"].ToString() + "], " + Shrinkfile.ToString() + ");";

                try
                {
                    ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);

                    XMLMessage = XMLMessage + "<exec command='" + scriptLine + "' status='Done'/>";
                }
                catch (Exception ex)
                {
                    XMLMessage = XMLMessage + "<Exec command='" + scriptLine + "' status ='" + ex.Message.ToString().Replace("'", "") + "'/>";
                }
            }
        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);

            SendMessage.PostBack(XMLMessage);
            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            Message = "Failed";

            ToThrow = ex;
        }
        finally
        {
            SendMessage.PostBack("---> iniciou");
            SendMessage.PostLogMSG("ShrinkingLogFiles", OperationUID, Message, XMLMessage, true);
            SendMessage.PostBack("---> terminou");

            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartUpdateStats()
    {
        string OperationUID = Guid.NewGuid().ToString();
        int Version = 0;
        string TypeXML_JSON = "";
        string JSON_Message = "";
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG("UpdateStatistics", OperationUID, Message, XMLMessage, false);

        Message = "Finished";

        string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
        ProductVersion = ProductVersion.Substring(0, ProductVersion.IndexOf('.'));
        Version = Convert.ToInt32(ProductVersion);

        try
        {

            scriptLine = @"SELECT name FROM sys.databases WHERE state_desc not in ('OFFLINE','RESTORING') and is_in_standby = 0 and is_read_only = 0";
            DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

            if (Version >= 30) 
            {
                TypeXML_JSON = "Get_Json";
                scriptLine = @"
                              SELECT TOP 1 JSON_VALUE(ParametersJson, '$.UpdateStatistics.ExcludeDB') AS [ExcludeDB] FROM [dbo].[ConfigDB]
                              ";
            }
            else
            {
                TypeXML_JSON = "Get_XML";
                scriptLine = @"
                              SELECT TOP 1 ParametersXML.value('(/Customer/UpdateStatistics/ExcludeDB)[1]', 'varchar(max)') FROM [dbo].[ConfigDB]
                              ";
                
            }

            DataTable ExcludeDBs = ExecuteSql.Reader(OperationUID, scriptLine);

            string[] ExcludeDBsArray = { "" };
            foreach (DataRow e in ExcludeDBs.Rows)
            {
                ExcludeDBsArray = e[0].ToString().Split(';');
            }

            foreach (DataRow Database in Databases.Rows)
            {
                int notFound = 0;

                foreach (string DB in ExcludeDBsArray)
                {
                    if (Database[0].ToString() == DB)
                        notFound += 1;
                }

                if (notFound == 0)
                {
                    scriptLine = "USE [" + Database[0] + "];  EXECUTE sp_updatestats";

                    try 
                    {
                        ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);

                        XMLMessage = XMLMessage + "<Executed Database='" + Database[0] + "' command='" + scriptLine + "' status='Done'/>";
                    }
                    catch (Exception ex)
                    {
                        XMLMessage = XMLMessage + "<Executed Database='" + Database[0] + "' command='" + scriptLine + "' status='" + ex.Message.ToString().Replace("'", "") + "'/>";
                    }
                }
                else
                {
                    XMLMessage = XMLMessage + "<Executed Database='" + Database[0] + "' command='Not allowed in Config'/>";
                }
            }
        }
        catch (Exception ex)
        {
            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            Message = "Failed";
            ToThrow = ex;
        }
        finally
        {
            SendMessage.PostLogMSG("UpdateStatistics", OperationUID, Message, XMLMessage, true);
            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpStartCheckDB()
    {
        string OperationUID = Guid.NewGuid().ToString();
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG("CheckDB", OperationUID, Message, XMLMessage, false);

        Message = "Finished";

        string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");


        try
        {
            string SQLServerVersion = "11"; // >= MSSQL 2012

            scriptLine = "DELETE FROM dbo.HistoricoDBCC WHERE MessageText NOT Like '%CHECKDB%' AND convert(varchar, TimeStamp,101) = convert(varchar, GETDATE(), 101)";

            ExecuteSql.NonQuery(OperationUID, scriptLine, false, false, false);

            scriptLine = "SELECT CAST(SERVERPROPERTY('productversion') AS VARCHAR(100))";

            SQLServerVersion = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);

            string NocheckDataBase = ExecuteSql.ExecuteQuery("SELECT CASE WHEN IgnoraDatabase IS NULL THEN '''''' ELSE IgnoraDatabase END AS IgnoraDatabase FROM [dbo].[AlertaParametro] where Nm_Alerta = 'Check DB'");

            scriptLine = @"
                            SELECT name                                                                                                                                                 
                            FROM sys.databases db                                                                                                                                      
                            WHERE name NOT IN ("+ NocheckDataBase + @")     
                                AND db.state_desc = 'ONLINE'                                                                                                                           
                                AND source_database_id IS NULL --(Not Snapshots)                                                                                                       
                                AND is_read_only = 0  
                          ";

            DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

            foreach (DataRow row in Databases.Rows)
            {
                string DBName = row["name"].ToString();

                if (Int32.Parse(SQLServerVersion.Substring(0, SQLServerVersion.IndexOf('.'))) >= 11) // MSSQL 2012
                {
                    scriptLine = @" 
                                  INSERT INTO [dbo].[HistoricoDBCC](  [Error], [Level], [State], [MessageText], [RepairLevel], [Status], [DbId], [DbFragId], [ObjectId], [IndexId], [PartitionId], [AllocUnitId], [RidDbld], [RidPruId], [File], [Page], [Slot], [RefDBId], [RefPruId], [RefFile], [RefPage], [RefSlot], [Allocation])
                                  EXEC('DBCC CHECKDB([" + DBName + @"]) WITH TABLERESULTS'); 
                                  ";
                }
                else // MSSQL 2008 R2 or earlier
                {
                    scriptLine = @" 
                                  INSERT INTO [dbo].[HistoricoDBCC] (  [Error], [Level], [State], [MessageText], [RepairLevel], [Status], [DbId], [Id], [IndId], [PartitionId], [AllocUnitId], [File], [Page], [Slot], [RefFile], [RefPage], [RefSlot], [Allocation]) 
                                  EXEC('DBCC CHECKDB([" + DBName + @"]) WITH TABLERESULTS'); 
                                  ";
                }

                ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
            }


        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);

            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            Message = "Failed";

            ToThrow = ex;
        }
        finally
        {
            //Finished log
            scriptLine = @"   
                          SELECT  Error, Level, State, MessageText, CONVERT(varchar, TimeStamp, 113) as [TimeStamp] 
                          FROM dbo.HistoricoDBCC                                                                                      
                          WHERE MessageText Like '%CHECKDB%' AND TimeStamp >= '" + StartTime + @"'                                    
                          FOR XML RAW('CheckedLine')   
                         ";

            string Result = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);

            XMLMessage = XMLMessage + Result.Replace("'", "");

            SendMessage.PostLogMSG("CheckDB", OperationUID, Message, XMLMessage, true);

            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendMsgCurl(SqlChars d, SqlChars url)
    {
        var client = new WebClient();
        if (d.IsNull)
            throw new ArgumentException("Você deve especificar os dados que serão enviados para o endpoint", "@d");
        var response =
                client.UploadString(
                    Uri.EscapeUriString(url.ToSqlString().Value),
                    d.ToSqlString().Value
                    );
        SqlContext.Pipe.Send("Realizado. " + response);
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendMsgTeams(string msg, string channel, int ap)
    {

        // Core.ExecutestpSendMsgTeams(msg, who);
        try
        {
            string who = ExecuteSql.ExecuteQuery(@"SELECT A.Token FROM [dbo].[AlertaMsgToken] A 
                                                                  INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_02 where b.Ds_Menssageiro_02 = " + channel + @" AND B.Id_AlertaParametro = " + ap + @" AND B.Ativo = 1");
            string url = who;

            string address = "" + url + "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.ContentType = "application/json; charset=utf-8";
            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = "{\"text\": \"" + msg + "\"}";

                streamWriter.Write(json);
                streamWriter.Flush();
            }

            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);
        }
        finally
        {
        }

    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpSendMsgTelegram(SqlString Destino, SqlString Msg)
    {
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        string scriptLine;
        scriptLine = ExecuteSql.ExecuteQuery("SELECT Token FROM [dbo].[AlertaMsgToken] (NOLOCK) WHERE Nome = 'Telegram'");

        string token = scriptLine; // "187235235:AAEVrK7cZsiAnfVt-KyH6VAg5aboObBY3hI";

        try
        {

            var mensagem = Msg.Value;
            var canais = Destino.Value.Split(';');

            foreach (var canal in canais)
            {

                var dsScript = $"chat_id={canal.Trim()}&text={mensagem}&parse_mode=Markdown";

                var url = $"https://api.telegram.org/bot{token}/sendMessage";

                var request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.UserAgent = "curl/7.45.0";
                request.ContentType = "application/x-www-form-urlencoded";

                var buffer = Encoding.GetEncoding("UTF-8").GetBytes(dsScript);
                using (var reqstr = request.GetRequestStream())
                {

                    reqstr.Write(buffer, 0, buffer.Length);
                    using (var response = request.GetResponse())
                    {

                        using (var dataStream = response.GetResponseStream())
                        {

                            if (dataStream == null) return;

                            using (var reader = new StreamReader(dataStream))
                            {
                                var responseFromServer = reader.ReadToEnd();
                                object e = null;
                                //SendMessage.PostBack(e.ToString());
                                Core.Mensagem(responseFromServer);
                            }
                        }

                    }

                }

            }

        }
        catch (Exception e)
        {
            SendMessage.PostBack(e.Message);
        }

    }
    
    private static string ReplaceChars(string Message)
    {
        Message = Message.Replace("'", "");
        return Message;
    }

    public static void stpStartBackupCustomOLD(string BackupType)
    {

        string OperationUID = Guid.NewGuid().ToString();
        string TypeXML_JSON = "";
        string JSON_Message = "";
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, false);

        Message = "Finished.";

        // get SQL Server Version
        int Version = 0;
        try
        {
            string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
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


        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

        if (BackupType == "Help")
        {
            SendMessage.PostBack(@"

            EXECUTE [dbo].[stpStartBackup]  'BackupFull' 
            EXECUTE [dbo].[stpStartBackup]  'BackupDifferential' 
            EXECUTE [dbo].[stpStartBackup]  'BackupLog'");
        }
        else
        {

            try  // Operation = [BackupFull,BackupDifferential,BackupLog]
            {

                if (Version >= 30) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".ExcludeDB') AS [ExcludeDB],
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".WITH') AS [WITH],
                              SERVERPROPERTY('ProductVersion') as ProductVersion 
                          FROM [dbo].[ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1   
	                          ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')
	                            +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,
                              ParametersXML.value('(/Customer/" + BackupType + @"/ExcludeDB)[1]', 'varchar(max)') as ExcludeDB,                         
                              ParametersXML.value('(/Customer/" + BackupType + @"/WITH)[1]', 'varchar(max)') as [WITH],                                 
                              SERVERPROPERTY('ProductVersion') as ProductVersion                                                                   
                          FROM [dbo].[ConfigDB]
                          ";
                }

                DataTable ConfigDB = ExecuteSql.Reader(OperationUID, scriptLine);

                string BackupPath = ConfigDB.Rows[0][0].ToString();
                string[] ExcludeDB = ConfigDB.Rows[0][1].ToString().Split(';');
                string WITH = ConfigDB.Rows[0][2].ToString();
                string[] ProductVersion = ConfigDB.Rows[0][3].ToString().Split('.');

                if (Int32.Parse(ProductVersion[0]) >= 11)
                    scriptLine = @"
                            SELECT  RTRIM(name) as name, 
                                    state_desc, 
                                    recovery_model_desc, 
                                    is_in_standby, 
                                    isnull(db_name(source_database_id), '') as source_database, 
                                    SERVERPROPERTY('IsHadrEnabled') as IsHadrEnabled, 
                                    sys.fn_hadr_backup_is_preferred_replica(name) as prefered_replica 
                            FROM sys.databases WHERE Name <> 'tempdb' 
                              ";
                else
                    scriptLine = @"
                            SELECT RTRIM(name) as name, 
                                   state_desc, 
                                   recovery_model_desc, 
                                   is_in_standby, 
                                   isnull(db_name(source_database_id),'') as source_database 
                            FROM sys.databases WHERE Name <> 'tempdb' ";

                DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

                string DBIsHadrEnabled = "";
                foreach (DataRow row in Databases.Rows)
                {
                    try
                    {
                        DBIsHadrEnabled = row["IsHadrEnabled"].ToString();
                    }
                    catch
                    {

                    }
                }

                DataTable AlwaysOnDBDetails = null;
                if (DBIsHadrEnabled == "1")
                {
                    scriptLine = @"
                            SELECT AGC.name, 
                                RCS.replica_server_name, 
                                ARS.is_local, 
                                ARS.role_desc, 
                                ADC.database_name                               
                            FROM sys.availability_groups_cluster as AGC                                                                            
                            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
                            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
                            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
                            WHERE ARS.is_local = 1 
                              ";

                    AlwaysOnDBDetails = ExecuteSql.Reader(OperationUID, scriptLine);
                }


                foreach (DataRow row in Databases.Rows)
                {
                    WITH = ConfigDB.Rows[0][2].ToString();

                    string DBName = row["name"].ToString();
                    string DBStateDesc = row["state_desc"].ToString();
                    string DBRecoveryModel = row["recovery_model_desc"].ToString();
                    string DBis_in_standby = row["is_in_standby"].ToString();
                    string DBsource_database = row["source_database"].ToString();

                    //Is Prefered Replica?
                    string DBprefered_replica = "True";
                    try
                    {
                        //For SQL Server 2008, it will fail and set Yes
                        DBprefered_replica = row["prefered_replica"].ToString();
                    }
                    catch { }

                    string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string DBMessage = "";

                    //FOR ALWAYSON PRIMARY OR SECONDARY
                    string DBrole_desc = "PRIMARY";
                    if (DBIsHadrEnabled == "1")
                    {
                        foreach (DataRow r in AlwaysOnDBDetails.Rows)
                        {
                            if (r["database_name"].ToString() == DBName)
                                DBrole_desc = r["role_desc"].ToString();
                        }
                    }

                    if (DBStateDesc == "OFFLINE") DBMessage = "DB is Offline";   //Offline
                    else if (DBStateDesc == "RESTORING") DBMessage = "DB is in Restoring";  //RESTORE WITH NORECOVERY
                    else if (Array.IndexOf(ExcludeDB, DBName) >= 0) DBMessage = "Not Allowed in Config";  //Not Allowed
                    else if (DBis_in_standby == "1") DBMessage = "DB is in Warm StandBy";  //RESTORE WITH STANDBY
                    else if (DBsource_database != "") DBMessage = "Snapshot of " + DBsource_database;  //SNAPSHOT OF DATABASE
                    else if (DBprefered_replica.ToUpper() == "FALSE") DBMessage = "AlwaysOn not Prefered Replica";  //AlwaysOn Preference Replica Backup
                    else if (BackupType == "BackupDifferential" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY") DBMessage = "BackupDiff not supported on Secondary";
                    else if (BackupType == "BackupLog" && DBRecoveryModel == "SIMPLE") DBMessage = "Recovery Model is Simple";
                    else if (BackupType == "BackupLog" && DBName.ToUpper() == "MASTER") DBMessage = "Master dosen't take log";

                    else //Execute Backup
                    {
                        string FilePath = BackupPath + DBName;

                        if (Directory.Exists(FilePath) == false)
                            Directory.CreateDirectory(FilePath);

                        if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                        {
                            if (WITH == "")
                                WITH = "WITH COPY_ONLY";
                            else
                                WITH = WITH + ", COPY_ONLY";
                        }

                        string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                        switch (BackupType)
                        {
                            case "BackupFull":
                                FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                scriptLine = "EXECUTE [SafeBase].[dbo].[stpStartDeleteBackupsCustom]  'BackupFull', '" + DBName + "'; BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH + "; ";
                                break;

                            case "BackupDifferential":
                                FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;

                            case "BackupLog":
                                FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;
                        }

                        try
                        {
                            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                            DBMessage = "Succeeded";
                        }
                        catch (Exception ex)
                        {
                            DBMessage = ReplaceChars(ex.Message.ToString());
                            Message = "Failed";
                            ToThrow = ex;
                        }
                    }

                    string EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    JSON_Message = @"{
                               ""Message"":{ 
                               ""@MessageStatus"": " + DBMessage + @",
                               ""@Database"": " + DBName + @",
                               ""@TypeXML_JSON"": " + TypeXML_JSON + @",
                               ""@DateTimeStarted"": " + StartTime + @",
                               ""@DateTimeFinished"": " + EndTime + @"}}
                               ";

                    XMLMessage = XMLMessage +
                                    "<Message Status='" + DBMessage + "' " +
                                    "Database='" + DBName + "' " +
                                    "TypeXML_JSON='" + TypeXML_JSON + "' " +
                                    "DateTimeStarted='" + StartTime + "' " +
                                    "DateTimeFinished='" + EndTime + "'" +
                                    " /> ";

                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, true);

                SendMessage.ThrowIfNeeded(ToThrow);
            }

        }
    }

    public static void stpQueueInfoSendMail()
    {
        string OperationUID = Guid.NewGuid().ToString();
        string XMLMessage = "";
        Exception ToThrow = null;
        string scriptLine;
        try
        {
            string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            //get command
            scriptLine = @"

                        ";

            scriptLine = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);

            // process start here
            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, false);


            //get result in XML format
            scriptLine = @"
                          SET DATEFORMAT YMD; 
                          SELECT ISNULL( (SELECT  indexDefrag_id, databaseID, databaseName, objectID,             
                                                  objectName, indexID, indexName, partitionNumber,                
                                                  fragmentation, page_count, dateTimeStart, dateTimeEnd,          
                                                  durationSeconds, sqlStatement, errorMessage,       [fillfactor] 
                                          FROM dbo.HistIndexDefragLog                                             
                                          WHERE dateTimeStart > '" + StartTime + @"' AND dateTimeEnd < GETDATE()   
                                          FOR XML RAW('defragged')                                                
                          				),' <defragged/> ')    ";


            XMLMessage = ExecuteSql.Reader_FirstRowColumnOnly(OperationUID, scriptLine);
        }
        catch (Exception ex)
        {
            SendMessage.PostBack(ex.Message);

            XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
            ToThrow = ex;
        }
        finally
        {
            //SendMessage.PostLog("IndexDefrag", OperationUID, Message, XMLMessage, true);

            SendMessage.ThrowIfNeeded(ToThrow);
        }
    }

    public static void stpStartBackup(string BackupType)
    {

        string OperationUID = Guid.NewGuid().ToString();
        string TypeXML_JSON = "";
        string JSON_Message = "";
        string XMLMessage = "";
        string Message = "Starting";
        string scriptLine;
        Exception ToThrow = null;

        SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, false);

        Message = "Finished.";

        // get SQL Server Version
        int Version = 0;
        try
        {
            string ProductVersion = ExecuteSql.ExecuteQueryReadFast("", "SELECT SERVERPROPERTY('ProductVersion')");
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


        /*
          BackupFull, 
          BackupDifferential, 
          BackupLog
        */

        if (BackupType == "Help")
        {
            SendMessage.PostBack(@"

            EXECUTE [dbo].[stpStartBackup]  'BackupFull' 
            EXECUTE [dbo].[stpStartBackup]  'BackupDifferential' 
            EXECUTE [dbo].[stpStartBackup]  'BackupLog'");
        }
        else
        {

            try  // Operation = [BackupFull,BackupDifferential,BackupLog]
            {

                if (Version >= 13) // (Version >= 13)
                {
                    TypeXML_JSON = "Get_Json";
                    scriptLine = @"
                          SELECT top 1
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath') 
	                            +CASE WHEN RIGHT((JSON_VALUE(ParametersJson, '$." + BackupType + @".BackupPath')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END 
                              AS BackupPath,
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".ExcludeDB') AS [ExcludeDB],
                              JSON_VALUE(ParametersJson, '$." + BackupType + @".WITH') AS [WITH],
                              SERVERPROPERTY('ProductVersion') as ProductVersion 
                          FROM [dbo].[ConfigDB]
                          ";
                }
                else
                {
                    TypeXML_JSON = "Get_XML";
                    scriptLine = @"
                          SELECT TOP 1   
	                          ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')
	                            +CASE WHEN RIGHT((ParametersXML.value('(/Customer/" + BackupType + @"/BackupPath)[1]', 'varchar(max)')), 1) = '\\' THEN '' ELSE '\\' END	+ @@ServerName + '\\' + 
	                            +CASE WHEN RIGHT(@@ServerName, LEN(@@ServiceName)) = @@ServiceName THEN '' ELSE @@ServiceName + '\\' END AS BackupPath,
                              ParametersXML.value('(/Customer/" + BackupType + @"/ExcludeDB)[1]', 'varchar(max)') as ExcludeDB,                         
                              ParametersXML.value('(/Customer/" + BackupType + @"/WITH)[1]', 'varchar(max)') as [WITH],                                 
                              SERVERPROPERTY('ProductVersion') as ProductVersion                                                                   
                          FROM [dbo].[ConfigDB]
                          ";
                }

                DataTable ConfigDB = ExecuteSql.Reader(OperationUID, scriptLine);

                string BackupPath = ConfigDB.Rows[0][0].ToString();
                string[] ExcludeDB = ConfigDB.Rows[0][1].ToString().Split(';');
                string WITH = ConfigDB.Rows[0][2].ToString();
                string[] ProductVersion = ConfigDB.Rows[0][3].ToString().Split('.');

                if (Int32.Parse(ProductVersion[0]) >= 11)
                    scriptLine = @"
                            SELECT  RTRIM(name) as name, 
                                    state_desc, 
                                    recovery_model_desc, 
                                    is_in_standby, 
                                    isnull(db_name(source_database_id), '') as source_database, 
                                    SERVERPROPERTY('IsHadrEnabled') as IsHadrEnabled, 
                                    sys.fn_hadr_backup_is_preferred_replica(name) as prefered_replica 
                            FROM sys.databases WHERE Name <> 'tempdb' 
                              ";
                else
                    scriptLine = @"
                            SELECT RTRIM(name) as name, 
                                   state_desc, 
                                   recovery_model_desc, 
                                   is_in_standby, 
                                   isnull(db_name(source_database_id),'') as source_database 
                            FROM sys.databases WHERE Name <> 'tempdb' ";

                DataTable Databases = ExecuteSql.Reader(OperationUID, scriptLine);

                string DBIsHadrEnabled = "";
                foreach (DataRow row in Databases.Rows)
                {
                    try
                    {
                        DBIsHadrEnabled = row["IsHadrEnabled"].ToString();
                    }
                    catch
                    {

                    }
                }

                DataTable AlwaysOnDBDetails = null;
                if (DBIsHadrEnabled == "1")
                {
                    scriptLine = @"
                            SELECT AGC.name, 
                                RCS.replica_server_name, 
                                ARS.is_local, 
                                ARS.role_desc, 
                                ADC.database_name                               
                            FROM sys.availability_groups_cluster as AGC                                                                            
                            JOIN sys.dm_hadr_availability_replica_cluster_states as RCS ON AGC.group_id = RCS.group_id                             
                            JOIN sys.dm_hadr_availability_replica_states as ARS ON RCS.replica_id = ARS.replica_id and RCS.group_id = ARS.group_id 
                            JOIN sys.availability_databases_cluster as ADC ON AGC.group_id = ADC.group_id                                          
                            WHERE ARS.is_local = 1 
                              ";

                    AlwaysOnDBDetails = ExecuteSql.Reader(OperationUID, scriptLine);
                }


                foreach (DataRow row in Databases.Rows)
                {
                    WITH = ConfigDB.Rows[0][2].ToString();

                    string DBName = row["name"].ToString();
                    string DBStateDesc = row["state_desc"].ToString();
                    string DBRecoveryModel = row["recovery_model_desc"].ToString();
                    string DBis_in_standby = row["is_in_standby"].ToString();
                    string DBsource_database = row["source_database"].ToString();

                    //Is Prefered Replica?
                    string DBprefered_replica = "True";
                    try
                    {
                        //For SQL Server 2008, it will fail and set Yes
                        DBprefered_replica = row["prefered_replica"].ToString();
                    }
                    catch { }

                    string StartTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string DBMessage = "";

                    //FOR ALWAYSON PRIMARY OR SECONDARY
                    string DBrole_desc = "PRIMARY";
                    if (DBIsHadrEnabled == "1")
                    {
                        foreach (DataRow r in AlwaysOnDBDetails.Rows)
                        {
                            if (r["database_name"].ToString() == DBName)
                                DBrole_desc = r["role_desc"].ToString();
                        }
                    }

                    if (DBStateDesc == "OFFLINE") DBMessage = "DB is Offline";   //Offline
                    else if (DBStateDesc == "RESTORING") DBMessage = "DB is in Restoring";  //RESTORE WITH NORECOVERY
                    else if (Array.IndexOf(ExcludeDB, DBName) >= 0) DBMessage = "Not Allowed in Config";  //Not Allowed
                    else if (DBis_in_standby == "1") DBMessage = "DB is in Warm StandBy";  //RESTORE WITH STANDBY
                    else if (DBsource_database != "") DBMessage = "Snapshot of " + DBsource_database;  //SNAPSHOT OF DATABASE
                    else if (DBprefered_replica.ToUpper() == "FALSE") DBMessage = "AlwaysOn not Prefered Replica";  //AlwaysOn Preference Replica Backup
                    else if (BackupType == "BackupDifferential" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY") DBMessage = "BackupDiff not supported on Secondary";
                    else if (BackupType == "BackupLog" && DBRecoveryModel == "SIMPLE") DBMessage = "Recovery Model is Simple";
                    else if (BackupType == "BackupLog" && DBName.ToUpper() == "MASTER") DBMessage = "Master dosen't take log";

                    else //Execute Backup
                    {
                        string FilePath = BackupPath + DBName;

                        if (Directory.Exists(FilePath) == false)
                            Directory.CreateDirectory(FilePath);

                        if (BackupType == "BackupFull" && DBIsHadrEnabled == "1" && DBrole_desc == "SECONDARY")
                        {
                            if (WITH == "")
                                WITH = "WITH COPY_ONLY";
                            else
                                WITH = WITH + ", COPY_ONLY";
                        }

                        string Time = StartTime.Replace("/", ".").Replace(":", ".").Replace(" ", "_");
                        switch (BackupType)
                        {
                            case "BackupFull":
                                FilePath = FilePath + "\\" + DBName + "_FULL_" + Time + ".BAK";
                                scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;

                            case "BackupDifferential":
                                FilePath = FilePath + "\\" + DBName + "_DIFF_" + Time + ".DIF";
                                scriptLine = "BACKUP DATABASE [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;

                            case "BackupLog":
                                FilePath = FilePath + "\\" + DBName + "_LOG_" + Time.Substring(0, 10) + ".TRN";
                                scriptLine = "BACKUP LOG [" + DBName + "] TO DISK='" + FilePath + "' " + WITH;
                                break;
                        }

                        try
                        {
                            ExecuteSql.NonQuery(OperationUID, scriptLine, true, false, true);
                            DBMessage = "Succeeded";
                        }
                        catch (Exception ex)
                        {
                            DBMessage = ReplaceChars(ex.Message.ToString());
                            Message = "Failed";
                            ToThrow = ex;
                        }
                    }

                    string EndTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                    JSON_Message = @"{
                               ""Message"":{ 
                               ""@MessageStatus"": " + DBMessage + @",
                               ""@Database"": " + DBName + @",
                               ""@TypeXML_JSON"": " + TypeXML_JSON + @",
                               ""@DateTimeStarted"": " + StartTime + @",
                               ""@DateTimeFinished"": " + EndTime + @"}}
                               ";

                    XMLMessage = XMLMessage +
                                    "<Message Status='" + DBMessage + "' " +
                                    "Database='" + DBName + "' " +
                                    "TypeXML_JSON='" + TypeXML_JSON + "' " +
                                    "DateTimeStarted='" + StartTime + "' " +
                                    "DateTimeFinished='" + EndTime + "'" +
                                    " /> ";

                }
            }
            catch (Exception ex)
            {
                SendMessage.PostBack(ex.Message);

                XMLMessage = "<ErrorMessage> " + ex.Message.ToString() + "</ErrorMessage>";
                Message = "Failed";
                ToThrow = ex;
            }
            finally
            {
                SendMessage.PostLogMSG(BackupType, OperationUID, Message, XMLMessage, true);

                SendMessage.ThrowIfNeeded(ToThrow);
            }

        }
    }

}

