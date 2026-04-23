using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaAlwaysOn()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Tipo TINYINT, @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @AlertaStatusDatabasesHeader VARCHAR(MAX), @AlertaStatusDatabasesTable VARCHAR(MAX),			
	            @SendMail VARCHAR(50), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20),@TLMsg VARCHAR(50), @MntMsg VARCHAR(150), @TextRel VARCHAR(250), @NomeRel VARCHAR(150), @EmailDestination VARCHAR(200), @CaminhoPath VARCHAR(50),
	            @Tempo_Conexoes_Hs tinyint, @Tempdb_Parametro int, @Tamanho_Tempdb INT, @Fl_Tipo TINYINT, @AlertaTamanhoMDFTempdbHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbTable VARCHAR(MAX), @AlertaTempdbUtilizacaoArquivoHeader VARCHAR(MAX), 
	            @AlertaTamanhoMDFTempdbConexoesTable VARCHAR(MAX),@TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)


	            -- AlwaysOn
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'AlwaysOn')
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro
		
	            /*
	            Analisa último Tipo do Alerta registrado
	            0: SEM PROBLEMA
	            1: ALERTA
	            */				

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            TRUNCATE TABLE [dbo].[AlertaAlwaysOn]
	            INSERT [dbo].[AlertaAlwaysOn] EXEC [dbo].[stpAlwaysOnStats] 'F', 'NULL'
	            --update [dbo].[AlertaAlwaysOn] set [status] = 'NOT' WHERE [database] = 'CentralAplicacoes'
	            --update [dbo].[AlertaAlwaysOn] set [status] = 'SYNCHRONIZED' WHERE [database] = 'CentralAplicacoes'
	            -- SELECT * FROM [dbo].[Alerta]
	            -- SELECT * FROM [dbo].[AlertaAlwaysOn]
	            -- delete from [dbo].[Alerta] where Id_Alerta >= 1053
	            -- Verifica Database que não está ONLINE
	
	            --IF EXISTS	(SELECT [Status] FROM [dbo].[AlertaAlwaysOn] WHERE [Status] <> 'SYNCHRONIZED')
	            IF (SELECT COUNT([Status])FROM [dbo].[AlertaAlwaysOn] WHERE [Status] NOT LIKE 'SYNCHRONIZED') >= 1
		            BEGIN 	
			            IF ISNULL(@Fl_Tipo, 0) = 0	-- Realiza o envia o Alerta apenas uma vez
				            BEGIN	
					            --Envio de e-mail	
					            SET @Subject = 'ALERTA #AlwaysOn - Problema no '  +  @@SERVERNAME
					            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema no AlwaysOn no servidor: <b>' + @@SERVERNAME +',</b> verifique essa informação com <b>urgência</b>.'  
					            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
					            -- Gera Primeiro bloco de HTML
					            SET @Query = 'SELECT [Database],[Status],[Sync] FROM [dbo].[AlertaAlwaysOn] WHERE [Status] NOT LIKE ''SYNCHRONIZED'''
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

					            -- Registra informação na Tabela [AlertaStatusDatabases], Tipo = 1 -- ALERTA
					            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
						            SELECT @Id_AlertaParametro, @Subject, 1			

				            END
		            END		
	            ELSE 
	                BEGIN				
		                IF ISNULL(@Fl_Tipo, 0) = 1
				            BEGIN
					            -- Carga 
					            --TRUNCATE TABLE [dbo].[AlertaAlwaysOn]
					            --INSERT [dbo].[AlertaAlwaysOn] EXEC [dbo].[MonitAlwaysOnStats]
					            --Envio de e-mail	
					            SET @Subject =	(SELECT 'Solução #AlwaysOn - Não existem mais inconsistência no Servidor: ' + @@SERVERNAME)
					            SET @TextRel1 = 'Prezados,<BR /><BR /> Sem registro de problemas no AlwaysOn na Instância <b>'+(SELECT @@SERVERNAME)+',</b>.'	
					            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
					            -- Gera Primeiro bloco de HTML
					            SET @Query = 'SELECT * FROM [dbo].[AlertaAlwaysOn]'
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
						            @Msg = @MntMsg 
				 			
					             -- Registra informação na Tabela [AlertaStatusDatabases] -> Tipo = 0 : CLEAR
					            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
					            SELECT @Id_AlertaParametro, @Subject, 0	

			            END
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};