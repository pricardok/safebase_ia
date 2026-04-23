using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaArquivoLogFull
    {
        public static string Query()
        {
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaArquivoLogFull',GETDATE())";
			@"
              	SET NOCOUNT ON;

			SET QUOTED_IDENTIFIER ON;

	            -- Arquivo de Log Full
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Arquivo de Log Full' and Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
	
	            -- Declara as variaveis
	            DECLARE @Tamanho_Minimo_Alerta_log INT, @AlertaLogHeader VARCHAR(MAX), @AlertaLogTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Log_Full_Parametro TINYINT,
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200), 
			            @BuscaParametro VARCHAR(80), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), 
			            @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Log_Full_Parametro = Vl_Parametro, 
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

	            -- Tamanho Minimo do Alerta 100 MB = 100000
	            SELECT @Tamanho_Minimo_Alerta_log = 100000	
	
	            -- Verifica o último Tipo do Alerta registrado
	            -- 0: CLEAR 
	            -- 1: ALERTA	
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            -- Cria a tabela que ira armazenar os dados dos processos
	            IF ( OBJECT_ID('tempdb..#Resultado_WhoisActive') IS NOT NULL )
		            DROP TABLE #Resultado_WhoisActive
				
	            CREATE TABLE #Resultado_WhoisActive (		
		            [dd hh:mm:ss.mss]		VARCHAR(20),
		            [database_name]		NVARCHAR(128),		
		            [login_name]			NVARCHAR(128),
		            [host_name]			NVARCHAR(128),
		            [start_time]			DATETIME,
		            [status]				VARCHAR(30),
		            [session_id]			INT,
		            [blocking_session_id]	INT,
		            [wait_info]			VARCHAR(MAX),
		            [open_tran_count]		INT,
		            [CPU]				VARCHAR(MAX),
		            [reads]				VARCHAR(MAX),
		            [writes]				VARCHAR(MAX),
		            [sql_command]			XML
	            )

	            -- Cria a tabela log que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaLog') IS NOT NULL )
		            DROP TABLE ##VerificaLog
				
	            CREATE TABLE ##VerificaLog (		
		            [Database]				    VARCHAR(100),
		            [Tamanho Log (MB)]			    VARCHAR(50),		
		            [Percentual Log Utilizado (%)]    VARCHAR(50)
	            )

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProcFull') IS NOT NULL )
		            DROP TABLE ##VerificaProcFull
	
                CREATE TABLE ##VerificaProcFull
	               ([Duração]             VARCHAR(50),
	                [database_name]       VARCHAR(200),
	                [login_name]          VARCHAR(50),
	                [host_name]           VARCHAR(50),
	                [start_time]          VARCHAR(50),
	                [status]              VARCHAR(50),
	                [session_id]          VARCHAR(50),
	                [blocking_session_id] VARCHAR(50),
	                [Wait]                VARCHAR(50),
	                [open_tran_count]     VARCHAR(50),
	                [CPU]                 VARCHAR(50),
	                [reads]               VARCHAR(50),
	                [writes]              VARCHAR(50),
	                [sql_command]         VARCHAR(MAX),
	               );

	            /*******************************************************************************************************************************
	            -- Verifica se existe algum LOG com muita utilização
	            *******************************************************************************************************************************/
	            IF EXISTS(
				            SELECT	db.[name]							AS [Database Name],
						            db.[recovery_model_desc]			AS [Recovery Model],
						            db.[log_reuse_wait_desc]			AS [Log Reuse Wait DESCription],
						            ls.[cntr_value]						AS [Log Size (KB)],
						            lu.[cntr_value]						AS [Log Used (KB)],
						            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
								            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
										            THEN 1 
										            ELSE CAST(ls.[cntr_value] AS FLOAT) 
								            END AS DECIMAL(18,2)) * 100 AS [Percente_Log_Used] ,
						            db.[compatibility_level]			AS [DB Compatibility Level] ,
						            db.[page_verify_option_desc]		AS [Page Verify Option]
				            FROM [sys].[databases] AS db
				            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
				            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
				            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
						            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%' 
						            AND ls.[cntr_value] > @Tamanho_Minimo_Alerta_log		-- Maior que 100 MB
						            AND (
								            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
										            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
												            THEN 1 
												            ELSE CAST(ls.[cntr_value] AS FLOAT) 
										            END AS DECIMAL(18,2)) * 100
							            ) > @Log_Full_Parametro
			             )
	            BEGIN	-- INICIO - ALERTA
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
			
			            -- Verifica se não existe nenhum processo em Execução
			            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
			            BEGIN
				            INSERT INTO #Resultado_WhoisActive
				            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
			            END	
			
			            -- Insert de dados para envio de Log
			            INSERT INTO ##VerificaLog ( [Database], [Tamanho Log (MB)], [Percentual Log Utilizado (%)])
			            SELECT	db.[name]	AS [DatabaseName] ,
					            CAST(ls.[cntr_value] / 1024.00 AS DECIMAL(18,2))	AS [cntr_value],
					            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
					            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
					                THEN 1 
					                ELSE CAST(ls.[cntr_value] AS FLOAT) 
					            END AS DECIMAL(18,2)) * 100					AS [Percente_Log_Used]
			            FROM [sys].[databases] AS db
			            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
			            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
			            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
			            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%' 
			            AND ls.[cntr_value] > @Tamanho_Minimo_Alerta_log -- Maior que 100 MB
			            AND (
				            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
							            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
													            THEN 1 
														            ELSE CAST(ls.[cntr_value] AS FLOAT) 
												            END AS DECIMAL(18,2)) * 100
				            ) > @Log_Full_Parametro

				        -- Insert de dados para envio de Processo
                        BEGIN
						
			                INSERT INTO ##VerificaProcFull ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                    SELECT 
								ISNULL([dd hh:mm:ss.mss], '-')		AS [Duração],
								ISNULL([database_name], '-')		AS [database_name],
								ISNULL([login_name], '-')			AS [login_name],
								ISNULL([host_name], '-')			AS [host_name],
								ISNULL([start_time], '-')			AS [Duração],
								ISNULL([status], '-')				AS [Duração],
								ISNULL([session_id], '-')			AS [Duração],
								ISNULL([blocking_session_id], '-')	AS [Duração],
								ISNULL([wait_info], '-')			AS [Duração],
								ISNULL([open_tran_count], '-')		AS [Duração],
								ISNULL([CPU], '-')					AS [Duração],
								ISNULL([reads], '-')				AS [Duração],
								ISNULL([writes], '-')				AS [Duração],
								cast([sql_command]					as varchar(max))  
							FROM #Resultado_WhoisActive

		                IF (@@ROWCOUNT = 0)
                          BEGIN
                            INSERT INTO ##VerificaProcFull ([Duração],[database_name])
                            SELECT GETDATE(),'Não encontrei um processo que possa estar causando o crescimento de log, <b>necessário uma analise mais detalhada</b>'
                          END
                        END
				
			            /*******************************************************************************************************************************
			            --	ALERTA - CRIA O EMAIL
			            *******************************************************************************************************************************/
			            -- Parametros do Alerta
			            SET @Subject =  @Ds_Email_Assunto_alerta  + CAST((@Log_Full_Parametro) AS VARCHAR)+ '% de utilização no Servidor: ' + @@SERVERNAME
			            SET @TextRel1 =  @Ds_Email_Texto_alerta 	
			            SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaLog]'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Query = 'SELECT * FROM [##VerificaProcFull]'
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
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR
		            IF @Fl_Tipo = 1
		            BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	CLEAR - DADOS - WHOISACTIVE
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
			
			            -- Verifica se não existe nenhum processo em Execução
			            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
			            BEGIN
				            INSERT INTO #Resultado_WhoisActive
				            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
			            END
		
			
			            -- Insert de dados para envio de Log
			            INSERT INTO ##VerificaLog ( [Database], [Tamanho Log (MB)], [Percentual Log Utilizado (%)])
			            SELECT	db.[name]											AS [DatabaseName] ,
								            CAST(ls.[cntr_value] / 1024.00 AS DECIMAL(18,2))	AS [cntr_value],
								            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
										            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
												            THEN 1 
												            ELSE CAST(ls.[cntr_value] AS FLOAT) 
										            END AS DECIMAL(18,2)) * 100					AS [Percente_Log_Used] 
						            FROM [sys].[databases] AS db
						            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
						            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
						            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
								            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%'
								            AND ls.[cntr_value] > 1000 -- Maior que 100 MB

			            -- Insert de dados para envio de Processo
                        BEGIN

			                INSERT INTO ##VerificaProcFull ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                    SELECT 
								ISNULL([dd hh:mm:ss.mss], '-')		AS [Duração],
								ISNULL([database_name], '-')		AS [database_name],
								ISNULL([login_name], '-')			AS [login_name],
								ISNULL([host_name], '-')			AS [host_name],
								ISNULL([start_time], '-')			AS [Duração],
								ISNULL([status], '-')				AS [Duração],
								ISNULL([session_id], '-')			AS [Duração],
								ISNULL([blocking_session_id], '-')	AS [Duração],
								ISNULL([wait_info], '-')			AS [Duração],
								ISNULL([open_tran_count], '-')		AS [Duração],
								ISNULL([CPU], '-')					AS [Duração],
								ISNULL([reads], '-')				AS [Duração],
								ISNULL([writes], '-')				AS [Duração],
								cast([sql_command]					as varchar(max))  
							FROM #Resultado_WhoisActive

		                IF (@@ROWCOUNT = 0)
                          BEGIN
                            INSERT INTO ##VerificaProcFull ([Duração],[database_name])
                            SELECT GETDATE(),'Não encontrei um processo que possa ter causando o crescimento de log, <b>necessário uma analise mais detalhada</b>'
                          END
                        END

			            /*******************************************************************************************************************************
			            --  ALERTA - ENVIA O EMAIL E MENSSAGEIROS
			            *******************************************************************************************************************************/
			             SET @Subject =  @Ds_Email_Assunto_solucao +' ' +CAST((@Log_Full_Parametro) AS VARCHAR)+ '% de utilização no Servidor: ' + @@SERVERNAME
			             SET @TextRel1 =  @Ds_Email_Texto_solucao	
			             SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			             SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'

			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaLog]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             -- Gera Segundo bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProcFull]'
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
			            --	ALERTA - ENVIA O EMAIL
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
	            END		-- FIM - CLEAR                

             ";
        }
    }
}
