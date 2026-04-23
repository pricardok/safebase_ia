using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckJobsRunning()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            TRUNCATE TABLE [dbo].[CheckJobsRunning]

	            INSERT INTO [dbo].[CheckJobsRunning] (Nm_JOB, Dt_Inicio, Qt_Duracao, Nm_Step)
	            SELECT
		            j.name AS Nm_JOB,
		            CONVERT(VARCHAR(16), start_execution_date,120) AS Dt_Inicio,
		            RTRIM(CONVERT(CHAR(17), DATEDIFF(SECOND, CONVERT(DATETIME, start_execution_date), GETDATE()) / 86400)) + ' Dia(s) ' +
		            RIGHT('00' + RTRIM(CONVERT(CHAR(7), DATEDIFF(SECOND, CONVERT(DATETIME, start_execution_date), GETDATE()) % 86400 / 3600)), 2) + ' Hora(s) ' +
		            RIGHT('00' + RTRIM(CONVERT(CHAR(7), DATEDIFF(SECOND, CONVERT(DATETIME, start_execution_date), GETDATE()) % 86400 % 3600 / 60)), 2) + ' Minuto(s) ' AS Qt_Duracao,
		            js.step_name AS Nm_Step
	            FROM msdb.dbo.sysjobactivity ja 
	            LEFT JOIN msdb.dbo.sysjobhistory jh 
		            ON ja.job_history_id = jh.instance_id
	            JOIN msdb.dbo.sysjobs j 
	            ON ja.job_id = j.job_id
	            JOIN msdb.dbo.sysjobsteps js
		            ON ja.job_id = js.job_id
		            AND ISNULL(ja.last_executed_step_id,0)+1 = js.step_id
	            WHERE	ja.session_id = (SELECT TOP 1 session_id FROM msdb.dbo.syssessions ORDER BY agent_start_date DESC)
			            AND start_execution_date is not null
			            AND stop_execution_date is null
			            AND DATEDIFF(minute,start_execution_date, GETDATE()) >= 10		-- No minimo 10 minutos em execução

	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckJobsRunning] (Nm_JOB, Dt_Inicio, Qt_Duracao, Nm_Step)
		            SELECT 'Sem JOBs em execução a mais de 10 minutos', NULL, NULL, NULL
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};