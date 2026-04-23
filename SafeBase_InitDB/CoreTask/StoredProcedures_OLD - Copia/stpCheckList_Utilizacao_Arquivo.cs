using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpCheckList_Utilizacao_Arquivo()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            DECLARE @Dt_Referencia DATETIME = CAST(GETDATE()-1 AS DATE)

	            -- WRITES
	            if (OBJECT_ID('tempdb..#arquivos_writes') is not null)
		            drop table #arquivos_writes

	            select  TOP 10
			            A.Nm_Database, A.file_id
			            , B.io_stall_write_ms - A.io_stall_write_ms AS io_stall_write_ms		
			            , B.num_of_writes - A.num_of_writes AS num_of_writes
			            , CASE WHEN (1.0 + B.num_of_writes - A.num_of_writes) <> 0 THEN
					            CAST(((B.io_stall_write_ms - A.io_stall_write_ms)/(1.0+ B.num_of_writes - A.num_of_writes)) AS NUMERIC(15,1)) 
				            ELSE
					            0
			              END AS [avg_write_stall_ms]
	            into #arquivos_writes		  
	            from [dbo].HistoricoUtilizacaoArquivo A
	            JOIN [dbo].HistoricoUtilizacaoArquivo B on	A.Nm_Database = B.Nm_Database and A.file_id = B.file_id
													            and B.Dt_Registro >= @Dt_Referencia and B.Dt_Registro < @Dt_Referencia + 1
													            and DATEPART(HH,B.Dt_Registro) = 18 and DATEPART(MINUTE,B.Dt_Registro) BETWEEN 0 AND 5	-- 18 HORAS
	            where	A.Dt_Registro >= @Dt_Referencia and A.Dt_Registro < @Dt_Referencia + 1
			            and DATEPART(HH,A.Dt_Registro) = 9 and DATEPART(MINUTE,A.Dt_Registro) BETWEEN 0 AND 5											-- 9 HORAS	
	            order by num_of_writes  DESC 
	
	            -- READS
	            if (OBJECT_ID('tempdb..#arquivos_reads') is not null)
		            drop table #arquivos_reads

	            select  TOP 10
			            A.Nm_Database, A.file_id
			            , B.io_stall_read_ms - A.io_stall_read_ms AS io_stall_read_ms
			            , B.num_of_reads - A.num_of_reads AS num_of_reads		
			            , CASE WHEN (1.0 + B.num_of_reads - A.num_of_reads) <> 0 THEN
					            CAST(((B.io_stall_read_ms - A.io_stall_read_ms)/(1.0 + B.num_of_reads - A.num_of_reads)) AS NUMERIC(15,1))
				            ELSE 
					            0
			              END AS [avg_read_stall_ms]
	            into #arquivos_reads		  
	            from [dbo].HistoricoUtilizacaoArquivo A
	            JOIN [dbo].HistoricoUtilizacaoArquivo B on	A.Nm_Database = B.Nm_Database and A.file_id = B.file_id
													            and B.Dt_Registro >= @Dt_Referencia and B.Dt_Registro < @Dt_Referencia + 1
													            and DATEPART(HH,B.Dt_Registro) = 18 and DATEPART(MINUTE,B.Dt_Registro) BETWEEN 0 AND 5	-- 18 HORAS
	            where	A.Dt_Registro >= @Dt_Referencia and A.Dt_Registro < @Dt_Referencia + 1
			            and DATEPART(HH,A.Dt_Registro) = 9 and DATEPART(MINUTE,A.Dt_Registro) BETWEEN 0 AND 5											-- 9 HORAS	
	            order by num_of_reads  DESC 

	            -- WRITES
	            TRUNCATE TABLE [dbo].[CheckUtilizacaoArquivoWrites]
	
	            INSERT INTO [dbo].[CheckUtilizacaoArquivoWrites]
	            SELECT *
	            FROM #arquivos_writes

	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckUtilizacaoArquivoWrites]
		            SELECT 'Sem registro de Utilização dos Arquivos - Writes', NULL, NULL, NULL, NULL
	            END
	
	            -- READS
	            TRUNCATE TABLE [dbo].[CheckUtilizacaoArquivoReads]
	
	            INSERT INTO [dbo].[CheckUtilizacaoArquivoReads]
	            SELECT *
	            FROM #arquivos_reads
	
	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckUtilizacaoArquivoReads]
		            SELECT 'Sem registro de Utilização dos Arquivos - Reads', NULL, NULL, NULL, NULL
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};