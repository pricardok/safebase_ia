using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaErroBancoDados()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Fl_Tipo TINYINT, @importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaPaginaCorrompidaHeader VARCHAR(MAX), @AlertaPaginaCorrompidaTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @AlertaStatusDatabasesHeader VARCHAR(MAX), @AlertaStatusDatabasesTable VARCHAR(MAX), @StatusDB INT, 
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)			
	
	            -- Status Database
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Página Corrompida')

				--------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @StatusDB = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

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

		            SET @Subject =	'ALERTA #PaginaCorrompida - Existe uma página corrompida no Servidor ' + @@SERVERNAME
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema de página corrompida no Servidor <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo com <b>urgência</b>.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
		 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT B.name AS [Nome Database], CAST(file_id AS VARCHAR) AS file_id, CAST(page_id AS VARCHAR) AS page_id, CAST(event_type AS VARCHAR) AS event_type, CAST(error_count AS VARCHAR) AS error_count,CONVERT(VARCHAR(20), last_update_date, 120) AS last_update_date FROM ##PaginaC A JOIN [sys].[databases] B ON B.[database_id] = A.[database_id]'
		            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
		            -- Gera Segundo bloco de HTML
		            SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
		            select @HTML = @HTML + @Ass
		            -- Salva Arquivo HTML de Envio
		            EXEC dbo.stpEscreveArquivo 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

		            /*******************************************************************************************************************************
		            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
		            *******************************************************************************************************************************/	
		            EXEC [msdb].[dbo].[sp_send_dbmail]
						                @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
		            SET @MntMsg = @Subject+', Verifique os detalhes no e-mail com *urgência*'
		            EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
			                @Msg = @MntMsg 	

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
	            SELECT @Id_AlertaParametro = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Status Database')
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @StatusDB = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
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
			            SET @Subject = (SELECT 'ALERTA #DatabaseOffLine - Database OFFLINE no Servidor: ' + @@SERVERNAME)
			            SET @TextRel1 =  'Prezados,<BR /><BR /> Identifiquei que nem todas as Databases estao ONLINE na Instância <b>'+(SELECT @@SERVERNAME)+',</b> verifique o relatório abaixo com urgência.'	
			            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT [name] AS [DataBase], [state_desc] AS [Status]  FROM [sys].[databases] WHERE [state_desc] NOT IN(''ONLINE'', ''RESTORING'')'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpEscreveArquivo 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/	
			            EXEC [msdb].[dbo].[sp_send_dbmail]
						                @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
			            SET @MntMsg = @Subject+', Verifique os detalhes no e-mail com *urgência*'
			            EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
			                @Msg = @MntMsg 	
				
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
			
			            SET @Subject = (SELECT 'Solução #DatabaseOnLine - Todas as Databases estão ONLINE no Servidor: ' + @@SERVERNAME)
			            SET @TextRel1 =  'Prezados,<BR /><BR /> Correção realizada com sucesso todas as Databases estao ONLINE na Instância <b>'+(SELECT @@SERVERNAME)+',</b>.'	
			            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			            -- Gera Primeiro bloco de HTML
			            SET @Query = 'SELECT [name] AS [DataBase], [state_desc] AS [Status]  FROM [sys].[databases] WHERE [state_desc] NOT IN(''ONLINE'', ''RESTORING'')'
			            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			            -- Gera Segundo bloco de HTML
			            SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
			            select @HTML = @HTML + @Ass
			            -- Salva Arquivo HTML de Envio
			            EXEC dbo.stpEscreveArquivo 
				            @Ds_Texto = @HTML, -- nvarchar(max)
				            @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
				            @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
				            @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
				            @Fl_Append = 0 -- bit

			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/	
			            EXEC [msdb].[dbo].[sp_send_dbmail]
						                @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                @importance = @importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
			            SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
			            EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
			                @Msg = @MntMsg 	
						
			            /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0
		            END
	            END		-- FIM - CLEAR";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};