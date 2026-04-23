using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCargaAlteracaoDB()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
              SET NOCOUNT ON

              --TRUNCATE TABLE [dbo].[ServerAudi]
              INSERT INTO [dbo].[ServerAudi] ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
                 SELECT CAST(event_time AS date) AS event_time,server_instance_name,database_name,action_id,server_principal_name,statement--,* 
                 FROM Sys.fn_get_audit_file('C:\data\Logs\*.sqlaudit',default,default)
                 WHERE [statement] like '%MODIFY NAME%' OR [statement] like '%DROP%DATABASE%' OR [statement] like '%CREATE%' AND CAST(event_time AS date) = CONVERT(VARCHAR(10),GETDATE(),112)

              IF (@@ROWCOUNT = 0)
              BEGIN
                INSERT INTO [dbo].[ServerAudi] ([DataEvento] ,[serverInstanceName],[DatabaseName],[ActionId],[Session],[statement])
                SELECT GETDATE(),'Sem registro de Alteraçoes nas bases', NULL, NULL, NULL, NULL
              END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};