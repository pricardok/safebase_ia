using System;
using System.Collections.Generic;
using System.Text;

namespace InitDB.Client
{
    class stpAlertaJobFalha
    {
        public static string Query()
        {
            return
			// @"insert into [dbo].[Testedb] ([Nome],[DateTest]) values ('Teste da ferramenta DB - stpAlertaJobFalha',GETDATE())";
			@"  SET NOCOUNT ON;
				
				SET QUOTED_IDENTIFIER ON;
		
	            IF ( OBJECT_ID('tempdb..#Result_History_Jobs') IS NOT NULL )
		            DROP TABLE #Result_History_Jobs

	            CREATE TABLE #Result_History_Jobs (
		            [Cod]				INT IDENTITY(1,1),
		            [Instance_Id]		INT,
		            [Job_Id]			VARCHAR(255),
		            [Job_Name]			VARCHAR(255),
		            [Step_Id]			INT,
		            [Step_Name]			VARCHAR(255),
		            [SQl_Message_Id]	INT,
		            [Sql_Severity]		INT,
		            [SQl_Message]		VARCHAR(4490),
		            [Run_Status]		INT,
		            [Run_Date]			VARCHAR(20),
		            [Run_Time]			VARCHAR(20),
		            [Run_Duration]		INT,
		            [Operator_Emailed]	VARCHAR(100),
		            [Operator_NetSent]	VARCHAR(100),
		            [Operator_Paged]	VARCHAR(100),
		            [Retries_Attempted]	INT,
		            [Nm_Server]			VARCHAR(100)  
	            )

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            DECLARE @JobFailed_Parametro INT, @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Importance AS VARCHAR(6),@Queries_Demoradas_Parametro INT, 
			            @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), 
			            @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX),@Query VARCHAR(MAX), @Ds_Email_Assunto_alerta VARCHAR (600), 
                        @Ds_Email_Assunto_solucao VARCHAR (600), @Ds_Email_Texto_alerta VARCHAR (600), @Ds_Email_Texto_solucao VARCHAR (600), 
                        @Ds_Menssageiro_01 VARCHAR (30), @Ds_Menssageiro_02 VARCHAR (30), @Ds_Menssageiro_03 VARCHAR (30)
	
	            -- Job Falha
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Job Falha' AND Ativo = 1)
                DECLARE @Ds_Caminho_Base VARCHAR(100) = (SELECT Ds_Caminho FROM [dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'CheckList')
                DECLARE @Telegram INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Telegram')
                DECLARE @Teams INT = (select Id_AlertaParametro from AlertaParametro WHERE Nm_Alerta = 'Envia Teams')

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @JobFailed_Parametro = Vl_Parametro, 
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

	            -- Declara as variaveis
	            DECLARE @Dt_Inicial VARCHAR (8), @Dt_Referencia DATETIME	
	            SELECT @Dt_Referencia = GETDATE()
	            SELECT	@Dt_Inicial  =	CONVERT(VARCHAR(8), (DATEADD (HOUR, -@JobFailed_Parametro, @Dt_Referencia)), 112)
	
	            INSERT INTO #Result_History_Jobs
	            EXEC [msdb].[dbo].[sp_help_jobhistory] 
			            @mode = 'FULL', 
			            @start_run_date = @Dt_Inicial

	            -- Busca os dados dos JOBS que Falharam
	            IF ( OBJECT_ID('tempdb..##Alerta_Job_Falharam') IS NOT NULL )
		            DROP TABLE ##Alerta_Job_Falharam
	
	            SELECT	TOP 50
			            [Nm_Server] AS [Server],
			            [Job_Name], 
			            CASE	WHEN [Run_Status] = 0 THEN 'Failed'
					            WHEN [Run_Status] = 1 THEN 'Succeeded'
					            WHEN [Run_Status] = 2 THEN 'Retry (step only)'
					            WHEN [Run_Status] = 3 THEN 'Cancelled'
					            WHEN [Run_Status] = 4 THEN 'In-progress message'
					            WHEN [Run_Status] = 5 THEN 'Unknown' 
			            END AS [Status],
			            CAST(	[Run_Date] + ' ' +
					            RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-5), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-3), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-1), 2), 2) AS VARCHAR
				            ) AS [Dt_Execucao],
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR), (LEN([Run_Duration])-5), 2), 2) + ':' +
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR), (LEN([Run_Duration])-3), 2), 2) + ':' +
			            RIGHT('00' + SUBSTRING(CAST([Run_Duration] AS VARCHAR), (LEN([Run_Duration])-1), 2), 2) AS [Run_Duration],
			            CAST([SQl_Message] AS VARCHAR(3990)) AS [SQL_Message]
	            INTO ##Alerta_Job_Falharam
	            FROM #Result_History_Jobs 
	            WHERE 
		              [Step_Id] = 0 AND 
		              [Run_Status] <> 1 AND
		              CAST	(	
					            [Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-3), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time], (LEN([Run_Time])-1), 2), 2) AS DATETIME
				            ) >= DATEADD(HOUR, -@JobFailed_Parametro, @Dt_Referencia) AND
		              CAST	(	[Run_Date] + ' ' + RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-5), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-3), 2), 2) + ':' +
					            RIGHT('00' + SUBSTRING([Run_Time],(LEN([Run_Time])-1), 2), 2) AS DATETIME
				            ) < @Dt_Referencia
	            ORDER BY [Dt_Execucao] DESC
			
	            /*******************************************************************************************************************************
	            --	Verifica se algum JOB Falhou
	            *******************************************************************************************************************************/
	            IF EXISTS(SELECT * FROM ##Alerta_Job_Falharam)
	            BEGIN
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
		            SET @Subject =	@Ds_Email_Assunto_alerta + ' ' + CAST(@JobFailed_Parametro AS VARCHAR) + ' Horas no Servidor ' + @@SERVERNAME
		            SET @TextRel1 = @Ds_Email_Texto_alerta 	
		            SET @CaminhoFim = @Ds_Caminho_Base + @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT [Job_Name] AS [Nome do JOB], [Dt_Execucao] AS [Data da Execução], [Run_Duration] AS [Duração],[SQL_Message] AS [Mensagem Erro] FROM ##Alerta_Job_Falharam'
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
                    --SET @MntMsg = @Subject+', Verifique os detalhes no *E-Mail*'

                    -- Envio do Telegram    
                    --EXEC dbo.StpSendMsgTelegram 
                    --          @Destino = @CanalTelegram,
                    --          @Msg = @MntMsg

                    -- MS TEAMS
                    --SET @MntMsg = (select replace (@MntMsg, '\', '-'))
                    --EXEC [dbo].[stpSendMsgTeams]
	                --        @msg = @MntMsg,
	                --        @channel = @Ds_Menssageiro_02,
                    --        @ap = @Teams

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
                ";

        }
    }
}
