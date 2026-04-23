using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaCheckDB()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            IF ( OBJECT_ID('tempdb..#TempLog') IS NOT NULL ) 
		            DROP TABLE #TempLog
	
	            CREATE TABLE #TempLog (
		            [LogDate]		DATETIME,
		            [ProcessInfo]	NVARCHAR(50),
		            [Text]			NVARCHAR(MAX)
	            )

	            IF ( OBJECT_ID('tempdb..#logF') IS NOT NULL ) 
		            DROP TABLE #logF
	
	            CREATE TABLE #logF (
		            ArchiveNumber     INT,
		            LogDate           DATETIME,
		            LogSize           INT 
	            )

	            -- Seleciona o número de arquivos.
	            --INSERT INTO #logF  
	            --EXEC sp_enumerrorlogs
	
	
	            -- Utilizar caso apresente erro no script acima
	            IF (OBJECT_ID('tempdb..#logFAux') IS NOT NULL)
		            DROP TABLE #logFAux
	
	            CREATE TABLE #logFAux (
		            [ArchiveNumber] INT,
		            [LogDate]		VARCHAR(20),
		            [LogSize]		INT 
	            )
	
	            -- Seleciona o número de arquivos.
	            INSERT INTO #logFAux  
	            EXEC sp_enumerrorlogs

	            insert into #logF
	            select ArchiveNumber, cast((substring(LogDate,7,4)+substring(LogDate,1,2)+substring(LogDate,4,2)) as datetime), LogSize
	            from #logFAux
	
	
	            DELETE FROM #logF
	            WHERE LogDate < GETDATE()-2

	            DECLARE @TSQL NVARCHAR(2000), @lC INT	

	            SELECT @lC = MIN(ArchiveNumber) FROM #logF

	            --Loop para realizar a leitura de todo o log
	            WHILE @lC IS NOT NULL
	            BEGIN
		              INSERT INTO #TempLog
		              EXEC sp_readerrorlog @lC
		  
		              SELECT @lC = MIN(ArchiveNumber) 
		              FROM #logF
		              WHERE ArchiveNumber > @lC
	            END

	            IF OBJECT_ID('_Result_Corrupcao') IS NOT NULL
		            DROP TABLE _Result_Corrupcao
		
	            SELECT	LogDate,
			            SUBSTRING(Text, 15, CHARINDEX(')', Text, 15) - 15) AS Nm_Database,
			            SUBSTRING(Text,charindex('found',Text),(charindex('Elapsed time',Text)-charindex('found',Text))) AS Erros,   
			            Text 
	            INTO _Result_Corrupcao
	            FROM #TempLog
	            WHERE LogDate >= GETDATE() - 1	 
		            and Text like '%DBCC CHECKDB (%'
		            and Text not like '%IDR%'
		            and substring(Text,charindex('found',Text), charindex('Elapsed time',Text) - charindex('found',Text)) <> 'found 0 errors and repaired 0 errors.'

	            -- Declara as variaveis
	            DECLARE @Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaBancoCorrompidoHeader VARCHAR(MAX), @AlertaBancoCorrompidoTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Banco de Dados Corrompido
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Banco de Dados Corrompido')

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

	            /*******************************************************************************************************************************
	            -- Verifica se existe algum Banco de Dados Corrompido
	            *******************************************************************************************************************************/
	            IF EXISTS (SELECT NULL FROM [InitDB].[dbo].[_Result_Corrupcao]) 
	            BEGIN	-- INICIO - ALERTA
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/			
		            SET @Subject =	'ALERTA #DatabaseCorrompido - Existe algum Banco de Dados Corrompido no Servidor ' + @@SERVERNAME + '. Verifique com urgência!'	
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei um problema de <b>Dados Corrompido</b> no Servidor <b>' + @@SERVERNAME +',</b> verifique essa informação com urgência.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT	CONVERT(VARCHAR(20), [LogDate], 120) AS [Data Log], [Nm_Database] AS [Nome Database],[Erros] AS [Erros],[Text] AS [Descricao] FROM [Traces].[dbo].[_Result_Corrupcao]'
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
				
	            IF ( OBJECT_ID('_Result_Corrupcao') IS NOT NULL )
		            DROP TABLE _Result_Corrupcao
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};