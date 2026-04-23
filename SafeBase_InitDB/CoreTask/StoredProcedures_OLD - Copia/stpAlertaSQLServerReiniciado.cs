using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaSQLServerReiniciado()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- SQL Server Reiniciado
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'SQL Server Reiniciado')

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            DECLARE @SQL_Reiniciado_Parametro INT, @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), 
			            @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
			            @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), 
			            @Query VARCHAR(MAX), @Importance AS VARCHAR(6), @Subject VARCHAR(500)

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @SQL_Reiniciado_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Verifica se o SQL Server foi Reiniciado
	            IF ( OBJECT_ID('tempdb..##Alerta_SQL_Reiniciado') IS NOT NULL ) 
		            DROP TABLE ##Alerta_SQL_Reiniciado
	
	            SELECT [create_date]
	            INTO ##Alerta_SQL_Reiniciado
	            FROM [sys].[databases] WITH(NOLOCK)
	            WHERE	[database_id] = 2 -- Verifica a Database TempDb

                        AND[create_date] >= DATEADD(MINUTE, -@SQL_Reiniciado_Parametro, GETDATE())

                /*******************************************************************************************************************************
	            --	Verifica se o SQL foi Reiniciado
	            *******************************************************************************************************************************/
                IF EXISTS(SELECT* FROM ##Alerta_SQL_Reiniciado )
	            BEGIN
                    /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/

                    SET @Subject = 'ALERTA #SQLServerReboot - SQL Server Reiniciado nos últimos ' + CAST((@SQL_Reiniciado_Parametro)AS VARCHAR) + ' Minutos no Servidor: ' + @@SERVERNAME
                    SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei que o servidor: <b>' + @@SERVERNAME +',</b> foi reiniciado, verifique essa informação com <b>urgência</b>.'

                    SET @CaminhoFim = @CaminhoPath + @NomeRel + '.html'

                    -- Gera Primeiro bloco de HTML
                    SET @Query = 'SELECT CONVERT(VARCHAR(20), [create_date], 120) AS [Horario Restart] FROM ##Alerta_SQL_Reiniciado'

                    SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
                    -- Gera Segundo bloco de HTML
                    SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
		            select @HTML = @HTML + @Ass
                    -- Salva Arquivo HTML de Envio
                    EXEC dbo.stpEscreveArquivo
                            @Ds_Texto = @HTML, --nvarchar(max)

                            @Ds_Caminho = @CaminhoFim, --nvarchar(max)

                            @Ds_Codificacao = N'UTF-8', --nvarchar(max)

                            @Ds_Formato_Quebra_Linha = N'windows', --nvarchar(max)

                            @Fl_Append = 0-- bit

                   /*******************************************************************************************************************************
                   --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
                   *******************************************************************************************************************************/
                   EXEC[msdb].[dbo].[sp_send_dbmail]
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

                END";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};