using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaQueriesDemoradas()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Queries Demoradas
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Queries Demoradas')

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            DECLARE @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Importance AS VARCHAR(6),@Queries_Demoradas_Parametro INT, @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), 
			            @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), 
			            @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), 
			            @Query VARCHAR(MAX)
	

	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Queries_Demoradas_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Cria a tabela com as queries demoradas
	            IF ( OBJECT_ID('tempdb..#Queries_Demoradas_Temp') IS NOT NULL )
		            DROP TABLE #Queries_Demoradas_Temp

	            SELECT	[StartTime], 
			            [DataBaseName], 
			            [Duration],
			            [Reads],
			            [Writes],
			            [CPU],
			            [TextData]
	            INTO #Queries_Demoradas_Temp
	            FROM [dbo].[SQLTraceLog]
	            WHERE [StartTime] >= DATEADD(mi, -5, GETDATE())
	            ORDER BY [Duration] DESC
		
	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProc') IS NOT NULL )
		            DROP TABLE ##VerificaProc
	
                 CREATE TABLE ##VerificaProc
	               ([Duração]             VARCHAR(100),
	                [database_name]       VARCHAR(100),
	                [login_name]          VARCHAR(100),
	                [host_name]           VARCHAR(100),
	                [start_time]          VARCHAR(100),
	                [status]              VARCHAR(100),
	                [session_id]          VARCHAR(100),
	                [blocking_session_id] VARCHAR(100),
	                [Wait]                VARCHAR(100),
	                [open_tran_count]     VARCHAR(100),
	                [CPU]                 VARCHAR(100),
	                [reads]               VARCHAR(100),
	                [writes]              VARCHAR(100),
	                [sql_command]         VARCHAR(100),
	               );

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaQ') IS NOT NULL )
		            DROP TABLE ##VerificaQ
	
                 CREATE TABLE ##VerificaQ
	               ([StartTime]	VARCHAR(200),
	                [DataBaseName]   VARCHAR(100),
	                [Duration]	     VARCHAR(100),
	                [Reads]		VARCHAR(100),
	                [Writes]	     VARCHAR(100),
	                [CPU]		     VARCHAR(100),
	                [TextData]		VARCHAR(100));

	            -- Declara a variavel e retorna a quantidade de Queries Lentas
	            DECLARE @Quantidade_Queries_Demoradas INT = ( SELECT COUNT(*) FROM #Queries_Demoradas_Temp ) 
	
	            /*******************************************************************************************************************************
	            --	Verifica se existem mais de 100 Queries Lentas nos últimos 5 minutos
	            *******************************************************************************************************************************/
	            IF (@Quantidade_Queries_Demoradas > @Queries_Demoradas_Parametro)
	            BEGIN
		            -- Cria a tabela que ira armazenar os dados dos processos
		            IF ( OBJECT_ID('tempdb..#Resultado_WhoisActive') IS NOT NULL )
			            DROP TABLE #Resultado_WhoisActive
				
		            CREATE TABLE #Resultado_WhoisActive (		
			            [dd hh:mm:ss.mss]		VARCHAR(20),
			            [database_name]			NVARCHAR(128),		
			            [login_name]			NVARCHAR(128),
			            [host_name]				NVARCHAR(128),
			            [start_time]			DATETIME,
			            [status]				VARCHAR(30),
			            [session_id]			INT,
			            [blocking_session_id]	INT,
			            [wait_info]				VARCHAR(MAX),
			            [open_tran_count]		INT,
			            [CPU]					VARCHAR(MAX),
			            [reads]					VARCHAR(MAX),
			            [writes]				VARCHAR(MAX),
			            [sql_command]			XML
		            )

		            --------------------------------------------------------------------------------------------------------------------------------
		            --	ALERTA - DADOS - WHOISACTIVE
		            --------------------------------------------------------------------------------------------------------------------------------
		            -- Retorna todos os processos que estão sendo executados no momento
		            EXEC [dbo].[sp_WhoIsActive]
				            @get_outer_command =	1,
				            @output_column_list =	'[dd hh:mm:ss.mss][database_name][login_name][host_name][start_time][status][session_id][blocking_session_id][wait_info][open_tran_count][CPU][reads][writes][sql_command]',
				            @destination_table =	'#Resultado_WhoisActive'
						    
		            -- Altera a coluna que possui o comando SQL
		            ALTER TABLE #Resultado_WhoisActive
		            ALTER COLUMN [sql_command] VARCHAR(MAX)
			
		            UPDATE #Resultado_WhoisActive
		            SET [sql_command] = REPLACE( REPLACE( REPLACE( REPLACE( CAST([sql_command] AS VARCHAR(1000)), '<?query --', ''), '--?>', ''), '&gt;', '>'), '&lt;', '')
			
		            -- Verifica se não existe nenhum processo em Execução
		            IF NOT EXISTS ( SELECT TOP 1 * FROM #Resultado_WhoisActive )
		            BEGIN
			            INSERT INTO #Resultado_WhoisActive
			            SELECT NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
		            END

		            INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		            SELECT	TOP 50 * FROM #Resultado_WhoisActive

		            INSERT INTO ##VerificaQ ([StartTime],[DataBaseName],[Duration],[Reads],[Writes],[CPU],[TextData])
		            SELECT TOP 50
							            CONVERT(VARCHAR(20), [StartTime], 120)	AS [StartTime], 
							            [DataBaseName], 
							            CAST([Duration] AS VARCHAR)				AS [Duration],
							            CAST([Reads] AS VARCHAR)				AS [Reads],
							            CAST([Writes] AS VARCHAR)				AS [Writes],
							            CAST([CPU] AS VARCHAR)					AS [CPU],
							            SUBSTRING([TextData], 1, 150)			AS [TextData]
		            FROM #Queries_Demoradas_Temp
		            ORDER BY [Duration] DESC

		
		            /*******************************************************************************************************************************
		            --	CRIA O EMAIL - ALERTA
		            *******************************************************************************************************************************/
	
		            SET @Subject =	'ALERTA #QueriesDemoradas - Existem ' + CAST(@Quantidade_Queries_Demoradas AS VARCHAR) + ' queries demoradas nos últimos 5 minutos no Servidor: ' + @@SERVERNAME
		            SET @TextRel1 = 'Prezados,<BR /><BR /> Segue os TOP 50 - Processos em execução no Servidor: <b>' + @@SERVERNAME +',</b> verifique o relatório abaixo.'	
		            SET @TextRel2 =  'TOP 50 - Queries Demoradas.'	
		            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
		            -- Gera Primeiro bloco de HTML
		            SET @Query = 'SELECT * FROM [##VerificaProc]'
		            SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
		            -- Gera Segundo bloco de HTML
		            SET @Query = 'SELECT * FROM [##VerificaQ]'
		            SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
		            -- Gera Terceiro bloco de HTML
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


	            END
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};