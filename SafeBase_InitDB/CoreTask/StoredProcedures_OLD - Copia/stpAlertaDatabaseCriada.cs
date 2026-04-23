using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaDatabaseCriada()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                -- Database Criada
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Database Criada')

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            DECLARE @Database_Criada_Parametro INT, @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), 
			            @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
			            @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), 
			            @Query VARCHAR(MAX), @Importance AS VARCHAR(6), @Subject VARCHAR(500)
	
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Database_Criada_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Verifica se alguma base foi criada no dia anterior
	            IF ( OBJECT_ID('tempdb..##Alerta_Base_Criada') IS NOT NULL ) 
		            DROP TABLE ##Alerta_Base_Criada
	
	            SELECT [name], [recovery_model_desc], [create_date]
	            INTO ##Alerta_Base_Criada
	            FROM [sys].[databases] WITH(NOLOCK)
	            WHERE	[database_id] <> 2 
                        AND[create_date] >= DATEADD(HOUR, -@Database_Criada_Parametro, GETDATE())

                /*******************************************************************************************************************************
	            --	Verifica se alguam base foi criada
	            *******************************************************************************************************************************/
                IF EXISTS(SELECT* FROM ##Alerta_Base_Criada )
	            BEGIN
                    /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
                    SET @Subject = 'ALERTA #DatabaseCriada - Database Criada nas últimas ' + CAST((@Database_Criada_Parametro)AS VARCHAR) + ' Horas no Servidor: ' + @@SERVERNAME
                    SET @TextRel1 = 'Prezados,<BR /><BR /> Identifiquei alteraçoes de Database no servidor: <b>' + @@SERVERNAME +',</b> verifique essa informação realizando o planejamento necessário para que não falte de espaço em disco.'
                    SET @CaminhoFim = @CaminhoPath + @NomeRel + '.html'

                    -- Gera Primeiro bloco de HTML
                    SET @Query = 'SELECT [name] AS [Nome], [recovery_model_desc] AS [Recovery Model], CONVERT(VARCHAR(20), [create_date], 120) AS [Data Criação] FROM ##Alerta_Base_Criada'
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
                    SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
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