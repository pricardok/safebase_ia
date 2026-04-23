using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlwaysOnStatsOLD()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"SELECT
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
	ORDER BY [AOPrimary].[Database]";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};