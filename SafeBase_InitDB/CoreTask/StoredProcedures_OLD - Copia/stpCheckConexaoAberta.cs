using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckConexaoAberta()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            TRUNCATE TABLE [dbo].[CheckConexaoAberta]
	            TRUNCATE TABLE [dbo].[CheckConexaoAberta_Email]

	            INSERT INTO [dbo].[CheckConexaoAberta] ([login_name], [session_count])
	            SELECT login_name, COUNT(login_name) AS [session_count] 
	            FROM sys.dm_exec_sessions 
	            WHERE session_id > 50
	            GROUP BY login_name
	            ORDER BY COUNT(login_name) DESC, login_name
	
	            IF (@@ROWCOUNT <> 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckConexaoAberta_Email] ([Nr_Ordem], [login_name], [session_count])
		            SELECT TOP 10 1, [login_name], [session_count]
		            FROM [dbo].[CheckConexaoAberta]
		            ORDER BY [session_count] DESC, [login_name]

		            INSERT INTO [dbo].[CheckConexaoAberta_Email] ([Nr_Ordem], [login_name], [session_count])
		            SELECT 2, 'TOTAL', SUM([session_count])
		            FROM [dbo].[CheckConexaoAberta]		
	            END
	            ELSE
	            BEGIN
		            INSERT INTO [dbo].[CheckConexaoAberta_Email] ([Nr_Ordem], [login_name], [session_count])
		            SELECT NULL, 'Sem conexões de usuários abertas', NULL
	            END
	            ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};