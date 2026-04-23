using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCargaHistoricoErrosDB
    {
        public static string Query()
        {

            string checkInicio = ExecuteSql.ExecuteQuery("SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList'");
            string checkFim = ExecuteSql.ExecuteQuery("SELECT Ds_Caminho_Log FROM [dbo].AlertaParametro (NOLOCK) where Nm_Alerta = 'Historico Erros'");
            string LocalFile = checkInicio + checkFim;

            // VALIDA CRIAÇÃO DE DIRETORIOS DE LOGS E AFINS 
            Core.ExecuteCheckDir();

            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpHistoricoErrosDB',GETDATE())";
            @"      
                    SET NOCOUNT ON
             		IF (OBJECT_ID('dbo.HistoricoErrosDB') IS NULL)
					BEGIN
						-- DROP TABLE dbo.HistoricoErrosDB
						--CREATE TABLE dbo.HistoricoErrosDB (
						--	DataEvento DATETIME,
						--	SessionID INT,
						--	[DatabaseName] VARCHAR(100),
						--	SessionUsername VARCHAR(100),
						--	ClientHostname VARCHAR(100),
						--	ClientAppName VARCHAR(100),
						--	[ErrorNumber] INT,
						--	Severity INT,
						--	[State] INT,
						--	SqlText XML,
						--	[message] VARCHAR(MAX)
						--)
						CREATE CLUSTERED INDEX IX_HistoricoErrosDB ON dbo.HistoricoErrosDB(DataEvento)
						-- Apaga a sessão, caso ela já exista
                    
						IF ((SELECT COUNT(*) FROM sys.dm_xe_sessions WHERE [name] = 'CapturaErrosSistema') > 0) DROP EVENT SESSION [CapturaErrosSistema] ON SERVER 
					

						CREATE EVENT SESSION [CapturaErrosSistema] ON SERVER 
						ADD EVENT sqlserver.error_reported (
							ACTION(client_app_name,sqlserver.client_hostname,sqlserver.database_name,sqlserver.session_id,sqlserver.session_nt_username,sqlserver.sql_text)

							-- Adicionado manualmente, pois não é possível filtrar pela coluna 'Severity' pela interface
							WHERE severity > 10
						)
						ADD TARGET package0.event_file(SET filename = N'" + LocalFile + @"', max_rollover_files = (0))
						WITH(STARTUP_STATE = ON) -- Será iniciado automaticamente com a instância

						--Ativando a sessão(por padrão, ela é criada desativada)
						ALTER EVENT SESSION[CapturaErrosSistema] ON SERVER STATE = START
                    END

                    DECLARE @TimeZone INT = DATEDIFF(HOUR, GETUTCDATE(), GETDATE())
                    DECLARE @Dt_Ultimo_Evento DATETIME = ISNULL((SELECT MAX(DataEvento) FROM dbo.HistoricoErrosDB WITH(NOLOCK)), '1990-01-01')


                    IF(OBJECT_ID('tempdb..#Eventos') IS NOT NULL) DROP TABLE #Eventos

                    ; WITH CTE AS(
                         SELECT CONVERT(XML, event_data) AS event_data
 
                         FROM sys.fn_xe_file_target_read_file(N'" + LocalFile + @"*.xel', NULL, NULL, NULL)
                     )
                    SELECT
                        DATEADD(HOUR, @TimeZone, CTE.event_data.value('(//event/@timestamp)[1]', 'datetime')) AS DataEvento,
                        CTE.event_data
                    INTO
                        #Eventos
                    FROM
                        CTE
                    WHERE
                        DATEADD(HOUR, @TimeZone, CTE.event_data.value('(//event/@timestamp)[1]', 'datetime')) > @Dt_Ultimo_Evento


                    SET QUOTED_IDENTIFIER ON

                    INSERT INTO dbo.HistoricoErrosDB
                    SELECT
                        A.DataEvento,
                        xed.event_data.value('(action[@name=''session_id'']/value)[1]', 'int') AS [session_id],
                        xed.event_data.value('(action[@name=''database_name'']/value)[1]', 'varchar(100)') AS [database_name],
                        xed.event_data.value('(action[@name=''session_nt_username'']/value)[1]', 'varchar(100)') AS [session_nt_username],
                        xed.event_data.value('(action[@name=''client_hostname'']/value)[1]', 'varchar(100)') AS [client_hostname],
                        xed.event_data.value('(action[@name=''client_app_name'']/value)[1]', 'varchar(100)') AS [client_app_name],
                        xed.event_data.value('(data[@name=''error_number'']/value)[1]', 'int') AS [error_number],
                        xed.event_data.value('(data[@name=''severity'']/value)[1]', 'int') AS [severity],
                        xed.event_data.value('(data[@name=''state'']/value)[1]', 'int') AS [state],
                        TRY_CAST(xed.event_data.value('(action[@name=''sql_text'']/value)[1]', 'varchar(max)') AS XML) AS [sql_text],
                        xed.event_data.value('(data[@name=''message'']/value)[1]', 'varchar(max)') AS [message]
                    FROM
                    #Eventos A
                        CROSS APPLY A.event_data.nodes('//event') AS xed(event_data)



            ";
        }
    }
}
