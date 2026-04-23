using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void StpAlwaysOnStats(SqlString TYPE1, SqlString NAME1)
    {
        var TYPE = TYPE1.IsNull ? "NULL" : TYPE1.Value;
        var NAME = NAME1.IsNull ? "F" : NAME1.Value;
        //string NAME = null;

      

        // Create the command
        SqlCommand cmdAlwayOn = new SqlCommand();
        cmdAlwayOn.CommandText =
              @"DECLARE @Result int

                IF(@TYPE='M') -- Monitor
                BEGIN
	                SELECT 
		                @Result = DATEDIFF(MINUTE,[AOSecondary].[SyncLastCommit],[AOPrimary].[SyncLastCommit])
	                FROM 
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 1
		                ) [AOPrimary] 
	                INNER JOIN
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 0
		                ) [AOSecondary] 
	                ON [AOPrimary].[Database]=[AOSecondary].[Database]
	                WHERE [AOPrimary].[Database]=@NAME

	                PRINT ISNULL(@Result,0)
                END
                ELSE IF(@TYPE='S') -- Single database
                BEGIN
	                SELECT
		                [AOPrimary].[Database],
		                [AOSecondary].SyncState,
		                [AOSecondary].SyncHealth,
		                DATEDIFF(MINUTE,[AOSecondary].[SyncLastCommit],[AOPrimary].[SyncLastCommit])
	                FROM 
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,synchronization_health_desc AS [SyncHealth]
			                ,synchronization_state_desc AS [SyncState]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 1
		                ) [AOPrimary] 
	                INNER JOIN
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,synchronization_health_desc AS [SyncHealth]
			                ,synchronization_state_desc AS [SyncState]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 0
		                ) [AOSecondary] 
	                ON [AOPrimary].[Database]=[AOSecondary].[Database]
	                WHERE [AOPrimary].[Database]=@NAME
                END 
                ELSE IF(@TYPE='F') -- All databases
                BEGIN
	                SELECT
		                [AOPrimary].[Database],
		                [AOSecondary].SyncState,
		                [AOSecondary].SyncHealth,
		                DATEDIFF(MINUTE,[AOSecondary].[SyncLastCommit],[AOPrimary].[SyncLastCommit])
	                FROM 
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,synchronization_health_desc AS [SyncHealth]
			                ,synchronization_state_desc AS [SyncState]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 1
		                ) [AOPrimary] 
	                INNER JOIN
		                (
		                SELECT
			                 DB_NAME(database_id) AS [Database]
			                ,synchronization_health_desc AS [SyncHealth]
			                ,synchronization_state_desc AS [SyncState]
			                ,last_commit_time AS [SyncLastCommit]
		                FROM sys.dm_hadr_database_replica_states
		                WHERE is_local= 0
		                ) [AOSecondary] 
	                ON [AOPrimary].[Database]=[AOSecondary].[Database]
	                ORDER BY [AOPrimary].[Database]
                END";
        cmdAlwayOn.Parameters.AddWithValue("@NAME", NAME1);
        cmdAlwayOn.Parameters.AddWithValue("@TYPE", TYPE1);
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(cmdAlwayOn);
    }
};