using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaConexaoSQLServer()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Conexões SQL Server
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Conexão SQL Server')

	            -- Declara as variaveis
	            DECLARE @EmailBody VARCHAR(MAX), @AlertaConexaoSQLServerHeader VARCHAR(MAX), @AlertaConexaoSQLServerTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), 
		               @Importance AS VARCHAR(6), @Subject VARCHAR(500), @Qtd_Conexoes INT, @Conexoes_SQLServer_Parametro INT, @Fl_Tipo INT, @EmailDestination VARCHAR(200),	
		               @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), 
		               @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),
		               @HTML VARCHAR(MAX), @Query VARCHAR(MAX)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Conexoes_SQLServer_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            SELECT @Qtd_Conexoes = count(*) FROM sys.dm_exec_sessions WHERE session_id > 50

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            /*******************************************************************************************************************************
	            --	Verifica se o limite de conexões para o Alerta foi atingido
	            *******************************************************************************************************************************/
	            IF (@Qtd_Conexoes > @Conexoes_SQLServer_Parametro)
	            BEGIN	-- INICIO - ALERTA		
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
		
		            if object_id('tempdb..##ConexoesAbertas') is not null
			            drop table ##ConexoesAbertas

		            SELECT	TOP 25 IDENTITY(INT, 1, 1) AS id, 
				            replace(replace(ec.client_net_address,'<',''),'>','') client_net_address, 
				            case when es.[program_name] = '' then 'Sem nome na string de conexão' else [program_name] end [program_name], 
				            es.[host_name], es.login_name, '' Base,-- db_name(database_id) Base,
				            COUNT(ec.session_id)  AS [connection count] 
		            into ##ConexoesAbertas
		            FROM sys.dm_exec_sessions AS es  
		            INNER JOIN sys.dm_exec_connections AS ec ON es.session_id = ec.session_id   
		            GROUP BY ec.client_net_address, es.[program_name], es.[host_name], es.login_name,
		            db_name(database_id)	-- Somente a partir do SQL Server 2008 R2
		            order by [connection count] desc

		            SET @Subject =	'ALERTA #ConexoesAbertas - Existem ' + cast(@Qtd_Conexoes as varchar) + ' conexões Abertas no Servidor: ' + @@SERVERNAME
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Segue as TOP 25 conexões Abertas no Servidor: <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo com <b>urgência</b>.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT client_net_address AS [IP], [program_name] AS [Aplicacao], [host_name] AS [Hostname], login_name AS [Login], Base AS [Database],cast([connection count] as varchar) [Qtd. Conexões],id AS [ID] FROM ##ConexoesAbertas'
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
						                @importance = @Importance,
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
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR		
		            IF @Fl_Tipo = 1
		            BEGIN
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/
		
		                if object_id('tempdb..##ConexoesAbertasClear') is not null
				            drop table ##ConexoesAbertasClear

		                SELECT	top 25 IDENTITY(INT, 1, 1) AS id, 
					            replace(replace(ec.client_net_address,'<',''),'>','') client_net_address, 
					            case when es.[program_name] = '' then 'Sem nome na string de conexão' else [program_name] end [program_name], 
					            es.[host_name], es.login_name, '' Base,-- db_name(database_id) Base,
					            COUNT(ec.session_id)  AS [connection count] 
		                into ##ConexoesAbertasClear
		                FROM sys.dm_exec_sessions AS es  
		                INNER JOIN sys.dm_exec_connections AS ec  
		                ON es.session_id = ec.session_id   
		                GROUP BY ec.client_net_address, es.[program_name], es.[host_name], es.login_name, 
		                db_name(database_id)
   		                order by [connection count] desc

		                SET @Subject =	'Solução #ConexoesAbertas - Existem ' + cast(@Qtd_Conexoes as varchar) + ' conexões Abertas no Servidor: ' + @@SERVERNAME
		                SET @TextRel1 = 'Prezados,<BR /><BR /> Conexões Abertas no Servidor: <b>' + @@SERVERNAME +',</b>.'	
		                SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		                -- Gera Primeiro bloco de HTML
		                SET @Query = 'SELECT client_net_address AS [IP], [program_name] AS [Aplicacao], [host_name] AS [Hostname], login_name AS [Login], Base AS [Database],cast([connection count] as varchar) [Qtd. Conexões],id AS [ID] FROM ##ConexoesAbertasClear'
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
							               @importance = @Importance,
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