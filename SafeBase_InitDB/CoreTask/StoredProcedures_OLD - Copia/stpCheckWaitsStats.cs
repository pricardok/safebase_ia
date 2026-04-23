using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckWaitsStats()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            DECLARE @Dt_Referencia DATETIME, @Dt_Inicio DATETIME, @Dt_Fim DATETIME
	            SET @Dt_Referencia = CAST(GETDATE()-1 AS DATE)
	
	            SELECT @Dt_Inicio = DATEADD(hh, 7, @Dt_Referencia), @Dt_Fim = DATEADD(hh, 23, @Dt_Referencia)   

	            TRUNCATE TABLE [dbo].[CheckWaitsStats]

	            INSERT INTO [dbo].[CheckWaitsStats](	[WaitType], [Min_Log], [Max_Log], [DIf_Wait_S], [DIf_Resource_S], [DIf_Signal_S], 
												            [DIf_WaitCount], [DIf_Percentage], [Last_Percentage] )
	            EXEC [dbo].[stpHistoricoWaitsStats] @Dt_Inicio, @Dt_Fim
	
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckWaitsStats](	[WaitType], [Min_Log], [Max_Log], [DIf_Wait_S], [DIf_Resource_S], [DIf_Signal_S], 
													            [DIf_WaitCount], [DIf_Percentage], [Last_Percentage] )
		            SELECT 'Sem registro de Waits Stats.', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};