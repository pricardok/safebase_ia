using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCargaHistoricoWaitsStats()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                    SET NOCOUNT ON
                    -- Seleciona o último wait por WaitType.
	                declare @Waits_Before table (WaitType varchar(60), WaitCount bigint, Id_Coleta int)
	                declare @Id_Coleta int

	                -- Seleciona o Id_Coleta da última coleta de dados.
	                select @Id_Coleta = Id_Coleta
	                from HistoricoWaitsStats A
		                join	(
					                select max(Id_HistoricoWaitsStats) AS Id_HistoricoWaitsStats
					                from HistoricoWaitsStats
				                ) B on A.Id_HistoricoWaitsStats = B.Id_HistoricoWaitsStats

	                insert into @Waits_Before
	                select A.WaitType, A.WaitCount, A.Id_Coleta
	                from HistoricoWaitsStats A
		                join	(
					                select [WaitType], max(Id_HistoricoWaitsStats) Id_HistoricoWaitsStats
					                from HistoricoWaitsStats
					                group by [WaitType] 
				                ) B on A.Id_HistoricoWaitsStats = B.Id_HistoricoWaitsStats
			
	                ;WITH Waits AS
		                (
			                SELECT
				                wait_type,
				                wait_time_ms / 1000.0 AS WaitS,
				                (wait_time_ms - signal_wait_time_ms) / 1000.0 AS ResourceS,
				                signal_wait_time_ms / 1000.0 AS SignalS,
				                waiting_tasks_count AS WaitCount,
				                100.0 * wait_time_ms / SUM (wait_time_ms) OVER() AS Percentage,
				                ROW_NUMBER() OVER(ORDER BY wait_time_ms DESC) AS RowNum
			                FROM sys.dm_os_wait_stats
			                WHERE wait_type NOT IN (
				                'CLR_SEMAPHORE', 'LAZYWRITER_SLEEP', 'RESOURCE_QUEUE', 'SLEEP_TASK', 'SLEEP_SYSTEMTASK', 'SQLTRACE_BUFFER_FLUSH', 'WAITFOR', 
				                'CHECKPOINT_QUEUE', 'REQUEST_FOR_DEADLOCK_SEARCH', 'XE_TIMER_EVENT', 'BROKER_TO_FLUSH', 'BROKER_TASK_STOP', 'CLR_MANUAL_EVENT',
				                'CLR_AUTO_EVENT', 'DISPATCHER_QUEUE_SEMAPHORE', 'FT_IFTS_SCHEDULER_IDLE_WAIT', 'XE_DISPATCHER_WAIT', 'XE_DISPATCHER_JOIN', 
				                'BROKER_EVENTHANDLER', 'TRACEWRITE', 'FT_IFTSHC_MUTEX', 'SQLTRACE_INCREMENTAL_FLUSH_SLEEP', 'BROKER_RECEIVE_WAITFOR', 
				                'DBMIRROR_EVENTS_QUEUE', 'DBMIRRORING_CMD', 'BROKER_TRANSMITTER', 'SQLTRACE_WAIT_ENTRIES', 'SLEEP_BPOOL_FLUSH', 'SQLTRACE_LOCK', 
				                'QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP', 'QDS_PERSIST_TASK_MAIN_LOOP_SLEEP', 'HADR_FILESTREAM_IOMGR_IOCOMPLETION',
				                'DIRTY_PAGE_POLL', 'SP_SERVER_DIAGNOSTICS_SLEEP', 'ONDEMAND_TASK_QUEUE','LOGMGR_QUEUE')
		                )
			
	                INSERT INTO HistoricoWaitsStats(WaitType,Wait_S,Resource_S,Signal_S,WaitCount,Percentage,Id_Coleta)
	                SELECT
		                W1.wait_type AS WaitType, 
		                CAST (W1.WaitS AS DECIMAL(14, 2)) AS Wait_S,
		                CAST (W1.ResourceS AS DECIMAL(14, 2)) AS Resource_S,
		                CAST (W1.SignalS AS DECIMAL(14, 2)) AS Signal_S,
		                W1.WaitCount AS WaitCount,
		                CAST (W1.Percentage AS DECIMAL(4, 2)) AS Percentage, isnull(@Id_Coleta,0) + 1
		                --CAST ((W1.WaitS / W1.WaitCount) AS DECIMAL (14, 4)) AS AvgWait_S,
	                   -- CAST ((W1.ResourceS / W1.WaitCount) AS DECIMAL (14, 4)) AS AvgRes_S,
		                --CAST ((W1.SignalS / W1.WaitCount) AS DECIMAL (14, 4)) AS AvgSig_S
	                FROM Waits AS W1
		                INNER JOIN Waits AS W2 ON W2.RowNum <= W1.RowNum
	                GROUP BY W1.RowNum, W1.wait_type, W1.WaitS, W1.ResourceS, W1.SignalS, W1.WaitCount, W1.Percentage
	                HAVING SUM (W2.Percentage) - W1.Percentage < 95 -- percentage threshold
	                OPTION (RECOMPILE); 

	                -- Verifica se o valor Wait_S diminuiu para algum WaitType.
	                if exists	(
					                select null
					                from HistoricoWaitsStats A
					                join	(	
								                select [WaitType], max(Id_HistoricoWaitsStats) Id_HistoricoWaitsStats
								                from HistoricoWaitsStats
								                group by [WaitType] 
							                ) B on A.Id_HistoricoWaitsStats = B.Id_HistoricoWaitsStats
					                join @Waits_Before C on A.WaitType = C.WaitType and A.WaitCount < C.WaitCount 
											                and isnull(A.Id_Coleta,0)  = isnull(C.Id_Coleta,0) + 1 
				                )
	                BEGIN
		                INSERT INTO HistoricoWaitsStats(WaitType)
		                values('RESET WAITS STATS')
	                END

	            ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};