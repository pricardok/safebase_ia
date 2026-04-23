using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpCheckQueriesRunning
    {
        public static string Query()
        {
            return
            // @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpCheckQueriesRunning',GETDATE())";
            @"SET NOCOUNT ON
                
	            IF ( OBJECT_ID('tempdb..#Resultado_WhoisActive') IS NOT NULL )
		            DROP TABLE #Resultado_WhoisActive
				
	            CREATE TABLE #Resultado_WhoisActive (		
		            [dd hh:mm:ss.mss]		VARCHAR(20),
		            [database_name]			NVARCHAR(128),		
		            [login_name]			NVARCHAR(128),
		            [host_name]				NVARCHAR(128),
		            [start_time]			DATETIME,
		            [status]				VARCHAR(30),
		            [session_id]			INT,
		            [blocking_session_id]	INT,
		            [wait_info]				VARCHAR(MAX),
		            [open_tran_count]		INT,
		            [CPU]					VARCHAR(MAX),
		            [reads]					VARCHAR(MAX),
		            [writes]				VARCHAR(MAX),
		            [sql_command]			XML
	            )
	
	            -- Retorna todos os processos que estão sendo executados no momento
	            EXEC [dbo].[sp_WhoIsActive]
			            @get_outer_command =	1,
			            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
			            @destination_table =	'#Resultado_WhoisActive'

	            ALTER TABLE #Resultado_WhoisActive
	            ALTER COLUMN [sql_command] VARCHAR(MAX)
	
	            UPDATE #Resultado_WhoisActive
	            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')

	            -- Exclui os registros das queries com menos de 2 horas de execução
	            DELETE #Resultado_WhoisActive	
	            where DATEDIFF(MINUTE, start_time, GETDATE()) < 120
	
	            TRUNCATE TABLE [dbo].[CheckQueriesRunning]

	            INSERT INTO [dbo].[CheckQueriesRunning]
	            SELECT * FROM #Resultado_WhoisActive

	            IF (@@ROWCOUNT = 0)
	            BEGIN
		            INSERT INTO [dbo].[CheckQueriesRunning]( [dd hh:mm:ss.mss], database_name, login_name, host_name, start_time, status, session_id, blocking_session_id, wait_info, open_tran_count, CPU, reads, writes, sql_command )
		            SELECT NULL, 'Sem registro de Queries executando a mais de 2 horas', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
	            END
                ";

        }
    }
}
