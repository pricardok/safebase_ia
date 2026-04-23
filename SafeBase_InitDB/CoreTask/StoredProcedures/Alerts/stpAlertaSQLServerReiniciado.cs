using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaSQLServerReiniciado
    {
        public static string Query()
        {
            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaSQLServerReiniciado',GETDATE())";
            @"
                SET NOCOUNT ON;

                SET QUOTED_IDENTIFIER ON;

	            -- SQL Server Reiniciado
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'SQL Server Reiniciado' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            DECLARE @SQL_Reiniciado_Parametro INT, @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), 
			            @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
			            @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), 
			            @Query VARCHAR(MAX), @Importance AS VARCHAR(6), @Subject VARCHAR(500), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @SQL_Reiniciado_Parametro = Vl_Parametro, 
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

                    SET @Subject = @Ds_Email_Assunto_alerta + ' ' + CAST((@SQL_Reiniciado_Parametro)AS VARCHAR) + ' Minutos na instância: ' + @@SERVERNAME
                    SET @TextRel1 = @Ds_Email_Texto_alerta 
                    SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel + '.html'

                    -- Gera Primeiro bloco de HTML
                    SET @Query = 'SELECT CONVERT(VARCHAR(20), [create_date], 120) AS [Horario Restart] FROM ##Alerta_SQL_Reiniciado'
                    SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
                    -- Gera Segundo bloco de HTML
		            select @HTML = @HTML + @Ass
                    -- Salva Arquivo HTML de Envio
                    EXEC dbo.stpWriteFile
                            @Ds_Texto = @HTML, --nvarchar(max)
                            @Ds_Caminho = @CaminhoFim, --nvarchar(max)
                            @Ds_Codificacao = N'UTF-8', --nvarchar(max)
                            @Ds_Formato_Quebra_Linha = N'windows', --nvarchar(max)
                            @Fl_Append = 0-- bit

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

                END";
        }
    }
}
