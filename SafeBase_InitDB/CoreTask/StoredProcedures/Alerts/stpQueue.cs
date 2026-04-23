using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpQueue
    {
        public static string Query()
        {
            return
            //@"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpQueue',GETDATE())";
            @"
            SET NOCOUNT ON;

            SET QUOTED_IDENTIFIER ON;

		    -- Recupera os parametros base
            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Alerta Queue' AND Ativo = 1)
            DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
            DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
            DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')
		
            -- Recupera os parametros do Alerta
            DECLARE @Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX), @AlertaBancoCorrompidoHeader VARCHAR(MAX), 
			    @AlertaBancoCorrompidoTable VARCHAR(MAX), @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300), 
			    @MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			    @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000), @HTML VARCHAR(MAX), @Query VARCHAR(MAX), @msg VARCHAR(8000)= '', @recipients VARCHAR(4000), 
			    @Ds_Email_Assunto_alerta VARCHAR (600), @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
			    @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)

            SELECT @NomeRel = Nm_Alerta, 
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
		    --	BLOCO DE ANÁLISE
		    *******************************************************************************************************************************/
            IF EXISTS
			    (SELECT *
				    FROM tempdb.sys.objects
				    WHERE name = 'ResultsQueue'
			    )
                DROP TABLE tempdb.dbo.ResultsQueue;

            CREATE TABLE tempdb.dbo.ResultsQueue(
		    c1 VARCHAR(8000));

		    DECLARE @DBName VARCHAR(100);
		    DECLARE Qe CURSOR FOR 				 
			    SELECT name 
			    FROM sys.databases 
			    WHERE name NOT IN ('master','model','msdb','tempdb')
			    AND state_desc = 'ONLINE'
			    AND is_broker_enabled = 1 
						
		    OPEN Qe;
		    FETCH NEXT FROM Qe INTO @DBName;
		    WHILE @@FETCH_STATUS = 0
			    BEGIN
						
				    DECLARE @out AS VARCHAR(6000) = ''
				    EXEC [dbo].[stpQueueInfoAutoRestart] @DBName, @out OUTPUT;
					
				    IF @out <> '' 
					    BEGIN
						    INSERT INTO tempdb.dbo.ResultsQueue(C1) SELECT @out
							
						    SELECT @msg = ISNULL(STUFF(
							    (
								    SELECT c1
								    FROM tempdb.dbo.ResultsQueue FOR XML PATH(''), TYPE
							    ).value('.', 'NVARCHAR(MAX)'), 1, 0, ''), '');


						    IF(OBJECT_ID('tempdb..##CheckQueue') IS NOT NULL)
							    DROP TABLE ##CheckQueue;
						    CREATE TABLE ##CheckQueue
							    ([QueueInfo] VARCHAR(200));
						    INSERT INTO ##CheckQueue
							    SELECT @msg

 						    /*******************************************************************************************************************************
						    --	CRIA O EMAIL - ALERTA
						    *******************************************************************************************************************************/			
						    SELECT @subject = @Ds_Email_Assunto_alerta + ' ' +@@SERVERNAME
						    SET @TextRel1 = @Ds_Email_Texto_alerta +' '+(SELECT TOP 1 SUBSTRING(QueueInfo, CHARINDEX('Queue: ', QueueInfo) + 6, LEN(QueueInfo)) FROM ##CheckQueue)+'</b> verifique essa informação, listagem completa abaixo.'	
						    SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'

						    -- Gera Primeiro bloco de HTML
						    SET @Query = 'Select Case When [QueueInfo] <> NULL THEN '''' ELSE ''<p style=""color: red; "">''+[QueueInfo]+''</p>'' END [QueueInfo] FROM ##CheckQueue'

                            SET @HTML = [dbo].[fncExportaMultiHTML](@Query, @TextRel1, 2, 1)
                            -- Gera Segundo bloco de HTML
                            -- SET @Ass = (SELECT Assinatura FROM MailAssinatura WHERE Id = 1)
						    select @HTML = @HTML + @Ass
                            -- Salva Arquivo HTML de Envio
                            EXEC[dbo].[stpWriteFile]
                                    @Ds_Texto = @HTML, -- nvarchar(max)
                                    @Ds_Caminho = @CaminhoFim, -- nvarchar(max)
                                    @Ds_Codificacao = N'UTF-8', -- nvarchar(max)
                                    @Ds_Formato_Quebra_Linha = N'windows', -- nvarchar(max)
                                    @Fl_Append = 0 -- bit

                                IF @msg<> '' 
								    BEGIN

                                    IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                WHERE B.Ativo = 1
                                                AND B.Des LIKE '%Email'
                                                AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                )

                                    BEGIN

                                            EXEC[msdb].[dbo].[sp_send_dbmail]
                                                @profile_name = @ProfileDBMail,
											    @recipients = @EmailDestination,
											    @body_format = @BodyFormatMail,
											    @subject = @Subject,
											    @importance = @Importance,
											    @body = @HTML;

								    END
 
								    -- Parametro Menssageiro
                                    SET @MntMsg = @Subject + ', Verifique os detalhes no *E-Mail*'

                                    IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                WHERE B.Ativo = 1
                                                AND B.Des LIKE '%Telegram'
                                                AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                )

                                    BEGIN
									    -- Envio do Telegram
                                        EXEC dbo.StpSendMsgTelegram
                                                @Destino = @CanalTelegram,
                                                @Msg = @MntMsg

                                    END

                                    IF EXISTS(SELECT B.Ativo from AlertaParametro A
                                                INNER JOIN [dbo].[AlertaEnvio] B ON B.IdAlertaParametro = A.Id_AlertaParametro
                                                WHERE B.Ativo = 1
                                                AND B.Des LIKE '%Teams'
                                                AND[Id_AlertaParametro] = @Id_AlertaParametro
                                                )

                                    BEGIN
									    -- MS TEAMS
                                        SET @MntMsg = (select replace(@MntMsg, '\', '-'))
                                        EXEC[dbo].[stpSendMsgTeams]
                                            @msg = @MntMsg,
                                            @channel = @Ds_Menssageiro_02,
                                            @ap = @Teams

                                    END

                                    END;

            END

            FETCH NEXT FROM Qe INTO @DBName;
		    END;
		    CLOSE Qe;
            DEALLOCATE Qe;   

             ";
        }
    }
}
