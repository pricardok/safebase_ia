using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckJobDemorados
    {
        public static string Query()
        {
            return
            @"  SET NOCOUNT ON
                
	            IF (OBJECT_ID('tempdb..#Result_History_Jobs') IS NOT NULL)
		            DROP TABLE #Result_History_Jobs
		
	            CREATE TABLE #Result_History_Jobs (
		            [Cod]				INT	IDENTITY(1,1),
		            [Instance_Id]		INT,
		            [Job_Id]			VARCHAR(255),
		            [Job_Name]			VARCHAR(255),
		            [Step_Id]			INT,
		            [Step_Name]			VARCHAR(255),
		            [Sql_Message_Id]	INT,
		            [Sql_Severity]		INT,
		            [SQl_Message]		VARCHAR(4490),
		            [Run_Status]		INT,
		            [Run_Date]			VARCHAR(20),
		            [Run_Time]			VARCHAR(20),
		            [Run_Duration]		INT,
		            [Operator_Emailed]	VARCHAR(100),
		            [Operator_NetSent]	VARCHAR(100),
		            [Operator_Paged]	VARCHAR(100),
		            [Retries_Attempted] INT,
		            [Nm_Server]			VARCHAR(100)  
	            )
	
	            DECLARE @ontem VARCHAR(8)
	            SET @ontem  =  CONVERT(VARCHAR(8), (DATEADD(DAY, -1, GETDATE())), 112)

	            INSERT INTO #Result_History_Jobs
	            EXEC [msdb].[dbo].[sp_help_jobhistory] @mode = 'FULL', @start_run_date = @ontem

	            TRUNCATE TABLE [dbo].[CheckJobDemorados]
	
	            INSERT INTO [dbo].[CheckJobDemorados] ( [Job_Name], [Status], [Dt_Execucao], [Run_Duration], [SQL_Message] )
	            SELECT	[Job_Name], 
			            CASE	WHEN [Run_Status] = 0 THEN 'Failed'
					            WHEN [Run_Status] = 1 THEN 'Succeeded'
					            WHEN [Run_Status] = 2 THEN 'Retry (step only)'
					            WHEN [Run_Status] = 3 THEN 'Canceled'
					            WHEN [Run_Status] = 4 THEN 'In-progress message'
					            WHEN [Run_Status] = 5 THEN 'Unknown' 
			            END [Status],
			            CAST([Run_Date] + ' ' +
				            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
				            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-3), 2), 2) + ':' +
				            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-1), 2), 2) AS VARCHAR) AS [Dt_Execucao],
			            RIGHT('00' + SUBSTRING(CAST(Run_Duration AS VARCHAR),(LEN(Run_Duration)-5), 2), 2)+ ':' +
				            RIGHT('00' + SUBSTRING(CAST(Run_Duration AS VARCHAR),(LEN(Run_Duration)-3), 2) ,2) + ':' +
				            RIGHT('00' + SUBSTRING(CAST(Run_Duration AS VARCHAR),(LEN(Run_Duration)-1), 2) ,2) AS [Run_Duration],
			            CAST([SQl_Message] AS VARCHAR(3990)) AS [SQL_Message]	
	            FROM #Result_History_Jobs
	            WHERE 
		              CAST([Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
		              RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-3), 2), 2) + ':' +
		              RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-1), 2), 2) AS DATETIME) >= GETDATE() -1 and
		              CAST([Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2)+ ':' +
		              RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-3), 2), 2) + ':' +
		              RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-1), 2), 2) AS DATETIME) < GETDATE() 
		              AND [Step_Id] = 0
		              AND [Run_Status] = 1
		              AND [Run_Duration] >= 100  -- JOBS que demoraram mais de 1 minuto

	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckJobDemorados] ( [Job_Name], [Status], [Dt_Execucao], [Run_Duration], [SQL_Message] )
		            SELECT 'Sem registro de JOBs Demorados', NULL, NULL, NULL, NULL
	            END
                ";
        }
    }
}
