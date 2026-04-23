using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaJobFalha()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
		
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
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX),@Query VARCHAR(MAX)
	
	            -- Job Falha
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Job Falha')

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @JobFailed_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro

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
		            SET @Subject =	'ALERTA #JobsFail - Falha de execução de Jobs nas últimas ' + CAST(@JobFailed_Parametro AS VARCHAR) + ' Horas no Servidor ' + @@SERVERNAME
		            SET @TextRel1 = 'Prezados,<BR /><BR />Segue as TOP 50 jobs que Falharam nas últimas ' +  CAST((@JobFailed_Parametro) AS VARCHAR) + ' Horas no Servidor: <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT [Job_Name] AS [Nome do JOB], [Dt_Execucao] AS [Data da Execução], [Run_Duration] AS [Duração],[SQL_Message] AS [Mensagem Erro] FROM ##Alerta_Job_Falharam'
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
	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};