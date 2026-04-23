using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpEnviaEmailProcessosExecucao()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON
                
	            -- Declara as variaveis
	            DECLARE	@Subject VARCHAR(500), @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200),
			            @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), @TLMsg VARCHAR(200), 
			            @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)
	 
	            -- Cria a tabela que ira armazenar os dados dos processos
	            IF ( OBJECT_ID('TempDb..#Resultado_WhoisActive') IS NOT NULL )
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

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProcessos') IS NOT NULL )
		            DROP TABLE ##VerificaProcessos
	
                CREATE TABLE ##VerificaProcessos
	               ([Duração]             VARCHAR(50),
	                [database_name]       VARCHAR(50),
	                [login_name]          VARCHAR(50),
	                [host_name]           VARCHAR(50),
	                [start_time]          VARCHAR(50),
	                [status]              VARCHAR(50),
	                [session_id]          VARCHAR(50),
	                [blocking_session_id] VARCHAR(50),
	                [Wait]                VARCHAR(50),
	                [open_tran_count]     VARCHAR(50),
	                [CPU]                 VARCHAR(50),
	                [reads]               VARCHAR(50),
	                [writes]              VARCHAR(50),
	                [sql_command]         VARCHAR(50),
	               );
      
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


	            -- Insert de dados para envio de Processo
	            INSERT INTO ##VerificaProcessos ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
	            SELECT	ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
						            ISNULL([database_name], '-')							AS [database_name],
						            ISNULL([login_name], '-')								AS [login_name],
						            ISNULL([host_name], '-')								AS [host_name],
						            ISNULL(CONVERT(VARCHAR(20), [start_time], 120), '-')	AS [start_time],
						            ISNULL([status], '-')									AS [status],
						            ISNULL(CAST([session_id] AS VARCHAR), '-')				AS [session_id],
						            ISNULL(CAST([blocking_session_id] AS VARCHAR), '-')		AS [blocking_session_id],
						            ISNULL([wait_info], '-')								AS [Wait],
						            ISNULL(CAST([open_tran_count] AS VARCHAR), '-')			AS [open_tran_count],
						            ISNULL([CPU], '-')										AS [CPU],
						            ISNULL([reads], '-')									AS [reads],
						            ISNULL([writes], '-')									AS [writes],
						            ISNULL(SUBSTRING([sql_command], 1, 300), '-')			AS [sql_command]
				            FROM #Resultado_WhoisActive
	
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Processos em Execução
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [DBController].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Processos em Execução')
	
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
	            --	CRIA O EMAIL
	            *******************************************************************************************************************************/							
	
	            SET @Subject =	'#Processos - Processos em execução no Servidor: ' + @@SERVERNAME	
	            SET @TextRel1 = 'Prezados,<BR /><BR /> Segue os processos em execução no Servidor <b>' + @@SERVERNAME +',</b> verifique essa informação.'	
	            SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
	            -- Gera Primeiro bloco de HTML
	            SET @Query = 'SELECT * FROM ##VerificaProcessos'
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
			                --@Destino = '49353855', 
			                @Msg = @MntMsg
	
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};