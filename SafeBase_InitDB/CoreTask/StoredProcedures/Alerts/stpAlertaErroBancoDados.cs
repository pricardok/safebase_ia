using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaErroBancoDados
    {
        public static string Query()
        {
            return
			//@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaErroBancoDados',GETDATE())";
			@"
                SET NOCOUNT ON;

				SET QUOTED_IDENTIFIER ON;

	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Fl_Tipo TINYINT, @importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaPaginaCorrompidaHeader VARCHAR(MAX), @AlertaPaginaCorrompidaTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @AlertaStatusDatabasesHeader VARCHAR(MAX), @AlertaStatusDatabasesTable VARCHAR(MAX), @StatusDB INT, 
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)			
	
	            -- Status Database
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Página Corrompida' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

				--------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @StatusDB = Vl_Parametro, 
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

	            /*******************************************************************************************************************************
	            --	ALERTA: PAGINA CORROMPIDA
	            *******************************************************************************************************************************/
				-- Check Utilizacao Arquivo Reads
				IF(OBJECT_ID('tempdb..#tempCorrupcaoPagina') IS NOT NULL)
					DROP TABLE #tempCorrupcaoPagina;
   
                SELECT SP.*
                INTO #tempCorrupcaoPagina
                FROM [msdb].[dbo].[suspect_pages] SP
	                LEFT JOIN [dbo].[HistoricoSuspectPages] HSP ON SP.database_id = HSP.database_id
											             AND SP.file_id = HSP.file_id
											             AND SP.[page_id] = HSP.[page_id]
											             AND CAST(SP.last_update_date AS DATE) = CAST(HSP.Dt_Corrupcao AS DATE)
                WHERE HSP.[page_id] IS NULL;


                -- Cria a tabela que ira armazenar os dados dos processos
                IF ( OBJECT_ID('tempdb..##PaginaC') IS NOT NULL )
		            DROP TABLE ##PaginaC
		
                CREATE TABLE ##PaginaC (		
		            [Nome Database]		VARCHAR(70),
		            [file_id]				VARCHAR(70),		
		            [page_id]				VARCHAR(70),
		            [event_type]			VARCHAR(70),
		            [error_count]			VARCHAR(70),
		            [last_update_date]		VARCHAR(70))


	            /*******************************************************************************************************************************
	            -- Verifica se existe alguma Página Corrompida
	            *******************************************************************************************************************************/
	            IF EXISTS (SELECT TOP 1 page_id FROM #tempCorrupcaoPagina) 
	            BEGIN	-- INICIO - ALERTA	
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/			

		            INSERT INTO ##PaginaC
		            SELECT * FROM #tempCorrupcaoPagina

		            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME
		            SET @TextRel1 = @Ds_Email_Texto_alerta 	
		            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
		 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT B.name AS [Nome Database], CAST(file_id AS VARCHAR) AS file_id, CAST(page_id AS VARCHAR) AS page_id, CAST(event_type AS VARCHAR) AS event_type, CAST(error_count AS VARCHAR) AS error_count,CONVERT(VARCHAR(20), last_update_date, 120) AS last_update_date FROM ##PaginaC A JOIN [sys].[databases] B ON B.[database_id] = A.[database_id]'
		            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
		            -- Gera Segundo bloco de HTML
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
		            -- Insere um Registro na Tabela de Controle dos Alertas
		            *******************************************************************************************************************************/
		            INSERT INTO [dbo].[HistoricoSuspectPages]
		            SELECT	[database_id] ,
				            [file_id] ,
				            [page_id] ,
				            [event_type] ,
				            [last_update_date]
		            FROM #tempCorrupcaoPagina
		
		            /*******************************************************************************************************************************
		            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
		            *******************************************************************************************************************************/
		            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
		            SELECT @Id_AlertaParametro, @Subject, 1	
	            END		-- FIM - ALERTA
			

	            /*******************************************************************************************************************************
	            --	ALERTA: DATABASE INDISPONIVEL
	            *******************************************************************************************************************************/	
	            -- Status Database
	            SELECT @Id_AlertaParametro = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Status Database' AND Ativo = 1)
                -- DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @StatusDB = Vl_Parametro, 
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

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]		
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            /*******************************************************************************************************************************
	            -- Verifica se alguma Database não está ONLINE
	            *******************************************************************************************************************************/ 
	            IF EXISTS	(
					            SELECT NULL
					            FROM [sys].[databases]
					            WHERE [state_desc] NOT IN ('ONLINE','RESTORING')
				            )
	            BEGIN	-- INICIO - ALERTA		
		            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
		            BEGIN			
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA
			            *******************************************************************************************************************************/
			            SET @Subject = @Ds_Email_Assunto_alerta + ' ' + @@SERVERNAME
			            SET @TextRel1 =  @Ds_Email_Texto_alerta 	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT [name] AS [DataBase], [state_desc] AS [Status]  FROM [sys].[databases] WHERE [state_desc] NOT IN(''ONLINE'', ''RESTORING'')'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
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
		            IF ISNULL(@Fl_Tipo, 0) = 1
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/			
			
			            SET @Subject = @Ds_Email_Assunto_solucao + ' ' + @@SERVERNAME
			            SET @TextRel1 =  @Ds_Email_Texto_solucao + ' <b>' + @@SERVERNAME + '</b>.'	
			            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT [name] AS [DataBase], [state_desc] AS [Status]  FROM [sys].[databases] WHERE [state_desc] NOT IN(''ONLINE'', ''RESTORING'')'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
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
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0
		            END
	            END		-- FIM - CLEAR";
        }
    }
}
