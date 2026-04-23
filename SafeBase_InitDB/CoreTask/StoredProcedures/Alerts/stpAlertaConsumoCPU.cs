using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaConsumoCPU
    {
        public static string Query()
        {
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaConsumoCPU',GETDATE())";
			@"
                 SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;

                -- Consumo CPU
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Consumo CPU' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	            -- Declara as variaveis
	            DECLARE	@Subject VARCHAR(500), @Fl_Tipo TINYINT, @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @CPU_Parametro INT,
			            @AlertaCPUAgarradosHeader VARCHAR(MAX), @AlertaCPUAgarradosTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200), 
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)


	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @CPU_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG,
				      @Ds_Menssageiro_01 = A.Ds_Menssageiro_01,
				      @Ds_Menssageiro_02 = A.Ds_Menssageiro_02,
                      @Ds_Menssageiro_03 = A.Ds_Menssageiro_03,
		              @CaminhoPath = Ds_Caminho_Log, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail,
                      @Ds_Email_Assunto_solucao = B.SubjectSolution,
                      @Ds_Email_Texto_solucao = B.MailTextSolution,
                      @Ds_Email_Assunto_alerta = B.SubjectProblem,
                      @Ds_Email_Texto_alerta = B.MailTextProblem,
                      @Ass = C.Assinatura
	            FROM [dbo].[AlertaParametro] A
                INNER JOIN [dbo].[AlertaParametroMenssage] B ON A.Id_AlertaParametro = B.IdAlertaParametro
			    INNER JOIN [dbo].[MailAssinatura] C ON C.Id = A.IdMailAssinatura
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

                DECLARE @CanalTelegram VARCHAR(100) = (SELECT A.canal FROM [dbo].[AlertaMsgToken] A
                      INNER JOIN [dbo].AlertaParametro B ON A.Id = B.Ds_Menssageiro_01 where b.Ds_Menssageiro_01 = @Ds_Menssageiro_01 AND B.Id_AlertaParametro = @Telegram AND B.Ativo = 1) 

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            --	Cria Tabela para armazenar os Dados da SP_WHOISACTIVE
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Cria a tabela que ira armazenar os dados dos processos
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

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProc') IS NOT NULL )
		            DROP TABLE ##VerificaProc
	
                 CREATE TABLE ##VerificaProc
	               ([Duração]             VARCHAR(100),
	                [database_name]       VARCHAR(100),
	                [login_name]          VARCHAR(100),
	                [host_name]           VARCHAR(100),
	                [start_time]          VARCHAR(100),
	                [status]              VARCHAR(100),
	                [session_id]          VARCHAR(100),
	                [blocking_session_id] VARCHAR(100),
	                [Wait]                VARCHAR(100),
	                [open_tran_count]     VARCHAR(100),
	                [CPU]                 VARCHAR(100),
	                [reads]               VARCHAR(100),
	                [writes]              VARCHAR(100),
	                [sql_command]         VARCHAR(100),
	               );

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaCPU') IS NOT NULL )
		            DROP TABLE ##VerificaCPU
	
                 CREATE TABLE ##VerificaCPU
	               ([SQLProcessUtilization]    VARCHAR(100),
	                [OtherProcessUtilization]  VARCHAR(100),
	                [SystemIdle]			 VARCHAR(100),
	                [CPU_Utilization]          VARCHAR(100)
	               );

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Verifica a utilização da CPU
	            --------------------------------------------------------------------------------------------------------------------------------	
	            IF ( OBJECT_ID('tempdb..#CPU_Utilization') IS NOT NULL )
		            DROP TABLE #CPU_Utilization
	
	            SELECT TOP(2)
		            record_id,
		            [SQLProcessUtilization],
		            100 - SystemIdle - SQLProcessUtilization as OtherProcessUtilization,
		            [SystemIdle],
		            100 - SystemIdle AS CPU_Utilization
	            INTO #CPU_Utilization
	            FROM	( 
				            SELECT	record.value('(./Record/@id)[1]', 'int')													AS [record_id], 
						            record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int')			AS [SystemIdle],
						            record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int')	AS [SQLProcessUtilization], 
						            [timestamp] 
				            FROM ( 
						            SELECT [timestamp], CONVERT(XML, [record]) AS [record] 
						            FROM [sys].[dm_os_ring_buffers] 
						            WHERE	[ring_buffer_type] = N'RING_BUFFER_SCHEDULER_MONITOR' 
								            AND [record] LIKE '%<SystemHealth>%'
					            ) AS X					   
			            ) AS Y
	            ORDER BY record_id DESC

	            /*******************************************************************************************************************************
	            --	Verifica se o Consumo de CPU está maior do que o parametro
	            *******************************************************************************************************************************/
	            IF (
			            select CPU_Utilization from #CPU_Utilization
			            where record_id = (select max(record_id) from #CPU_Utilization)
		            ) > @CPU_Parametro
	            BEGIN	-- INICIO - ALERTA	
		            IF (
			            select CPU_Utilization from #CPU_Utilization
			            where record_id = (select min(record_id) from #CPU_Utilization)
		            ) > @CPU_Parametro
		            BEGIN
			            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
			            BEGIN
				            --------------------------------------------------------------------------------------------------------------------------------
				            --	ALERTA - DADOS - WHOISACTIVE
				            --------------------------------------------------------------------------------------------------------------------------------
				            -- Retorna todos os processos que estão sendo executados no momento
				            EXEC [dbo].[sp_WhoIsActive]
						            @get_outer_command =	1,
						            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
						            @destination_table =	'#Resultado_WhoisActive'
						    
				            -- Altera a coluna que possui o comando SQL
				            ALTER TABLE #Resultado_WhoisActive
				            ALTER COLUMN [sql_command] VARCHAR(MAX)
			
				            UPDATE #Resultado_WhoisActive
				            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
			
				            -- select * from #Resultado_WhoisActive
			
				            -- Verifica se não existe nenhum processo em Execução
				            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
				            BEGIN
					            INSERT INTO #Resultado_WhoisActive
					            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
				            END

			             -- Insert de dados para envio de Log
 			             INSERT INTO  ##VerificaCPU ([SQLProcessUtilization],[OtherProcessUtilization],[SystemIdle],[CPU_Utilization])
			                select	TOP 1
									            CAST([SQLProcessUtilization] AS VARCHAR) [SQLProcessUtilization],
									            CAST((100 - SystemIdle - SQLProcessUtilization) AS VARCHAR) as OtherProcessUtilization,
									            CAST([SystemIdle] AS VARCHAR) AS [SystemIdle],
									            CAST(100 - SystemIdle AS VARCHAR) AS CPU_Utilization
							            from #CPU_Utilization
							            order by record_id DESC
		
			             -- Insert de dados para envio de Processo
		                  INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                  SELECT	ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
								            ISNULL([database_name], '-')							AS [database_name],
								            ISNULL([login_name], '-')								AS [login_name],
								            ISNULL([host_name], '-')								AS [host_name],
								            ISNULL(CONVERT(VARCHAR(20), [start_time], 120), '-')	AS [start_time],
								            ISNULL([status], '-')									AS [status],
								            ISNULL(CAST([session_id] AS VARCHAR), '-')				AS [session_id],
								            ISNULL(CAST([blocking_session_id] AS VARCHAR), '-')		AS [blocking_session_id],
								            ISNULL([wait_info], '-')								AS [Wait],
								            ISNULL(CAST([open_tran_count] AS VARCHAR), '-')			AS [open_tran_count],
								            ISNULL([CPU], '-')										AS [CPU],
								            ISNULL([reads], '-')									AS [reads],
								            ISNULL([writes], '-')									AS [writes],
								            ISNULL(SUBSTRING([sql_command], 1, 300), '-')			AS [sql_command]
						            FROM #Resultado_WhoisActive
				

			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA
			            *******************************************************************************************************************************/
							
			            SET @Subject =  @Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME +', Utilização acima de ' +CAST((@CPU_Parametro) AS VARCHAR)+'%.'
			            SET @TextRel1 = @Ds_Email_Texto_alerta 	
			            SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaCPU]'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaProc]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			            -- Gera Terceiro bloco de HTML
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpWriteFile 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL E MENSSAGEIROS
			            *******************************************************************************************************************************/	
                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Email'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN

                             EXEC [msdb].[dbo].[sp_send_dbmail]
                                    @profile_name = @ProfileDBMail,
                                    @recipients = @EmailDestination,
                                    @body_format = @BodyFormatMail,
                                    @subject = @Subject,
                                    @importance = @Importance,
                                    @body = @HTML;

                        END
 
	                    -- Parametro Menssageiro
                        SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Telegram'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN
                            -- Envio do Telegram    
                            EXEC dbo.StpSendMsgTelegram 
                                  @Destino = @CanalTelegram,
                                  @Msg = @MntMsg
                        END

                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Teams'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN
                            -- MS TEAMS
                            SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                            EXEC [dbo].[stpSendMsgTeams]
	                            @msg = @MntMsg,
	                            @channel = @Ds_Menssageiro_02,
                                @ap = @Teams
                        END
		
			
				        /*******************************************************************************************************************************
				        -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
				        *******************************************************************************************************************************/
				        INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
				        SELECT @Id_AlertaParametro, @Subject, 1			
			            
                        END
		            END
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR		
		            IF @Fl_Tipo = 1
		            BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	ALERTA - DADOS - WHOISACTIVE
			            --------------------------------------------------------------------------------------------------------------------------------
			            -- Retorna todos os processos que estão sendo executados no momento
			            EXEC [dbo].[sp_WhoIsActive]
					            @get_outer_command =	1,
					            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
					            @destination_table =	'#Resultado_WhoisActive'
						    
			            -- Altera a coluna que possui o comando SQL
			            ALTER TABLE #Resultado_WhoisActive
			            ALTER COLUMN [sql_command] VARCHAR(MAX)
			
			            UPDATE #Resultado_WhoisActive
			            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
			
			            -- select * from #Resultado_WhoisActive
			
			            -- Verifica se não existe nenhum processo em Execução
			            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
			            BEGIN
				            INSERT INTO #Resultado_WhoisActive
				            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
			            END
		
			            -- Insert de dados para envio de Log
			             INSERT INTO  ##VerificaCPU ([SQLProcessUtilization],[OtherProcessUtilization],[SystemIdle],[CPU_Utilization])
			             select	TOP 1
									            CAST([SQLProcessUtilization] AS VARCHAR) [SQLProcessUtilization],
									            CAST((100 - SystemIdle - SQLProcessUtilization) AS VARCHAR) as OtherProcessUtilization,
									            CAST([SystemIdle] AS VARCHAR) AS [SystemIdle],
									            CAST(100 - SystemIdle AS VARCHAR) AS CPU_Utilization
							            from #CPU_Utilization
							            order by record_id DESC
		
			             -- Insert de dados para envio de Processo
		                  INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                  SELECT	ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
								            ISNULL([database_name], '-')							AS [database_name],
								            ISNULL([login_name], '-')								AS [login_name],
								            ISNULL([host_name], '-')								AS [host_name],
								            ISNULL(CONVERT(VARCHAR(20), [start_time], 120), '-')	AS [start_time],
								            ISNULL([status], '-')									AS [status],
								            ISNULL(CAST([session_id] AS VARCHAR), '-')				AS [session_id],
								            ISNULL(CAST([blocking_session_id] AS VARCHAR), '-')		AS [blocking_session_id],
								            ISNULL([wait_info], '-')								AS [Wait],
								            ISNULL(CAST([open_tran_count] AS VARCHAR), '-')			AS [open_tran_count],
								            ISNULL([CPU], '-')										AS [CPU],
								            ISNULL([reads], '-')									AS [reads],
								            ISNULL([writes], '-')									AS [writes],
								            ISNULL(SUBSTRING([sql_command], 1, 300), '-')			AS [sql_command]
						            FROM #Resultado_WhoisActive

			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/

			            SET @Subject =  @Ds_Email_Assunto_solucao + ' ' +  CAST((@CPU_Parametro) AS VARCHAR) + '% no Servidor: ' + @@SERVERNAME
			            SET @TextRel1 = @Ds_Email_Texto_solucao +' ' +  CAST((@CPU_Parametro) AS VARCHAR) + '% no Servidor: <b>' + @@SERVERNAME +',</b>.'	
			            SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaCPU]'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaProc]'
			            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) 
			            -- Gera Terceiro bloco de HTML
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpWriteFile 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
			            *******************************************************************************************************************************/	
                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Email'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN

                             EXEC [msdb].[dbo].[sp_send_dbmail]
                                    @profile_name = @ProfileDBMail,
                                    @recipients = @EmailDestination,
                                    @body_format = @BodyFormatMail,
                                    @subject = @Subject,
                                    @importance = @Importance,
                                    @body = @HTML;

                        END

	                    -- Parametro Menssageiro
                        SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Telegram'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN
                            -- Envio do Telegram    
                            EXEC dbo.StpSendMsgTelegram 
                                  @Destino = @CanalTelegram,
                                  @Msg = @MntMsg
                        END

                        IF EXISTS  (SELECT B.Ativo from AlertaParametro A 
			                        INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
			                        WHERE B.Ativo = 1
			                        AND B.Des LIKE '%Teams'
			                        AND [Id_AlertaParametro] = @Id_AlertaParametro
			                        )
                        BEGIN
                            -- MS TEAMS
                            SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                            EXEC [dbo].[stpSendMsgTeams]
	                            @msg = @MntMsg,
	                            @channel = @Ds_Menssageiro_02,
                                @ap = @Teams
                        END

			            /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0		
		            END
	            END		-- FIM - CLEAR";
        }
    }
}
