using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void stpAlertaProcessoBloqueado()
    {
        // Create the command
        SqlCommand myCommand = new SqlCommand();
        myCommand.CommandText =
              @"
                SET NOCOUNT ON

	            -- Processo Bloqueado
	            DECLARE @Id_AlertaParametro INT = (SELECT Id_AlertaParametro FROM [InitDB].[dbo].AlertaParametro (NOLOCK) WHERE Nm_Alerta = 'Processo Bloqueado')
	
	            -- Declara as variaveis
	            DECLARE	@Subject VARCHAR(500), @Fl_Tipo TINYINT, @Qtd_Segundos INT, @Consulta VARCHAR(8000), @Importance AS VARCHAR(6), @Dt_Atual DATETIME,
			            @EmailBody VARCHAR(MAX), @AlertaLockHeader VARCHAR(MAX), @AlertaLockTable VARCHAR(MAX), @EmptyBodyEmail VARCHAR(MAX),
			            @AlertaLockRaizHeader VARCHAR(MAX), @AlertaLockRaizTable VARCHAR(MAX), @Processo_Bloqueado_Parametro INT, @Qt_Tempo_Raiz_Lock INT,
			            @EmailDestination VARCHAR(200), @TextRel1 VARCHAR(4000), @TextRel2 VARCHAR(4000), @NomeRel VARCHAR(300),
			            @MntMsg VARCHAR(200), @TLMsg VARCHAR(200), @SendMail VARCHAR(200), @ProfileDBMail VARCHAR(50), @BodyFormatMail VARCHAR(20), 
			            @CaminhoPath VARCHAR(50), @CaminhoFim VARCHAR(50), @Ass VARCHAR(4000),@HTML VARCHAR(MAX), @Query VARCHAR(MAX)

	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Recupera os parametros do Alerta
	            --------------------------------------------------------------------------------------------------------------------------------
	            SELECT @NomeRel = Nm_Alerta, 
		              @Processo_Bloqueado_Parametro = Vl_Parametro, 
		              @EmailDestination = Ds_Email, 
		              @TLMsg = Ds_MSG, 
		              @CaminhoPath = Ds_Caminho, 
		              @ProfileDBMail = Ds_ProfileDBMail, 
		              @BodyFormatMail = Ds_BodyFormatMail,
		              @importance = Ds_TipoMail
	            FROM [dbo].[AlertaParametro]
	            WHERE [Id_AlertaParametro] = @Id_AlertaParametro	

	            -- Quantidade em Minutos -- Query que esta gerando o lock (rodando a mais de 1 minuto)
	            SELECT @Qt_Tempo_Raiz_Lock	= 1		

	            --------------------------------------------------------------------------------------------------------------------------------
	            --	Cria Tabela para armazenar os Dados da SP_WHOISACTIVE
	            --------------------------------------------------------------------------------------------------------------------------------
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
      
	            -- Seta a hora atual
	            SELECT @Dt_Atual = GETDATE()

	            -- Cria a tabela processo que em bloqueio que ira armazenar os dados de envio de email
	            IF ( OBJECT_ID('tempdb..##VerificaProcBlock') IS NOT NULL )
		            DROP TABLE ##VerificaProcBlock
	
                 CREATE TABLE ##VerificaProcBlock
	               ([Nr_Nivel_Lock]       VARCHAR(50),
	                [Duração]             VARCHAR(50),
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
	            --------------------------------------------------------------------------------------------------------------------------------
	            --	Carrega os Dados da SP_WHOISACTIVE
	            --------------------------------------------------------------------------------------------------------------------------------
	            -- Retorna todos os processos que estão sendo executados no momento
	            EXEC master.[dbo].[sp_WhoIsActive]
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

	            -- Verifica o último Tipo do Alerta registrado -> 0: CLEAR / 1: ALERTA
	            SELECT @Fl_Tipo = [Fl_Tipo]
	            FROM [dbo].[Alerta]
	            WHERE [Id_Alerta] = (SELECT MAX(Id_Alerta) FROM [dbo].[Alerta] WHERE [Id_AlertaParametro] = @Id_AlertaParametro )
	
	            /*******************************************************************************************************************************
	            --	Verifica se existe algum Processo Bloqueado
	            *******************************************************************************************************************************/	
	            IF EXISTS	(
					            SELECT NULL 
					            FROM #Resultado_WhoisActive A
					            JOIN #Resultado_WhoisActive B ON A.[blocking_session_id] = B.[session_id]
					            WHERE	DATEDIFF(SECOND,A.[start_time], @Dt_Atual) > @Processo_Bloqueado_Parametro * 60		-- A query que está sendo bloqueada está rodando a mais 2 minutos
							            AND DATEDIFF(SECOND,B.[start_time], @Dt_Atual) > @Qt_Tempo_Raiz_Lock * 60			-- A query que está bloqueando está rodando a mais de 1 minuto
				            )
	            BEGIN	-- INICIO - ALERTA
		            IF ISNULL(@Fl_Tipo, 0) = 0	-- Envia o Alerta apenas uma vez
		            BEGIN
			            --------------------------------------------------------------------------------------------------------------------------------
			            --	Verifica a quantidade de processos bloqueados
			            --------------------------------------------------------------------------------------------------------------------------------
			            -- Declara a variavel e retorna a quantidade de Queries Lentas
			            DECLARE @QtdProcessosBloqueados INT = (
										            SELECT COUNT(*)
										            FROM #Resultado_WhoisActive A
										            JOIN #Resultado_WhoisActive B ON A.[blocking_session_id] = B.[session_id]
										            WHERE	DATEDIFF(SECOND,A.[start_time], @Dt_Atual) > @Processo_Bloqueado_Parametro	* 60
												            AND DATEDIFF(SECOND,B.[start_time], @Dt_Atual) > @Qt_Tempo_Raiz_Lock * 60
									            )

			            DECLARE @QtdProcessosBloqueadosLocks INT = (
										            SELECT COUNT(*)
										            FROM #Resultado_WhoisActive A
										            WHERE [blocking_session_id] IS NOT NULL
									            )

			            --------------------------------------------------------------------------------------------------------------------------------
			            --	Verifica o Nivel dos Locks
			            --------------------------------------------------------------------------------------------------------------------------------
			            ALTER TABLE #Resultado_WhoisActive
			            ADD Nr_Nivel_Lock TINYINT 

			            -- Nivel 0
			            UPDATE A
			            SET Nr_Nivel_Lock = 0
			            FROM #Resultado_WhoisActive A
			            WHERE blocking_session_id IS NULL AND session_id IN ( SELECT DISTINCT blocking_session_id 
						            FROM #Resultado_WhoisActive WHERE blocking_session_id IS NOT NULL)

			            UPDATE A
			            SET Nr_Nivel_Lock = 1
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 0)

			            UPDATE A
			            SET Nr_Nivel_Lock = 2
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 1)

			            UPDATE A
			            SET Nr_Nivel_Lock = 3
			            FROM #Resultado_WhoisActive A
			            WHERE	Nr_Nivel_Lock IS NULL
					            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 2)
					
			            -- Tratamento quando não tem um Lock Raiz
			            IF NOT EXISTS(select * from #Resultado_WhoisActive where Nr_Nivel_Lock IS NOT NULL)
			            BEGIN
				            UPDATE A
				            SET Nr_Nivel_Lock = 0
				            FROM #Resultado_WhoisActive A
				            WHERE session_id IN ( SELECT DISTINCT blocking_session_id 
					            FROM #Resultado_WhoisActive WHERE blocking_session_id IS NOT NULL)
          
				            UPDATE A
				            SET Nr_Nivel_Lock = 1
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 0)

				            UPDATE A
				            SET Nr_Nivel_Lock = 2
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 1)

				            UPDATE A
				            SET Nr_Nivel_Lock = 3
				            FROM #Resultado_WhoisActive A
				            WHERE	Nr_Nivel_Lock IS NULL
						            AND blocking_session_id IN ( SELECT DISTINCT session_id FROM #Resultado_WhoisActive WHERE Nr_Nivel_Lock = 2)
			            END

			            -- Insert de dados para envio de Processo
		                 INSERT INTO ##VerificaProcBlock ([Nr_Nivel_Lock],[Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 SELECT	TOP 50
								            CAST(Nr_Nivel_Lock AS VARCHAR)							AS [Nr_Nivel_Lock],
								            ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
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
						            WHERE Nr_Nivel_Lock IS NOT NULL
						            ORDER BY [Nr_Nivel_Lock], [start_time] 

			            -- Insert de dados para envio de Processo
		                 INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
		                 SELECT	TOP 50
								            ISNULL([dd hh:mm:ss.mss], '-')							AS [Duração], 
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
						            ORDER BY [start_time]

			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - ALERTA
			            *******************************************************************************************************************************/							
			             -- Parametros do Alerta
			             SET @Subject = 'Alerta - Existe(m) processo(s) bloqueado(s) no Servidor: ' + @@SERVERNAME
			             SET @TextRel1 = 'Prezados,<BR /><BR />Existe(m) ' + CAST(@QtdProcessosBloqueados AS VARCHAR) + ' Processo(s) Bloqueado(s) a mais de ' +  CAST((@Processo_Bloqueado_Parametro) AS VARCHAR) + ' minuto(s)' +' e um total de ' + CAST(@QtdProcessosBloqueadosLocks AS VARCHAR) +  ' Lock(s) no Servidor: <b>' + @@SERVERNAME + ', </b>Baixo segue os TOP 50 processos em lock'
			             SET @TextRel2 =  'Processos em execução no Banco de Dados.'	
			             SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			 
			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProcBlock]'
			             SET @HTML = dbo.fncExportaMultiHTML(@Query, @TextRel1, 2, 1)
			             -- Gera Segundo bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProc]'
			             SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1)
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
			            /*******************************************************************************************************************************
			            --	CRIA O EMAIL - CLEAR
			            *******************************************************************************************************************************/

			            -- Insert de dados para envio de Processo
		                 INSERT INTO ##VerificaProc ( [Duração],[database_name],[login_name],[host_name],[start_time],[status],[session_id],[blocking_session_id],[Wait],[open_tran_count],[CPU],[reads],[writes],[sql_command] )
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
				
			            /*******************************************************************************************************************************
			            --	ALERTA - ENVIA O EMAIL - ENVIA TELEGRAM
			            *******************************************************************************************************************************/
			 
			 
			             SET @Subject =  (SELECT 'Solução - Não existem mais inconsistência de Processo Bloqueado no Servidor: ' + @@SERVERNAME)
			             SET @TextRel1 = 'Prezados,<BR /><BR />Sem registro de Processo Bloqueado a mais de ' + CAST((@Processo_Bloqueado_Parametro) AS VARCHAR) + ' minuto(s) no Servidor: ' + @@SERVERNAME + ', segue abaixo processos em execução no Banco de Dados'
			             SET @CaminhoFim = @CaminhoPath + @NomeRel +'.html'
			             -- Gera Primeiro bloco de HTML
			             SET @Query = 'SELECT * FROM [##VerificaProc]'
			             SET @HTML += dbo.fncExportaMultiHTML(@Query, @TextRel2, 2, 1) -- na 2a query não precisa do HTML completo
			             -- Gera segundo bloco de HTML
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
						                @importance = @Importance,
						                @body = @HTML;
			 
			             -- Envio do Telegram		
			             SET @MntMsg = @Subject+', Verifique os detalhes no e-mail'
			             EXEC dbo.StpSendMsgTelegram 
			                @Destino = @TLMsg,
			                --@Destino = '49353855', 
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