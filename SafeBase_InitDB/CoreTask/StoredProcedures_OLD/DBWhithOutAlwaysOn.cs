using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class DBWhithOutAlwaysOn
    {
        public static string Query()
        {
            return
            @"SELECT name FROM sys.databases
                                    WHERE[name] NOT IN(SELECT[AOPrimary].[Database]
                                                                    FROM

                                                                                    (
                                                                                        SELECT DB_NAME(database_id) AS [Database]
                                                                                            FROM sys.dm_hadr_database_replica_states
                                                                                        WHERE is_local = 1
                                                                                    ) [AOPrimary]
                        INNER JOIN
                                                                                    (
                                                                                        SELECT DB_NAME(database_id) AS [Database]
                                                                                            FROM sys.dm_hadr_database_replica_states
                                                                                        WHERE is_local = 0
                                                                                    ) [AOSecondary] ON[AOPrimary].[Database] = [AOSecondary].[Database])
                                    AND is_read_only = 0
                                    AND[name] NOT IN('master','tempdb','model','msdb')
                    FOR XML RAW('DB'),ROOT('NoAwaysOn')";
        }
    }
}
