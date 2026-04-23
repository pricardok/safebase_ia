using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaStatusDatabases()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                
                SELECT @@VERSION                
                /*
                SET NOCOUNT ON

	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Tipo TINYINT, @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @AlertaStatusDatabasesHeader VARCHAR(MAX), @AlertaStatusDatabasesTable VARCHAR(MAX),			
	            @SendMail VARCHAR(50), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20),@TLMsg VARCHAR(50), @MntMsg VARCHAR(150), @TextRel VARCHAR(250), @NomeRel VARCHAR(150), @EmailDestination VARCHAR(200), @CaminhoPath VARCHAR(50),
	            @Tempo_Conexoes_Hs tinyint, @Tempdb_Parametro int, @Tamanho_Tempdb INT, @Fl_Tipo TINYINT, @AlertaTamanhoMDFTempdbHeader VARCHAR(MAX), @AlertaTamanhoMDFTempdbTable VARCHAR(MAX), @AlertaTempdbUtilizacaoArquivoHeader VARCHAR(MAX), 
	            @AlertaTamanhoMDFTempdbConexoesTable VARCHAR(MAX),@TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)


	            -- Banco de Dados Corrompido
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [DBController].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Status das Databases')
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = 'StatusDB', 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro
	            --SET @SendMail = 'ti@gvenergy.com.br'
		
	            /*
	            Analisa último Tipo do Alerta registrado
	            0: SEM PROBLEMA
	            1: ALERTA
	            */				

	            SELECT @Tipo = [Tipo]
	            FROM [dbo].[AlertaStatusDatabases]		
	            WHERE [IdAlerta] = (SELECT MAX(IdAlerta) FROM [dbo].[AlertaStatusDatabases] WHERE [NomeAlerta] = 'Database Indisponivel' )
	
	
	            -- Verifica Database que não está ONLINE
	            IF EXISTS	(
			               SELECT NULL
			               FROM [sys].[databases]
			               WHERE [state_desc] NOT IN ('ONLINE','RESTORING')
			            )
	            BEGIN 	
		            IF ISNULL(@Tipo, 0) = 0	-- Realiza o envia o Alerta apenas uma vez
		            BEGIN	
				
    	            --Envio de e-mail	
		            SET @Subject =	(SELECT 'ALERTA: Database OFFLINE no Servidor: ' + @@SERVERNAME)
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei que nem todas as Databases estao ONLINE na Instância <b>'+(SELECT @@SERVERNAME)+',</b> verifique o relatório abaixo com urgência.'	
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


		              -- Envio do Telegram		
		              SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
		              EXEC dbo.StpSendMsgTelegram 
			            @Destino = @TLMsg, 
			            @Msg = @MntMsg 
		
		              -- Registra informação na Tabela [AlertaStatusDatabases], Tipo = 1 -- ALERTA
		              INSERT INTO [dbo].[AlertaStatusDatabases] ( [NomeAlerta], [DesMensagem], [Tipo] )
		              SELECT 'Database Indisponivel', @Subject, 1			

		            END
	            END		
	            ELSE 
	            BEGIN				
		            IF ISNULL(@Tipo, 0) = 1
		            BEGIN
		
		            --Envio de e-mail	
		            SET @Subject =	(SELECT 'Todas as Databases estão ONLINE no Servidor: ' + @@SERVERNAME)
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Correção realizada com sucesso todas, as Databases estao ONLINE na Instância <b>'+(SELECT @@SERVERNAME)+',</b>.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT [name] AS [DataBase], [state_desc] AS [Status] FROM [sys].[databases]'
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

		              -- Envio do Telegram
		              SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
		              EXEC dbo.StpSendMsgTelegram 
		                 @Destino = @TLMsg, 
			            @Msg = @MntMsg 
				 			
		              -- Registra informação na Tabela [AlertaStatusDatabases] -> Tipo = 0 : CLEAR
		              INSERT INTO [dbo].[AlertaStatusDatabases] ( NomeAlerta, DesMensagem, [Tipo] )
		              SELECT 'Database Indisponivel', @Subject, 0

		            END
	            END		
                */
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};