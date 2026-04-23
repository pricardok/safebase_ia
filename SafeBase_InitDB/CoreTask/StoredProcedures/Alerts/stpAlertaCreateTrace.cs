using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCreateTrace
    {
        public static string Query()
        {

            // VALIDA CRIAÇÃO DE DIRETORIOS DE LOGS E AFINS 
            Core.ExecuteCheckDir();

            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCreateTrace',GETDATE())";

            @"  SET NOCOUNT ON;

                SET QUOTED_IDENTIFIER ON;

                DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Trace Queries Demoradas' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base varchar(256) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Trace_Id INT, @Path VARCHAR(MAX), @CaminhoPath nvarchar(256)

                SELECT 
		                @Trace_Id = id,
		                @Path = [path]
                FROM sys.traces
                WHERE [path] LIKE '%ResultadoTraceLog.trc'

                -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
                SELECT @CaminhoPath = Ds_Caminho_Log
                FROM [dbo].[AlertaParametro]
                            WHERE [Id_AlertaParametro] = @Id_AlertaParametro

                DECLARE @ca nvarchar(256) = @Ds_Caminho_Base + @CaminhoPath

                IF (@Trace_Id IS NOT NULL)
                BEGIN

                    -- Interrompe o rastreamento especificado.
                    EXEC sys.sp_trace_setstatus
                        @Trace_Id = @Trace_Id, 
                        @status = 0

                    -- Fecha o rastreamento especificado e exclui sua definição do servidor.
                    EXEC sys.sp_trace_setstatus 
                        @Trace_Id = @Trace_Id,
                        @status = 2

                    IF (OBJECT_ID('dbo.ResultadoTraceLog') IS NULL)
                    BEGIN

                        CREATE TABLE [safebase].[dbo].[ResultadoTraceLog] (
                            [TextData] [text] NULL,
                            [NTUserName] [varchar] (128) NULL,
                            [HostName] [varchar] (128) NULL,
                            [ApplicationName] [varchar] (128) NULL,
                            [LoginName] [varchar] (128) NULL,
                            [SPID] [int] NULL,
                            [Duration] [numeric] (15, 2) NULL,
                            [StartTime] [datetime] NULL,
                            [EndTime] [datetime] NULL,
                            [Reads] [int] NULL,
                            [Writes] [int] NULL,
                            [CPU] [int] NULL,
                            [ServerName] [varchar] (128) NULL,
                            [DataBaseName] [varchar] (128) NULL,
                            [RowCounts] [int] NULL,
                            [SessionLoginName] [varchar] (128) NULL
                        )
                        WITH ( DATA_COMPRESSION = PAGE )
                        CREATE CLUSTERED INDEX [SK01_Traces] ON [safebase].[dbo].[ResultadoTraceLog] ([StartTime]) WITH (FILLFACTOR=80, STATISTICS_NORECOMPUTE=ON, DATA_COMPRESSION = PAGE) ON [PRIMARY]
    
                    END

                    INSERT INTO [safebase].[dbo].[ResultadoTraceLog] (
                        TextData, 
                        NTUserName, 
                        HostName, 
                        ApplicationName, 
                        LoginName, 
                        SPID, 
                        Duration, 
                        StartTime,
                        EndTime, 
                        Reads,
                        Writes, 
                        CPU, 
                        ServerName, 
                        DataBaseName, 
                        RowCounts, 
                        SessionLoginName
                    )
                    SELECT
                        TextData,
                        NTUserName,
                        HostName,
                        ApplicationName,
                        LoginName,
                        SPID,
                        CAST(Duration / 1000 / 1000.00 AS NUMERIC(15, 2)) Duration,
                        StartTime,
                        EndTime,
                        Reads,
                        Writes,
                        CPU,
                        ServerName,
                        DatabaseName,
                        RowCounts,
                        SessionLoginName
                    FROM
                        ::fn_trace_gettable(@Path, DEFAULT)
                    WHERE
                        Duration IS NOT NULL
                        AND Reads < 100000000
                    ORDER BY
                        StartTime

                    -- Apaga o arquivo de trace
	                -- exec dbo.stpApagaArquivo @Path
                    exec dbo.stpDeleteFile @Path
	                -- exec dbo.stpDeleteFile 'C:\Data\Logs\ResultadoTraceLog.trc'

                END

                -- Ativa o trace novamenmte
                DECLARE
                    @resource INT,
                    @maxfilesize BIGINT = 50,
                    @on BIT = 1, -- Habilitado
                    @bigintfilter BIGINT = (1000000 * 7) -- 7 segundos

                -- Criação do trace
                SET @Trace_Id = NULL

                EXEC @resource = sys.sp_trace_create @Trace_Id OUTPUT, 0, @ca, @maxfilesize, NULL 

                IF (@resource = 0)
                BEGIN

                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 1, @on  
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 6, @on  
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 8, @on  
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 10, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 11, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 12, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 13, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 14, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 15, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 16, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 17, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 18, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 26, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 35, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 40, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 48, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 10, 64, @on 

                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 1,  @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 6,  @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 8,  @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 10, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 11, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 12, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 13, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 14, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 15, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 16, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 17, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 18, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 26, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 35, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 40, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 48, @on 
                    EXEC sys.sp_trace_setevent @Trace_Id, 12, 64, @on 

                    -- Aqui é onde filtramos o tempo da query que irá cair no trace
                    EXEC sys.sp_trace_setfilter @Trace_Id, 13, 0, 4, @bigintfilter -- O 4 significa >= @bigintfilter 

                    -- Ativa o trace
                    EXEC sys.sp_trace_setstatus @Trace_Id, 1

                END

            
        ";
        }
    }
}
