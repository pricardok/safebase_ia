using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckContadores
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckContadores',GETDATE())";
            @"
                SET NOCOUNT ON
                TRUNCATE TABLE [dbo].[CheckContadores]
	            TRUNCATE TABLE [dbo].[CheckContadoresEmail]

	            DECLARE @Dt_Referencia DATETIME
	            SET @Dt_Referencia = CAST(GETDATE()-1 AS DATE)
	
	            INSERT INTO [dbo].[CheckContadores]( [Hora], [Nm_Contador], [Media] )
	            SELECT DATEPART(hh, [Dt_Log]) AS [Hora], [Nm_Contador], AVG([Valor]) AS [Media]
	            FROM [dbo].[ContadorRegistro] A
		            JOIN [dbo].[Contador] B ON A.[Id_Contador] = B.[Id_Contador]
	            WHERE [Dt_Log] >= DATEADD(hh, 7, @Dt_Referencia) AND [Dt_Log] < DATEADD(hh, 23, @Dt_Referencia)   
	            GROUP BY DATEPART(hh, [Dt_Log]), [Nm_Contador]
	
	            INSERT INTO [dbo].[CheckContadores]( [Hora], [Nm_Contador], [Media] )	
	            SELECT DATEPART(HH, [StartTime]), 'Qtd Queries Lentas', COUNT(*)
	            FROM [dbo].[SQLTraceLog]
	            WHERE	[StartTime] >= @Dt_Referencia AND [StartTime] < @Dt_Referencia + 1
			            AND DATEPART(HH, [StartTime]) >= 7 AND DATEPART(HH, [StartTime]) < 23
	            GROUP BY DATEPART(HH, [StartTime])
	
	            INSERT INTO [dbo].[CheckContadores]( [Hora], [Nm_Contador], [Media] )	
	            SELECT DATEPART(HH, [StartTime]), 'Reads Queries Lentas', SUM(CAST(Reads AS BIGINT))
	            FROM [dbo].[SQLTraceLog]
	            WHERE	[StartTime] >= @Dt_Referencia AND [StartTime] < @Dt_Referencia + 1
			            AND DATEPART(HH, [StartTime]) >= 7 AND DATEPART(HH, [StartTime]) < 23
	            GROUP BY DATEPART(HH, [StartTime])
	
	            IF NOT EXISTS (SELECT TOP 1 NULL FROM [dbo].[CheckContadores])
	            BEGIN
		            INSERT INTO [dbo].[CheckContadores]( [Hora], [Nm_Contador], [Media] )
		            SELECT NULL, 'Sem registro de Contador', NULL
	            END
		
	            INSERT INTO [dbo].[CheckContadoresEmail]
	            SELECT	ISNULL(CAST(U.[Hora]					AS VARCHAR), '-')	AS [Hora], 
			            ISNULL(CAST(U.[BatchRequests]			AS VARCHAR), '-')	AS [BatchRequests],
			            ISNULL(CAST(U.[CPU]						AS VARCHAR), '-')	AS [CPU],
			            ISNULL(CAST(U.[Page Life Expectancy]	AS VARCHAR), '-')	AS [Page_Life_Expectancy], 
			            ISNULL(CAST(U.[User_Connection]			AS VARCHAR), '-')	AS [User_Connection],
			            ISNULL(CAST(U.[Qtd Queries Lentas]		AS VARCHAR), '-')	AS [Qtd_Queries_Lentas], 
			            ISNULL(CAST(U.[Reads Queries Lentas]	AS VARCHAR), '-')	AS [Reads_Queries_Lentas]
	            FROM [dbo].[CheckContadores] AS C
	            PIVOT	(
				            SUM([Media]) 
				            FOR [Nm_Contador] IN (	[BatchRequests], [CPU], [Page Life Expectancy], 
										            [User_Connection], [Qtd Queries Lentas], [Reads Queries Lentas])
			            ) AS U
	            ";
        }
    }
}
