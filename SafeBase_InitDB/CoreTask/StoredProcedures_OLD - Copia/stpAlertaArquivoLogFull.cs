using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaArquivoLogFull()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Arquivo de Log Full
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Arquivo de Log Full')
	
	            -- Declara as variaveis
	            DECLARE @Tamanho_Minimo_Alerta_log INT, @AlertaLogHeader VARCHAR(MAX), @AlertaLogTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @Importance AS VARCHAR(6), @EmailBody VARCHAR(MAX), @Subject VARCHAR(500), @Fl_Tipo TINYINT, @Log_Full_Parametro TINYINT,
			            @ResultadoWhoisactiveHeader VARCHAR(MAX), @ResultadoWhoisactiveTable VARCHAR(MAX), @EmailDestination VARCHAR(200), 
			            @BuscaParametro VARCHAR(80), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),@MntMsg VARCHAR(200), 
			            @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), @CaminhoPath VARCHAR(50), 
			            @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Email, Parametro, Id Telegram, Caminho dos reports, Profile DB Mail, Body Format Mail 
	            SELECT @NomeRel = Nm_Alerta, 
		              @Log_Full_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Tamanho Minimo do Alerta 100 MB = 100000
	            SELECT @Tamanho_Minimo_Alerta_log = 100000	
	
	            -- Verifica o último Tipo do Alerta registrado
	            -- 0: CLEAR 
	            -- 1: ALERTA	
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            -- Cria a tabela que ira armazenar os dados dos processos
	            IF ( OBJECT_ID('tempdb..#Resultado_WhoisActive') IS NOT NULL )
		            DROP TABLE #Resultado_WhoisActive
				
	            CREATE TABLE #Resultado_WhoisActive (		
		            [dd hh:mm:ss.mss]		VARCHAR(20),
		            [database_name]		NVARCHAR(128),		
		            [login_name]			NVARCHAR(128),
		            [host_name]			NVARCHAR(128),
		            [start_time]			DATETIME,
		            [status]				VARCHAR(30),
		            [session_id]			INT,
		            [blocking_session_id]	INT,
		            [wait_info]			VARCHAR(MAX),
		            [open_tran_count]		INT,
		            [CPU]				VARCHAR(MAX),
		            [reads]				VARCHAR(MAX),
		            [writes]				VARCHAR(MAX),
		            [sql_command]			XML
	            )

	            -- Cria a tabela log que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaLog') IS NOT NULL )
		            DROP TABLE ##VerificaLog
				
	            CREATE TABLE ##VerificaLog (		
		            [Database]				    VARCHAR(50),
		            [Tamanho Log (MB)]			    VARCHAR(50),		
		            [Percentual Log Utilizado (%)]    VARCHAR(50)
	            )

	            -- Cria a tabela processo que ira armazenar os dados envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProc') IS NOT NULL )
		            DROP TABLE ##VerificaProc
	
                CREATE TABLE ##VerificaProc
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

	            /*******************************************************************************************************************************
	            -- Verifica se existe algum LOG com muita utilização
	            *******************************************************************************************************************************/
	            IF EXISTS(
				            SELECT	db.[name]							AS [Database Name],
						            db.[recovery_model_desc]			AS [Recovery Model],
						            db.[log_reuse_wait_desc]			AS [Log Reuse Wait DESCription],
						            ls.[cntr_value]						AS [Log Size (KB)],
						            lu.[cntr_value]						AS [Log Used (KB)],
						            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
								            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
										            THEN 1 
										            ELSE CAST(ls.[cntr_value] AS FLOAT) 
								            END AS DECIMAL(18,2)) * 100 AS [Percente_Log_Used] ,
						            db.[compatibility_level]			AS [DB Compatibility Level] ,
						            db.[page_verify_option_desc]		AS [Page Verify Option]
				            FROM [sys].[databases] AS db
				            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
				            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
				            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
						            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%' 
						            AND ls.[cntr_value] > @Tamanho_Minimo_Alerta_log		-- Maior que 100 MB
						            AND (
								            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
										            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
												            THEN 1 
												            ELSE CAST(ls.[cntr_value] AS FLOAT) 
										            END AS DECIMAL(18,2)) * 100
							            ) > @Log_Full_Parametro
			             )
	            BEGIN	-- INICIO - ALERTA
		            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
		            BEGIN
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
			
			            -- Insert de dados para envio de Log
			            INSERT INTO ##VerificaLog ( [Database], [Tamanho Log (MB)], [Percentual Log Utilizado (%)])
			            SELECT	db.[name]	AS [DatabaseName] ,
					            CAST(ls.[cntr_value] / 1024.00 AS DECIMAL(18,2))	AS [cntr_value],
					            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
					            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
					                THEN 1 
					                ELSE CAST(ls.[cntr_value] AS FLOAT) 
					            END AS DECIMAL(18,2)) * 100					AS [Percente_Log_Used]
			            FROM [sys].[databases] AS db
			            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
			            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
			            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
			            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%' 
			            AND ls.[cntr_value] > @Tamanho_Minimo_Alerta_log -- Maior que 100 MB
			            AND (
				            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
							            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
													            THEN 1 
														            ELSE CAST(ls.[cntr_value] AS FLOAT) 
												            END AS DECIMAL(18,2)) * 100
				            ) > @Log_Full_Parametro

			            -- Insert de dados para envio de Processo
		                  INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                  SELECT * FROM #Resultado_WhoisActive
				
			            /*******************************************************************************************************************************
			            --	ALERTA - CRIA O EMAIL
			            *******************************************************************************************************************************/
			             -- Parametros do Alerta
			             SET @Subject =  (SELECT 'ALERTA #TransactionLog - Detectado inconsistência de Transaction Log com mais de ' +CAST((@Log_Full_Parametro) AS VARCHAR)+ '% de utilização no Servidor: ' + @@SERVERNAME)
			             SET @TextRel1 =  'Prezados,<BR /><BR /> Identificada inconsistência de <b>transaction log </b> acima de ' +CAST((@Log_Full_Parametro) AS VARCHAR)+ '% na Instância <b>'+(SELECT @@SERVERNAME)+',</b> verifique o relatório abaixo com <b>urgência</b>.'	
			             SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			             SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaLog]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             -- Gera Segundo bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProc]'
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
						                --@file_attachments = 'E:\LOGS\AUDIT\LogMedicao.txt',
						                @importance = @Importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
			             SET @MntMsg = @Subject+', Verifique os detalhes no e-mail com *urgência*'
			             EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg, 
			                @Msg = @MntMsg 	
		
           	            /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 1 : ALERTA
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 1	
					
		            END
	            END		-- FIM - ALERTA
	            ELSE 
	            BEGIN	-- INICIO - CLEAR
		            IF @Fl_Tipo = 1
		            BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	CLEAR - DADOS - WHOISACTIVE
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
		
			
			            -- Insert de dados para envio de Log
			            INSERT INTO ##VerificaLog ( [Database], [Tamanho Log (MB)], [Percentual Log Utilizado (%)])
			            SELECT	db.[name]											AS [DatabaseName] ,
								            CAST(ls.[cntr_value] / 1024.00 AS DECIMAL(18,2))	AS [cntr_value],
								            CAST(	CAST(lu.[cntr_value] AS FLOAT) / 
										            CASE WHEN CAST(ls.[cntr_value] AS FLOAT) = 0 
												            THEN 1 
												            ELSE CAST(ls.[cntr_value] AS FLOAT) 
										            END AS DECIMAL(18,2)) * 100					AS [Percente_Log_Used] 
						            FROM [sys].[databases] AS db
						            JOIN [sys].[dm_os_performance_counters] AS lu  ON db.[name] = lu.[instance_name]
						            JOIN [sys].[dm_os_performance_counters] AS ls  ON db.[name] = ls.[instance_name]
						            WHERE	lu.[counter_name] LIKE 'Log File(s) Used Size (KB)%'
								            AND ls.[counter_name] LIKE 'Log File(s) Size (KB)%'
								            AND ls.[cntr_value] > 1000 -- Maior que 100 MB

			            -- Insert de dados para envio de Processo
		                  INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                  SELECT * FROM #Resultado_WhoisActive
				
			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/
			             SET @Subject =  (SELECT 'Solução #TransactioLog - Não existem mais inconsistência de Transaction Log com mais de ' +CAST((@Log_Full_Parametro) AS VARCHAR)+ '% de utilização no Servidor: ' + @@SERVERNAME)
			             SET @TextRel1 =  'Informações dos Arquivos de Log.'	
			             SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			             SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'

			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaLog]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             -- Gera Segundo bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProc]'
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
			            --	ALERTA - ENVIA O EMAIL
			            *******************************************************************************************************************************/	
			             EXEC [msdb].[dbo].[sp_send_dbmail]
						                @profile_name = @ProfileDBMail,
						                @recipients = @EmailDestination,
						                @body_format = @BodyFormatMail,
						                @subject = @Subject,
						                --@file_attachments = 'E:\LOGS\AUDIT\LogMedicao.txt',
						                @importance = @Importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
			             SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
			             EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg, 
			                @Msg = @MntMsg 	
						
			            /*******************************************************************************************************************************
			            -- Insere um Registro na Tabela de Controle dos Alertas -> Fl_Tipo = 0 : CLEAR
			            *******************************************************************************************************************************/
			            INSERT INTO [dbo].[Alerta] ( [Id_AlertaParametro], [Ds_Mensagem], [Fl_Tipo] )
			            SELECT @Id_AlertaParametro, @Subject, 0		
		            END
	            END		-- FIM - CLEAR
                ";
        // Execute the command and send back the results
        SqlContext.Pipe.ExecuteAndSend(myCommand);
    }
};