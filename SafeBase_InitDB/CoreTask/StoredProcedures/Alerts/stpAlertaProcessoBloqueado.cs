using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaProcessoBloqueado
    {
        public static string Query()
        {
            return
			 //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaProcessoBloqueado',GETDATE())";
			 @"	SET NOCOUNT ON;
				
				SET QUOTED_IDENTIFIER ON;

	            -- Processo Bloqueado
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Processo Bloqueado' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
	
	            -- Declara as variaveis
	            DECLARE	@Subject VARCHAR(500), @Fl_Tipo TINYINT, @Qtd_Segundos INT, @Consulta VARCHAR(8000), @Importance AS VARCHAR(6), @Dt_Atual DATETIME,
			            @EmailBody VARCHAR(MAX), @AlertaLockHeader VARCHAR(MAX), @AlertaLockTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaLockRaizHeader VARCHAR(MAX), @AlertaLockRaizTable VARCHAR(MAX), @Processo_Bloqueado_Parametro INT, @Qt_Tempo_Raiz_Lock INT,
			            @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),
			            @MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), 
			            @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            SELECT @NomeRel = Nm_Alerta, 
		               @Processo_Bloqueado_Parametro = Vl_Parametro, 
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


	            -- Quantidade em Minutos -- Query que esta gerando o lock (rodando a mais de 1 minuto)
	            SELECT @Qt_Tempo_Raiz_Lock	= 1	

	            --------------------------------------------------------------------------------------------------------------------------------
	            --	Cria Tabela temporaria para armazenar os Dados da SP_WHOISACTIVE
	            --------------------------------------------------------------------------------------------------------------------------------
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
      
	            -- Seta a hora atual
	            SELECT @Dt_Atual = GETDATE()
				
	            --------------------------------------------------------------------------------------------------------------------------------
	            --	Carrega os Dados da SP_WHOISACTIVE
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Retorna todos os processos que estão sendo executados no momento
	            EXEC [dbo].[sp_WhoIsActive]
			            @get_outer_command =	1,
			            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
			            @destination_table =	'#Resultado_WhoisActive'
				-- select * from #Resultado_WhoisActive
	
				ALTER TABLE #Resultado_WhoisActive
					ADD Nr_Nivel_Lock TINYINT 

	            -- Altera a coluna que possui o comando SQL
	            ALTER TABLE #Resultado_WhoisActive
	            ALTER COLUMN [sql_command] VARCHAR(MAX)
	
	            UPDATE #Resultado_WhoisActive
	            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')


				INSERT INTO #Resultado_WhoisActive ([dd hh:mm:ss.mss],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[wait_info],[open_tran_count],[CPU],[reads],[writes],[sql_command],[Nr_Nivel_Lock] )
											 SELECT [dd hh:mm:ss.mss],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[wait_info],[open_tran_count],[CPU],[reads],[writes],[sql_command],[Nr_Nivel_Lock] FROM #Resultado_WhoisActive
	
	            -- Verifica se não existe nenhum processo em Execução
	            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive)
	            BEGIN
		            INSERT INTO #Resultado_WhoisActive ([dd hh:mm:ss.mss],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[wait_info],[open_tran_count],[CPU],[reads],[writes],[sql_command],[Nr_Nivel_Lock] )
		            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL--, NULL
	            END

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )

	            /*******************************************************************************************************************************
	            --	Verifica se existe algum Processo Bloqueado 
	            *******************************************************************************************************************************/	
	            IF EXISTS	(
					            SELECT NULL 
					            FROM #Resultado_WhoisActive A
					            JOIN #Resultado_WhoisActive B ON A.[blocking_session_id] = B.[session_id]
					            WHERE	DATEDIFF(SECOND,A.[start_time], @Dt_Atual) > @Processo_Bloqueado_Parametro * 60		-- A query que está sendo bloqueada está rodando a mais 2 minutos
							            AND DATEDIFF(SECOND,B.[start_time], @Dt_Atual) > @Qt_Tempo_Raiz_Lock * 60			-- A query que está bloqueando está rodando a mais de 1 minuto
				            )
	            BEGIN	-- INICIO - ALERTA
		            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
		            BEGIN

			            --------------------------------------------------------------------------------------------------------------------------------
			            --	Verifica a quantidade de processos bloqueados
			            --------------------------------------------------------------------------------------------------------------------------------
			            -- Declara a variavel e retorna a quantidade de Queries Lentas
			            DECLARE @QtdProcessosBloqueados INT = (
										            SELECT COUNT(*)
										            FROM #Resultado_WhoisActive A
										            JOIN #Resultado_WhoisActive B ON A.[blocking_session_id] = B.[session_id]
										            WHERE	DATEDIFF(SECOND,A.[start_time], @Dt_Atual) > @Processo_Bloqueado_Parametro	* 60
												            AND DATEDIFF(SECOND,B.[start_time], @Dt_Atual) > @Qt_Tempo_Raiz_Lock * 60
									            )

			            DECLARE @QtdProcessosBloqueadosLocks INT = (
										            SELECT COUNT(*)
										            FROM #Resultado_WhoisActive A
										            WHERE [blocking_session_id] IS NOT NULL
									            )

			            --------------------------------------------------------------------------------------------------------------------------------
			            --	Verifica o Nivel dos Locks
			            --------------------------------------------------------------------------------------------------------------------------------
			            --ALTER TABLE #Resultado_WhoisActive
			            -- ADD Nr_Nivel_Lock TINYINT 
						-- select * from #Resultado_WhoisActive
			            -- Nivel 0
			            UPDATE A
			            SET Nr_Nivel_Lock = 0
			            FROM #Resultado_WhoisActive A
			            WHERE blocking_session_id IS NULL AND session_id IN ( SELECT DISTINCT blocking_session_id 
						            FROM #Resultado_WhoisActive WHERE blocking_session_id IS NOT NULL)

			            UPDATE A
			            SET Nr_Nivel_Lock = 1
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 0)

			            UPDATE A
			            SET Nr_Nivel_Lock = 2
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 1)

			            UPDATE A
			            SET Nr_Nivel_Lock = 3
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 2)
					
			            -- Tratamento quando não tem um Lock Raiz
			            IF NOT EXISTS(select * from #Resultado_WhoisActive where Nr_Nivel_Lock IS NOT NULL)
			            BEGIN
				            UPDATE A
				            SET Nr_Nivel_Lock = 0
				            FROM #Resultado_WhoisActive A
				            WHERE session_id IN ( SELECT DISTINCT blocking_session_id 
					            FROM #Resultado_WhoisActive WHERE blocking_session_id IS NOT NULL)
          
				            UPDATE A
				            SET Nr_Nivel_Lock = 1
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 0)

				            UPDATE A
				            SET Nr_Nivel_Lock = 2
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 1)

				            UPDATE A
				            SET Nr_Nivel_Lock = 3
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 2)
			            END

			            -- Insert de dados para envio de Processo
		                 INSERT INTO [dbo].[ResultadoProcBlock] ([Nr_Nivel_Lock],[Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 SELECT	TOP 50
								            CAST(Nr_Nivel_Lock AS VARCHAR)							AS [Nr_Nivel_Lock],
								            ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
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
						            WHERE Nr_Nivel_Lock IS NOT NULL
						            ORDER BY [Nr_Nivel_Lock], [start_time] 

			            -- Insert de dados para envio de Processo
		                 INSERT INTO [dbo].[ResultadoProc] ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 SELECT	TOP 50
								            ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
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
						            ORDER BY [start_time]

			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA
			            *******************************************************************************************************************************/							
			             -- Parametros do Alerta
			             SET @Subject = @Ds_Email_Assunto_alerta +' ' + @@SERVERNAME + ' - Total: ' + CAST(@QtdProcessosBloqueados AS VARCHAR) + ' Processo(s) Bloqueado(s) a mais de ' +  CAST((@Processo_Bloqueado_Parametro) AS VARCHAR) + ' minuto(s)' +' e um total de ' + CAST(@QtdProcessosBloqueadosLocks AS VARCHAR) +  ' Lock(s)'
			             SET @TextRel1 = @Ds_Email_Texto_alerta
			             SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			             SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [dbo].[ResultadoProcBlock]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             -- Gera Segundo bloco de HTML
			             SET @Query = 'SELECT * FROM [dbo].[ResultadoProc]'
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
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
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
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/
			            -- Insert de dados para envio de Processo
		                 INSERT INTO [dbo].[ResultadoProc] ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
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
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/
			 
						 SET @Subject = @Ds_Email_Assunto_solucao + ' ' + @@SERVERNAME + ' - Parametro : ' + CAST((@Processo_Bloqueado_Parametro) AS VARCHAR) + ' minuto(s)'
			             SET @TextRel1 = @Ds_Email_Texto_solucao
			             SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath+'_'+ @NomeRel +'.html'
			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [dbo].[ResultadoProc]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
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
						
						TRUNCATE TABLE [dbo].[ResultadoProc]
						TRUNCATE TABLE [dbo].[ResultadoProcBlock]

		            END		
	            END		-- FIM - CLEAR
	            ";
        }
    }
}
