using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckAlteracaoJobs()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                DECLARE @hoje VARCHAR(8), @ontem VARCHAR(8)	
	            SELECT	@ontem  = CONVERT(VARCHAR(8),(DATEADD (DAY, -1, GETDATE())), 112),
			            @hoje = CONVERT(VARCHAR(8), GETDATE()+1, 112)

	            TRUNCATE TABLE [dbo].[CheckAlteracaoJobs]

	            INSERT INTO [dbo].[CheckAlteracaoJobs] ( [Nm_Job], [Fl_Habilitado], [Dt_Criacao], [Dt_Modificacao], [Nr_Versao] )
	            SELECT	[name] AS [Nm_Job], CONVERT(SMALLINT, [enabled]) AS [Fl_Habilitado], CONVERT(SMALLDATETIME, [date_created]) AS [Dt_Criacao], 
			            CONVERT(SMALLDATETIME, [date_modified]) AS [Dt_Modificacao], [version_number] AS [Nr_Versao]
	            FROM [msdb].[dbo].[sysjobs]  sj     
	            WHERE	( [date_created] >= @ontem AND [date_created] < @hoje) OR ([date_modified] >= @ontem AND [date_modified] < @hoje)	
	 
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckAlteracaoJobs] ( [Nm_Job], [Fl_Habilitado], [Dt_Criacao], [Dt_Modificacao], [Nr_Versao] )
		            SELECT 'Sem registro de JOB Alterado', NULL, NULL, NULL, NULL
	            END
	           ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};