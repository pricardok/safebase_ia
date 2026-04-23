using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckJobsFailed()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            IF (OBJECT_ID('tempdb..#Result_History_Jobs') IS NOT NULL)
		            DROP TABLE #Result_History_Jobs

	            CREATE TABLE #Result_History_Jobs (
		            [Cod] INT IDENTITY(1,1),
		            [Instance_Id] INT,
		            [Job_Id] VARCHAR(255),
		            [Job_Name] VARCHAR(255),
		            [Step_Id] INT,
		            [Step_Name] VARCHAR(255),
		            [SQl_Message_Id] INT,
		            [Sql_Severity] INT,
		            [SQl_Message] VARCHAR(4490),
		            [Run_Status] INT,
		            [Run_Date] VARCHAR(20),
		            [Run_Time] VARCHAR(20),
		            [Run_Duration] INT,
		            [Operator_Emailed] VARCHAR(100),
		            [Operator_NetSent] VARCHAR(100),
		            [Operator_Paged] VARCHAR(100),
		            [Retries_Attempted] INT,
		            [Nm_Server] VARCHAR(100)  
	            )

	            DECLARE @hoje VARCHAR(8), @ontem VARCHAR(8)	
	            SELECT	@ontem = CONVERT(VARCHAR(8),(DATEADD (DAY, -1, GETDATE())), 112), 
			            @hoje = CONVERT(VARCHAR(8), GETDATE() + 1, 112)

	            INSERT INTO #Result_History_Jobs
	            EXEC [msdb].[dbo].[sp_help_jobhistory] @mode = 'FULL', @start_run_date = @ontem

	            TRUNCATE TABLE [dbo].[CheckJobsFailed]
	
	            INSERT INTO [dbo].[CheckJobsFailed] ( [Server], [Job_Name], [Status], [Dt_Execucao], [Run_Duration], [SQL_Message] )
	            SELECT	Nm_Server AS [Server], [Job_Name], 
			            CASE	WHEN [Run_Status] = 0 THEN 'Failed'
					            WHEN [Run_Status] = 1 THEN 'Succeeded'
					            WHEN [Run_Status] = 2 THEN 'Retry (step only)'
					            WHEN [Run_Status] = 3 THEN 'Cancelled'
					            WHEN [Run_Status] = 4 THEN 'In-progress message'
					            WHEN [Run_Status] = 5 THEN 'Unknown' 
			            END [Status],
			            CAST(	[Run_Date] + ' ' +
					            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-3), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-1), 2), 2) AS VARCHAR) AS [Dt_Execucao],
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR),(LEN([Run_Duration])-5),2), 2) + ':' +
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR),(LEN([Run_Duration])-3),2), 2) + ':' +
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR),(LEN([Run_Duration])-1),2), 2) AS [Run_Duration],
			            CAST([SQl_Message] AS VARCHAR(3990)) AS [SQl_Message]
	            FROM #Result_History_Jobs 
	            WHERE 
		              CAST([Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
			              RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-3), 2), 2) + ':' +
			              RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-1), 2), 2) AS DATETIME) >= @ontem + ' 08:00' 
		              AND  /*dia anterior no horário*/
			            CAST([Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
			              RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-3), 2), 2) + ':' +
			              RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-1), 2), 2) AS DATETIME) < @hoje
		              AND [Step_Id] = 0
		              AND [Run_Status] <> 1
	 
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckJobsFailed] ( [Server], [Job_Name], [Status], [Dt_Execucao], [Run_Duration], [SQL_Message] )
		            SELECT NULL, 'Sem registro de Falha de JOB', NULL, NULL, NULL, NULL		
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};