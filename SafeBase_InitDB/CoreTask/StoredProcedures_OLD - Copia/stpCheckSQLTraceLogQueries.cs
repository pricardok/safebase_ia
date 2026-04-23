using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckSQLTraceLogQueries()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            DECLARE @Dt_Referencia DATETIME
	            SET @Dt_Referencia = CAST(GETDATE() AS DATE)
	
	            -- Busca as queries lentas
	            IF (OBJECT_ID('tempdb..#Queries_Demoradas') IS NOT NULL) 
		            DROP TABLE #Queries_Demoradas

	            SELECT	[TextData], [NTUserName], [HostName], [ApplicationName], [LoginName], [SPID], [Duration], [StartTime], 
			            [EndTime], [ServerName], cast([Reads] AS BIGINT) AS [Reads], [Writes], [CPU], [DataBaseName], [RowCounts], [SessionLoginName]
	            INTO #Queries_Demoradas
	            FROM [dbo].[SQLTraceLog] (nolock)
	            WHERE	[StartTime] >= DATEADD(DAY, -10, @Dt_Referencia)
			            AND [StartTime] < @Dt_Referencia
			            AND DATEPART(HOUR, [StartTime]) BETWEEN 7 AND 22	
	
	            ----------------------------------------------------------------------------------------------------------------------------
	            -- DIA ANTERIOR
	            ----------------------------------------------------------------------------------------------------------------------------
	            IF (OBJECT_ID('tempdb..#TOP10_Dia_Anterior') IS NOT NULL) 
		            DROP TABLE #TOP10_Dia_Anterior

	            SELECT	TOP 10 LTRIM(CAST([TextData] AS CHAR(150))) AS [PrefixoQuery], COUNT(*) AS [QTD], SUM([Duration]) AS [Total], 
			            AVG([Duration]) AS [Media], MIN([Duration]) AS [Menor], MAX([Duration]) AS [Maior],  
			            SUM([Writes]) AS [Writes], SUM([CPU]) AS [CPU], SUM([Reads]) AS [Reads]
	            INTO #TOP10_Dia_Anterior
	            FROM #Queries_Demoradas
	            WHERE	[StartTime] >= DATEADD(DAY, -1, @Dt_Referencia)
			            AND [StartTime] < @Dt_Referencia
	            GROUP BY LTRIM(CAST([TextData] AS CHAR(150)))
	            ORDER BY COUNT(*) DESC
		
	            TRUNCATE TABLE [dbo].[CheckDBControllerQueries]
		
	            INSERT INTO [dbo].[CheckDBControllerQueries] ( [PrefixoQuery], [QTD], [Total], [Media], [Menor], [Maior], [Writes], [CPU], [Reads], [Ordem] )
	            SELECT [PrefixoQuery], [QTD], [Total], [Media], [Menor], [Maior], [Writes], [CPU], [Reads], 1 AS [Ordem]
	            FROM #TOP10_Dia_Anterior	
		
	            IF (@@ROWCOUNT <> 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckDBControllerQueries] ( [PrefixoQuery], [QTD], [Total], [Media], [Menor], [Maior], [Writes], [CPU], [Reads], [Ordem] )
		            SELECT	'OUTRAS' AS [PrefixoQuery], COUNT(*) AS [QTD], SUM([Duration]) AS [Total], 
				            AVG([Duration]) AS [Media], MIN([Duration]) AS [Menor], MAX([Duration]) AS [Maior],  
				            SUM([Writes]) AS [Writes], SUM([CPU]) AS [CPU], SUM([Reads]) AS [Reads], 2 AS [Ordem]
		            FROM #Queries_Demoradas A
		            WHERE	LTRIM(CAST([TextData] AS CHAR(150))) NOT IN (SELECT [PrefixoQuery] FROM #TOP10_Dia_Anterior)
				            AND	[StartTime] >= DATEADD(DAY, -1, @Dt_Referencia)
				            AND [StartTime] < @Dt_Referencia

		            INSERT INTO [dbo].[CheckDBControllerQueries] ( [PrefixoQuery], [QTD], [Total], [Media], [Menor], [Maior], [Writes], [CPU], [Reads], [Ordem] )
		            SELECT	'TOTAL' AS [PrefixoQuery], SUM([QTD]), SUM([Total]), AVG([Media]), MIN([Menor]) AS [Menor], 
				            MAX([Maior]) AS [Maior], SUM([Writes]) AS [Writes], SUM([CPU]) AS [CPU], SUM([Reads]) AS [Reads], 3 AS [Ordem]
		            FROM [dbo].[CheckDBControllerQueries]
	            END
	            ELSE
	            BEGIN
		            INSERT INTO [dbo].[CheckDBControllerQueries] ( [PrefixoQuery], [QTD], [Total], [Media], [Menor], [Maior], [Writes], [CPU], [Reads], [Ordem] )	
		            SELECT 'Sem registro de Queries Demoradas', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1		
	            END

	            ----------------------------------------------------------------------------------------------------------------------------
	            -- GERAL - 10 DIAS ATRAS
	            ----------------------------------------------------------------------------------------------------------------------------	
	            IF (OBJECT_ID('tempdb..#TOP10_Geral') IS NOT NULL) 
		            DROP TABLE #TOP10_Geral

	            SELECT	TOP 10 CONVERT(VARCHAR(10), [StartTime], 120) AS Data, COUNT(*) AS [QTD]
	            INTO #TOP10_Geral
	            FROM #Queries_Demoradas
	            GROUP BY CONVERT(VARCHAR(10), [StartTime], 120)
	
	            TRUNCATE TABLE [dbo].[CheckDBControllerQueriesGeral]
		
	            INSERT INTO [dbo].[CheckDBControllerQueriesGeral] ( [Data], [QTD] )
	            SELECT [Data], [QTD]
	            FROM #TOP10_Geral
		
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckDBControllerQueriesGeral] ( [Data], [QTD] )	
		            SELECT 'Sem registro de Queries Demoradas', NULL		
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};