using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckFragmentacaoIndices()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            DECLARE @Max_Dt_Referencia DATETIME

	            SELECT @Max_Dt_Referencia = MAX(Dt_Referencia) FROM [dbo].[vwHistoricoFragmentacaoIndice]

	            TRUNCATE TABLE [dbo].[CheckFragmentacaoIndices]
	
	            INSERT INTO [dbo].[CheckFragmentacaoIndices] (	[Dt_Referencia], [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Nm_Indice], 
															            [Avg_Fragmentation_In_Percent], [Page_Count], [Fill_Factor], [Fl_Compressao] )
	            SELECT	[Dt_Referencia], [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Nm_Indice], 
			            [Avg_Fragmentation_In_Percent], [Page_Count], [Fill_Factor], [Fl_Compressao]
	            FROM [dbo].[vwHistoricoFragmentacaoIndice]
	            WHERE	CAST([Dt_Referencia] AS DATE) = CAST(@Max_Dt_Referencia AS DATE)
			            AND [Avg_Fragmentation_In_Percent] > 10
			            AND [Page_Count] > 1000
	
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckFragmentacaoIndices] (	[Dt_Referencia], [Nm_Servidor], [Nm_Database], [Nm_Tabela], [Nm_Indice], 
																            [Avg_Fragmentation_In_Percent], [Page_Count], [Fill_Factor], [Fl_Compressao] )
		            SELECT NULL, NULL, 'Sem registro de Índice com mais de 10% de Fragmentação', NULL, NULL, NULL, NULL, NULL, NULL
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};